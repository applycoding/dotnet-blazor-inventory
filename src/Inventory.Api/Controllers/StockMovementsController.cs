using Inventory.Application.Stock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/stock-movements")]
public sealed class StockMovementsController : ControllerBase
{
    private readonly IStockService _stock;

    public StockMovementsController(IStockService stock)
    {
        _stock = stock;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<StockMovementDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockMovementDto>>> List(
        [FromQuery] Guid? productId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var movements = await _stock.ListRecentAsync(productId, take, cancellationToken);
        return Ok(movements);
    }

    [HttpPost]
    [ProducesResponseType<StockMovementDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StockMovementDto>> Record(
        RecordMovementCommand command,
        CancellationToken cancellationToken)
    {
        var movement = await _stock.RecordAsync(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, movement);
    }
}
