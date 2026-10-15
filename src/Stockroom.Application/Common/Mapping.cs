using Stockroom.Contracts.Dtos;
using Stockroom.Domain.Entities;

namespace Stockroom.Application.Common;

internal static class Mapping
{
    public static ProductDto ToDto(this Product product, IReadOnlyList<StockMovementDto>? movements = null)
    {
        return new ProductDto(
            product.Id,
            product.Sku,
            product.Name,
            product.Description,
            product.UnitPrice,
            product.QuantityOnHand,
            product.ReorderLevel,
            product.IsActive,
            product.IsLowStock,
            movements ?? []);
    }

    public static StockMovementDto ToDto(this StockMovement movement)
    {
        return new StockMovementDto(
            movement.Id,
            movement.Delta,
            movement.BalanceAfter,
            movement.Reason,
            movement.CreatedAtUtc);
    }

    public static CustomerDto ToDto(this Customer customer)
    {
        return new CustomerDto(customer.Id, customer.Name, customer.Email, customer.Phone, customer.CreatedAtUtc);
    }

    public static OrderSummaryDto ToSummary(this Order order)
    {
        return new OrderSummaryDto(
            order.Id,
            order.OrderNumber,
            order.CustomerName,
            order.Status,
            order.OrderedAtUtc,
            order.Total,
            order.Lines.Count);
    }

    public static OrderDetailDto ToDetail(this Order order, string? customerEmail = null)
    {
        var lines = order.Lines
            .Select(line => new OrderLineDto(
                line.Id,
                line.ProductId,
                line.Sku,
                line.ProductName,
                line.Quantity,
                line.UnitPrice,
                line.LineTotal))
            .ToList();

        return new OrderDetailDto(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.CustomerName,
            customerEmail ?? order.Customer?.Email ?? string.Empty,
            order.Status,
            order.OrderedAtUtc,
            order.SubmittedAtUtc,
            order.FulfilledAtUtc,
            order.CancelledAtUtc,
            order.Total,
            lines);
    }
}
