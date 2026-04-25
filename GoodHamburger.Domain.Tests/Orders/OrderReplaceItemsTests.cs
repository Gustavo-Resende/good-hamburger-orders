using Ardalis.Result;
using FluentAssertions;
using GoodHamburger.Domain.Enums;
using GoodHamburger.Domain.Menu;
using GoodHamburger.Domain.Orders;

namespace GoodHamburger.Domain.Tests.Orders;

public class OrderReplaceItemsTests
{
    private static MenuItem Sandwich() => new(Guid.NewGuid(), "X-Burger", 5.00m, ItemCategory.Sandwich);
    private static MenuItem Side()     => new(Guid.NewGuid(), "Batata Frita", 2.00m, ItemCategory.Side);
    private static MenuItem Drink()    => new(Guid.NewGuid(), "Refrigerante", 2.50m, ItemCategory.Drink);

    [Fact]
    public void ReplaceItems_RemovesOldItemsAndAddsNew()
    {
        var order = Order.Create([Sandwich()]).Value;

        var result = order.ReplaceItems([Drink()]);

        result.IsSuccess.Should().BeTrue();
        order.Items.Should().HaveCount(1);
        order.Items.Single().Category.Should().Be(ItemCategory.Drink);
    }

    [Fact]
    public void ReplaceItems_WithDuplicateCategory_ReturnsInvalid()
    {
        var order = Order.Create([Sandwich()]).Value;

        var result = order.ReplaceItems([Sandwich(), Sandwich()]);

        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public void ReplaceItems_RecalculatesDiscount()
    {
        var order = Order.Create([Sandwich()]).Value;

        order.ReplaceItems([Sandwich(), Side(), Drink()]);

        order.Subtotal.Should().Be(9.50m);
        order.Discount.Should().Be(1.90m);
        order.Total.Should().Be(7.60m);
    }

    [Fact]
    public void ReplaceItems_SetsUpdatedAt()
    {
        var order = Order.Create([Sandwich()]).Value;
        var before = DateTime.UtcNow;

        order.ReplaceItems([Drink()]);

        order.UpdatedAt.Should().NotBeNull();
        order.UpdatedAt.Should().BeOnOrAfter(before);
    }
}
