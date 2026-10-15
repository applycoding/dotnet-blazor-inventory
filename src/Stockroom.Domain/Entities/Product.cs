using Stockroom.Domain.Common;

namespace Stockroom.Domain.Entities;

public sealed class Product
{
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

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public bool IsLowStock => IsActive && QuantityOnHand <= ReorderLevel;

    public static Product Create(
        string sku,
        string name,
        string? description,
        decimal unitPrice,
        int quantityOnHand,
        int reorderLevel,
        DateTimeOffset? createdAtUtc = null)
    {
        if (quantityOnHand < 0)
        {
            throw new DomainException("Quantity on hand cannot be negative.");
        }

        return new Product
        {
            Id = Guid.NewGuid(),
            Sku = NormalizeSku(sku),
            Name = RequireName(name),
            Description = NormalizeDescription(description),
            UnitPrice = RequirePrice(unitPrice),
            QuantityOnHand = quantityOnHand,
            ReorderLevel = RequireReorderLevel(reorderLevel),
            IsActive = true,
            CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow
        };
    }

    public void UpdateDetails(string name, string? description, decimal unitPrice, int reorderLevel, bool isActive)
    {
        Name = RequireName(name);
        Description = NormalizeDescription(description);
        UnitPrice = RequirePrice(unitPrice);
        ReorderLevel = RequireReorderLevel(reorderLevel);
        IsActive = isActive;
    }

    public void AdjustStock(int delta)
    {
        if (delta == 0)
        {
            throw new DomainException("Stock adjustment cannot be zero.");
        }

        var next = (long)QuantityOnHand + delta;
        if (next < 0 || next > int.MaxValue)
        {
            throw new DomainException("Quantity on hand cannot go below zero.");
        }

        QuantityOnHand = (int)next;
    }

    public static string NormalizeSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("SKU is required.");
        }

        var normalized = sku.Trim().ToUpperInvariant();
        if (normalized.Length > 32)
        {
            throw new DomainException("SKU cannot be longer than 32 characters.");
        }

        foreach (var character in normalized)
        {
            var allowed = char.IsAsciiLetterOrDigit(character) || character == '-';
            if (!allowed)
            {
                throw new DomainException("SKU can contain only letters, numbers, and dashes.");
            }
        }

        return normalized;
    }

    private static string RequireName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Name is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > 120)
        {
            throw new DomainException("Name cannot be longer than 120 characters.");
        }

        return trimmed;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        if (trimmed.Length > 500)
        {
            throw new DomainException("Description cannot be longer than 500 characters.");
        }

        return trimmed;
    }

    private static decimal RequirePrice(decimal unitPrice)
    {
        if (unitPrice < 0)
        {
            throw new DomainException("Unit price cannot be negative.");
        }

        if (unitPrice >= 1_000_000m)
        {
            throw new DomainException("Unit price must be less than 1000000.");
        }

        return decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero);
    }

    private static int RequireReorderLevel(int reorderLevel)
    {
        if (reorderLevel < 0)
        {
            throw new DomainException("Reorder level cannot be negative.");
        }

        return reorderLevel;
    }
}
