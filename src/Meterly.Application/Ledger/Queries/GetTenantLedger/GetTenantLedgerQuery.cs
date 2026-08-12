using MediatR;
using Meterly.Application.Common.Exceptions;
using Meterly.Application.Common.Interfaces;
using Meterly.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Meterly.Application.Ledger.Queries.GetTenantLedger;

public record GetTenantLedgerQuery : IRequest<List<LedgerEntryDto>>;

public record LedgerEntryDto(
    Guid Id,
    string Type,
    decimal Amount,
    string Currency,
    string Description,
    Guid ReferenceId,
    string ReferenceType,
    DateTime CreatedAt
);

public class GetTenantLedgerQueryHandler : IRequestHandler<GetTenantLedgerQuery, List<LedgerEntryDto>>
{
    private readonly IMeterlyDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public GetTenantLedgerQueryHandler(IMeterlyDbContext dbContext, ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<List<LedgerEntryDto>> Handle(GetTenantLedgerQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.TenantId == null)
            throw new UnauthorizedException("User is not associated with a tenant.");

        return await _dbContext.LedgerEntries
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new LedgerEntryDto(
                l.Id,
                l.Type == LedgerEntryType.Debit ? "Debit" : "Credit",
                l.Amount,
                l.Currency,
                l.Description,
                l.ReferenceId,
                l.ReferenceType,
                l.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}
