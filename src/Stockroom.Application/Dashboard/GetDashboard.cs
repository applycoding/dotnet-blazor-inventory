using MediatR;

using Microsoft.EntityFrameworkCore;

using Stockroom.Application.Abstractions;
using Stockroom.Application.Common;
using Stockroom.Contracts.Dtos;
using Stockroom.Domain.Enums;

namespace Stockroom.Application.Dashboard;

public sealed record GetDashboardQuery : IRequest<DashboardDto>;

public sealed class GetDashboardHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IStockroomDbContext _db;

    public GetDashboardHandler(IStockroomDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var activeProducts = await _db.Products.CountAsync(product => product.IsActive, cancellationToken);
        var lowStockQuery = _db.Products.AsNoTracking().Where(product => product.IsActive && product.QuantityOnHand <= product.ReorderLevel);
        var lowStockCount = await lowStockQuery.CountAsync(cancellationToken);
        var lowStock = await lowStockQuery
            .OrderBy(product => product.QuantityOnHand)
            .ThenBy(product => product.Name)
            .Take(5)
            .ToListAsync(cancellationToken);

        var openOrders = await _db.Orders.CountAsync(
            order => order.Status == OrderStatus.Draft || order.Status == OrderStatus.Submitted,
            cancellationToken);

        // SQLite's EF provider cannot SUM decimals, so the handful of fulfilled lines are totaled in memory.
        var fulfilledLines = await _db.OrderLines
            .AsNoTracking()
            .Where(line => _db.Orders.Any(order => order.Id == line.OrderId && order.Status == OrderStatus.Fulfilled))
            .Select(line => new { line.UnitPrice, line.Quantity })
            .ToListAsync(cancellationToken);
        var revenue = fulfilledLines.Sum(line => line.UnitPrice * line.Quantity);

        var recent = await _db.Orders
            .AsNoTracking()
            .Include(order => order.Lines)
            .OrderByDescending(order => order.OrderedAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);

        return new DashboardDto(
            activeProducts,
            lowStockCount,
            openOrders,
            decimal.Round(revenue, 2, MidpointRounding.AwayFromZero),
            lowStock.Select(product => product.ToDto()).ToList(),
            recent.Select(order => order.ToSummary()).ToList());
    }
}
