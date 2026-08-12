using Meterly.Domain.Common;
using Meterly.Domain.Enums;

namespace Meterly.Domain.Entities;

/// <summary>
/// Represents a single line in the financial ledger.
/// 
/// WHY DOUBLE-ENTRY?
/// Every financial event creates exactly TWO ledger entries: one Debit and one Credit.
/// This is NOT redundant — it gives you:
///   1. A self-balancing audit trail (sum of Debits must equal sum of Credits)
///   2. The ability to run financial reports: accounts receivable, revenue, refunds
///   3. Detection of missing or duplicate transactions
/// 
/// EXAMPLE — a $199 payment succeeding:
///   Debit  $199  "Accounts Receivable"  (invoice was owed)
///   Credit $199  "Revenue"              (payment received)
///
/// EXAMPLE — a $25 refund:
///   Debit  $25   "Revenue"              (reversing the credit)
///   Credit $25   "Accounts Payable"     (we now owe the customer)
/// </summary>
public class LedgerEntry : BaseEntity, ISoftDeletable
{
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    public LedgerEntryType Type { get; set; }

    /// <summary>Amount is always positive. Type (Debit/Credit) gives it direction.</summary>
    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    /// <summary>Human-readable description, e.g. "Payment received for INV-0042".</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The Invoice or Payment this entry relates to.</summary>
    public Guid ReferenceId { get; set; }

    /// <summary>e.g. "Invoice", "Payment", "Refund" — for filtering in reports.</summary>
    public string ReferenceType { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
