using Meterly.Domain.Common;
using Meterly.Domain.Enums;

namespace Meterly.Domain.Entities;

/// <summary>
/// Represents a customer's active or inactive agreement to pay for a plan.
/// </summary>
public class Subscription : BaseEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    public SubscriptionStatus Status { get; set; }

    public DateTime StartDate { get; set; }
    
    // For recurring billing, this defines the current active window
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    
    // If true, the subscription remains Active until CurrentPeriodEnd, 
    // then flips to Canceled instead of renewing.
    public bool CancelAtPeriodEnd { get; set; }

    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
