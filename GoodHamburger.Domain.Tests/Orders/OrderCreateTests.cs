using Ardalis.Result;
using FluentAssertions;
using GoodHamburger.Domain.Enums;
using GoodHamburger.Domain.Menu;
using GoodHamburger.Domain.Orders;
using GoodHamburger.Domain.Orders.Errors;

namespace GoodHamburger.Domain.Tests.Orders;

public class OrderCreateTests
{
    [Fact]
    public void Create_WithEmptyList_ReturnsInvalid()
    {
        var result = Order.Create([]);

        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e => e.ErrorMessage == OrderErrors.EmptyOrder);
    }

    [Fact]
    public void Create_WithNullList_ReturnsInvalid()
    {
        var result = Order.Create(null!);

        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public void Create_WithSingleSandwich_ReturnsSuccess()
    {
        var sandwich = new MenuItem(Guid.NewGuid(), "X-Burger", 5.00m, ItemCategory.Sandwich);

        var result = Order.Create([sandwich]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Subtotal.Should().Be(5.00m);
        result.Value.Discount.Should().Be(0m);
        result.Value.Total.Should().Be(5.00m);
        result.Value.Items.Should().HaveCount(1);
    }
}
