using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImmoDigger.Infrastructure.Persistence.Configurations;

public class SearchProfileConfiguration : IEntityTypeConfiguration<SearchProfile>
{
    public void Configure(EntityTypeBuilder<SearchProfile> builder)
    {
        builder.ToTable("SearchProfiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.MaximumPrice).HasPrecision(14, 2);
        builder.Property(p => p.MinimumGrossYield).HasPrecision(6, 3);
        builder.Property(p => p.MinimumLivingArea).HasPrecision(10, 2);
        builder.Property(p => p.MinimumOpportunityScore).HasPrecision(5, 2);

        // Native PostgreSQL text[] columns (Npgsql maps string[] automatically).
        builder.Property(p => p.PostalCodes).IsRequired();
        builder.Property(p => p.PropertyTypes).IsRequired();
    }
}
