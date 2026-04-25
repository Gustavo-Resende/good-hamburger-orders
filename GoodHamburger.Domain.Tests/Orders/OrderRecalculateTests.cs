using FluentAssertions;
using GoodHamburger.Domain.Enums;
using GoodHamburger.Domain.Menu;
using GoodHamburger.Domain.Orders;

namespace GoodHamburger.Domain.Tests.Orders;

public class OrderRecalculateTests
{
    private static MenuItem Sandwich(decimal price = 5.00m) =>
        new(Guid.NewGuid(), "X-Burger", price, ItemCategory.Sandwich);

    private static MenuItem Side(decimal price = 2.00m) =>
        new(Guid.NewGuid(), "Batata Frita", price, ItemCategory.Side);

    private static MenuItem Drink(decimal price = 2.50m) =>
        new(Guid.NewGuid(), "Refrigerante", price, ItemCategory.Drink);

    [Fact]
    public void Recalculate_SandwichOnly_ZeroDiscount()
    {
        var order = Order.Create([Sandwich(5.00m)]).Value;

        order.Subtotal.Should().Be(5.00m);
        order.Discount.Should().Be(0m);
        order.Total.Should().Be(5.00m);
    }

    [Fact]
    public void Recalculate_SideOnly_ZeroDiscount()
    {
        var order = Order.Create([Side(2.00m)]).Value;

        order.Subtotal.Should().Be(2.00m);
        order.Discount.Should().Be(0m);
        order.Total.Should().Be(2.00m);
    }

    [Fact]
    public void Recalculate_DrinkOnly_ZeroDiscount()
    {
        var order = Order.Create([Drink(2.50m)]).Value;

        order.Subtotal.Should().Be(2.50m);
        order.Discount.Should().Be(0m);
        order.Total.Should().Be(2.50m);
    }

    [Fact]
    public void Recalculate_SandwichAndSide_TenPercentDiscount()
    {
        var order = Order.Create([Sandwich(5.00m), Side(2.00m)]).Value;

        order.Subtotal.Should().Be(7.00m);
        order.Discount.Should().Be(0.70m);
        order.Total.Should().Be(6.30m);
    }

    [Fact]
    public void Recalculate_SandwichAndDrink_FifteenPercentDiscount()
    {
        var order = Order.Create([Sandwich(5.00m), Drink(2.50m)]).Value;

        order.Subtotal.Should().Be(7.50m);
        order.Discount.Should().Be(1.125m);
        order.Total.Should().Be(6.375m);
    }

    [Fact]
    public void Recalculate_SandwichSideAndDrink_TwentyPercentDiscount()
    {
        var order = Order.Create([Sandwich(5.00m), Side(2.00m), Drink(2.50m)]).Value;

        order.Subtotal.Should().Be(9.50m);
        order.Discount.Should().Be(1.90m);
        order.Total.Should().Be(7.60m);
    }

    [Fact]
    public void Recalculate_SandwichSideAndDrink_UsesMenuSeedValues()
    {
        var order = Order.Create([Sandwich(5.00m), Side(2.00m), Drink(2.50m)]).Value;

        order.Discount.Should().Be(order.Subtotal * 0.20m);
        order.Total.Should().Be(order.Subtotal - order.Discount);
    }
}
