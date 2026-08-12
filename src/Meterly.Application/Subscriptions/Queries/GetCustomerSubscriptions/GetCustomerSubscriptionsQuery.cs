using MediatR;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Application.Subscriptions.Queries.GetCustomerSubscriptions;

public record SubscriptionDto(
    Guid Id,
    Guid PlanId,
    string PlanName,
    SubscriptionStatus Status,
    DateTime StartDate,
    DateTime CurrentPeriodStart,
    DateTime CurrentPeriodEnd,
    bool CancelAtPeriodEnd
);

public record GetCustomerSubscriptionsQuery(Guid CustomerId) : IRequest<List<SubscriptionDto>>;

public class GetCustomerSubscriptionsQueryHandler : IRequestHandler<GetCustomerSubscriptionsQuery, List<SubscriptionDto>>
{
    private readonly IMeterlyDbContext _dbContext;

    public GetCustomerSubscriptionsQueryHandler(IMeterlyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<SubscriptionDto>> Handle(GetCustomerSubscriptionsQuery request, CancellationToken cancellationToken)
    {
        var subscriptions = await _dbContext.Subscriptions
            .AsNoTracking()
            .Include(s => s.Plan) // We need the Plan to get the name
            .Where(s => s.CustomerId == request.CustomerId)
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SubscriptionDto(
                s.Id,
                s.PlanId,
                s.Plan.Name,
                s.Status,
                s.StartDate,
                s.CurrentPeriodStart,
                s.CurrentPeriodEnd,
                s.CancelAtPeriodEnd
            ))
            .ToListAsync(cancellationToken);

        return subscriptions;
    }
}
