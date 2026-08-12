using Meterly.Domain.Common;

namespace Meterly.Domain.Entities;

/// <summary>
/// Represents a customer of a Tenant. These are the businesses or individuals
/// that our SaaS tenants will be billing.
/// </summary>
public class Customer : BaseEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    // Implementing ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
