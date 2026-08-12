using Meterly.Domain.Common;
using Meterly.Domain.Enums;

namespace Meterly.Domain.Entities;

/// <summary>
/// Represents a pricing plan for a specific product (e.g., "Basic $10/mo").
/// </summary>
public class Plan : BaseEntity, ISoftDeletable
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    
    // Storing prices using decimal to avoid floating point precision issues in finance
    public decimal Price { get; set; }
    
    // e.g., "USD", "EUR"
    public string Currency { get; set; } = "USD";
    
    public BillingPeriod BillingPeriod { get; set; }
    
    public bool IsActive { get; set; } = true;

    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
