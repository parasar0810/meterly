using FluentValidation;
using MediatR;
using Meterly.Application.Common.Exceptions;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Application.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string? Description
) : IRequest<Guid>;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IMeterlyDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public CreateProductCommandHandler(
        IMeterlyDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId 
            ?? throw new UnauthorizedException("User is not associated with a tenant.");

        // Check if product with this name already exists for this tenant
        var nameExists = await _dbContext.Products
            .AnyAsync(p => p.Name == request.Name, cancellationToken);

        if (nameExists)
        {
            throw new ConflictException($"A product with name '{request.Name}' already exists.");
        }

        var product = new Product
        {
            TenantId = tenantId,
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
