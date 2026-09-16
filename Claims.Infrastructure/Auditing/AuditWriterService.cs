using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Claims.Infrastructure.Auditing;

public sealed class AuditWriterService : BackgroundService
{
    private const int MaxBatchSize = 100;

    private readonly AuditQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditWriterService> _logger;

    public AuditWriterService(
        AuditQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<AuditWriterService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

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
            using var scope = _scopeFactory.CreateScope();
            var auditContext = scope.ServiceProvider.GetRequiredService<AuditContext>();

            foreach (var entry in batch)
            {
                if (entry.Entity == AuditedEntity.Claim)
                {
                    auditContext.ClaimAudits.Add(new ClaimAudit
                    {
                        ClaimId = entry.EntityId,
                        HttpRequestType = entry.HttpRequestType,
                        Created = entry.CreatedUtc
                    });
                }
                else
                {
                    auditContext.CoverAudits.Add(new CoverAudit
                    {
                        CoverId = entry.EntityId,
                        HttpRequestType = entry.HttpRequestType,
                        Created = entry.CreatedUtc
                    });
                }
            }

            await auditContext.SaveChangesAsync(CancellationToken.None);
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
