namespace Claims.Application.Common.Interfaces;

public interface IAuditService
{
    Task AuditClaimAsync(string claimId, string httpRequestType, CancellationToken cancellationToken = default);

    Task AuditCoverAsync(string coverId, string httpRequestType, CancellationToken cancellationToken = default);
}
