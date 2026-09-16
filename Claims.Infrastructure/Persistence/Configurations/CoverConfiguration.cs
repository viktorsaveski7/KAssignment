using Claims.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Claims.Infrastructure.Persistence.Configurations;

public sealed class CoverConfiguration : IEntityTypeConfiguration<Cover>
{
    public void Configure(EntityTypeBuilder<Cover> builder)
    {
        builder.ToCollection("covers");

        builder.HasKey(cover => cover.Id);

        builder.Property(cover => cover.StartDate)
            .HasElementName("startDate")
            .HasConversion(DateOnlyConverter.Instance);

        builder.Property(cover => cover.EndDate)
            .HasElementName("endDate")
            .HasConversion(DateOnlyConverter.Instance);
        // Stored as "claimType" because that is what the original schema used; renaming would orphan
        // existing documents.
        builder.Property(cover => cover.Type).HasElementName("claimType");
        builder.Property(cover => cover.Premium).HasElementName("premium");
    }
}
