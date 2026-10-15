using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Stockroom.Application.Abstractions;
using Stockroom.Application.Common;
using Stockroom.Contracts.Dtos;
using Stockroom.Domain.Common;
using Stockroom.Domain.Entities;

namespace Stockroom.Application.Orders;

public sealed record OrderLineInput(Guid ProductId, int Quantity);

public sealed record CreateOrderCommand(Guid CustomerId, IReadOnlyList<OrderLineInput> Lines, bool Submit) : IRequest<OrderDetailDto>;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.CustomerId).NotEqual(Guid.Empty);
        RuleFor(command => command.Lines).NotEmpty();
        RuleForEach(command => command.Lines).ChildRules(line =>
        {
            line.RuleFor(item => item.ProductId).NotEqual(Guid.Empty);
            line.RuleFor(item => item.Quantity).GreaterThan(0).LessThanOrEqualTo(10_000);
        });
    }
}

public sealed class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderDetailDto>
{
    private readonly IStockroomDbContext _db;

    public CreateOrderHandler(IStockroomDbContext db)
    {
        _db = db;
    }

    public async Task<OrderDetailDto> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(item => item.Id == command.CustomerId, cancellationToken);
        if (customer is null)
        {
            throw new NotFoundException("Customer was not found.");
        }

        var productIds = command.Lines.Select(line => line.ProductId).Distinct().ToList();
        if (productIds.Count != command.Lines.Count)
        {
            throw new DomainException("Each product can appear only once on an order.");
        }

        var products = await _db.Products
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync(cancellationToken);
        if (products.Count != productIds.Count)
        {
            throw new NotFoundException("A selected product was not found.");
        }

        var sequence = await _db.Orders.CountAsync(cancellationToken) + 1;
        var order = Order.Create(OrderNumber.Format(sequence), customer.Id, customer.Name);
        foreach (var line in command.Lines)
        {
            var product = products.First(item => item.Id == line.ProductId);
            if (!product.IsActive)
            {
                throw new DomainException($"{product.Name} is inactive and cannot be ordered.");
            }

            order.AddLine(product.Id, product.Sku, product.Name, line.Quantity, product.UnitPrice);
        }

        if (command.Submit)
        {
            order.Submit();
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);
        return order.ToDetail(customer.Email);
    }
}

public sealed record SubmitOrderCommand(Guid OrderId) : IRequest<OrderDetailDto>;

public sealed class SubmitOrderHandler : IRequestHandler<SubmitOrderCommand, OrderDetailDto>
{
    private readonly IStockroomDbContext _db;

    public SubmitOrderHandler(IStockroomDbContext db)
    {
        _db = db;
    }

    public async Task<OrderDetailDto> Handle(SubmitOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await LoadAsync(_db, command.OrderId, cancellationToken);
        order.Submit();
        await _db.SaveChangesAsync(cancellationToken);
        return order.ToDetail();
    }

    internal static async Task<Order> LoadAsync(IStockroomDbContext db, Guid orderId, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .Include(item => item.Lines)
            .Include(item => item.Customer)
            .FirstOrDefaultAsync(item => item.Id == orderId, cancellationToken);
        if (order is null)
        {
            throw new NotFoundException("Order was not found.");
        }

        return order;
    }
}

public sealed record CancelOrderCommand(Guid OrderId) : IRequest<OrderDetailDto>;

public sealed class CancelOrderHandler : IRequestHandler<CancelOrderCommand, OrderDetailDto>
{
    private readonly IStockroomDbContext _db;

    public CancelOrderHandler(IStockroomDbContext db)
    {
        _db = db;
    }

    public async Task<OrderDetailDto> Handle(CancelOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await SubmitOrderHandler.LoadAsync(_db, command.OrderId, cancellationToken);
        order.Cancel();
        await _db.SaveChangesAsync(cancellationToken);
        return order.ToDetail();
    }
}

public sealed record FulfillOrderCommand(Guid OrderId) : IRequest<OrderDetailDto>;

public sealed class FulfillOrderHandler : IRequestHandler<FulfillOrderCommand, OrderDetailDto>
{
    private readonly IStockroomDbContext _db;

    public FulfillOrderHandler(IStockroomDbContext db)
    {
        _db = db;
    }

    public async Task<OrderDetailDto> Handle(FulfillOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await SubmitOrderHandler.LoadAsync(_db, command.OrderId, cancellationToken);
        var productIds = order.Lines.Select(line => line.ProductId).ToList();
        var products = await _db.Products
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync(cancellationToken);
        if (products.Count != productIds.Distinct().Count())
        {
            throw new NotFoundException("A product on this order no longer exists.");
        }

        foreach (var line in order.Lines)
        {
            var product = products.First(item => item.Id == line.ProductId);
            if (product.QuantityOnHand < line.Quantity)
            {
                throw new DomainException($"{product.Name} has {product.QuantityOnHand} on hand, which is short of the {line.Quantity} ordered.");
            }
        }

        order.MarkFulfilled();
        foreach (var line in order.Lines)
        {
            var product = products.First(item => item.Id == line.ProductId);
            product.AdjustStock(-line.Quantity);
            _db.StockMovements.Add(StockMovement.Record(
                product.Id,
                -line.Quantity,
                product.QuantityOnHand,
                $"Fulfilled {order.OrderNumber}"));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return order.ToDetail();
    }
}
