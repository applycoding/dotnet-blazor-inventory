using MediatR;

using Microsoft.AspNetCore.Mvc;

using Stockroom.Application.Dashboard;
using Stockroom.Contracts.Dtos;

namespace Stockroom.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly ISender _sender;

    public DashboardController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(new GetDashboardQuery(), cancellationToken));
    }
}
