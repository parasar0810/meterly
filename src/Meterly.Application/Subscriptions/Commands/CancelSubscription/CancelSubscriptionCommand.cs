using FluentValidation;
using MediatR;
using Meterly.Application.Common.Exceptions;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Entities;
using Meterly.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Meterly.Application.Subscriptions.Commands.CancelSubscription;

public record CancelSubscriptionCommand : IRequest<Unit>
{
    [JsonIgnore]
    public Guid SubscriptionId { get; set; }
}

public class CancelSubscriptionCommandHandler : IRequestHandler<CancelSubscriptionCommand, Unit>
{
    private readonly IMeterlyDbContext _dbContext;

    public CancelSubscriptionCommandHandler(IMeterlyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Unit> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId, cancellationToken);

        if (subscription == null)
            throw new NotFoundException(nameof(Subscription), request.SubscriptionId);

        if (subscription.Status == SubscriptionStatus.Canceled)
            throw new BusinessRuleException("Subscription is already canceled.");

        // Instead of immediately killing access, we mark it to cancel at period end.
        // A background job (which we will build later) will handle flipping the status to Canceled
        // when CurrentPeriodEnd is reached.
        subscription.CancelAtPeriodEnd = true;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
