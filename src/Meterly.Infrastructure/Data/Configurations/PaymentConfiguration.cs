using Meterly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meterly.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(p => p.IdempotencyKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.GatewayReference)
            .HasMaxLength(100);

        builder.Property(p => p.FailureReason)
            .HasMaxLength(255);

        // A single tenant/idempotency key combo should be unique
        builder.HasIndex(p => new { p.TenantId, p.IdempotencyKey })
            .IsUnique();

        builder.HasOne(p => p.Tenant)
            .WithMany(t => t.Payments)
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasIndex(p => p.IsDeleted);
    }
}
