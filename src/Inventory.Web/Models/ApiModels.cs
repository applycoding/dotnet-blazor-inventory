using System.Globalization;

namespace Inventory.Web.Models;

public enum MovementType
{
    Receipt = 1,
    Issue = 2,
    Adjustment = 3
}

public sealed record ProductModel(
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

public sealed record PagedProducts(
    IReadOnlyList<ProductModel> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record MovementModel(
    Guid Id,
    Guid ProductId,
    string Sku,
    string ProductName,
    MovementType Type,
    int Quantity,
    int Delta,
    int QuantityAfter,
    string? Note,
    string PerformedBy,
    DateTimeOffset OccurredAt);

public sealed record DashboardModel(
    int ActiveProductCount,
    int LowStockCount,
    decimal InventoryValue,
    IReadOnlyList<ProductModel> LowStock);

public sealed record LoginResultModel(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string DisplayName,
    string Email,
    string Role);

public sealed class ProductDraft
{
    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal UnitPrice { get; set; }

    public int InitialQuantity { get; set; }

    public int ReorderLevel { get; set; } = 5;

    public bool IsActive { get; set; } = true;
}

public sealed class MovementDraft
{
    public Guid ProductId { get; set; }

    public MovementType Type { get; set; } = MovementType.Receipt;

    public int Quantity { get; set; } = 1;

    public string? Note { get; set; }
}

public static class CatalogFormat
{
    private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

    public static string Money(decimal value) => value.ToString("C", Us);

    public static string When(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("dd MMM yyyy HH:mm", Us) + " UTC";

    public static string Delta(int delta) => delta > 0 ? $"+{delta}" : delta.ToString(Us);

    public static string MovementLabel(MovementType type) => type switch
    {
        MovementType.Receipt => "Receipt",
        MovementType.Issue => "Issue",
        MovementType.Adjustment => "Count",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown movement type.")
    };
}
