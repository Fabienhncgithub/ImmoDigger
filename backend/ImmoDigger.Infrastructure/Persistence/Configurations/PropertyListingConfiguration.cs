using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImmoDigger.Infrastructure.Persistence.Configurations;

public class PropertyListingConfiguration : IEntityTypeConfiguration<PropertyListing>
{
    public void Configure(EntityTypeBuilder<PropertyListing> builder)
    {
        builder.ToTable("PropertyListings");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Source).HasMaxLength(100).IsRequired();
        builder.Property(l => l.ExternalId).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Url).HasMaxLength(2000).IsRequired();
        builder.Property(l => l.Title).HasMaxLength(500).IsRequired();
        builder.Property(l => l.Description).HasColumnType("text");
        builder.Property(l => l.Address).HasMaxLength(300);
        builder.Property(l => l.PostalCode).HasMaxLength(20);
        builder.Property(l => l.City).HasMaxLength(150);
        builder.Property(l => l.SaleType).HasMaxLength(50).IsRequired();
        builder.Property(l => l.PropertyType).HasMaxLength(50).IsRequired();
        builder.Property(l => l.PebRating).HasMaxLength(5);
        builder.Property(l => l.RiskLevel).HasMaxLength(20);
        builder.Property(l => l.RiskSummary).HasColumnType("text");
        builder.Property(l => l.PersonalNotes).HasColumnType("text");
        builder.Property(l => l.RawContentHash).HasMaxLength(128).IsRequired();

        builder.Property(l => l.AskingPrice).HasPrecision(14, 2);
        builder.Property(l => l.CurrentBid).HasPrecision(14, 2);
        builder.Property(l => l.EstimatedFinalPrice).HasPrecision(14, 2);
        builder.Property(l => l.LivingArea).HasPrecision(10, 2);
        builder.Property(l => l.LandArea).HasPrecision(10, 2);
        builder.Property(l => l.PebConsumption).HasPrecision(10, 2);
        builder.Property(l => l.CadastralIncome).HasPrecision(14, 2);
        builder.Property(l => l.OpportunityScore).HasPrecision(5, 2);
        builder.Property(l => l.EstimatedGrossYield).HasPrecision(6, 3);
        builder.Property(l => l.EstimatedRenovationCost).HasPrecision(14, 2);
        builder.Property(l => l.EstimatedMonthlyRentPerUnit).HasPrecision(10, 2);
        builder.Property(l => l.EstimatedAcquisitionCosts).HasPrecision(14, 2);
        builder.Property(l => l.EstimatedRenovationBudget).HasPrecision(14, 2);

        // Source + ExternalId is the primary deduplication key (level 1).
        // Not a hard unique constraint: the generic agency collector may not
        // always produce a reliable external id, and the deduplication
        // service (added in a later commit) is responsible for resolving
        // conflicts explicitly rather than relying on a DB-level rejection.
        builder.HasIndex(l => new { l.Source, l.ExternalId });
        builder.HasIndex(l => l.Url);
        builder.HasIndex(l => l.PostalCode);
        builder.HasIndex(l => l.City);
        builder.HasIndex(l => l.IsActive);
        builder.HasIndex(l => l.OpportunityScore);
        builder.HasIndex(l => l.FirstSeenAt);

        builder.HasMany(l => l.PriceHistory)
            .WithOne(h => h.PropertyListing)
            .HasForeignKey(h => h.PropertyListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(l => l.Notifications)
            .WithOne(n => n.PropertyListing)
            .HasForeignKey(n => n.PropertyListingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
