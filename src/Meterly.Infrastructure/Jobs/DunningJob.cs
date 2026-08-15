using Hangfire;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Entities;
using Meterly.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Meterly.Infrastructure.Jobs;

/// <summary>
/// The Dunning Job — runs on a recurring schedule to retry failed invoice payments.
///
/// WHAT IS DUNNING?
/// When a payment fails (insufficient funds, expired card, etc.), you don't
/// just abandon the customer. You try again, multiple times, on a specific
/// schedule. This process is called "dunning" — named after the old English
/// practice of firmly requesting overdue payment.
///
/// OUR DUNNING SCHEDULE:
///   Attempt 1 → Immediately (when payment first fails)
///   Attempt 2 → 1 day after first failure
///   Attempt 3 → 3 days after first failure
///   Attempt 4 → 7 days after first failure (final attempt)
///   If all 4 fail → Subscription is SUSPENDED
///
/// WHY HANGFIRE?
/// This job needs to run on a schedule, survive server restarts, and be
/// observable (we can see failed jobs in the Hangfire dashboard). Using
/// a background thread or a Timer would lose state on restart. Hangfire
/// persists job state to PostgreSQL so nothing is lost.
///
/// WHY NOT CALL PayInvoiceCommand DIRECTLY?
/// Dunning runs outside of an HTTP request — there's no authenticated user.
/// We must bypass ICurrentUserService (which reads from HttpContext) and
/// inject tenant context manually. This is why we query per-tenant instead
/// of relying on global query filters.
/// </summary>
public class DunningJob
{
    // Retry schedule: day offsets from the FIRST failure date
    // Day 0 = immediate, Day 1 = 1 day later, etc.
    private static readonly int[] RetryDelayDays = { 0, 1, 3, 7 };
    private const int MaxAttempts = 4;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DunningJob> _logger;

    public DunningJob(IServiceScopeFactory scopeFactory, ILogger<DunningJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Entry point called by Hangfire on its recurring schedule.
    /// We create a new DI scope so we get fresh DbContext instances per run.
    /// </summary>
    [AutomaticRetry(Attempts = 0)] // Don't auto-retry the job itself; let it fail cleanly if DB is down
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Dunning] Starting dunning sweep at {Time}", DateTime.UtcNow);

        using var scope = _scopeFactory.CreateScope();

        // Use MeterlyDbContext directly (not IMeterlyDbContext) so we can bypass
        // the global tenant filter — dunning must see ALL tenants' overdue invoices
        var dbContext = scope.ServiceProvider.GetRequiredService<Meterly.Infrastructure.Data.MeterlyDbContext>();
        var paymentGateway = scope.ServiceProvider.GetRequiredService<IPaymentGateway>();
        var ledgerService = scope.ServiceProvider.GetRequiredService<ILedgerService>();

        // Find all invoices that are OPEN, past due, and NOT already undergoing
        // a dunning attempt scheduled in the future.
        // We use IgnoreQueryFilters() to bypass the tenant filter (this runs globally).
        var overdueInvoices = await dbContext.Invoices
            .IgnoreQueryFilters()
            .Include(i => i.Subscription)
            .Include(i => i.DunningAttempts)
            .Include(i => i.Payments)
            .Where(i =>
                !i.IsDeleted &&
                i.Status == InvoiceStatus.Open &&
                i.DueDate < DateTime.UtcNow &&
                i.SubscriptionId != null)  // Only retry subscription invoices, not one-off
            .ToListAsync(cancellationToken);

        _logger.LogInformation("[Dunning] Found {Count} overdue invoices to process", overdueInvoices.Count);

        foreach (var invoice in overdueInvoices)
        {
            await ProcessInvoiceAsync(dbContext, paymentGateway, ledgerService, invoice, cancellationToken);
        }

        _logger.LogInformation("[Dunning] Sweep complete at {Time}", DateTime.UtcNow);
    }

    private async Task ProcessInvoiceAsync(
        Meterly.Infrastructure.Data.MeterlyDbContext dbContext,
        IPaymentGateway paymentGateway,
        ILedgerService ledgerService,
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        var attemptCount = invoice.DunningAttempts.Count;

        // If we've already exhausted all retries, suspend the subscription
        if (attemptCount >= MaxAttempts)
        {
            await SuspendSubscriptionAsync(dbContext, invoice, cancellationToken);
            return;
        }

        // Calculate when this attempt should fire based on the dunning schedule.
        // The clock starts from when the invoice first became past-due (DueDate).
        var daysSinceDue = (DateTime.UtcNow - invoice.DueDate).TotalDays;
        var expectedDelayDays = RetryDelayDays[attemptCount];

        if (daysSinceDue < expectedDelayDays)
        {
            // Not yet time to retry — the schedule says wait longer
            _logger.LogDebug(
                "[Dunning] Invoice {InvoiceId}: attempt {Attempt} not due yet (need {Required} days, have {Actual:F1})",
                invoice.Id, attemptCount + 1, expectedDelayDays, daysSinceDue);
            return;
        }

        _logger.LogInformation(
            "[Dunning] Invoice {InvoiceId}: firing attempt {Attempt}/{Max}",
            invoice.Id, attemptCount + 1, MaxAttempts);

        // Generate a stable idempotency key for this specific attempt
        var idempotencyKey = $"dunning-{invoice.Id}-attempt-{attemptCount + 1}";

        // Check if we already tried this exact attempt (job might re-run after a crash)
        var alreadyAttempted = await dbContext.DunningAttempts
            .IgnoreQueryFilters()
            .AnyAsync(d => d.InvoiceId == invoice.Id && d.AttemptNumber == attemptCount + 1, cancellationToken);

        if (alreadyAttempted)
        {
            _logger.LogWarning("[Dunning] Invoice {InvoiceId} attempt {Attempt} already processed. Skipping.", invoice.Id, attemptCount + 1);
            return;
        }

        // Create a pending Payment record BEFORE calling the gateway
        var payment = new Payment
        {
            TenantId = invoice.TenantId,
            InvoiceId = invoice.Id,
            Amount = invoice.Total,
            Currency = invoice.Currency,
            Status = PaymentStatus.Pending,
            IdempotencyKey = idempotencyKey
        };
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Attempt the charge
        PaymentResult result;
        try
        {
            result = await paymentGateway.ChargeAsync(
                invoice.Total,
                invoice.Currency,
                idempotencyKey,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Dunning] Gateway threw an exception for invoice {InvoiceId}", invoice.Id);
            result = new PaymentResult(false, null, ex.Message);
        }

        // Update the payment record
        payment.Status = result.IsSuccess ? PaymentStatus.Succeeded : PaymentStatus.Failed;
        payment.GatewayReference = result.GatewayReference;
        payment.FailureReason = result.FailureReason;

        if (result.IsSuccess)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;

            // Reactivate the subscription if it was PastDue
            if (invoice.Subscription?.Status == SubscriptionStatus.PastDue ||
                invoice.Subscription?.Status == SubscriptionStatus.Suspended)
            {
                invoice.Subscription.Status = SubscriptionStatus.Active;
            }
        }
        else
        {
            // Mark subscription PastDue on first failure
            if (invoice.Subscription != null && invoice.Subscription.Status == SubscriptionStatus.Active)
            {
                invoice.Subscription.Status = SubscriptionStatus.PastDue;
            }
        }

        // Record this dunning attempt for the audit trail
        var dunningAttempt = new DunningAttempt
        {
            TenantId = invoice.TenantId,
            InvoiceId = invoice.Id,
            PaymentId = payment.Id,
            AttemptNumber = attemptCount + 1,
            Succeeded = result.IsSuccess,
            FailureReason = result.FailureReason,
            // Schedule next retry if there are remaining attempts and this one failed
            NextRetryAt = (!result.IsSuccess && attemptCount + 1 < MaxAttempts)
                ? invoice.DueDate.AddDays(RetryDelayDays[attemptCount + 1])
                : null
        };

        dbContext.DunningAttempts.Add(dunningAttempt);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Post ledger entries for successful dunning payment
        if (result.IsSuccess)
        {
            await ledgerService.PostPaymentEntriesAsync(invoice.TenantId, invoice, payment, cancellationToken);
        }
        // If this was the FINAL failed attempt, suspend the subscription
        else if (attemptCount + 1 >= MaxAttempts)
        {
            await SuspendSubscriptionAsync(dbContext, invoice, cancellationToken);
        }

        _logger.LogInformation(
            "[Dunning] Invoice {InvoiceId} attempt {Attempt} result: {Result}",
            invoice.Id, attemptCount + 1, result.IsSuccess ? "SUCCESS" : "FAILED");
    }

    private async Task SuspendSubscriptionAsync(
        Meterly.Infrastructure.Data.MeterlyDbContext dbContext,
        Invoice invoice,
        CancellationToken cancellationToken)
    {
        if (invoice.Subscription == null || invoice.Subscription.Status == SubscriptionStatus.Suspended)
            return;

        _logger.LogWarning(
            "[Dunning] Suspending subscription {SubscriptionId} for invoice {InvoiceId} — all retries exhausted",
            invoice.SubscriptionId, invoice.Id);

        invoice.Subscription.Status = SubscriptionStatus.Suspended;
        invoice.Status = InvoiceStatus.Uncollectible;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
