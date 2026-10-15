using Stockroom.Domain.Common;
using Stockroom.Domain.Enums;

namespace Stockroom.Domain.Entities;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    private Order()
    {
    }

    public Guid Id { get; private set; }

    public string OrderNumber { get; private set; } = string.Empty;

    public Guid CustomerId { get; private set; }

    public Customer? Customer { get; private set; }

    public string CustomerName { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public DateTimeOffset OrderedAtUtc { get; private set; }

    public DateTimeOffset? SubmittedAtUtc { get; private set; }

    public DateTimeOffset? FulfilledAtUtc { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines;

    public decimal Total => _lines.Sum(line => line.LineTotal);

    public static Order Create(string orderNumber, Guid customerId, string customerName, DateTimeOffset? orderedAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new DomainException("Order number is required.");
        }

        if (customerId == Guid.Empty)
        {
            throw new DomainException("Order needs a customer.");
        }

        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new DomainException("Customer name is required.");
        }

        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber.Trim(),
            CustomerId = customerId,
            CustomerName = customerName.Trim(),
            Status = OrderStatus.Draft,
            OrderedAtUtc = orderedAtUtc ?? DateTimeOffset.UtcNow
        };
    }

    public void AddLine(Guid productId, string sku, string productName, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Draft)
        {
            throw new DomainException("Only draft orders can be changed.");
        }

        if (_lines.Any(line => line.ProductId == productId))
        {
            throw new DomainException("Each product can appear only once on an order.");
        }

        _lines.Add(OrderLine.Create(productId, sku, productName, quantity, unitPrice));
    }

    public void Submit(DateTimeOffset? atUtc = null)
    {
        if (Status != OrderStatus.Draft)
        {
            throw new DomainException("Only draft orders can be submitted.");
        }

        if (_lines.Count == 0)
        {
            throw new DomainException("Cannot submit an empty order.");
        }

        Status = OrderStatus.Submitted;
        SubmittedAtUtc = atUtc ?? DateTimeOffset.UtcNow;
    }

    public void Cancel(DateTimeOffset? atUtc = null)
    {
        if (Status is not (OrderStatus.Draft or OrderStatus.Submitted))
        {
            throw new DomainException("Only draft or submitted orders can be cancelled.");
        }

        Status = OrderStatus.Cancelled;
        CancelledAtUtc = atUtc ?? DateTimeOffset.UtcNow;
    }

    public void MarkFulfilled(DateTimeOffset? atUtc = null)
    {
        if (Status != OrderStatus.Submitted)
        {
            throw new DomainException("Only submitted orders can be fulfilled.");
        }

        Status = OrderStatus.Fulfilled;
        FulfilledAtUtc = atUtc ?? DateTimeOffset.UtcNow;
    }
}
