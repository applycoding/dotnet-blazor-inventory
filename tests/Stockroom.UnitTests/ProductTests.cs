using Stockroom.Domain.Common;
using Stockroom.Domain.Entities;

namespace Stockroom.UnitTests;

public sealed class ProductTests
{
    [Fact]
    public void Create_normalizes_the_sku()
    {
        var product = Product.Create("ket-01", "Gooseneck kettle", null, 10m, 2, 1);

        Assert.Equal("KET-01", product.Sku);
        Assert.False(product.IsLowStock);
    }

    [Fact]
    public void Create_rejects_a_negative_price()
    {
        var exception = Assert.Throws<DomainException>(() => Product.Create("KET-01", "Kettle", null, -1m, 0, 0));

        Assert.Contains("price", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AdjustStock_rejects_a_balance_below_zero()
    {
        var product = Product.Create("KET-01", "Kettle", null, 10m, 1, 0);

        var exception = Assert.Throws<DomainException>(() => product.AdjustStock(-2));

        Assert.Equal(1, product.QuantityOnHand);
        Assert.Contains("below zero", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Inactive_products_are_not_low_stock_alerts()
    {
        var product = Product.Create("DRP-02", "Dripper", null, 10m, 1, 5);
        product.UpdateDetails(product.Name, product.Description, product.UnitPrice, product.ReorderLevel, isActive: false);

        Assert.False(product.IsLowStock);
    }
}
