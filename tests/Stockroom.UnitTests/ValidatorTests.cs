using Stockroom.Application.Orders;
using Stockroom.Application.Products;

namespace Stockroom.UnitTests;

public sealed class ValidatorTests
{
    [Fact]
    public void CreateProduct_rejects_a_blank_sku()
    {
        var validator = new CreateProductCommandValidator();

        var result = validator.Validate(new CreateProductCommand(" ", "Kettle", null, 10m, 1, 1));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Sku");
    }

    [Fact]
    public void CreateOrder_rejects_an_empty_line_list()
    {
        var validator = new CreateOrderCommandValidator();

        var result = validator.Validate(new CreateOrderCommand(Guid.NewGuid(), [], false));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == "Lines");
    }
}
