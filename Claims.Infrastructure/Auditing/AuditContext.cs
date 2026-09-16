using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Auditing;

public class AuditContext : DbContext
{
    public AuditContext(DbContextOptions<AuditContext> options) : base(options)
    {
    }

    public DbSet<ClaimAudit> ClaimAudits => Set<ClaimAudit>();

    public DbSet<CoverAudit> CoverAudits => Set<CoverAudit>();
}
