using GoodHamburger.Application.Orders.DTOs;
using GoodHamburger.Domain.Menu;
using GoodHamburger.Domain.Orders;

namespace GoodHamburger.Application.Orders.Extensions;

public static class OrderMappings
{
    public static OrderItemDto ToDto(this OrderItem item) =>
        new(item.MenuItemId, item.Name, item.Price, item.Category.ToString());

    public static CreateOrderResponse ToCreateResponse(this Order order) =>
        new(order.Id,
            order.Items.Select(i => i.ToDto()).ToList(),
            order.Subtotal, order.Discount, order.Total, order.CreatedAt);

    public static GetOrderByIdResponse ToGetByIdResponse(this Order order) =>
        new(order.Id,
            order.Items.Select(i => i.ToDto()).ToList(),
            order.Subtotal, order.Discount, order.Total,
            order.CreatedAt, order.UpdatedAt);

    public static UpdateOrderResponse ToUpdateResponse(this Order order) =>
        new(order.Id,
            order.Items.Select(i => i.ToDto()).ToList(),
            order.Subtotal, order.Discount, order.Total, order.UpdatedAt);

    public static MenuItemResponse ToResponse(this MenuItem item) =>
        new(item.Id, item.Name, item.Price, item.Category.ToString());
}
