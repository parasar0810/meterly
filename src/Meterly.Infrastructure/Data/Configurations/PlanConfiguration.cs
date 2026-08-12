using Meterly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meterly.Infrastructure.Data.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3); // e.g., USD

        // For financial amounts, decimal(18,4) is common to prevent rounding errors
        builder.Property(p => p.Price)
            .HasColumnType("decimal(18,4)");

        // Foreign Key to Product
        builder.HasOne(p => p.Product)
            .WithMany(pr => pr.Plans)
            .HasForeignKey(p => p.ProductId)
            .OnDelete(DeleteBehavior.Cascade); // If a product is hard-deleted, plans should go too.

        builder.Property(p => p.RowVersion)
            .IsRowVersion()
            .HasColumnName("xmin")
            .HasColumnType("xid");

        builder.HasIndex(p => p.IsDeleted);
    }
}
