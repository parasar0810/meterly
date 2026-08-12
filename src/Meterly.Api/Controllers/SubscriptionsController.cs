using MediatR;
using Meterly.Application.Subscriptions.Commands.CancelSubscription;
using Meterly.Application.Subscriptions.Commands.CreateSubscription;
using Meterly.Application.Subscriptions.Queries.GetCustomerSubscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meterly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api")] // Notice we don't put [controller] here because we have multiple route bases
public class SubscriptionsController : ControllerBase
{
    private readonly ISender _sender;

    public SubscriptionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("subscriptions")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateSubscriptionCommand command, 
        CancellationToken cancellationToken)
    {
        var subscriptionId = await _sender.Send(command, cancellationToken);
        
        return Created("", subscriptionId);
    }

    [HttpPost("subscriptions/{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelSubscription(
        Guid id, 
        CancellationToken cancellationToken)
    {
        var command = new CancelSubscriptionCommand { SubscriptionId = id };
        await _sender.Send(command, cancellationToken);
        
        return NoContent();
    }

    // This route is under /api/customers/{customerId}/subscriptions
    [HttpGet("customers/{customerId:guid}/subscriptions")]
    [ProducesResponseType(typeof(List<SubscriptionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerSubscriptions(
        Guid customerId, 
        CancellationToken cancellationToken)
    {
        var query = new GetCustomerSubscriptionsQuery(customerId);
        var subscriptions = await _sender.Send(query, cancellationToken);
        
        return Ok(subscriptions);
    }
}
