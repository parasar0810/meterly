using Meterly.Domain.Common;
using Meterly.Domain.Enums;

namespace Meterly.Domain.Entities;

/// <summary>
/// Records a single payment attempt against an invoice.
/// 
/// KEY DESIGN PRINCIPLE: An Invoice and a Payment are separate entities.
/// One invoice can have MULTIPLE payment attempts — the first might fail 
/// (card declined), the second might succeed. The payment history is 
/// the full audit trail of all attempts.
/// 
/// IDEMPOTENCY KEY:
/// If a client sends the same "pay invoice" request twice (e.g., due to a 
/// network timeout causing a retry), we check the idempotency key first.
/// If we find an existing Payment with that key, we return it instead of 
/// charging again. This is how Stripe prevents double-charges.
/// </summary>
public class Payment : BaseEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Client-supplied key to prevent duplicate charges.
    /// Typically: "{invoiceId}-{attemptNumber}" or a UUID the client generates.
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>
    /// The transaction ID returned by the payment gateway (Stripe charge ID, etc.)
    /// Null if the payment failed before reaching the gateway.
    /// </summary>
    public string? GatewayReference { get; set; }

    /// <summary>
    /// Human-readable failure reason from the gateway (e.g., "insufficient_funds").
    /// Null on success.
    /// </summary>
    public string? FailureReason { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
