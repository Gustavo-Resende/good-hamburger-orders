using FluentAssertions;
using GoodHamburger.Application.Interfaces;
using GoodHamburger.Application.Orders.Queries;
using GoodHamburger.Domain.Enums;
using GoodHamburger.Domain.Menu;
using GoodHamburger.Domain.Orders;
using NSubstitute;

namespace GoodHamburger.Application.Tests.Orders;

public class GetOrdersHandlerTests
{
    private readonly IOrderRepository _repo = Substitute.For<IOrderRepository>();
    private readonly GetOrdersHandler _handler;

    public GetOrdersHandlerTests()
        => _handler = new GetOrdersHandler(_repo);

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsSuccessWithEmptyList()
    {
        _repo.ListAsync(Arg.Any<CancellationToken>())
             .Returns(new List<Order>());

        var result = await _handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Orders.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RepositoryWithOrders_ReturnsMappedOrders()
    {
        var sandwich = new MenuItem(Guid.NewGuid(), "X-Burger", 5.00m, ItemCategory.Sandwich);
        var order = Order.Create([sandwich]).Value;

        _repo.ListAsync(Arg.Any<CancellationToken>())
             .Returns(new List<Order> { order });

        var result = await _handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Orders.Should().HaveCount(1);
        result.Value.Orders[0].Id.Should().Be(order.Id);
    }
}
