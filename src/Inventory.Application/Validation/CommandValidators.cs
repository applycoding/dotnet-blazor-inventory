using FluentValidation;
using Inventory.Application.Auth;
using Inventory.Application.Products;
using Inventory.Application.Stock;
using Inventory.Domain.Products;

namespace Inventory.Application.Validation;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(Product.SkuMaxLength)
            .Matches(Product.SkuPattern)
            .WithMessage("SKU must start with a letter or number and contain only letters, numbers, and hyphens.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Product.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(Product.DescriptionMaxLength);
        RuleFor(x => x.UnitPrice).InclusiveBetween(0, 1_000_000m);
        RuleFor(x => x.InitialQuantity).InclusiveBetween(0, 1_000_000);
        RuleFor(x => x.ReorderLevel).InclusiveBetween(0, 1_000_000);
    }
}

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Product.NameMaxLength);
        RuleFor(x => x.Description).MaximumLength(Product.DescriptionMaxLength);
        RuleFor(x => x.UnitPrice).InclusiveBetween(0, 1_000_000m);
        RuleFor(x => x.ReorderLevel).InclusiveBetween(0, 1_000_000);
    }
}

public sealed class RecordMovementCommandValidator : AbstractValidator<RecordMovementCommand>
{
    public RecordMovementCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Quantity).GreaterThan(0)
            .When(x => x.Type is StockMovementType.Receipt or StockMovementType.Issue)
            .WithMessage("Quantity must be greater than zero.");
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0)
            .When(x => x.Type == StockMovementType.Adjustment)
            .WithMessage("Counted quantity cannot be negative.");
        RuleFor(x => x.Note).MaximumLength(StockMovement.NoteMaxLength);
    }
}

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}
