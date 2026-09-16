using Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Claims.Infrastructure.Persistence.Configurations;

public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToCollection("claims");

        builder.HasKey(claim => claim.Id);

        builder.Property(claim => claim.CoverId).HasElementName("coverId");
        builder.Property(claim => claim.Created)
            .HasElementName("created")
            .HasConversion(DateOnlyConverter.Instance);
        builder.Property(claim => claim.Name).HasElementName("name");
        builder.Property(claim => claim.Type).HasElementName("claimType");
        builder.Property(claim => claim.DamageCost).HasElementName("damageCost");
    }
}
