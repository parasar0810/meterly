using MediatR;
using Meterly.Application.Customers.Commands.CreateCustomer;
using Meterly.Application.Customers.Queries.GetCustomer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meterly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    // Notice we inject ISender (MediatR), NOT a specific CustomerService.
    // This keeps the controller extremely thin.
    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerCommand command, 
        CancellationToken cancellationToken)
    {
        // 1. The ValidationBehavior will automatically validate the command
        // 2. The Handler will execute the business logic
        var customerId = await _sender.Send(command, cancellationToken);
        
        return CreatedAtAction(nameof(GetCustomer), new { id = customerId }, customerId);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomer(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var query = new GetCustomerQuery(id);
        var customer = await _sender.Send(query, cancellationToken);
        
        return Ok(customer);
    }
}
