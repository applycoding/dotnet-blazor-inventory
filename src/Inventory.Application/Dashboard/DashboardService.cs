using Inventory.Application.Abstractions;
using Inventory.Application.Products;
using Inventory.Domain.Products;

namespace Inventory.Application.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private readonly IProductRepository _products;

    public DashboardService(IProductRepository products)
    {
        _products = products;
    }

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var active = await _products.CountActiveAsync(cancellationToken);
        var low = await _products.CountLowStockAsync(cancellationToken);
        var value = await _products.SumInventoryValueAsync(cancellationToken);
        var items = await _products.ListLowStockAsync(8, cancellationToken);
        return new DashboardDto(active, low, value, items.Select(ToDto).ToList());
    }

    private static ProductDto ToDto(Product product) =>
        new(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.UnitPrice,
            product.QuantityOnHand,
            product.ReorderLevel,
            product.IsActive,
            product.IsLowStock,
            product.UpdatedAt);
}
