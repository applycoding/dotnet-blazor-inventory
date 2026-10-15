using FluentValidation;
using Inventory.Application.Abstractions;
using Inventory.Application.Common;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Products;

namespace Inventory.Application.Products;

public sealed class ProductService : IProductService
{
    private readonly IProductRepository _products;
    private readonly IStockMovementRepository _movements;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;
    private readonly IValidator<CreateProductCommand> _createValidator;
    private readonly IValidator<UpdateProductCommand> _updateValidator;

    public ProductService(
        IProductRepository products,
        IStockMovementRepository movements,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider clock,
        IValidator<CreateProductCommand> createValidator,
        IValidator<UpdateProductCommand> updateValidator)
    {
        _products = products;
        _movements = movements;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<ProductDto>> ListAsync(
        string? search,
        bool includeArchived,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);
        var result = await _products.ListAsync(search, includeArchived, page, pageSize, cancellationToken);
        return new PagedResult<ProductDto>(
            result.Items.Select(ToDto).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<ProductDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await RequireAsync(id, cancellationToken);
        return ToDto(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);
        var now = _clock.GetUtcNow();
        var sku = command.Sku.Trim().ToUpperInvariant();
        var existing = await _products.GetBySkuAsync(sku, cancellationToken);
        if (existing is not null)
        {
            throw DomainException.Conflict($"SKU {sku} already exists.");
        }

        var product = Product.Create(
            command.Sku,
            command.Name,
            command.Description,
            command.UnitPrice,
            command.ReorderLevel,
            now);
        _products.Add(product);

        if (command.InitialQuantity > 0)
        {
            var movement = StockMovement.Record(
                product,
                StockMovementType.Receipt,
                command.InitialQuantity,
                "Opening balance",
                _currentUser.DisplayName,
                now);
            _movements.Add(movement);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(product);
    }

    public async Task<ProductDto> UpdateAsync(
        Guid id,
        UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);
        var product = await RequireAsync(id, cancellationToken);
        product.UpdateDetails(
            command.Name,
            command.Description,
            command.UnitPrice,
            command.ReorderLevel,
            command.IsActive,
            _clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ToDto(product);
    }

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await RequireAsync(id, cancellationToken);
        product.UpdateDetails(
            product.Name,
            product.Description,
            product.UnitPrice,
            product.ReorderLevel,
            isActive: false,
            _clock.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Product> RequireAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken);
        return product ?? throw DomainException.NotFound($"Product {id} was not found.");
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
