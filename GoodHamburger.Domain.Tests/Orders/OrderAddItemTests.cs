using Ardalis.Result;
using FluentAssertions;
using GoodHamburger.Domain.Enums;
using GoodHamburger.Domain.Menu;
using GoodHamburger.Domain.Orders;
using GoodHamburger.Domain.Orders.Errors;

namespace GoodHamburger.Domain.Tests.Orders;

public class OrderAddItemTests
{
    [Fact]
    public void AddItem_WithDuplicateSandwich_ReturnsInvalid()
    {
        var sandwich1 = new MenuItem(Guid.NewGuid(), "X-Burger", 5.00m, ItemCategory.Sandwich);
        var sandwich2 = new MenuItem(Guid.NewGuid(), "X-Egg", 4.50m, ItemCategory.Sandwich);

        var result = Order.Create([sandwich1, sandwich2]);

        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e =>
            e.ErrorMessage.Contains("Sandwich"));
    }

    [Fact]
    public void AddItem_DuplicateCategoryError_ContainsCategoryName()
    {
        var drink1 = new MenuItem(Guid.NewGuid(), "Refrigerante", 2.50m, ItemCategory.Drink);
        var drink2 = new MenuItem(Guid.NewGuid(), "Suco", 3.00m, ItemCategory.Drink);

        var result = Order.Create([drink1, drink2]);

        result.Status.Should().Be(ResultStatus.Invalid);
        result.ValidationErrors.Should().Contain(e =>
            e.ErrorMessage == string.Format(OrderErrors.DuplicateCategory, ItemCategory.Drink));
    }

    [Fact]
    public void AddItem_WithDistinctCategories_ReturnsSuccess()
    {
        var sandwich = new MenuItem(Guid.NewGuid(), "X-Burger", 5.00m, ItemCategory.Sandwich);
        var drink = new MenuItem(Guid.NewGuid(), "Refrigerante", 2.50m, ItemCategory.Drink);

        var result = Order.Create([sandwich, drink]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
    }
}
