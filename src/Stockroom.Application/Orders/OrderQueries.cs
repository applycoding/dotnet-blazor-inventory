using MediatR;

using Microsoft.EntityFrameworkCore;

using Stockroom.Application.Abstractions;
using Stockroom.Application.Common;
using Stockroom.Contracts.Dtos;
using Stockroom.Domain.Enums;

namespace Stockroom.Application.Orders;

public sealed record GetOrdersQuery(OrderStatus? Status, int Page, int PageSize) : IRequest<PagedResult<OrderSummaryDto>>;

public sealed class GetOrdersHandler : IRequestHandler<GetOrdersQuery, PagedResult<OrderSummaryDto>>
{
    private readonly IStockroomDbContext _db;

    public GetOrdersHandler(IStockroomDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<OrderSummaryDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = PageRequest.Normalize(request.Page, request.PageSize);
        var query = _db.Orders.AsNoTracking().Include(order => order.Lines).AsQueryable();
        if (request.Status is not null)
        {
            query = query.Where(order => order.Status == request.Status);
        }

        var total = await query.CountAsync(cancellationToken);
        var orders = await query
            .OrderByDescending(order => order.OrderedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderSummaryDto>(orders.Select(order => order.ToSummary()).ToList(), page, pageSize, total);
    }
}

public sealed record GetOrderByIdQuery(Guid Id) : IRequest<OrderDetailDto>;

public sealed class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailDto>
{
    private readonly IStockroomDbContext _db;

    public GetOrderByIdHandler(IStockroomDbContext db)
    {
        _db = db;
    }

    public async Task<OrderDetailDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(item => item.Lines)
            .Include(item => item.Customer)
            .FirstOrDefaultAsync(item => item.Id == request.Id, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException("Order was not found.");
        }

        return order.ToDetail();
    }
}
