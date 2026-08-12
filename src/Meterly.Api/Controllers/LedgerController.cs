using MediatR;
using Meterly.Application.Ledger.Queries.GetTenantLedger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meterly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LedgerController : ControllerBase
{
    private readonly ISender _sender;

    public LedgerController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<LedgerEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLedger(CancellationToken cancellationToken)
    {
        var entries = await _sender.Send(new GetTenantLedgerQuery(), cancellationToken);
        return Ok(entries);
    }
}
