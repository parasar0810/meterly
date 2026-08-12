using FluentValidation;
using MediatR;
using Meterly.Application.Common.Exceptions;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Entities;
using Meterly.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Application.Subscriptions.Commands.CreateSubscription;

public record CreateSubscriptionCommand(
    Guid CustomerId,
    Guid PlanId
) : IRequest<Guid>;

public class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(v => v.CustomerId).NotEmpty().WithMessage("Customer ID is required.");
        RuleFor(v => v.PlanId).NotEmpty().WithMessage("Plan ID is required.");
    }
}

public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, Guid>
{
    private readonly IMeterlyDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateSubscriptionCommandHandler(
        IMeterlyDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId 
            ?? throw new UnauthorizedException("User is not associated with a tenant.");

        // Validate Customer exists (Global Query Filter ensures it belongs to the tenant)
        var customerExists = await _dbContext.Customers
            .AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
            
        if (!customerExists)
            throw new NotFoundException(nameof(Customer), request.CustomerId);

        // Validate Plan exists
        var plan = await _dbContext.Plans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, cancellationToken);
            
        if (plan == null)
            throw new NotFoundException(nameof(Plan), request.PlanId);

        var now = DateTime.UtcNow;
        var currentPeriodEnd = plan.BillingPeriod == BillingPeriod.Monthly 
            ? now.AddMonths(1) 
            : now.AddYears(1);

        var subscription = new Subscription
        {
            TenantId = tenantId,
            CustomerId = request.CustomerId,
            PlanId = request.PlanId,
            Status = SubscriptionStatus.Active, // For now we jump straight to active
            StartDate = now,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = currentPeriodEnd,
            CancelAtPeriodEnd = false
        };

        _dbContext.Subscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return subscription.Id;
    }
}
