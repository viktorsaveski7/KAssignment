using Claims.Application.Common.Interfaces;

namespace Claims.Infrastructure.Auditing;

public sealed class AuditService : IAuditService
{
    private readonly AuditContext _auditContext;

    public AuditService(AuditContext auditContext) => _auditContext = auditContext;

    public async Task AuditClaimAsync(
        string claimId,
        string httpRequestType,
        CancellationToken cancellationToken = default)
    {
        _auditContext.ClaimAudits.Add(new ClaimAudit
        {
            ClaimId = claimId,
            HttpRequestType = httpRequestType,
            Created = DateTime.UtcNow
        });

        await _auditContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AuditCoverAsync(
        string coverId,
        string httpRequestType,
        CancellationToken cancellationToken = default)
    {
        _auditContext.CoverAudits.Add(new CoverAudit
        {
            CoverId = coverId,
            HttpRequestType = httpRequestType,
            Created = DateTime.UtcNow
        });

        await _auditContext.SaveChangesAsync(cancellationToken);
    }
}
