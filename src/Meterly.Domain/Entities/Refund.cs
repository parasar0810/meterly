using Meterly.Domain.Common;

namespace Meterly.Domain.Entities;

public enum RefundStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3
}

public class Refund : BaseEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;

    public RefundStatus Status { get; set; }

    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// For idempotency, similar to payment processing.
    /// Prevents double-refunding the same request.
    /// </summary>
    public string IdempotencyKey { get; set; } = string.Empty;

    public string? GatewayReference { get; set; }
    public string? FailureReason { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
