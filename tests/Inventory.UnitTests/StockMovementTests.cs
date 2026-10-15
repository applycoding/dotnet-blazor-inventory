using Inventory.Domain.Exceptions;
using Inventory.Domain.Products;

namespace Inventory.UnitTests;

public class StockMovementTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 2, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Receipt_records_a_positive_delta()
    {
        var product = Product.Create("NL-1", "Tape", null, 3m, 1, Now);
        var movement = StockMovement.Record(product, StockMovementType.Receipt, 4, " opening ", "Avery", Now);

        Assert.Equal(4, movement.Quantity);
        Assert.Equal(4, movement.Delta);
        Assert.Equal(4, movement.QuantityAfter);
        Assert.Equal("opening", movement.Note);
        Assert.Equal(4, product.QuantityOnHand);
    }

    [Fact]
    public void Issue_records_a_negative_delta()
    {
        var product = Product.Create("NL-1", "Tape", null, 3m, 1, Now);
        StockMovement.Record(product, StockMovementType.Receipt, 5, null, "Avery", Now);

        var movement = StockMovement.Record(product, StockMovementType.Issue, 2, null, "Avery", Now);

        Assert.Equal(-2, movement.Delta);
        Assert.Equal(3, movement.QuantityAfter);
    }

    [Fact]
    public void Adjustment_sets_the_counted_quantity()
    {
        var product = Product.Create("NL-1", "Tape", null, 3m, 1, Now);
        StockMovement.Record(product, StockMovementType.Receipt, 12, null, "Avery", Now);

        var movement = StockMovement.Record(product, StockMovementType.Adjustment, 10, "cycle count", "Avery", Now);

        Assert.Equal(StockMovementType.Adjustment, movement.Type);
        Assert.Equal(10, movement.Quantity);
        Assert.Equal(-2, movement.Delta);
        Assert.Equal(10, product.QuantityOnHand);
    }

    [Fact]
    public void Adjustment_with_no_change_does_not_touch_stock()
    {
        var product = Product.Create("NL-1", "Tape", null, 3m, 1, Now);
        StockMovement.Record(product, StockMovementType.Receipt, 4, null, "Avery", Now);
        var version = product.Version;

        var exception = Assert.Throws<DomainException>(() =>
            StockMovement.Record(product, StockMovementType.Adjustment, 4, null, "Avery", Now));

        Assert.Equal(DomainErrorKind.Validation, exception.Kind);
        Assert.Equal(4, product.QuantityOnHand);
        Assert.Equal(version, product.Version);
    }

    [Fact]
    public void Archived_product_cannot_be_moved()
    {
        var product = Product.Create("NL-1", "Tape", null, 3m, 1, Now);
        product.UpdateDetails(product.Name, null, product.UnitPrice, product.ReorderLevel, false, Now);

        var exception = Assert.Throws<DomainException>(() =>
            StockMovement.Record(product, StockMovementType.Receipt, 1, null, "Avery", Now));

        Assert.Equal(0, product.QuantityOnHand);
        Assert.Contains("Reactivate", exception.Message, StringComparison.Ordinal);
    }
}
