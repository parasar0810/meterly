namespace Meterly.Domain.Enums;

/// <summary>
/// In double-entry accounting, every transaction has two sides.
/// A Debit increases an asset or expense. A Credit increases a liability or revenue.
/// 
/// In our billing context:
/// - When an invoice is CREATED → Debit "Accounts Receivable" (customer owes us money)
/// - When a payment SUCCEEDS → Credit "Revenue Received" (we received the money)
/// 
/// The sum of all Debits must always equal the sum of all Credits. This is the
/// fundamental invariant of double-entry bookkeeping, and it's how you detect bugs.
/// </summary>
public enum LedgerEntryType
{
    Debit = 1,
    Credit = 2
}
