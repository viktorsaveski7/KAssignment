using Claims.Infrastructure.Auditing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Claims.UnitTests.Infrastructure;

public class AuditWriterServiceTests
{
    private static readonly DateTime At = new(2027, 5, 4, 10, 0, 0, DateTimeKind.Utc);

    private static AuditEntry Claim(string id, string verb = "POST") =>
        new(AuditedEntity.Claim, id, verb, At);

    private static AuditEntry Cover(string id, string verb = "POST") =>
        new(AuditedEntity.Cover, id, verb, At);

    private static AuditWriterService CreateWriter(AuditQueue queue, FakeAuditStore store) =>
        new(queue, store, NullLogger<AuditWriterService>.Instance);

    private static IEnumerable<string> IdsIn(FakeAuditStore store) =>
        store.Written.Select(entry => entry.EntityId);

    [Fact]
    public async Task Writes_queued_claim_and_cover_entries()
    {
        var queue = new AuditQueue();
        var store = new FakeAuditStore();
        var writer = CreateWriter(queue, store);

        await queue.EnqueueAsync(Claim("claim-1"));
        await queue.EnqueueAsync(Cover("cover-1", "DELETE"));

        await writer.StartAsync(CancellationToken.None);
        await store.WrittenCountReaches(2);
        await writer.StopAsync(CancellationToken.None);

        var claimEntry = Assert.Single(store.Written, entry => entry.Entity == AuditedEntity.Claim);
        Assert.Equal("claim-1", claimEntry.EntityId);
        Assert.Equal("POST", claimEntry.HttpRequestType);
        Assert.Equal(At, claimEntry.CreatedUtc);

        var coverEntry = Assert.Single(store.Written, entry => entry.Entity == AuditedEntity.Cover);
        Assert.Equal("cover-1", coverEntry.EntityId);
        Assert.Equal("DELETE", coverEntry.HttpRequestType);
    }

    [Fact]
    public async Task Drains_entries_still_queued_when_the_service_is_stopped()
    {
        var queue = new AuditQueue();
        var store = new FakeAuditStore();
        var writer = CreateWriter(queue, store);

        store.BlockNextWrite();
        await queue.EnqueueAsync(Claim("first"));
        await writer.StartAsync(CancellationToken.None);

        // The writer is now parked inside the first write, so nothing below is a race.
        await store.WriteStarted(1);

        // StopAsync cancels the stopping token before it awaits, so these two are queued behind a
        // cancellation the writer has not observed yet: they can only come out through the drain.
        var stopping = writer.StopAsync(CancellationToken.None);
        await queue.EnqueueAsync(Claim("second"));
        await queue.EnqueueAsync(Claim("third"));

        store.ReleaseBlockedWrite();
        await stopping;

        Assert.Equal(["first", "second", "third"], IdsIn(store));
    }

    [Fact]
    public async Task Keeps_running_after_a_failed_write()
    {
        var queue = new AuditQueue();
        var store = new FakeAuditStore();
        var writer = CreateWriter(queue, store);

        store.FailNextWrites(1);
        await queue.EnqueueAsync(Claim("dropped"));
        await writer.StartAsync(CancellationToken.None);

        // The failing write has finished failing; no sleeping required to know that.
        await store.WriteCompleted(1);

        await queue.EnqueueAsync(Claim("written"));
        await store.WrittenCountReaches(1);
        await writer.StopAsync(CancellationToken.None);

        Assert.Equal(["written"], IdsIn(store));
        Assert.Equal(2, store.CompletedWrites);
    }

    [Fact]
    public async Task Stops_cleanly_when_nothing_was_queued()
    {
        var queue = new AuditQueue();
        var store = new FakeAuditStore();
        var writer = CreateWriter(queue, store);

        await writer.StartAsync(CancellationToken.None);
        await writer.StopAsync(CancellationToken.None);

        Assert.Empty(store.Written);
        Assert.Equal(0, store.CompletedWrites);
    }
}
