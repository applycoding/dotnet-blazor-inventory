using Inventory.Domain.Exceptions;
using Inventory.Domain.Products;

namespace Inventory.UnitTests;

public class ProductTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 2, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_normalizes_sku_and_rounds_price()
    {
        var product = Product.Create(" ab-1 ", "  Copper elbow ", "  ", 1.239m, 4, Now);

        Assert.Equal("AB-1", product.Sku);
        Assert.Equal("Copper elbow", product.Name);
        Assert.Null(product.Description);
        Assert.Equal(1.24m, product.UnitPrice);
        Assert.Equal(0, product.QuantityOnHand);
        Assert.True(product.IsActive);
        Assert.True(product.IsLowStock);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-BAD")]
    [InlineData("has space")]
    public void Create_rejects_bad_sku(string sku)
    {
        var exception = Assert.Throws<DomainException>(() => Product.Create(sku, "Bolt", null, 1m, 0, Now));
        Assert.Equal(DomainErrorKind.Validation, exception.Kind);
    }

    [Fact]
    public void Create_rejects_negative_price_and_reorder_level()
    {
        Assert.Equal(DomainErrorKind.Validation, Assert.Throws<DomainException>(() => Product.Create("AB-1", "Bolt", null, -1m, 0, Now)).Kind);
        Assert.Equal(DomainErrorKind.Validation, Assert.Throws<DomainException>(() => Product.Create("AB-1", "Bolt", null, 1m, -1, Now)).Kind);
    }

    [Fact]
    public void Receipt_and_issue_change_on_hand()
    {
        var product = Product.Create("AB-1", "Bolt", null, 1m, 1, Now);

        Assert.Equal(5, product.ApplyReceipt(5, Now));
        Assert.Equal(2, product.ApplyIssue(3, Now));
        Assert.False(product.IsLowStock);
    }

    [Fact]
    public void Issue_beyond_on_hand_leaves_quantity_unchanged()
    {
        var product = Product.Create("AB-1", "Bolt", null, 1m, 0, Now);
        product.ApplyReceipt(2, Now);

        var exception = Assert.Throws<DomainException>(() => product.ApplyIssue(3, Now));

        Assert.Equal(DomainErrorKind.InsufficientStock, exception.Kind);
        Assert.Equal(2, product.QuantityOnHand);
    }

    [Fact]
    public void Archived_product_is_not_low_stock()
    {
        var product = Product.Create("AB-1", "Bolt", null, 2m, 5, Now);
        product.UpdateDetails("Bolt", "bin 4", 2.5m, 5, isActive: false, Now);

        Assert.False(product.IsActive);
        Assert.False(product.IsLowStock);
        Assert.Equal("bin 4", product.Description);
        Assert.Equal(2.50m, product.UnitPrice);
    }
}
