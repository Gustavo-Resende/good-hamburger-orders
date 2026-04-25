using Ardalis.Result;
using FluentAssertions;
using GoodHamburger.Application.Interfaces;
using GoodHamburger.Application.Orders.Commands;
using GoodHamburger.Domain.Enums;
using GoodHamburger.Domain.Menu;
using GoodHamburger.Domain.Orders;
using NSubstitute;

namespace GoodHamburger.Application.Tests.Orders;

public class CreateOrderHandlerTests
{
    private readonly IOrderRepository _repo = Substitute.For<IOrderRepository>();
    private readonly IMenuService _menu = Substitute.For<IMenuService>();
    private readonly CreateOrderHandler _handler;

    public CreateOrderHandlerTests()
        => _handler = new CreateOrderHandler(_repo, _menu);

    [Fact]
    public async Task Handle_EmptyList_ReturnsInvalid()
    {
        var command = new CreateOrderCommand([]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.Invalid);
    }

    [Fact]
    public async Task Handle_UnknownMenuItemId_ReturnsNotFound()
    {
        var unknownId = Guid.NewGuid();
        _menu.GetById(unknownId).Returns((MenuItem?)null);

        var command = new CreateOrderCommand([unknownId]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_ValidItems_AddsOrderAndReturnsSuccess()
    {
        var sandwich = new MenuItem(Guid.NewGuid(), "X-Burger", 5.00m, ItemCategory.Sandwich);
        _menu.GetById(sandwich.Id).Returns(sandwich);

        var command = new CreateOrderCommand([sandwich.Id]);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Subtotal.Should().Be(5.00m);
        await _repo.Received(1).AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }
}
