using Meterly.Domain.Common;
using Meterly.Domain.Enums;

namespace Meterly.Domain.Entities;

/// <summary>
/// Tracks each individual retry attempt in the dunning process.
///
/// WHY TRACK ATTEMPTS?
/// When a payment fails, you don't just charge again immediately — you wait
/// specific intervals (e.g., 1 day, 3 days, 7 days). Each attempt is recorded
/// here so the dunning job can:
///   1. Know how many attempts have been made
///   2. Know whether the next retry window has elapsed
///   3. Give up after MaxAttempts and suspend the subscription
///
/// This is an immutable audit trail — attempts are NEVER deleted or modified.
/// </summary>
public class DunningAttempt : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }

    /// <summary>
    /// Which attempt number is this? (1-indexed, e.g., 1st, 2nd, 3rd retry)
    /// </summary>
    public int AttemptNumber { get; set; }

    /// <summary>
    /// Whether this specific attempt resulted in a successful charge.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// The reason the payment failed (if it did).
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// When the next retry should be attempted (null if this was the final attempt).
    /// </summary>
    public DateTime? NextRetryAt { get; set; }
}
