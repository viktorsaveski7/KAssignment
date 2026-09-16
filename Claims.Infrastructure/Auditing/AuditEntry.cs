namespace Claims.Infrastructure.Auditing;

public enum AuditedEntity
{
    Claim = 0,
    Cover = 1
}

public sealed record AuditEntry(
    AuditedEntity Entity,
    string EntityId,
    string HttpRequestType,
    DateTime CreatedUtc);
