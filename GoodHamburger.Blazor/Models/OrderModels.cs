namespace GoodHamburger.Blazor.Models;

public record MenuItemResponse(
    Guid Id,
    string Name,
    decimal Price,
    string Category);

public record GetMenuResponse(List<MenuItemResponse> Items);

public record OrderItemResponse(
    Guid MenuItemId,
    string Name,
    decimal Price,
    string Category);

public record OrderResponse(
    Guid Id,
    List<OrderItemResponse> Items,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record GetOrdersResponse(List<OrderResponse> Orders);

public record CreateOrderRequest(List<Guid> MenuItemIds);
