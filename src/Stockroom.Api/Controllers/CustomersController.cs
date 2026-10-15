using MediatR;

using Microsoft.AspNetCore.Mvc;

using Stockroom.Application.Customers;
using Stockroom.Contracts.Dtos;
using Stockroom.Contracts.Requests;

namespace Stockroom.Api.Controllers;

[ApiController]
[Route("api/customers")]
public sealed class CustomersController : ControllerBase
{
    private readonly ISender _sender;

    public CustomersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> List([FromQuery] string? search, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetCustomersQuery(search), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var created = await _sender.Send(new CreateCustomerCommand(request.Name, request.Email, request.Phone), cancellationToken);
        return Created($"/api/customers/{created.Id}", created);
    }
}
