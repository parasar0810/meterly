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
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PayInvoice(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var key = idempotencyKey ?? Guid.NewGuid().ToString("N");
        var command = new PayInvoiceCommand 
        { 
            InvoiceId = id,
            IdempotencyKey = key 
        };
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("invoices/{id:guid}/refund")]
    [ProducesResponseType(typeof(Meterly.Application.Invoices.Commands.RefundInvoice.RefundInvoiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefundInvoice(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] Meterly.Application.Invoices.Commands.RefundInvoice.RefundInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        // Enforce the ID from the route matches the payload
        if (id != request.InvoiceId)
            return BadRequest();

        var key = idempotencyKey ?? Guid.NewGuid().ToString("N");
        
        var command = request with { InvoiceId = id, IdempotencyKey = key };
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
