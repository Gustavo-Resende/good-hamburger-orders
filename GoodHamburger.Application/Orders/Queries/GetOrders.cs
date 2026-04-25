using GoodHamburger.Application.Interfaces;
using GoodHamburger.Application.Orders.DTOs;
using GoodHamburger.Application.Orders.Extensions;

namespace GoodHamburger.Application.Orders.Queries;

public record GetOrdersQuery : IRequest<Result<GetOrdersResponse>>;

public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, Result<GetOrdersResponse>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<GetOrdersResponse>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _orderRepository.ListAsync(cancellationToken);
        var response = new GetOrdersResponse(orders.Select(o => o.ToGetByIdResponse()).ToList());
        return Result.Success(response);
    }
}
