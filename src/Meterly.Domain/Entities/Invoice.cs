using Meterly.Domain.Common;
using Meterly.Domain.Enums;

namespace Meterly.Domain.Entities;

/// <summary>
/// Represents a bill sent to a customer.
/// An Invoice can be linked to a Subscription (recurring) or standalone (one-off).
/// 
/// KEY CONCEPT: An Invoice is composed of one or more InvoiceLineItems.
/// The SubTotal, TaxTotal, and Total are computed from the line items when the
/// invoice is created, and then stored as denormalized values for query performance.
/// </summary>
public class Invoice : BaseEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>
    /// Nullable — an invoice doesn't HAVE to come from a subscription.
    /// One-off invoices (e.g., consulting fees, setup charges) are common in SaaS.
    /// </summary>
    public Guid? SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }

    /// <summary>
    /// Human-readable invoice number (e.g., INV-0001).
    /// Auto-generated per tenant on creation.
    /// </summary>
    public string Number { get; set; } = string.Empty;

    public InvoiceStatus Status { get; set; }
    public string Currency { get; set; } = "USD";

    public decimal SubTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }

    public DateTime DueDate { get; set; }
    public DateTime? PaidAt { get; set; }

    public ICollection<InvoiceLineItem> LineItems { get; set; } = new List<InvoiceLineItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    public ICollection<DunningAttempt> DunningAttempts { get; set; } = new List<DunningAttempt>();

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
