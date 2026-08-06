using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImmoDigger.Infrastructure.Persistence.Configurations;

public class ListingPriceHistoryConfiguration : IEntityTypeConfiguration<ListingPriceHistory>
{
    public void Configure(EntityTypeBuilder<ListingPriceHistory> builder)
    {
        builder.ToTable("ListingPriceHistories");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Price).HasPrecision(14, 2);

        builder.HasIndex(h => new { h.PropertyListingId, h.RecordedAt });
    }
}
