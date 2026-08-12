using MediatR;
using Meterly.Application.Invoices.Commands.CreateInvoice;
using Meterly.Application.Invoices.Commands.PayInvoice;
using Meterly.Application.Invoices.Queries.GetCustomerInvoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meterly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class InvoicesController : ControllerBase
{
    private readonly ISender _sender;

    public InvoicesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("invoices")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInvoice(
        [FromBody] CreateInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        var invoiceId = await _sender.Send(command, cancellationToken);
        return Created("", invoiceId);
    }

    [HttpPost("invoices/{id:guid}/pay")]
    [ProducesResponseType(typeof(PayInvoiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PayInvoice(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new PayInvoiceCommand 
        { 
            InvoiceId = id,
            IdempotencyKey = idempotencyKey 
        };
        
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("customers/{customerId:guid}/invoices")]
    [ProducesResponseType(typeof(List<InvoiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerInvoices(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var query = new GetCustomerInvoicesQuery(customerId);
        var invoices = await _sender.Send(query, cancellationToken);
        return Ok(invoices);
    }
}
