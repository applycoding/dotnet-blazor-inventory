using Inventory.Domain.Exceptions;

namespace Inventory.Domain.Products;

public sealed class StockMovement
{
    public const int NoteMaxLength = 240;
    public const int PerformedByMaxLength = 120;

    private StockMovement()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public StockMovementType Type { get; private set; }

    public int Quantity { get; private set; }

    public int Delta { get; private set; }

    public int QuantityAfter { get; private set; }

    public string? Note { get; private set; }

    public string PerformedBy { get; private set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; private set; }

    public static StockMovement Record(
        Product product,
        StockMovementType type,
        int quantity,
        string? note,
        string performedBy,
        DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (!product.IsActive)
        {
            throw DomainException.Validation("Archived products cannot be moved. Reactivate the product first.");
        }

        if (string.IsNullOrWhiteSpace(performedBy) || performedBy.Trim().Length > PerformedByMaxLength)
        {
            throw DomainException.Validation("A name is required on the movement.");
        }

        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (normalizedNote is { Length: > NoteMaxLength })
        {
            throw DomainException.Validation($"Note must be {NoteMaxLength} characters or fewer.");
        }

        var before = product.QuantityOnHand;
        switch (type)
        {
            case StockMovementType.Receipt:
                if (quantity <= 0)
                {
                    throw DomainException.Validation("Receipt quantity must be greater than zero.");
                }

                break;
            case StockMovementType.Issue:
                if (quantity <= 0)
                {
                    throw DomainException.Validation("Issue quantity must be greater than zero.");
                }

                if (quantity > before)
                {
                    throw DomainException.InsufficientStock(
                        $"Cannot issue {quantity}. Only {before} on hand for {product.Sku}.");
                }

                break;
            case StockMovementType.Adjustment:
                if (quantity < 0)
                {
                    throw DomainException.Validation("Counted quantity cannot be negative.");
                }

                if (quantity == before)
                {
                    throw DomainException.Validation("Adjustment does not change the quantity on hand.");
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown movement type.");
        }

        var after = type switch
        {
            StockMovementType.Receipt => product.ApplyReceipt(quantity, occurredAt),
            StockMovementType.Issue => product.ApplyIssue(quantity, occurredAt),
            StockMovementType.Adjustment => product.ApplyAdjustment(quantity, occurredAt),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown movement type.")
        };

        return new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Type = type,
            Quantity = quantity,
            Delta = after - before,
            QuantityAfter = after,
            Note = normalizedNote,
            PerformedBy = performedBy.Trim(),
            OccurredAt = occurredAt
        };
    }
}
