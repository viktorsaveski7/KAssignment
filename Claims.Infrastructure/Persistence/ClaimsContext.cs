using Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Claims.Infrastructure.Persistence;

public class ClaimsContext : DbContext
{
    public ClaimsContext(DbContextOptions<ClaimsContext> options) : base(options)
    {
    }

    public DbSet<Claim> Claims => Set<Claim>();

    public DbSet<Cover> Covers => Set<Cover>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

// Collection and element mapping live in configurations here rather than as attributes on the
        // entities, so the domain layer carries no dependency on the storage engine.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimsContext).Assembly);
    }
}
