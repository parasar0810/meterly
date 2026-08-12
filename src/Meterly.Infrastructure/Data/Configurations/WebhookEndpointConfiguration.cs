using Meterly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Meterly.Infrastructure.Data.Configurations;

public class WebhookEndpointConfiguration : IEntityTypeConfiguration<WebhookEndpoint>
{
    public void Configure(EntityTypeBuilder<WebhookEndpoint> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Url)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(w => w.Secret)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(w => w.Description)
            .HasMaxLength(500);

        builder.HasOne(w => w.Tenant)
            .WithMany(t => t.WebhookEndpoints)
            .HasForeignKey(w => w.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
