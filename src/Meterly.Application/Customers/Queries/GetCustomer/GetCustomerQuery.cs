using MediatR;
using Meterly.Application.Common.Exceptions;
using Meterly.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Application.Customers.Queries.GetCustomer;

// DTO to return to the API (we never return EF Core entities directly to the client)
public record CustomerDto(
    Guid Id,
    string Name,
    string Email,
    string? Description,
    DateTime CreatedAt
);

// 1. Query
public record GetCustomerQuery(Guid Id) : IRequest<CustomerDto>;

// 2. Handler
public class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, CustomerDto>
{
    private readonly IMeterlyDbContext _dbContext;

    public GetCustomerQueryHandler(IMeterlyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomerDto> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
    {
        // Global Query Filter ensures we only query our Tenant's customers
        var customer = await _dbContext.Customers
            .AsNoTracking() // Queries are read-only, so we disable change tracking for performance
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (customer == null)
        {
            throw new NotFoundException(nameof(customer), request.Id);
        }

        return new CustomerDto(
            Id: customer.Id,
            Name: customer.Name,
            Email: customer.Email,
            Description: customer.Description,
            CreatedAt: customer.CreatedAt
        );
    }
}
