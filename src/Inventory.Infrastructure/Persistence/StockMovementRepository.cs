using Inventory.Application.Abstractions;
using Inventory.Application.Stock;
using Inventory.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class StockMovementRepository : IStockMovementRepository
{
    private readonly AppDbContext _db;

    public StockMovementRepository(AppDbContext db)
    {
        _db = db;
    }

    public void Add(StockMovement movement) => _db.StockMovements.Add(movement);

    public async Task<IReadOnlyList<StockMovementDto>> ListRecentAsync(
        Guid? productId,
        int take,
        CancellationToken cancellationToken)
    {
        var query =
            from movement in _db.StockMovements.AsNoTracking()
            join product in _db.Products.AsNoTracking() on movement.ProductId equals product.Id
            select new { movement, product };

        if (productId is Guid id)
        {
            query = query.Where(x => x.movement.ProductId == id);
        }

        var rows = await query
            .OrderByDescending(x => x.movement.OccurredAt)
            .ThenByDescending(x => x.movement.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new StockMovementDto(
            x.movement.Id,
            x.movement.ProductId,
            x.product.Sku,
            x.product.Name,
            x.movement.Type,
            x.movement.Quantity,
            x.movement.Delta,
            x.movement.QuantityAfter,
            x.movement.Note,
            x.movement.PerformedBy,
            x.movement.OccurredAt)).ToList();
    }
}
