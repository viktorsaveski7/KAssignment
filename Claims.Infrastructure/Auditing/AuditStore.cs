using Microsoft.Extensions.DependencyInjection;

namespace Claims.Infrastructure.Auditing;

/// <summary>Writes audit entries to the audit database.</summary>
public sealed class AuditStore : IAuditStore
{
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>Creates the store.</summary>
    public AuditStore(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    /// <inheritdoc />
    public async Task WriteAsync(IReadOnlyList<AuditEntry> batch, CancellationToken cancellationToken = default)
    {
        // AuditContext is scoped and the caller is a singleton, so a scope is created per batch.
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

        await auditContext.SaveChangesAsync(cancellationToken);
    }
}
