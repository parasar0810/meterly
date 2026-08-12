using MediatR;
using Meterly.Application.Payments.Queries.GetInvoicePayments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meterly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    public PaymentsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("invoices/{invoiceId:guid}/payments")]
    [ProducesResponseType(typeof(List<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoicePayments(
        Guid invoiceId,
        CancellationToken cancellationToken)
    {
        var query = new GetInvoicePaymentsQuery(invoiceId);
        var payments = await _sender.Send(query, cancellationToken);
        return Ok(payments);
    }
}
