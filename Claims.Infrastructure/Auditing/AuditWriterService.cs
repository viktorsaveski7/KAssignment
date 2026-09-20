using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Claims.Infrastructure.Auditing;

/// <summary>Drains the audit queue and hands entries to the store in batches.</summary>
public sealed class AuditWriterService : BackgroundService
{
    private const int MaxBatchSize = 100;

    private readonly AuditQueue _queue;
    private readonly IAuditStore _store;
    private readonly ILogger<AuditWriterService> _logger;

    /// <summary>Creates the service.</summary>
    public AuditWriterService(AuditQueue queue, IAuditStore store, ILogger<AuditWriterService> logger)
    {
        _queue = queue;
        _store = store;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var entry in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                await WriteBatchAsync(Collect(entry));
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        // Shutdown must not strand entries that are already queued.
        await DrainRemainingAsync();
    }

    private async Task DrainRemainingAsync()
    {
        _queue.CompleteAdding();

        while (_queue.Reader.TryRead(out var entry))
        {
            await WriteBatchAsync(Collect(entry));
        }
    }

    private List<AuditEntry> Collect(AuditEntry first)
    {
        var batch = new List<AuditEntry> { first };

        while (batch.Count < MaxBatchSize && _queue.Reader.TryRead(out var next))
        {
            batch.Add(next);
        }

        return batch;
    }

    private async Task WriteBatchAsync(IReadOnlyList<AuditEntry> batch)
    {
        try
        {
            // Deliberately not cancellable: once an entry has left the queue, abandoning the write loses it.
            await _store.WriteAsync(batch, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to persist {Count} audit entries. They have been dropped.",
                batch.Count);
        }
    }
}
