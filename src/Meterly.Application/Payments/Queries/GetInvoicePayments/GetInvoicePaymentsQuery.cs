using MediatR;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Application.Payments.Queries.GetInvoicePayments;

public record PaymentDto(
    Guid Id,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    string IdempotencyKey,
    string? GatewayReference,
    string? FailureReason,
    DateTime CreatedAt
);

public record GetInvoicePaymentsQuery(Guid InvoiceId) : IRequest<List<PaymentDto>>;

public class GetInvoicePaymentsQueryHandler : IRequestHandler<GetInvoicePaymentsQuery, List<PaymentDto>>
{
    private readonly IMeterlyDbContext _dbContext;

    public GetInvoicePaymentsQueryHandler(IMeterlyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<PaymentDto>> Handle(GetInvoicePaymentsQuery request, CancellationToken cancellationToken)
    {
        var payments = await _dbContext.Payments
            .AsNoTracking()
            .Where(p => p.InvoiceId == request.InvoiceId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentDto(
                p.Id,
                p.Amount,
                p.Currency,
                p.Status,
                p.IdempotencyKey,
                p.GatewayReference,
                p.FailureReason,
                p.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return payments;
    }
}
