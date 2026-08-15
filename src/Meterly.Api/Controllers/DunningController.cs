using Hangfire;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Enums;
using Meterly.Infrastructure.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Api.Controllers;

/// <summary>
/// Exposes dunning information and manual trigger endpoints.
///
/// INTERVIEW POINT:
/// In production you'd typically NOT expose a "trigger dunning now" endpoint —
/// it's a security risk. But for observability and ops tooling you might expose
/// a "what's the dunning status of this subscription?" query, or a webhook
/// from your payment processor to retry immediately on card update.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DunningController : ControllerBase
{
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IMeterlyDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public DunningController(
        IRecurringJobManager recurringJobManager,
        IBackgroundJobClient backgroundJobClient,
        IMeterlyDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _recurringJobManager = recurringJobManager;
        _backgroundJobClient = backgroundJobClient;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Returns all dunning attempts for a specific invoice.
    /// Useful for debugging why a subscription was suspended.
    /// </summary>
    [HttpGet("invoices/{invoiceId}/attempts")]
    [ProducesResponseType(typeof(IEnumerable<DunningAttemptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDunningAttempts(Guid invoiceId, CancellationToken cancellationToken)
    {
        var attempts = await _dbContext.DunningAttempts
            .Where(d => d.InvoiceId == invoiceId)
            .OrderBy(d => d.AttemptNumber)
            .Select(d => new DunningAttemptDto(
                d.Id,
                d.InvoiceId,
                d.AttemptNumber,
                d.Succeeded,
                d.FailureReason,
                d.NextRetryAt,
                d.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(attempts);
    }

    /// <summary>
    /// Returns all overdue subscriptions for the current tenant.
    /// Useful for a billing ops dashboard.
    /// </summary>
    [HttpGet("overdue")]
    [ProducesResponseType(typeof(IEnumerable<OverdueSubscriptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverdueSubscriptions(CancellationToken cancellationToken)
    {
        var overdue = await _dbContext.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.PastDue || s.Status == SubscriptionStatus.Suspended)
            .Include(s => s.Customer)
            .Include(s => s.Plan)
            .Select(s => new OverdueSubscriptionDto(
                s.Id,
                s.Customer.Email,
                s.Plan.Name,
                s.Status.ToString(),
                s.Invoices
                    .Where(i => i.Status == InvoiceStatus.Open || i.Status == InvoiceStatus.Uncollectible)
                    .Select(i => i.Total)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return Ok(overdue);
    }

    /// <summary>
    /// Manually triggers a dunning sweep immediately (dev/ops tool).
    /// In production this would be behind an admin role check.
    /// </summary>
    [HttpPost("trigger")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult TriggerDunningSweep()
    {
        // Enqueue as a fire-and-forget background job (runs ASAP in the "dunning" queue)
        var jobId = _backgroundJobClient.Enqueue<DunningJob>(
            queue: "dunning",
            methodCall: job => job.RunAsync(CancellationToken.None));

        return Accepted(new { JobId = jobId, Message = "Dunning sweep enqueued." });
    }
}

// DTOs — kept in the controller file for simplicity in this phase
public record DunningAttemptDto(
    Guid Id,
    Guid InvoiceId,
    int AttemptNumber,
    bool Succeeded,
    string? FailureReason,
    DateTime? NextRetryAt,
    DateTime AttemptedAt);

public record OverdueSubscriptionDto(
    Guid SubscriptionId,
    string CustomerEmail,
    string PlanName,
    string Status,
    decimal AmountOutstanding);
