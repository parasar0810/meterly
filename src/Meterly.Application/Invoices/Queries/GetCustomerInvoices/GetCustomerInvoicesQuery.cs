using MediatR;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Application.Invoices.Queries.GetCustomerInvoices;

public record InvoiceLineItemDto(
    Guid Id,
    string Description,
    int Quantity,
    decimal UnitAmount,
    decimal TotalAmount
);

public record InvoiceDto(
    Guid Id,
    string Number,
    InvoiceStatus Status,
    string Currency,
    decimal SubTotal,
    decimal TaxTotal,
    decimal Total,
    DateTime DueDate,
    DateTime? PaidAt,
    Guid? SubscriptionId,
    List<InvoiceLineItemDto> LineItems
);

public record GetCustomerInvoicesQuery(Guid CustomerId) : IRequest<List<InvoiceDto>>;

public class GetCustomerInvoicesQueryHandler : IRequestHandler<GetCustomerInvoicesQuery, List<InvoiceDto>>
{
    private readonly IMeterlyDbContext _dbContext;

    public GetCustomerInvoicesQueryHandler(IMeterlyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<InvoiceDto>> Handle(GetCustomerInvoicesQuery request, CancellationToken cancellationToken)
    {
        var invoices = await _dbContext.Invoices
            .AsNoTracking()
            .Include(i => i.LineItems)
            .Where(i => i.CustomerId == request.CustomerId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InvoiceDto(
                i.Id,
                i.Number,
                i.Status,
                i.Currency,
                i.SubTotal,
                i.TaxTotal,
                i.Total,
                i.DueDate,
                i.PaidAt,
                i.SubscriptionId,
                i.LineItems.Select(li => new InvoiceLineItemDto(
                    li.Id,
                    li.Description,
                    li.Quantity,
                    li.UnitAmount,
                    li.TotalAmount
                )).ToList()
            ))
            .ToListAsync(cancellationToken);

        return invoices;
    }
}
