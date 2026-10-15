using Inventory.Application.Abstractions;
using Inventory.Application.Common;
using Inventory.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence;

public sealed class ProductRepository : IProductRepository
{
    private readonly AppDbContext _db;

    public ProductRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        return _db.Products.FirstOrDefaultAsync(x => x.Sku == normalized, cancellationToken);
    }

    public async Task<PagedResult<Product>> ListAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = _db.Products.AsNoTracking();
        if (!includeArchived)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var sku = term.ToUpperInvariant();
            query = query.Where(x => x.Sku.Contains(sku) || EF.Functions.Like(x.Name, $"%{term}%"));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Sku)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<Product>(items, page, pageSize, total);
    }

    public Task<int> CountActiveAsync(CancellationToken cancellationToken) =>
        _db.Products.CountAsync(x => x.IsActive, cancellationToken);

    public Task<int> CountLowStockAsync(CancellationToken cancellationToken) =>
        _db.Products.CountAsync(x => x.IsActive && x.QuantityOnHand <= x.ReorderLevel, cancellationToken);

    public async Task<decimal> SumInventoryValueAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.Products
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new { x.UnitPrice, x.QuantityOnHand })
            .ToListAsync(cancellationToken);
        return rows.Sum(x => x.UnitPrice * x.QuantityOnHand);
    }

    public async Task<IReadOnlyList<Product>> ListLowStockAsync(int take, CancellationToken cancellationToken) =>
        await _db.Products
            .AsNoTracking()
            .Where(x => x.IsActive && x.QuantityOnHand <= x.ReorderLevel)
            .OrderBy(x => x.QuantityOnHand)
            .ThenBy(x => x.Sku)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void Add(Product product) => _db.Products.Add(product);
}
