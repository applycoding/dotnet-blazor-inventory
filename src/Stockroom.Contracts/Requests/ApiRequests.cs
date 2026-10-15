namespace Stockroom.Contracts.Requests;

public sealed record LoginRequest(string Email, string Password);

public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    int QuantityOnHand,
    int ReorderLevel);

public sealed record UpdateProductRequest(
    string Name,
    string? Description,
    decimal UnitPrice,
    int ReorderLevel,
    bool IsActive);

public sealed record AdjustStockRequest(int Delta, string Reason);

public sealed record CreateCustomerRequest(string Name, string Email, string? Phone);

public sealed record CreateOrderLineRequest(Guid ProductId, int Quantity);

public sealed record CreateOrderRequest(
    Guid CustomerId,
    IReadOnlyList<CreateOrderLineRequest> Lines,
    bool Submit);
