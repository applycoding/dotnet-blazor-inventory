using MediatR;

using Microsoft.AspNetCore.Mvc;

using Stockroom.Application.Products;
using Stockroom.Contracts.Dtos;
using Stockroom.Contracts.Requests;

namespace Stockroom.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new GetProductsQuery(search, page, pageSize), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetProductByIdQuery(id), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var created = await _sender.Send(
            new CreateProductCommand(request.Sku, request.Name, request.Description, request.UnitPrice, request.QuantityOnHand, request.ReorderLevel),
            cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var updated = await _sender.Send(
            new UpdateProductCommand(id, request.Name, request.Description, request.UnitPrice, request.ReorderLevel, request.IsActive),
            cancellationToken);
        return Ok(updated);
    }

    [HttpPost("{id:guid}/stock-adjustments")]
    public async Task<ActionResult<ProductDto>> AdjustStock(Guid id, AdjustStockRequest request, CancellationToken cancellationToken)
    {
        var updated = await _sender.Send(new AdjustStockCommand(id, request.Delta, request.Reason), cancellationToken);
        return Ok(updated);
    }
}
