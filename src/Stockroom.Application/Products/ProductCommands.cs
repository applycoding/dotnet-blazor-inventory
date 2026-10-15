using FluentValidation;

using MediatR;

using Microsoft.EntityFrameworkCore;

using Stockroom.Application.Abstractions;
using Stockroom.Application.Common;
using Stockroom.Contracts.Dtos;
using Stockroom.Domain.Entities;

namespace Stockroom.Application.Products;

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    int QuantityOnHand,
    int ReorderLevel) : IRequest<ProductDto>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Sku)
            .NotEmpty()
            .MaximumLength(32)
            .Matches("^[A-Za-z0-9-]+$")
            .WithMessage("SKU can contain only letters, numbers, and dashes.");
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Description).MaximumLength(500);
        RuleFor(command => command.UnitPrice).GreaterThanOrEqualTo(0).LessThan(1_000_000);
        RuleFor(command => command.QuantityOnHand).GreaterThanOrEqualTo(0);
        RuleFor(command => command.ReorderLevel).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateProductHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IStockroomDbContext _db;

    public CreateProductHandler(IStockroomDbContext db)
    {
        _db = db;
    }

    public async Task<ProductDto> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {
        var sku = Product.NormalizeSku(command.Sku);
        var exists = await _db.Products.AnyAsync(product => product.Sku == sku, cancellationToken);
        if (exists)
        {
            throw new ConflictException($"SKU '{sku}' already exists.");
        }

        var product = Product.Create(sku, command.Name, command.Description, command.UnitPrice, command.QuantityOnHand, command.ReorderLevel);
        _db.Products.Add(product);
        if (product.QuantityOnHand > 0)
        {
            _db.StockMovements.Add(StockMovement.Record(product.Id, product.QuantityOnHand, product.QuantityOnHand, "Opening balance"));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return product.ToDto();
    }
}

public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string? Description,
    decimal UnitPrice,
    int ReorderLevel,
    bool IsActive) : IRequest<ProductDto>;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEqual(Guid.Empty);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(120);
        RuleFor(command => command.Description).MaximumLength(500);
        RuleFor(command => command.UnitPrice).GreaterThanOrEqualTo(0).LessThan(1_000_000);
        RuleFor(command => command.ReorderLevel).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateProductHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IStockroomDbContext _db;

    public UpdateProductHandler(IStockroomDbContext db)
    {
        _db = db;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(item => item.Id == command.ProductId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

        product.UpdateDetails(command.Name, command.Description, command.UnitPrice, command.ReorderLevel, command.IsActive);
        await _db.SaveChangesAsync(cancellationToken);
        return await ProductReader.WithMovementsAsync(_db, product, cancellationToken);
    }
}

public sealed record AdjustStockCommand(Guid ProductId, int Delta, string Reason) : IRequest<ProductDto>;

public sealed class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEqual(Guid.Empty);
        RuleFor(command => command.Delta).NotEqual(0);
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(200);
    }
}

public sealed class AdjustStockHandler : IRequestHandler<AdjustStockCommand, ProductDto>
{
    private readonly IStockroomDbContext _db;

    public AdjustStockHandler(IStockroomDbContext db)
    {
        _db = db;
    }

    public async Task<ProductDto> Handle(AdjustStockCommand command, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FirstOrDefaultAsync(item => item.Id == command.ProductId, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

        product.AdjustStock(command.Delta);
        _db.StockMovements.Add(StockMovement.Record(product.Id, command.Delta, product.QuantityOnHand, command.Reason));
        await _db.SaveChangesAsync(cancellationToken);
        return await ProductReader.WithMovementsAsync(_db, product, cancellationToken);
    }
}

internal static class ProductReader
{
    public static async Task<ProductDto> WithMovementsAsync(
        IStockroomDbContext db,
        Product product,
        CancellationToken cancellationToken)
    {
        var movements = await db.StockMovements
            .AsNoTracking()
            .Where(movement => movement.ProductId == product.Id)
            .OrderByDescending(movement => movement.CreatedAtUtc)
            .Take(8)
            .ToListAsync(cancellationToken);

        return product.ToDto(movements.Select(movement => movement.ToDto()).ToList());
    }
}
