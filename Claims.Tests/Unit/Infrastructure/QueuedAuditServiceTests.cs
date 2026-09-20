using Claims.Infrastructure.Auditing;
using Microsoft.Extensions.Time.Testing;

namespace Claims.Tests.Unit.Infrastructure;

public class QueuedAuditServiceTests
{
    private static readonly DateTimeOffset Now = new(2027, 5, 4, 10, 30, 0, TimeSpan.Zero);

    private static (QueuedAuditService Service, AuditQueue Queue) Create()
    {
        var queue = new AuditQueue();
        return (new QueuedAuditService(queue, new FakeTimeProvider(Now)), queue);
    }

    [Fact]
    public async Task Enqueues_a_claim_audit()
    {
        var (service, queue) = Create();

        await service.AuditClaimAsync("claim-1", "POST");

        Assert.True(queue.Reader.TryRead(out var entry));
        Assert.Equal(AuditedEntity.Claim, entry!.Entity);
        Assert.Equal("claim-1", entry.EntityId);
        Assert.Equal("POST", entry.HttpRequestType);
    }

    [Fact]
    public async Task Enqueues_a_cover_audit()
    {
        var (service, queue) = Create();

        await service.AuditCoverAsync("cover-1", "DELETE");

        Assert.True(queue.Reader.TryRead(out var entry));
        Assert.Equal(AuditedEntity.Cover, entry!.Entity);
        Assert.Equal("cover-1", entry.EntityId);
        Assert.Equal("DELETE", entry.HttpRequestType);
    }

    [Fact]
    public async Task Stamps_the_time_at_enqueue_rather_than_at_write()
    {
        var (service, queue) = Create();

        await service.AuditClaimAsync("claim-1", "POST");

        Assert.True(queue.Reader.TryRead(out var entry));
        Assert.Equal(Now.UtcDateTime, entry!.CreatedUtc);
    }

    [Fact]
    public async Task Returns_without_waiting_for_a_reader()
    {
        var (service, _) = Create();

        var enqueue = service.AuditClaimAsync("claim-1", "POST");

        Assert.True(enqueue.IsCompleted);
        await enqueue;
    }

    [Fact]
    public async Task Preserves_the_order_entries_were_enqueued_in()
    {
        var (service, queue) = Create();

        await service.AuditClaimAsync("first", "POST");
        await service.AuditCoverAsync("second", "POST");
        await service.AuditClaimAsync("third", "DELETE");

        Assert.True(queue.Reader.TryRead(out var first));
        Assert.True(queue.Reader.TryRead(out var second));
        Assert.True(queue.Reader.TryRead(out var third));
        Assert.Equal("first", first!.EntityId);
        Assert.Equal("second", second!.EntityId);
        Assert.Equal("third", third!.EntityId);
    }
}

public class AuditQueueTests
{
    [Fact]
    public async Task Round_trips_an_entry()
    {
        var queue = new AuditQueue();
        var entry = new AuditEntry(AuditedEntity.Claim, "claim-1", "POST", DateTime.UtcNow);

        await queue.EnqueueAsync(entry);

        Assert.True(queue.Reader.TryRead(out var read));
        Assert.Equal(entry, read);
    }

    [Fact]
    public void Reports_empty_when_nothing_is_queued()
    {
        var queue = new AuditQueue();

        Assert.False(queue.Reader.TryRead(out _));
    }

    [Fact]
    public async Task Completing_stops_further_reads_once_drained()
    {
        var queue = new AuditQueue();
        await queue.EnqueueAsync(new AuditEntry(AuditedEntity.Cover, "cover-1", "POST", DateTime.UtcNow));

        queue.CompleteAdding();

        Assert.True(queue.Reader.TryRead(out _));
        Assert.False(queue.Reader.TryRead(out _));
        Assert.True(queue.Reader.Completion.IsCompleted);
    }

    [Fact]
    public async Task Applies_back_pressure_rather_than_dropping_when_full()
    {
        var queue = new AuditQueue(capacity: 1);
        await queue.EnqueueAsync(new AuditEntry(AuditedEntity.Claim, "first", "POST", DateTime.UtcNow));

        var second = queue.EnqueueAsync(
            new AuditEntry(AuditedEntity.Claim, "second", "POST", DateTime.UtcNow));

        Assert.False(second.IsCompleted);

        Assert.True(queue.Reader.TryRead(out var first));
        Assert.Equal("first", first!.EntityId);

        await second;
        Assert.True(queue.Reader.TryRead(out var read));
        Assert.Equal("second", read!.EntityId);
    }
}
