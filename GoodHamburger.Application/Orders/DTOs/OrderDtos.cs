namespace GoodHamburger.Application.Orders.DTOs;

public record CreateOrderRequest(List<Guid> MenuItemIds);
public record CreateOrderResponse(Guid Id, List<OrderItemDto> Items,
    decimal Subtotal, decimal Discount, decimal Total, DateTime CreatedAt);

public record GetOrderByIdResponse(Guid Id, List<OrderItemDto> Items,
    decimal Subtotal, decimal Discount, decimal Total,
    DateTime CreatedAt, DateTime? UpdatedAt);

public record GetOrdersResponse(List<GetOrderByIdResponse> Orders);

public record UpdateOrderRequest(List<Guid> MenuItemIds);
public record UpdateOrderResponse(Guid Id, List<OrderItemDto> Items,
    decimal Subtotal, decimal Discount, decimal Total, DateTime? UpdatedAt);

public record OrderItemDto(Guid MenuItemId, string Name, decimal Price, string Category);

public record MenuItemResponse(Guid Id, string Name, decimal Price, string Category);
public record GetMenuResponse(List<MenuItemResponse> Items);
