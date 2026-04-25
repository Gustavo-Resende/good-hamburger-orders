using GoodHamburger.Application.Interfaces;
using GoodHamburger.Application.Orders.Errors;

namespace GoodHamburger.Application.Orders.Commands;

public record DeleteOrderCommand(Guid Id) : IRequest<Result>;

public class DeleteOrderHandler : IRequestHandler<DeleteOrderCommand, Result>
{
    private readonly IOrderRepository _orderRepository;

    public DeleteOrderHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order is null)
            return Result.NotFound(OrderQueryErrors.OrderNotFound);

        await _orderRepository.DeleteAsync(order, cancellationToken);
        return Result.Success();
    }
}
