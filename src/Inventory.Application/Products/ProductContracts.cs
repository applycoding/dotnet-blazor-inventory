using Inventory.Application.Common;

namespace Inventory.Application.Products;

public sealed record ProductDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    int QuantityOnHand,
    int ReorderLevel,
    bool IsActive,
    bool IsLowStock,
    DateTimeOffset UpdatedAt);

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    int InitialQuantity,
    int ReorderLevel);

public sealed record UpdateProductCommand(
    string Name,
    string? Description,
    decimal UnitPrice,
    int ReorderLevel,
    bool IsActive);

public interface IProductService
{
    Task<PagedResult<ProductDto>> ListAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ProductDto> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<ProductDto> CreateAsync(CreateProductCommand command, CancellationToken cancellationToken);

    Task<ProductDto> UpdateAsync(Guid id, UpdateProductCommand command, CancellationToken cancellationToken);

    Task ArchiveAsync(Guid id, CancellationToken cancellationToken);
}
