using Meterly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meterly.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Number)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(i => i.SubTotal).HasColumnType("decimal(18,4)");
        builder.Property(i => i.TaxTotal).HasColumnType("decimal(18,4)");
        builder.Property(i => i.Total).HasColumnType("decimal(18,4)");

        builder.Property(i => i.Status).IsRequired();

        // Relationships
        builder.HasOne(i => i.Tenant)
            .WithMany(t => t.Invoices)
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Customer)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Subscription)
            .WithMany(s => s.Invoices)
            .HasForeignKey(i => i.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull); // If the subscription is deleted, keep the invoice

        // Unique invoice number per tenant
        builder.HasIndex(i => new { i.TenantId, i.Number }).IsUnique();

        builder.Property(i => i.RowVersion)
            .IsRowVersion()
            .HasColumnName("xmin")
            .HasColumnType("xid");

        builder.HasIndex(i => i.IsDeleted);
        builder.HasIndex(i => i.CustomerId);
    }
}
