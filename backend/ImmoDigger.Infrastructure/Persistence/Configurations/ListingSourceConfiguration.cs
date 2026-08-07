using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImmoDigger.Infrastructure.Persistence.Configurations;

public class ListingSourceConfiguration : IEntityTypeConfiguration<ListingSource>
{
    public void Configure(EntityTypeBuilder<ListingSource> builder)
    {
        builder.ToTable("ListingSources");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.BaseUrl).HasMaxLength(500).IsRequired();
        builder.Property(s => s.LastError).HasColumnType("text");
        builder.Property(s => s.CollectionMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.Notes).HasColumnType("text");

        builder.HasIndex(s => s.Name).IsUnique();
    }
}
