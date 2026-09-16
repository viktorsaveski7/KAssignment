namespace Claims.Application.Common.Interfaces;

/// <summary>
/// Records that a claim or cover was created or deleted. The application states that something should
/// be audited; how and when the record reaches storage is an infrastructure decision behind this seam.
/// </summary>
public interface IAuditService
{
    Task AuditClaimAsync(string claimId, string httpRequestType, CancellationToken cancellationToken = default);

    Task AuditCoverAsync(string coverId, string httpRequestType, CancellationToken cancellationToken = default);
}
