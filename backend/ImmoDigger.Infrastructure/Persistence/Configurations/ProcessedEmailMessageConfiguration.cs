using ImmoDigger.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImmoDigger.Infrastructure.Persistence.Configurations;

public class ProcessedEmailMessageConfiguration : IEntityTypeConfiguration<ProcessedEmailMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedEmailMessage> builder)
    {
        builder.ToTable("ProcessedEmailMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.EmailMessageId).HasMaxLength(998).IsRequired(); // RFC 5322 Message-ID max length
        builder.Property(m => m.Subject).HasMaxLength(998);
        builder.Property(m => m.Sender).HasMaxLength(320); // RFC 5321 max mailbox length

        builder.HasIndex(m => m.EmailMessageId).IsUnique();
    }
}
