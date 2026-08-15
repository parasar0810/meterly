using Meterly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meterly.Infrastructure.Data.Configurations;

public class DunningAttemptConfiguration : IEntityTypeConfiguration<DunningAttempt>
{
    public void Configure(EntityTypeBuilder<DunningAttempt> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FailureReason)
            .HasMaxLength(500);

        // Relationships — explicit navigation properties prevent shadow property issues
        builder.HasOne(d => d.Invoice)
            .WithMany(i => i.DunningAttempts)
            .HasForeignKey(d => d.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Payment)
            .WithMany()
            .HasForeignKey(d => d.PaymentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Tenant)
            .WithMany()
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index to quickly find all attempts for an invoice
        builder.HasIndex(d => d.InvoiceId);
        builder.HasIndex(d => new { d.TenantId, d.InvoiceId });
    }
}
