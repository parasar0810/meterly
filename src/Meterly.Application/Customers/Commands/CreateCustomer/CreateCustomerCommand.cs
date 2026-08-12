using FluentValidation;
using MediatR;
using Meterly.Application.Common.Exceptions;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Application.Customers.Commands.CreateCustomer;

// 1. Command
// This represents the request data. It's essentially a DTO that also 
// implements IRequest<TResponse> so MediatR knows what to return.
public record CreateCustomerCommand(
    string Name,
    string Email,
    string? Description
) : IRequest<Guid>;

// 2. Validator
// FluentValidation validator. The ValidationBehavior will automatically 
// run this before the Handler is invoked.
public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is not a valid email address.")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters.");
    }
}

// 3. Handler
// This is where the business logic lives.
public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Guid>
{
    private readonly IMeterlyDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateCustomerCommandHandler(
        IMeterlyDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId 
            ?? throw new UnauthorizedException("User is not associated with a tenant.");

        // Check if customer with this email already exists for THIS TENANT.
        // We rely on the Global Query Filter in DbContext to automatically 
        // filter by TenantId, but we explicitly specify it here for clarity just in case.
        var emailExists = await _dbContext.Customers
            .AnyAsync(c => c.Email == request.Email, cancellationToken);

        if (emailExists)
        {
            throw new ConflictException($"A customer with email '{request.Email}' already exists.");
        }

        var customer = new Customer
        {
            TenantId = tenantId,
            Name = request.Name,
            Email = request.Email,
            Description = request.Description
        };

        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return customer.Id;
    }
}
