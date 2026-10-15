using Inventory.Application.Auth;
using Inventory.Application.Common;
using Inventory.Application.Stock;
using Inventory.Domain.Identity;
using Inventory.Domain.Products;

namespace Inventory.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken);

    Task<PagedResult<Product>> ListAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<int> CountActiveAsync(CancellationToken cancellationToken);

    Task<int> CountLowStockAsync(CancellationToken cancellationToken);

    Task<decimal> SumInventoryValueAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> ListLowStockAsync(int take, CancellationToken cancellationToken);

    void Add(Product product);
}

public interface IStockMovementRepository
{
    void Add(StockMovement movement);

    Task<IReadOnlyList<StockMovementDto>> ListRecentAsync(
        Guid? productId,
        int take,
        CancellationToken cancellationToken);
}

public interface IUserRepository
{
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string password);
}

public interface IJwtTokenIssuer
{
    IssuedToken Issue(AppUser user, DateTimeOffset now);
}

public interface ICurrentUser
{
    string Email { get; }

    string DisplayName { get; }
}
