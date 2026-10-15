using FluentValidation;
using Inventory.Application.Abstractions;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Products;

namespace Inventory.Application.Stock;

public sealed class StockService : IStockService
{
    private readonly IProductRepository _products;
    private readonly IStockMovementRepository _movements;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;
    private readonly IValidator<RecordMovementCommand> _validator;

    public StockService(
        IProductRepository products,
        IStockMovementRepository movements,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider clock,
        IValidator<RecordMovementCommand> validator)
    {
        _products = products;
        _movements = movements;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _clock = clock;
        _validator = validator;
    }

    public Task<IReadOnlyList<StockMovementDto>> ListRecentAsync(
        Guid? productId,
        int take,
        CancellationToken cancellationToken)
    {
        take = take < 1 ? 50 : Math.Min(take, 200);
        return _movements.ListRecentAsync(productId, take, cancellationToken);
    }

    public async Task<StockMovementDto> RecordAsync(
        RecordMovementCommand command,
        CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);
        var product = await _products.GetByIdAsync(command.ProductId, cancellationToken)
            ?? throw DomainException.NotFound($"Product {command.ProductId} was not found.");
        var movement = StockMovement.Record(
            product,
            command.Type,
            command.Quantity,
            command.Note,
            _currentUser.DisplayName,
            _clock.GetUtcNow());
        _movements.Add(movement);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new StockMovementDto(
            movement.Id,
            movement.ProductId,
            product.Sku,
            product.Name,
            movement.Type,
            movement.Quantity,
            movement.Delta,
            movement.QuantityAfter,
            movement.Note,
            movement.PerformedBy,
            movement.OccurredAt);
    }
}
