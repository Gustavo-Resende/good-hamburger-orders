using Ardalis.Result;
using FluentAssertions;
using GoodHamburger.Application.Interfaces;
using GoodHamburger.Application.Orders.Commands;
using GoodHamburger.Domain.Enums;
using GoodHamburger.Domain.Menu;
using GoodHamburger.Domain.Orders;
using NSubstitute;

namespace GoodHamburger.Application.Tests.Orders;

public class DeleteOrderHandlerTests
{
    private readonly IOrderRepository _repo = Substitute.For<IOrderRepository>();
    private readonly DeleteOrderHandler _handler;

    public DeleteOrderHandlerTests()
        => _handler = new DeleteOrderHandler(_repo);

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Order?)null);

        var result = await _handler.Handle(new DeleteOrderCommand(id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_OrderFound_CallsDeleteAsyncAndReturnsSuccess()
    {
        var sandwich = new MenuItem(Guid.NewGuid(), "X-Burger", 5.00m, ItemCategory.Sandwich);
        var order = Order.Create([sandwich]).Value;
        _repo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(new DeleteOrderCommand(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repo.Received(1).DeleteAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }
}
