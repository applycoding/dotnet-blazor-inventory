using Stockroom.Domain.Common;

namespace Stockroom.Domain.Entities;

public sealed class OrderLine
{
    private OrderLine()
    {
    }

    public Guid Id { get; private set; }

    public Guid OrderId { get; private set; }

    public Guid ProductId { get; private set; }

    public string Sku { get; private set; } = string.Empty;

    public string ProductName { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => decimal.Round(UnitPrice * Quantity, 2, MidpointRounding.AwayFromZero);

    public static OrderLine Create(Guid productId, string sku, string productName, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Order line needs a product.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Quantity must be positive.");
        }

        if (unitPrice < 0)
        {
            throw new DomainException("Unit price cannot be negative.");
        }

        return new OrderLine
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Sku = sku.Trim().ToUpperInvariant(),
            ProductName = productName.Trim(),
            Quantity = quantity,
            UnitPrice = decimal.Round(unitPrice, 2, MidpointRounding.AwayFromZero)
        };
    }
}
