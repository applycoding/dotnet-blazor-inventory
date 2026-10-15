using System.Text.RegularExpressions;
using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Products;

public sealed class Product
{
    public const int SkuMaxLength = 32;
    public const int NameMaxLength = 120;
    public const int DescriptionMaxLength = 500;
    public const string SkuPattern = "^[A-Za-z0-9][A-Za-z0-9-]{0,31}$";

    private Product()
    {
    }

    public Guid Id { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public decimal UnitPrice { get; private set; }

    public int QuantityOnHand { get; private set; }

    public int ReorderLevel { get; private set; }

    public bool IsActive { get; private set; }

    public Guid Version { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsLowStock => IsActive && QuantityOnHand <= ReorderLevel;

    public static Product Create(
        string sku,
        string name,
        string? description,
        decimal unitPrice,
        int reorderLevel,
        DateTimeOffset now)
    {
        var normalizedSku = ValidateSku(sku);
        var normalizedName = ValidateName(name);
        var normalizedDescription = ValidateDescription(description);
        var normalizedPrice = ValidatePrice(unitPrice);
        ValidateReorderLevel(reorderLevel);

        return new Product
        {
            Id = Guid.NewGuid(),
            Sku = normalizedSku,
            Name = normalizedName,
            Description = normalizedDescription,
            UnitPrice = normalizedPrice,
            QuantityOnHand = 0,
            ReorderLevel = reorderLevel,
            IsActive = true,
            Version = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDetails(
        string name,
        string? description,
        decimal unitPrice,
        int reorderLevel,
        bool isActive,
        DateTimeOffset now)
    {
        Name = ValidateName(name);
        Description = ValidateDescription(description);
        UnitPrice = ValidatePrice(unitPrice);
        ValidateReorderLevel(reorderLevel);
        ReorderLevel = reorderLevel;
        IsActive = isActive;
        Touch(now);
    }

    public int ApplyReceipt(int quantity, DateTimeOffset now)
    {
        if (quantity <= 0)
        {
            throw DomainException.Validation("Receipt quantity must be greater than zero.");
        }

        if (QuantityOnHand > int.MaxValue - quantity)
        {
            throw DomainException.Validation("Quantity on hand would overflow.");
        }

        QuantityOnHand += quantity;
        Touch(now);
        return QuantityOnHand;
    }

    public int ApplyIssue(int quantity, DateTimeOffset now)
    {
        if (quantity <= 0)
        {
            throw DomainException.Validation("Issue quantity must be greater than zero.");
        }

        if (quantity > QuantityOnHand)
        {
            throw DomainException.InsufficientStock(
                $"Cannot issue {quantity}. Only {QuantityOnHand} on hand for {Sku}.");
        }

        QuantityOnHand -= quantity;
        Touch(now);
        return QuantityOnHand;
    }

    public int ApplyAdjustment(int countedQuantity, DateTimeOffset now)
    {
        if (countedQuantity < 0)
        {
            throw DomainException.Validation("Counted quantity cannot be negative.");
        }

        QuantityOnHand = countedQuantity;
        Touch(now);
        return QuantityOnHand;
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
        Version = Guid.NewGuid();
    }

    private static string ValidateSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku) || !Regex.IsMatch(sku.Trim(), SkuPattern))
        {
            throw DomainException.Validation(
                "SKU must start with a letter or number and contain only letters, numbers, and hyphens.");
        }

        return sku.Trim().ToUpperInvariant();
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > NameMaxLength)
        {
            throw DomainException.Validation($"Name is required and must be {NameMaxLength} characters or fewer.");
        }

        return name.Trim();
    }

    private static string? ValidateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        if (trimmed.Length > DescriptionMaxLength)
        {
            throw DomainException.Validation($"Description must be {DescriptionMaxLength} characters or fewer.");
        }

        return trimmed;
    }

    private static decimal ValidatePrice(decimal unitPrice)
    {
        if (unitPrice < 0 || unitPrice > 1_000_000m)
        {
            throw DomainException.Validation("Unit price must be between 0 and 1,000,000.");
        }

        return decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
    }

    private static void ValidateReorderLevel(int reorderLevel)
    {
        if (reorderLevel < 0)
        {
            throw DomainException.Validation("Reorder level cannot be negative.");
        }
    }
}
