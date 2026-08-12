using Meterly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meterly.Infrastructure.Data.Configurations;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Amount)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(l => l.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(l => l.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(l => l.ReferenceType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Type)
            .IsRequired();

        // Index for efficient financial report queries by tenant + reference
        builder.HasIndex(l => new { l.TenantId, l.ReferenceId });
        builder.HasIndex(l => new { l.TenantId, l.CreatedAt });

        builder.HasOne(l => l.Tenant)
            .WithMany()
            .HasForeignKey(l => l.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
