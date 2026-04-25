using System.Net.Http.Json;
using GoodHamburger.Blazor.Models;

namespace GoodHamburger.Blazor.Services;

public class OrderService
{
    private readonly HttpClient _http;

    public OrderService(HttpClient http) => _http = http;

    public async Task<List<MenuItemResponse>> GetMenuAsync()
    {
        var response = await _http.GetFromJsonAsync<GetMenuResponse>("api/menu");
        return response?.Items ?? new List<MenuItemResponse>();
    }

    public async Task<List<OrderResponse>> GetAllOrdersAsync()
    {
        var response = await _http.GetFromJsonAsync<GetOrdersResponse>("api/orders");
        return response?.Orders ?? new List<OrderResponse>();
    }

    public async Task<OrderResponse?> GetOrderByIdAsync(Guid id)
    {
        try
        {
            return await _http.GetFromJsonAsync<OrderResponse>($"api/orders/{id}");
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<(OrderResponse? Order, string? Error)> CreateOrderAsync(CreateOrderRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/orders", request);

        if (response.IsSuccessStatusCode)
        {
            var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
            return (order, null);
        }

        return (null, $"Erro ao criar pedido: {response.StatusCode}");
    }

    public async Task<(OrderResponse? Order, string? Error)> UpdateOrderAsync(Guid id, CreateOrderRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/orders/{id}", request);

        if (response.IsSuccessStatusCode)
        {
            var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
            return (order, null);
        }

        return (null, $"Erro ao atualizar pedido: {response.StatusCode}");
    }

    public async Task<(bool Success, string? Error)> DeleteOrderAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/orders/{id}");
        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, $"Erro ao deletar pedido: {response.StatusCode}");
    }
}
