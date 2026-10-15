using Stockroom.Domain.Common;

namespace Stockroom.Domain.Entities;

public sealed class StockMovement
{
    private StockMovement()
    {
    }

    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public int Delta { get; private set; }

    public int BalanceAfter { get; private set; }

    public string Reason { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static StockMovement Record(
        Guid productId,
        int delta,
        int balanceAfter,
        string reason,
        DateTimeOffset? createdAtUtc = null)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Stock movement needs a product.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A stock movement needs a reason.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > 200)
        {
            throw new DomainException("Stock movement reason cannot be longer than 200 characters.");
        }

        return new StockMovement
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Delta = delta,
            BalanceAfter = balanceAfter,
            Reason = trimmed,
            CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow
        };
    }
}
