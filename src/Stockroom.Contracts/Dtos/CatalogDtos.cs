using Stockroom.Domain.Enums;

namespace Stockroom.Contracts.Dtos;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record StockMovementDto(
    Guid Id,
    int Delta,
    int BalanceAfter,
    string Reason,
    DateTimeOffset CreatedAtUtc);

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
    IReadOnlyList<StockMovementDto> RecentMovements);

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    DateTimeOffset CreatedAtUtc);

public sealed record OrderLineDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    OrderStatus Status,
    DateTimeOffset OrderedAtUtc,
    decimal Total,
    int LineCount);

public sealed record OrderDetailDto(
    Guid Id,
    string OrderNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    OrderStatus Status,
    DateTimeOffset OrderedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? FulfilledAtUtc,
    DateTimeOffset? CancelledAtUtc,
    decimal Total,
    IReadOnlyList<OrderLineDto> Lines);

public sealed record DashboardDto(
    int ActiveProducts,
    int LowStockProducts,
    int OpenOrders,
    decimal FulfilledRevenue,
    IReadOnlyList<ProductDto> LowStock,
    IReadOnlyList<OrderSummaryDto> RecentOrders);

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string DisplayName,
    string Email,
    string Role);
