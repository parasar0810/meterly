using MediatR;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Application.Products.Queries.GetProducts;

public record PlanDto(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    BillingPeriod BillingPeriod,
    bool IsActive
);

public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    List<PlanDto> Plans
);

public record GetProductsQuery : IRequest<List<ProductDto>>;

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly IMeterlyDbContext _dbContext;

    public GetProductsQueryHandler(IMeterlyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Global Query Filter on Product automatically filters to the current tenant.
        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.Plans.Where(plan => !plan.IsDeleted))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return products.Select(p => new ProductDto(
            Id: p.Id,
            Name: p.Name,
            Description: p.Description,
            IsActive: p.IsActive,
            Plans: p.Plans.Select(plan => new PlanDto(
                Id: plan.Id,
                Name: plan.Name,
                Price: plan.Price,
                Currency: plan.Currency,
                BillingPeriod: plan.BillingPeriod,
                IsActive: plan.IsActive
            )).ToList()
        )).ToList();
    }
}
