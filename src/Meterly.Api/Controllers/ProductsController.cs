using MediatR;
using Meterly.Application.Products.Commands.CreatePlan;
using Meterly.Application.Products.Commands.CreateProduct;
using Meterly.Application.Products.Queries.GetProducts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Meterly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct(
        [FromBody] CreateProductCommand command, 
        CancellationToken cancellationToken)
    {
        var productId = await _sender.Send(command, cancellationToken);
        
        // We don't have a GetProduct endpoint yet, so we just return 201 Created with the ID for now
        return Created("", productId); 
    }

    [HttpPost("{productId:guid}/plans")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePlan(
        Guid productId,
        [FromBody] CreatePlanCommand command, 
        CancellationToken cancellationToken)
    {
        // Bind the ID from the route to the command
        command.ProductId = productId;
        
        var planId = await _sender.Send(command, cancellationToken);
        
        return Created("", planId);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var query = new GetProductsQuery();
        var products = await _sender.Send(query, cancellationToken);
        
        return Ok(products);
    }
}
