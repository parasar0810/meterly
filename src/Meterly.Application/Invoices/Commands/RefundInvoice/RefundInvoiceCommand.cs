using FluentValidation;
using MediatR;
using Meterly.Application.Common.Exceptions;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Entities;
using Meterly.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Application.Invoices.Commands.RefundInvoice;

public record RefundInvoiceCommand(
    Guid InvoiceId,
    decimal Amount,
    string Reason,
    string IdempotencyKey
) : IRequest<RefundInvoiceResult>;

public record RefundInvoiceResult(
    bool IsSuccess,
    string? GatewayReference,
    string? FailureReason
);

public class RefundInvoiceCommandValidator : AbstractValidator<RefundInvoiceCommand>
{
    public RefundInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(100);
    }
}

public class RefundInvoiceCommandHandler : IRequestHandler<RefundInvoiceCommand, RefundInvoiceResult>
{
    private readonly IMeterlyDbContext _dbContext;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILedgerService _ledgerService;

    public RefundInvoiceCommandHandler(
        IMeterlyDbContext dbContext,
        IPaymentGateway paymentGateway,
        ICurrentUserService currentUserService,
        ILedgerService ledgerService)
    {
        _dbContext = dbContext;
        _paymentGateway = paymentGateway;
        _currentUserService = currentUserService;
        _ledgerService = ledgerService;
    }

    public async Task<RefundInvoiceResult> Handle(RefundInvoiceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId
            ?? throw new UnauthorizedException("User is not associated with a tenant.");

        // 1. Validate invoice exists and is Paid
        var invoice = await _dbContext.Invoices
            .Include(i => i.Payments)
            .Include(i => i.Refunds)
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(Invoice), request.InvoiceId);

        if (invoice.Status != InvoiceStatus.Paid && invoice.Status != InvoiceStatus.PartiallyRefunded)
        {
            throw new Meterly.Application.Common.Exceptions.ValidationException(
                new Dictionary<string, string[]> { { "InvoiceId", new[] { "Only Paid or Partially Refunded invoices can be refunded." } } }
            );
        }

        // 2. Idempotency Check
        var existingRefund = invoice.Refunds.FirstOrDefault(r => r.IdempotencyKey == request.IdempotencyKey);
        if (existingRefund != null)
        {
            return new RefundInvoiceResult(
                existingRefund.Status == RefundStatus.Succeeded,
                existingRefund.GatewayReference,
                existingRefund.FailureReason
            );
        }

        // 3. Find successful payment to refund against
        var successfulPayment = invoice.Payments
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefault(p => p.Status == PaymentStatus.Succeeded)
            ?? throw new Meterly.Application.Common.Exceptions.ValidationException(
                new Dictionary<string, string[]> { { "InvoiceId", new[] { "No successful payment found to refund against." } } }
            );

        // 4. Validate Amount (can't refund more than paid minus already refunded)
        var totalRefunded = invoice.Refunds
            .Where(r => r.Status == RefundStatus.Succeeded)
            .Sum(r => r.Amount);
            
        var amountAvailableToRefund = successfulPayment.Amount - totalRefunded;

        if (request.Amount > amountAvailableToRefund)
        {
            throw new Meterly.Application.Common.Exceptions.ValidationException(
                new Dictionary<string, string[]> { { "Amount", new[] { $"Cannot refund more than available amount ({amountAvailableToRefund} {invoice.Currency})." } } }
            );
        }

        // 5. Create pending Refund record
        var refund = new Refund
        {
            TenantId = tenantId,
            InvoiceId = invoice.Id,
            PaymentId = successfulPayment.Id,
            Amount = request.Amount,
            Currency = invoice.Currency,
            Status = RefundStatus.Pending,
            Reason = request.Reason,
            IdempotencyKey = request.IdempotencyKey
        };

        _dbContext.Refunds.Add(refund);
        await _dbContext.SaveChangesAsync(cancellationToken); // Save pending state

        // 6. Call Gateway
        var result = await _paymentGateway.RefundAsync(
            successfulPayment.GatewayReference!,
            request.Amount,
            invoice.Currency,
            request.IdempotencyKey,
            cancellationToken);

        // 7. Update status based on result
        refund.Status = result.IsSuccess ? RefundStatus.Succeeded : RefundStatus.Failed;
        refund.GatewayReference = result.GatewayReference;
        refund.FailureReason = result.FailureReason;

        // 8. Update Invoice Status if successful
        if (result.IsSuccess)
        {
            var newTotalRefunded = totalRefunded + request.Amount;
            invoice.Status = newTotalRefunded >= successfulPayment.Amount 
                ? InvoiceStatus.Refunded 
                : InvoiceStatus.PartiallyRefunded;
                
            // 9. Post double-entry ledger entries for refund
            await _ledgerService.PostRefundEntriesAsync(tenantId, refund, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RefundInvoiceResult(
            result.IsSuccess,
            result.GatewayReference,
            result.FailureReason
        );
    }
}
