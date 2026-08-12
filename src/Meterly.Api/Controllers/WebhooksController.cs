using MediatR;
using Meterly.Application.Webhooks.Commands.CreateWebhookEndpoint;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meterly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly ISender _sender;

    public WebhooksController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("endpoints")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEndpoint(
        [FromBody] CreateWebhookEndpointCommand command,
        CancellationToken cancellationToken)
    {
        var endpointId = await _sender.Send(command, cancellationToken);
        return Created("", endpointId);
    }
}
