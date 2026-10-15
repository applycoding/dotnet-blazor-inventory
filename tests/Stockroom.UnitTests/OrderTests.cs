using Stockroom.Domain.Common;
using Stockroom.Domain.Entities;
using Stockroom.Domain.Enums;

namespace Stockroom.UnitTests;

public sealed class OrderTests
{
    [Fact]
    public void Submit_requires_a_line()
    {
        var order = Order.Create("SR-2026-0009", Guid.NewGuid(), "Harbor Cafe");

        var exception = Assert.Throws<DomainException>(() => order.Submit());

        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Submit_moves_a_draft_with_a_line_to_submitted()
    {
        var order = SampleOrder();

        order.Submit();

        Assert.Equal(OrderStatus.Submitted, order.Status);
        Assert.NotNull(order.SubmittedAtUtc);
    }

    [Fact]
    public void AddLine_is_rejected_after_submit()
    {
        var order = SampleOrder();
        order.Submit();

        var exception = Assert.Throws<DomainException>(() => order.AddLine(Guid.NewGuid(), "FLT-03", "Filters", 1, 2m));

        Assert.Contains("draft", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cancel_is_allowed_from_submitted()
    {
        var order = SampleOrder();
        order.Submit();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_is_rejected_after_fulfillment()
    {
        var order = SampleOrder();
        order.Submit();
        order.MarkFulfilled();

        var exception = Assert.Throws<DomainException>(() => order.Cancel());

        Assert.Contains("cancelled", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fulfill_requires_a_submitted_order()
    {
        var order = SampleOrder();

        var exception = Assert.Throws<DomainException>(() => order.MarkFulfilled());

        Assert.Contains("submitted", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Total_sums_snapshotted_line_prices()
    {
        var order = Order.Create("SR-2026-0009", Guid.NewGuid(), "Harbor Cafe");
        order.AddLine(Guid.NewGuid(), "KET-01", "Kettle", 2, 10m);
        order.AddLine(Guid.NewGuid(), "FLT-03", "Filters", 4, 2.5m);

        Assert.Equal(30m, order.Total);
    }

    private static Order SampleOrder()
    {
        var order = Order.Create("SR-2026-0009", Guid.NewGuid(), "Harbor Cafe");
        order.AddLine(Guid.NewGuid(), "KET-01", "Kettle", 1, 10m);
        return order;
    }
}
