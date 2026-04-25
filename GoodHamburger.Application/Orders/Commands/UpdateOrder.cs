using GoodHamburger.Application.Interfaces;
using GoodHamburger.Application.Orders.DTOs;
using GoodHamburger.Application.Orders.Errors;
using GoodHamburger.Application.Orders.Extensions;
using GoodHamburger.Domain.Menu;
using GoodHamburger.Domain.Menu.Errors;
using GoodHamburger.Domain.Orders.Errors;

namespace GoodHamburger.Application.Orders.Commands;

public record UpdateOrderCommand(Guid Id, List<Guid> MenuItemIds) : IRequest<Result<UpdateOrderResponse>>;

public class UpdateOrderHandler : IRequestHandler<UpdateOrderCommand, Result<UpdateOrderResponse>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMenuService _menuService;

    public UpdateOrderHandler(IOrderRepository orderRepository, IMenuService menuService)
    {
        _orderRepository = orderRepository;
        _menuService = menuService;
    }

    public async Task<Result<UpdateOrderResponse>> Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (order is null)
            return Result.NotFound(OrderQueryErrors.OrderNotFound);

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

        var replaceResult = order.ReplaceItems(menuItems);
        if (!replaceResult.IsSuccess)
            return Result.Invalid(replaceResult.ValidationErrors.ToArray());

        await _orderRepository.UpdateAsync(order, cancellationToken);

        return Result.Success(order.ToUpdateResponse());
    }
}
