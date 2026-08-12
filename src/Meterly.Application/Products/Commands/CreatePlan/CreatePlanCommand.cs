using FluentValidation;
using MediatR;
using Meterly.Application.Common.Exceptions;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Entities;
using Meterly.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Meterly.Application.Products.Commands.CreatePlan;

public record CreatePlanCommand(
    string Name,
    decimal Price,
    string Currency,
    BillingPeriod BillingPeriod
) : IRequest<Guid>
{
    // Ignored in JSON so the controller can bind it from the route
    [JsonIgnore]
    public Guid ProductId { get; set; }
}

public class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(v => v.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

        RuleFor(v => v.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.");

        RuleFor(v => v.BillingPeriod)
            .IsInEnum().WithMessage("Invalid Billing Period.");
    }
}

public class CreatePlanCommandHandler : IRequestHandler<CreatePlanCommand, Guid>
{
    private readonly IMeterlyDbContext _dbContext;

    public CreatePlanCommandHandler(IMeterlyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        // Global Query Filter on Product ensures we only find it if it belongs to our Tenant
        var productExists = await _dbContext.Products
            .AnyAsync(p => p.Id == request.ProductId, cancellationToken);

        if (!productExists)
        {
            throw new NotFoundException(nameof(Product), request.ProductId);
        }

        // Check if plan with this name already exists for this product
        var nameExists = await _dbContext.Plans
            .AnyAsync(p => p.ProductId == request.ProductId && p.Name == request.Name, cancellationToken);

        if (nameExists)
        {
            throw new ConflictException($"A plan with name '{request.Name}' already exists for this product.");
        }

        var plan = new Plan
        {
            ProductId = request.ProductId,
            Name = request.Name,
            Price = request.Price,
            Currency = request.Currency.ToUpperInvariant(),
            BillingPeriod = request.BillingPeriod,
            IsActive = true
        };

        _dbContext.Plans.Add(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return plan.Id;
    }
}
