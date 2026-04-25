using Ardalis.Result;
using FluentAssertions;
using GoodHamburger.Application.Interfaces;
using GoodHamburger.Application.Orders.Commands;
using GoodHamburger.Domain.Enums;
using GoodHamburger.Domain.Menu;
using GoodHamburger.Domain.Orders;
using NSubstitute;

namespace GoodHamburger.Application.Tests.Orders;

public class UpdateOrderHandlerTests
{
    private readonly IOrderRepository _repo = Substitute.For<IOrderRepository>();
    private readonly IMenuService _menu = Substitute.For<IMenuService>();
    private readonly UpdateOrderHandler _handler;

    public UpdateOrderHandlerTests()
        => _handler = new UpdateOrderHandler(_repo, _menu);

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Order?)null);

        var result = await _handler.Handle(
            new UpdateOrderCommand(id, [Guid.NewGuid()]), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_MenuItemNotFound_ReturnsNotFound()
    {
        var sandwich = new MenuItem(Guid.NewGuid(), "X-Burger", 5.00m, ItemCategory.Sandwich);
        var order = Order.Create([sandwich]).Value;
        _repo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var unknownId = Guid.NewGuid();
        _menu.GetById(unknownId).Returns((MenuItem?)null);

        var result = await _handler.Handle(
            new UpdateOrderCommand(order.Id, [unknownId]), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_ValidUpdate_CallsUpdateAsyncAndReturnsSuccess()
    {
        var sandwich = new MenuItem(Guid.NewGuid(), "X-Burger", 5.00m, ItemCategory.Sandwich);
        var drink = new MenuItem(Guid.NewGuid(), "Refrigerante", 2.50m, ItemCategory.Drink);
        var order = Order.Create([sandwich]).Value;

        _repo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _menu.GetById(drink.Id).Returns(drink);

        var result = await _handler.Handle(
            new UpdateOrderCommand(order.Id, [drink.Id]), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Subtotal.Should().Be(2.50m);
        await _repo.Received(1).UpdateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }
}
