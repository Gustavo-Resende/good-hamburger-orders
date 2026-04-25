using Ardalis.Result;
using FluentAssertions;
using GoodHamburger.Application.Interfaces;
using GoodHamburger.Application.Orders.Queries;
using GoodHamburger.Domain.Enums;
using GoodHamburger.Domain.Menu;
using GoodHamburger.Domain.Orders;
using NSubstitute;

namespace GoodHamburger.Application.Tests.Orders;

public class GetOrderByIdHandlerTests
{
    private readonly IOrderRepository _repo = Substitute.For<IOrderRepository>();
    private readonly GetOrderByIdHandler _handler;

    public GetOrderByIdHandlerTests()
        => _handler = new GetOrderByIdHandler(_repo);

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Order?)null);

        var result = await _handler.Handle(new GetOrderByIdQuery(id), CancellationToken.None);

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_OrderFound_ReturnsCorrectResponse()
    {
        var sandwich = new MenuItem(Guid.NewGuid(), "X-Burger", 5.00m, ItemCategory.Sandwich);
        var order = Order.Create([sandwich]).Value;
        _repo.GetByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        var result = await _handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(order.Id);
        result.Value.Subtotal.Should().Be(5.00m);
        result.Value.Items.Should().HaveCount(1);
    }
}
