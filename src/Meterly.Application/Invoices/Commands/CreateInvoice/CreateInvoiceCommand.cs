using FluentValidation;
using MediatR;
using Meterly.Application.Common.Exceptions;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Entities;
using Meterly.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Application.Invoices.Commands.CreateInvoice;

/// <summary>
/// Represents a single line item in the invoice creation request.
/// </summary>
public record CreateInvoiceLineItemDto(
    string Description,
    int Quantity,
    decimal UnitAmount
);

public record CreateInvoiceCommand(
    Guid CustomerId,
    Guid? SubscriptionId,
    string Currency,
    DateTime DueDate,
    List<CreateInvoiceLineItemDto> LineItems
) : IRequest<Guid>;

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(v => v.CustomerId).NotEmpty().WithMessage("Customer ID is required.");
        
        RuleFor(v => v.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.");

        RuleFor(v => v.DueDate)
            .Must(d => d.ToUniversalTime() > DateTime.UtcNow)
            .WithMessage("Due date must be in the future.");

        RuleFor(v => v.LineItems)
            .NotEmpty().WithMessage("At least one line item is required.");

        RuleForEach(v => v.LineItems).ChildRules(item =>
        {
            item.RuleFor(i => i.Description).NotEmpty().WithMessage("Line item description is required.");
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0.");
            item.RuleFor(i => i.UnitAmount).GreaterThanOrEqualTo(0).WithMessage("Unit amount cannot be negative.");
        });
    }
}

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Guid>
{
    private readonly IMeterlyDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateInvoiceCommandHandler(
        IMeterlyDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId 
            ?? throw new UnauthorizedException("User is not associated with a tenant.");

        // Validate Customer exists
        var customerExists = await _dbContext.Customers
            .AnyAsync(c => c.Id == request.CustomerId, cancellationToken);
        if (!customerExists)
            throw new NotFoundException(nameof(Customer), request.CustomerId);

        // Validate Subscription exists (if provided)
        if (request.SubscriptionId.HasValue)
        {
            var subExists = await _dbContext.Subscriptions
                .AnyAsync(s => s.Id == request.SubscriptionId.Value, cancellationToken);
            if (!subExists)
                throw new NotFoundException(nameof(Subscription), request.SubscriptionId.Value);
        }

        // Auto-generate invoice number: INV-XXXX based on tenant's invoice count
        var invoiceCount = await _dbContext.Invoices
            .IgnoreQueryFilters() // Count ALL invoices (including deleted) for unique numbering
            .CountAsync(i => i.TenantId == tenantId, cancellationToken);
        var invoiceNumber = $"INV-{(invoiceCount + 1):D4}";

        // Build line items and compute totals
        var lineItems = request.LineItems.Select(dto => new InvoiceLineItem
        {
            Description = dto.Description,
            Quantity = dto.Quantity,
            UnitAmount = dto.UnitAmount,
            TotalAmount = dto.Quantity * dto.UnitAmount
        }).ToList();

        var subTotal = lineItems.Sum(li => li.TotalAmount);

        var invoice = new Invoice
        {
            TenantId = tenantId,
            CustomerId = request.CustomerId,
            SubscriptionId = request.SubscriptionId,
            Number = invoiceNumber,
            Status = InvoiceStatus.Draft,
            Currency = request.Currency.ToUpperInvariant(),
            SubTotal = subTotal,
            TaxTotal = 0, // Tax calculation will be a later enhancement
            Total = subTotal,
            // Normalize to UTC — PostgreSQL 'timestamp with time zone' requires UTC
            DueDate = request.DueDate.Kind == DateTimeKind.Utc
                ? request.DueDate
                : request.DueDate.ToUniversalTime(),
            LineItems = lineItems
        };

        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return invoice.Id;
    }
}
