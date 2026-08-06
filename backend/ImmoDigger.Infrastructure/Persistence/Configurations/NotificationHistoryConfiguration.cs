using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImmoDigger.Infrastructure.Persistence.Configurations;

public class NotificationHistoryConfiguration : IEntityTypeConfiguration<NotificationHistory>
{
    public void Configure(EntityTypeBuilder<NotificationHistory> builder)
    {
        builder.ToTable("NotificationHistories");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Channel).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Status).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Error).HasColumnType("text");

        builder.HasIndex(n => new { n.PropertyListingId, n.Channel });
    }
}
