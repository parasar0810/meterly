using MediatR;
using Meterly.Application.Common.Exceptions;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Entities;
using Meterly.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Meterly.Application.Invoices.Commands.PayInvoice;

public record PayInvoiceCommand : IRequest<PayInvoiceResult>
{
    [JsonIgnore]
    public Guid InvoiceId { get; set; }

    /// <summary>
    /// Optional client-supplied idempotency key.
    /// If not provided, we generate one from the InvoiceId.
    /// </summary>
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// The result of a payment attempt — tells the caller whether it succeeded or failed.
/// </summary>
public record PayInvoiceResult(
    bool Success,
    string? GatewayReference,
    string? FailureReason
);

public class PayInvoiceCommandHandler : IRequestHandler<PayInvoiceCommand, PayInvoiceResult>
{
    private readonly IMeterlyDbContext _dbContext;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ICurrentUserService _currentUserService;
    private readonly IWebhookDispatcher _webhookDispatcher;
    private readonly ILedgerService _ledgerService;

    public PayInvoiceCommandHandler(
        IMeterlyDbContext dbContext,
        IPaymentGateway paymentGateway,
        ICurrentUserService currentUserService,
        IWebhookDispatcher webhookDispatcher,
        ILedgerService ledgerService)
    {
        _dbContext = dbContext;
        _paymentGateway = paymentGateway;
        _currentUserService = currentUserService;
        _webhookDispatcher = webhookDispatcher;
        _ledgerService = ledgerService;
    }

    public async Task<PayInvoiceResult> Handle(PayInvoiceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId
            ?? throw new UnauthorizedException("User is not associated with a tenant.");

        var invoice = await _dbContext.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken);

        if (invoice == null)
            throw new NotFoundException(nameof(Invoice), request.InvoiceId);

        if (invoice.Status == InvoiceStatus.Void)
            throw new BusinessRuleException("Cannot pay a voided invoice.");

        // Build an idempotency key. If the client provides one, use it.
        // Otherwise fall back to a deterministic key based on invoice + attempt count.
        var attemptCount = await _dbContext.Payments
            .CountAsync(p => p.InvoiceId == request.InvoiceId, cancellationToken);

        var idempotencyKey = request.IdempotencyKey
            ?? $"{request.InvoiceId}-attempt-{attemptCount + 1}";

        // Check if we've already processed this exact idempotency key (duplicate request)
        // MUST DO THIS BEFORE checking if the invoice is paid! Otherwise, an idempotent 
        // retry of a successful payment will fail with "Invoice is already paid".
        var existingPayment = await _dbContext.Payments
            .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, cancellationToken);

        if (existingPayment != null)
        {
            // Idempotent response — return the same result without charging again
            return new PayInvoiceResult(
                existingPayment.Status == PaymentStatus.Succeeded,
                existingPayment.GatewayReference,
                existingPayment.FailureReason
            );
        }

        // Now validate if the invoice is already paid by some OTHER payment
        if (invoice.Status == InvoiceStatus.Paid)
            throw new BusinessRuleException("Invoice is already paid.");

        // Create a pending payment record BEFORE calling the gateway.
        // If the process crashes mid-flight, we have a record to investigate.
        var payment = new Payment
        {
            TenantId = tenantId,
            InvoiceId = invoice.Id,
            Amount = invoice.Total,
            Currency = invoice.Currency,
            Status = PaymentStatus.Pending,
            IdempotencyKey = idempotencyKey
        };

        _dbContext.Payments.Add(payment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Call the payment gateway (simulated)
        var result = await _paymentGateway.ChargeAsync(
            invoice.Total,
            invoice.Currency,
            idempotencyKey,
            cancellationToken);

        // Update the payment record based on gateway result
        payment.Status = result.IsSuccess ? PaymentStatus.Succeeded : PaymentStatus.Failed;
        payment.GatewayReference = result.GatewayReference;
        payment.FailureReason = result.FailureReason;

        // Only mark invoice as Paid if the charge actually succeeded
        if (result.IsSuccess)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Dispatch Webhook
        var webhookEvent = new Meterly.Domain.Common.WebhookEvent
        {
            EventType = result.IsSuccess ? "payment.succeeded" : "payment.failed",
            Data = System.Text.Json.JsonSerializer.SerializeToElement(new
            {
                InvoiceId = invoice.Id,
                PaymentId = payment.Id,
                Amount = payment.Amount,
                Currency = payment.Currency,
                GatewayReference = payment.GatewayReference,
                FailureReason = payment.FailureReason
            })
        };

        // Note: For Phase 9 this is a synchronous call.
        // In a production system, this should be published to a queue/Hangfire.
        await _webhookDispatcher.DispatchAsync(tenantId, webhookEvent, cancellationToken);

        // Post double-entry ledger entries for successful payments
        if (result.IsSuccess)
        {
            await _ledgerService.PostPaymentEntriesAsync(tenantId, invoice, payment, cancellationToken);
        }

        return new PayInvoiceResult(
            result.IsSuccess,
            result.GatewayReference,
            result.FailureReason
        );
    }
}
