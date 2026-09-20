namespace Claims.Infrastructure.Auditing;

/// <summary>
/// Persists a batch of audit entries. Kept separate from the background writer so that the writer
/// owns draining and batching while this owns storage, and so that tests can substitute it.
/// </summary>
public interface IAuditStore
{
    /// <summary>Writes a batch of entries. Throws if the batch could not be persisted.</summary>
    Task WriteAsync(IReadOnlyList<AuditEntry> batch, CancellationToken cancellationToken = default);
}
