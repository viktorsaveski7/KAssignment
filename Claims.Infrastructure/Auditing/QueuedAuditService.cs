using Claims.Application.Common.Interfaces;

namespace Claims.Infrastructure.Auditing;

/// <summary>
/// Queues audit entries instead of writing them, so the request never waits on the audit database.
/// Entries are timestamped on enqueue, not on write, so the record reflects when the operation happened.
/// </summary>
public sealed class QueuedAuditService : IAuditService
{
    private readonly AuditQueue _queue;
    private readonly TimeProvider _timeProvider;

    public QueuedAuditService(AuditQueue queue, TimeProvider timeProvider)
    {
        _queue = queue;
        _timeProvider = timeProvider;
    }

    public Task AuditClaimAsync(
        string claimId,
        string httpRequestType,
        CancellationToken cancellationToken = default) =>
        EnqueueAsync(AuditedEntity.Claim, claimId, httpRequestType, cancellationToken);

    public Task AuditCoverAsync(
        string coverId,
        string httpRequestType,
        CancellationToken cancellationToken = default) =>
        EnqueueAsync(AuditedEntity.Cover, coverId, httpRequestType, cancellationToken);

    private Task EnqueueAsync(
        AuditedEntity entity,
        string entityId,
        string httpRequestType,
        CancellationToken cancellationToken)
    {
        var entry = new AuditEntry(
            entity,
            entityId,
            httpRequestType,
            _timeProvider.GetUtcNow().UtcDateTime);

        return _queue.EnqueueAsync(entry, cancellationToken).AsTask();
    }
}
