using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Entities;
using Meterly.Domain.Enums;

namespace Meterly.Infrastructure.Services;

public class LedgerService : ILedgerService
{
    private readonly IMeterlyDbContext _dbContext;

    public LedgerService(IMeterlyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task PostPaymentEntriesAsync(
        Guid tenantId,
        Invoice invoice,
        Payment payment,
        CancellationToken cancellationToken)
    {
        // DOUBLE-ENTRY: Every payment creates two ledger entries.
        // The Debit records the original liability (money owed to us).
        // The Credit records the actual money received.
        // Together they sum to zero, proving the books balance.

        var debit = new LedgerEntry
        {
            TenantId = tenantId,
            Type = LedgerEntryType.Debit,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Description = $"Accounts Receivable cleared for invoice {invoice.Number}",
            ReferenceId = invoice.Id,
            ReferenceType = "Invoice"
        };

        var credit = new LedgerEntry
        {
            TenantId = tenantId,
            Type = LedgerEntryType.Credit,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Description = $"Payment received — {payment.GatewayReference}",
            ReferenceId = payment.Id,
            ReferenceType = "Payment"
        };

        _dbContext.LedgerEntries.Add(debit);
        _dbContext.LedgerEntries.Add(credit);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PostRefundEntriesAsync(
        Guid tenantId,
        Refund refund,
        CancellationToken cancellationToken)
    {
        // DOUBLE-ENTRY FOR REFUNDS: Reverses the original payment
        // Debit: Revenue (decreases the revenue we thought we had)
        // Credit: Accounts Payable (money we owe back to customer, leaving our bank)
        
        var debit = new LedgerEntry
        {
            TenantId = tenantId,
            Type = LedgerEntryType.Debit,
            Amount = refund.Amount,
            Currency = refund.Currency,
            Description = $"Refund initiated for invoice {refund.InvoiceId}",
            ReferenceId = refund.Id,
            ReferenceType = "Refund"
        };

        var credit = new LedgerEntry
        {
            TenantId = tenantId,
            Type = LedgerEntryType.Credit,
            Amount = refund.Amount,
            Currency = refund.Currency,
            Description = $"Refund sent out — {refund.GatewayReference}",
            ReferenceId = refund.Id,
            ReferenceType = "Refund"
        };

        _dbContext.LedgerEntries.Add(debit);
        _dbContext.LedgerEntries.Add(credit);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
