using Claims.Infrastructure.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Claims.UnitTests.Infrastructure;

public class AuditWriterServiceTests
{
    private static ServiceProvider BuildProvider(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AuditContext>(options => options.UseInMemoryDatabase(databaseName));
        return services.BuildServiceProvider();
    }

    private static AuditEntry Claim(string id, string verb = "POST") =>
        new(AuditedEntity.Claim, id, verb, new DateTime(2027, 5, 4, 10, 0, 0, DateTimeKind.Utc));

    private static AuditEntry Cover(string id, string verb = "POST") =>
        new(AuditedEntity.Cover, id, verb, new DateTime(2027, 5, 4, 10, 0, 0, DateTimeKind.Utc));

    private static AuditWriterService CreateWriter(
        AuditQueue queue,
        IServiceScopeFactory scopeFactory) =>
        new(queue, scopeFactory, NullLogger<AuditWriterService>.Instance);

    private sealed class GatedScopeFactory : IServiceScopeFactory
    {
        private readonly IServiceScopeFactory _inner;
        private readonly SemaphoreSlim _gate = new(0);
        private readonly TaskCompletionSource _entered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _calls;

        public GatedScopeFactory(IServiceScopeFactory inner) => _inner = inner;

        public Task FirstWriteStarted => _entered.Task;

        public void ReleaseFirstWrite() => _gate.Release();

        public IServiceScope CreateScope()
        {
            if (Interlocked.Increment(ref _calls) == 1)
            {
                _entered.TrySetResult();
                _gate.Wait(TimeSpan.FromSeconds(10));
            }

            return _inner.CreateScope();
        }
    }

    private sealed class FailingOnceScopeFactory : IServiceScopeFactory
    {
        private readonly IServiceScopeFactory _inner;
        private int _calls;

        public FailingOnceScopeFactory(IServiceScopeFactory inner) => _inner = inner;

        public IServiceScope CreateScope()
        {
            if (Interlocked.Increment(ref _calls) == 1)
            {
                throw new InvalidOperationException("audit database is unreachable");
            }

            return _inner.CreateScope();
        }
    }

    [Fact]
    public async Task Writes_queued_claim_and_cover_entries()
    {
        await using var provider = BuildProvider(Guid.NewGuid().ToString());
        var queue = new AuditQueue();
        var writer = CreateWriter(queue, provider.GetRequiredService<IServiceScopeFactory>());

        await queue.EnqueueAsync(Claim("claim-1"));
        await queue.EnqueueAsync(Cover("cover-1", "DELETE"));

        await writer.StartAsync(CancellationToken.None);
        await writer.StopAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuditContext>();

        var claimAudit = Assert.Single(await context.ClaimAudits.ToListAsync());
        Assert.Equal("claim-1", claimAudit.ClaimId);
        Assert.Equal("POST", claimAudit.HttpRequestType);
        Assert.Equal(new DateTime(2027, 5, 4, 10, 0, 0, DateTimeKind.Utc), claimAudit.Created);

        var coverAudit = Assert.Single(await context.CoverAudits.ToListAsync());
        Assert.Equal("cover-1", coverAudit.CoverId);
        Assert.Equal("DELETE", coverAudit.HttpRequestType);
    }

    [Fact]
    public async Task Drains_entries_still_queued_when_the_service_is_stopped()
    {
        await using var provider = BuildProvider(Guid.NewGuid().ToString());
        var queue = new AuditQueue();
        var gated = new GatedScopeFactory(provider.GetRequiredService<IServiceScopeFactory>());
        var writer = CreateWriter(queue, gated);

        await queue.EnqueueAsync(Claim("first"));
        await writer.StartAsync(CancellationToken.None);

        await gated.FirstWriteStarted.WaitAsync(TimeSpan.FromSeconds(10));

        await queue.EnqueueAsync(Claim("second"));
        await queue.EnqueueAsync(Claim("third"));

        var stopping = writer.StopAsync(CancellationToken.None);
        gated.ReleaseFirstWrite();
        await stopping.WaitAsync(TimeSpan.FromSeconds(30));

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
        var written = await context.ClaimAudits.Select(audit => audit.ClaimId).ToListAsync();

        Assert.Equal(3, written.Count);
        Assert.Contains("first", written);
        Assert.Contains("second", written);
        Assert.Contains("third", written);
    }

    [Fact]
    public async Task Keeps_running_after_a_failed_write()
    {
        await using var provider = BuildProvider(Guid.NewGuid().ToString());
        var queue = new AuditQueue();
        var failing = new FailingOnceScopeFactory(provider.GetRequiredService<IServiceScopeFactory>());
        var writer = CreateWriter(queue, failing);

        await queue.EnqueueAsync(Claim("dropped"));
        await writer.StartAsync(CancellationToken.None);
        await Task.Delay(200);

        await queue.EnqueueAsync(Claim("written"));
        await writer.StopAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
        var written = await context.ClaimAudits.Select(audit => audit.ClaimId).ToListAsync();

        Assert.Equal(new[] { "written" }, written);
    }

    [Fact]
    public async Task Stops_cleanly_when_nothing_was_queued()
    {
        await using var provider = BuildProvider(Guid.NewGuid().ToString());
        var queue = new AuditQueue();
        var writer = CreateWriter(queue, provider.GetRequiredService<IServiceScopeFactory>());

        await writer.StartAsync(CancellationToken.None);
        await writer.StopAsync(CancellationToken.None);

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuditContext>();
        Assert.Empty(await context.ClaimAudits.ToListAsync());
    }
}
