using GoodHamburger.Application.Interfaces;
using GoodHamburger.Application.Orders.DTOs;
using GoodHamburger.Application.Orders.Extensions;
using GoodHamburger.Domain.Menu;
using GoodHamburger.Domain.Menu.Errors;
using GoodHamburger.Domain.Orders;
using GoodHamburger.Domain.Orders.Errors;

namespace GoodHamburger.Application.Orders.Commands;

public record CreateOrderCommand(List<Guid> MenuItemIds) : IRequest<Result<CreateOrderResponse>>;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<CreateOrderResponse>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMenuService _menuService;

    public CreateOrderHandler(IOrderRepository orderRepository, IMenuService menuService)
    {
        _orderRepository = orderRepository;
        _menuService = menuService;
    }

    public async Task<Result<CreateOrderResponse>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (request.MenuItemIds == null || request.MenuItemIds.Count == 0)
            return Result.Invalid(new ValidationError(OrderErrors.EmptyOrder));

        var menuItems = new List<MenuItem>();
        foreach (var id in request.MenuItemIds)
        {
            var item = _menuService.GetById(id);
            if (item is null)
                return Result.NotFound(string.Format(MenuErrors.ItemNotFound, id));
            menuItems.Add(item);
        }

        var orderResult = Order.Create(menuItems);
        if (!orderResult.IsSuccess)
            return Result.Invalid(orderResult.ValidationErrors.ToArray());

        await _orderRepository.AddAsync(orderResult.Value, cancellationToken);

        return Result.Success(orderResult.Value.ToCreateResponse());
    }
}
