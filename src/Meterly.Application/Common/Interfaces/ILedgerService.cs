using Meterly.Domain.Entities;
using Meterly.Domain.Enums;

namespace Meterly.Application.Common.Interfaces;

/// <summary>
/// Service responsible for posting balanced double-entry ledger records.
/// 
/// Every call to PostPaymentEntriesAsync creates EXACTLY TWO ledger entries:
/// one Debit and one Credit. If they don't balance, we have a bug.
/// </summary>
public interface ILedgerService
{
    /// <summary>
    /// Posts the double-entry pair for a successful payment.
    /// Creates: Debit "Accounts Receivable" + Credit "Revenue"
    /// </summary>
    Task PostPaymentEntriesAsync(
        Guid tenantId,
        Invoice invoice,
        Payment payment,
        CancellationToken cancellationToken);

    /// <summary>
    /// Posts the double-entry pair for a refund.
    /// Creates: Debit "Revenue" + Credit "Accounts Payable / Cash"
    /// </summary>
    Task PostRefundEntriesAsync(
        Guid tenantId,
        Refund refund,
        CancellationToken cancellationToken);
}
