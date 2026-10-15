using Inventory.Application.Products;

namespace Inventory.Application.Dashboard;

public sealed record DashboardDto(
    int ActiveProductCount,
    int LowStockCount,
    decimal InventoryValue,
    IReadOnlyList<ProductDto> LowStock);

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(CancellationToken cancellationToken);
}
