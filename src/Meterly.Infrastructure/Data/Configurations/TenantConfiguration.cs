using Meterly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meterly.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the Tenant entity using the Fluent API.
///
/// WHY USE FLUENT API?
/// We could have put [MaxLength(100)] directly on the Name property in Tenant.cs
/// (called Data Annotations). But the Domain layer shouldn't know or care about
/// database column limits. Using Fluent API here keeps the Domain clean.
/// </summary>
public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        // Table name is usually inferred from the DbSet, but explicitly stating
        // it makes it clear.
        builder.ToTable("Tenants");

        // Primary key configuration is inferred from BaseEntity, but good to be explicit
        builder.HasKey(t => t.Id);

        // Required field with a sensible maximum length.
        // PostgreSQL handles string lengths efficiently, but setting MaxLength
        // adds a constraint that protects the database from garbage data.
        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(50);

        // Configure the concurrency token inherited from BaseEntity.
        // For PostgreSQL using Npgsql, we mark it as xmin so it maps to the built-in system column.
        // This makes optimistic concurrency work automatically.
        builder.Property(t => t.RowVersion)
            .IsRowVersion()
            .HasColumnName("xmin")
            .HasColumnType("xid");

        // GLOBAL QUERY FILTER:
        // This is a powerful EF Core feature. Any LINQ query against the Tenants table
        // will automatically have "WHERE IsDeleted = false" appended to it.
        // You don't have to remember to check IsDeleted in your application code.
        builder.HasQueryFilter(t => !t.IsDeleted);
        
        // Index on IsDeleted for faster filtering
        builder.HasIndex(t => t.IsDeleted);
    }
}
