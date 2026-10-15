using Inventory.Domain.Products;

namespace Inventory.Application.Stock;

public sealed record StockMovementDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    StockMovementType Type,
    int Quantity,
    int Delta,
    int QuantityAfter,
    string? Note,
    string PerformedBy,
    DateTimeOffset OccurredAt);

public sealed record RecordMovementCommand(
    Guid ProductId,
    StockMovementType Type,
    int Quantity,
    string? Note);

public interface IStockService
{
    Task<IReadOnlyList<StockMovementDto>> ListRecentAsync(
        Guid? productId,
        int take,
        CancellationToken cancellationToken);

    Task<StockMovementDto> RecordAsync(RecordMovementCommand command, CancellationToken cancellationToken);
}
