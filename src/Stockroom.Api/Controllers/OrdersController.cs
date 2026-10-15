using MediatR;

using Microsoft.AspNetCore.Mvc;

using Stockroom.Application.Orders;
using Stockroom.Contracts.Dtos;
using Stockroom.Contracts.Requests;
using Stockroom.Domain.Enums;

namespace Stockroom.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    public OrdersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> List(
        [FromQuery] OrderStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _sender.Send(new GetOrdersQuery(status, page, pageSize), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDetailDto>> Get(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetOrderByIdQuery(id), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<OrderDetailDto>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var lines = request.Lines.Select(line => new OrderLineInput(line.ProductId, line.Quantity)).ToList();
        var created = await _sender.Send(new CreateOrderCommand(request.CustomerId, lines, request.Submit), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<OrderDetailDto>> Submit(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new SubmitOrderCommand(id), cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<OrderDetailDto>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new CancelOrderCommand(id), cancellationToken));
    }

    [HttpPost("{id:guid}/fulfill")]
    public async Task<ActionResult<OrderDetailDto>> Fulfill(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new FulfillOrderCommand(id), cancellationToken));
    }
}
