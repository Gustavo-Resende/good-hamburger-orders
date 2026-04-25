using GoodHamburger.Application.Interfaces;
using GoodHamburger.Application.Orders.DTOs;
using GoodHamburger.Application.Orders.Errors;
using GoodHamburger.Application.Orders.Extensions;

namespace GoodHamburger.Application.Orders.Queries;

public record GetOrderByIdQuery(Guid Id) : IRequest<Result<GetOrderByIdResponse>>;

public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, Result<GetOrderByIdResponse>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<GetOrderByIdResponse>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order is null)
            return Result.NotFound(OrderQueryErrors.OrderNotFound);

        return Result.Success(order.ToGetByIdResponse());
    }
}
