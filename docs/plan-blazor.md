# Plano de Implementação — Frontend Blazor

> Status: aguardando aprovação  
> Data: 2026-04-24

---

## Visão geral

Adicionar o projeto `GoodHamburger.Blazor` (Blazor WebAssembly, .NET 8) à solution existente, consumindo a API REST via `HttpClient`. O backend precisa ser ajustado com CORS antes de qualquer coisa, pois o Blazor roda em porta diferente.

Ordem de execução obrigatória — cada item depende dos anteriores:

```
1. CORS no backend
2. Projeto Blazor + solution
3. index.html (Bootstrap)
4. appsettings.json
5. Program.cs (Blazor)
6. Models
7. OrderService
8. Pages (Orders, CreateOrder, OrderDetail)
9. NavMenu + MainLayout
10. Makefile
```

---

## 1. CORS no backend

### Dependências
Nenhuma — é o ponto de partida.

### `GoodHamburger.API/Extensions/ApiServiceExtensions.cs`

**Alterar** — adicionar registro da policy `BlazorPolicy` dentro de `AddApiServices`:

```diff
 public static IServiceCollection AddApiServices(this IServiceCollection services)
 {
     services.AddControllers(options => options.AddDefaultResultConvention());
     services.AddEndpointsApiExplorer();
     services.AddSwaggerGen();
     services.AddExceptionHandler<GlobalExceptionHandler>();
+
+    services.AddCors(options =>
+    {
+        options.AddPolicy("BlazorPolicy", policy =>
+        {
+            policy.WithOrigins("http://localhost:5173", "https://localhost:7173")
+                  .AllowAnyHeader()
+                  .AllowAnyMethod();
+        });
+    });
+
     return services;
 }
```

### `GoodHamburger.API/Program.cs`

**Alterar** — ativar o middleware de CORS entre `UseExceptionHandler` e `MapControllers`:

```diff
 app.UseExceptionHandler(_ => { });

 if (app.Environment.IsDevelopment())
 {
     app.UseSwagger();
     app.UseSwaggerUI();
 }

 app.UseHttpsRedirection();
+app.UseCors("BlazorPolicy");
 app.MapControllers();
 app.Run();
```

> Ordem importa: `UseCors` deve vir antes de `MapControllers`.

---

## 2. Criação do projeto Blazor e adição à solution

### Dependências
Nenhuma além do .NET SDK já instalado.

### Comandos a executar (na raiz da solution)

```bash
dotnet new blazorwasm -n GoodHamburger.Blazor -o GoodHamburger.Blazor -f net8.0
dotnet sln GoodHamburger.slnx add GoodHamburger.Blazor
```

### Resultado esperado
- Pasta `GoodHamburger.Blazor/` criada com estrutura padrão Blazor WASM.
- Projeto referenciado no `GoodHamburger.slnx`.

### Arquivos gerados pelo template que serão modificados nos passos seguintes
- `GoodHamburger.Blazor/wwwroot/index.html`
- `GoodHamburger.Blazor/wwwroot/appsettings.json`
- `GoodHamburger.Blazor/Program.cs`
- `GoodHamburger.Blazor/Shared/MainLayout.razor`
- `GoodHamburger.Blazor/Shared/NavMenu.razor`

### Arquivos gerados pelo template que serão removidos
- `GoodHamburger.Blazor/Pages/Counter.razor`
- `GoodHamburger.Blazor/Pages/FetchData.razor`
- `GoodHamburger.Blazor/Pages/Index.razor` (substituído por redirecionamento simples ou página home mínima)

---

## 3. Bootstrap no `wwwroot/index.html`

### Dependências
Passo 2 concluído.

### `GoodHamburger.Blazor/wwwroot/index.html`

**Alterar** — adicionar o link do Bootstrap 5 dentro do `<head>`, antes dos outros estilos:

```diff
 <head>
     <meta charset="utf-8" />
     <meta name="viewport" content="width=device-width, initial-scale=1.0" />
     <title>GoodHamburger.Blazor</title>
+    <link rel="stylesheet"
+          href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" />
     <base href="/" />
     ...
 </head>
```

---

## 4. `wwwroot/appsettings.json`

### Dependências
Passo 2 concluído.

### `GoodHamburger.Blazor/wwwroot/appsettings.json`

**Substituir** o conteúdo gerado pelo template:

```json
{
  "ApiBaseUrl": "http://localhost:5149"
}
```

> A porta `5149` é a porta HTTP da API definida em `GoodHamburger.API/Properties/launchSettings.json`. Verificar antes de implementar; ajustar se diferente.

---

## 5. `Program.cs` do Blazor

### Dependências
Passos 2 e 4 concluídos (lê `ApiBaseUrl` do appsettings).

### `GoodHamburger.Blazor/Program.cs`

**Substituir** o conteúdo gerado pelo template:

```csharp
using GoodHamburger.Blazor;
using GoodHamburger.Blazor.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? throw new InvalidOperationException("ApiBaseUrl não configurado.");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});

builder.Services.AddScoped<OrderService>();

await builder.Build().RunAsync();
```

---

## 6. Models (`OrderModels.cs`)

### Dependências
Passo 2 concluído.

### `GoodHamburger.Blazor/Models/OrderModels.cs`

**Criar** o arquivo (pasta `Models/` não existe no template — criar junto):

```csharp
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
```

---

## 7. `OrderService`

### Dependências
Passos 5 e 6 concluídos (`HttpClient` registrado no DI; models disponíveis).

### `GoodHamburger.Blazor/Services/OrderService.cs`

**Criar** o arquivo (pasta `Services/` não existe no template — criar junto):

```csharp
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

    public async Task<(bool Success, string? Error)> DeleteOrderAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/orders/{id}");
        return response.IsSuccessStatusCode
            ? (true, null)
            : (false, $"Erro ao deletar pedido: {response.StatusCode}");
    }
}
```

---

## 8a. `Orders.razor` — listagem

### Dependências
Passo 7 concluído (`OrderService` disponível).

### `GoodHamburger.Blazor/Pages/Orders.razor`

**Criar** (substituir o `Index.razor` gerado ou criar novo arquivo; `Index.razor` pode ser deletado ou mantido como redirecionamento):

Comportamento:
- `OnInitializedAsync` → `OrderService.GetAllOrdersAsync()`
- Tabela Bootstrap com colunas: Id (8 chars), Total, Desconto, Data de criação, Ações
- Botão "Ver" por linha → `Navigation.NavigateTo($"/orders/{order.Id}")`
- Botão "Deletar" por linha → `DeleteOrder(order.Id)` → recarrega lista; exibe erro se falhar
- Botão "Novo Pedido" no topo → `Navigation.NavigateTo("/orders/new")`
- Estado de carregando: `<p>Carregando...</p>` (controlado por flag `isLoading`)
- Erro: `<p class="text-danger">@errorMessage</p>`

```razor
@page "/orders"
@inject OrderService OrderService
@inject NavigationManager Navigation
@using GoodHamburger.Blazor.Models
@using GoodHamburger.Blazor.Services

<div class="container mt-4">
    <div class="d-flex justify-content-between align-items-center mb-3">
        <h1>Pedidos</h1>
        <button class="btn btn-primary" @onclick='() => Navigation.NavigateTo("/orders/new")'>
            Novo Pedido
        </button>
    </div>

    @if (errorMessage != null)
    {
        <p class="text-danger">@errorMessage</p>
    }

    @if (isLoading)
    {
        <p>Carregando...</p>
    }
    else
    {
        <table class="table table-striped">
            <thead>
                <tr>
                    <th>Id</th>
                    <th>Total</th>
                    <th>Desconto</th>
                    <th>Criado em</th>
                    <th>Ações</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var order in orders)
                {
                    <tr>
                        <td>@order.Id.ToString()[..8]</td>
                        <td>R$ @order.Total.ToString("F2")</td>
                        <td>R$ @order.Discount.ToString("F2")</td>
                        <td>@order.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")</td>
                        <td>
                            <button class="btn btn-sm btn-outline-primary me-1"
                                    @onclick='() => Navigation.NavigateTo($"/orders/{order.Id}")'>
                                Ver
                            </button>
                            <button class="btn btn-sm btn-danger"
                                    @onclick='() => DeleteOrder(order.Id)'>
                                Deletar
                            </button>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    }
</div>

@code {
    private List<OrderResponse> orders = new();
    private string? errorMessage;
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        orders = await OrderService.GetAllOrdersAsync();
        isLoading = false;
    }

    private async Task DeleteOrder(Guid id)
    {
        var (success, error) = await OrderService.DeleteOrderAsync(id);
        if (success)
            orders = await OrderService.GetAllOrdersAsync();
        else
            errorMessage = error;
    }
}
```

---

## 8b. `CreateOrder.razor` — criar pedido

### Dependências
Passo 7 concluído.

### `GoodHamburger.Blazor/Pages/CreateOrder.razor`

**Criar:**

Comportamento:
- `OnInitializedAsync` → `OrderService.GetMenuAsync()`
- Itens do cardápio agrupados por categoria (`GroupBy(x => x.Category)`)
- Botão "Adicionar" por item → `AddItem(item)` — desabilitado se já existe item da mesma categoria em `selectedItems`
- Lista de selecionados com botão "Remover" → `RemoveItem(item)`
- "Criar Pedido" → `Submit()` → navega para `/orders/{id}` em sucesso; exibe erro
- "Cancelar" → navega para `/orders`
- Layout em duas colunas Bootstrap (col-md-7 cardápio / col-md-5 selecionados)

```razor
@page "/orders/new"
@inject OrderService OrderService
@inject NavigationManager Navigation
@using GoodHamburger.Blazor.Models
@using GoodHamburger.Blazor.Services

<div class="container mt-4">
    <h1 class="mb-3">Novo Pedido</h1>

    @if (errorMessage != null)
    {
        <p class="text-danger">@errorMessage</p>
    }

    <div class="row">
        <div class="col-md-7">
            <h5>Cardápio</h5>
            @foreach (var group in menuItems.GroupBy(x => x.Category))
            {
                <h6 class="mt-3 text-muted">@group.Key</h6>
                @foreach (var item in group)
                {
                    <div class="d-flex justify-content-between align-items-center border rounded p-2 mb-1">
                        <span>@item.Name — R$ @item.Price.ToString("F2")</span>
                        <button class="btn btn-sm btn-outline-success"
                                @onclick='() => AddItem(item)'
                                disabled="@IsDisabled(item)">
                            Adicionar
                        </button>
                    </div>
                }
            }
        </div>

        <div class="col-md-5">
            <h5>Itens Selecionados</h5>
            @if (!selectedItems.Any())
            {
                <p class="text-muted">Nenhum item selecionado.</p>
            }
            else
            {
                @foreach (var item in selectedItems)
                {
                    <div class="d-flex justify-content-between align-items-center border rounded p-2 mb-1">
                        <span>@item.Name — R$ @item.Price.ToString("F2")</span>
                        <button class="btn btn-sm btn-danger"
                                @onclick='() => RemoveItem(item)'>
                            Remover
                        </button>
                    </div>
                }
            }
        </div>
    </div>

    <div class="mt-4">
        <button class="btn btn-primary me-2" @onclick="Submit" disabled="@isLoading">
            @(isLoading ? "Criando..." : "Criar Pedido")
        </button>
        <button class="btn btn-secondary" @onclick='() => Navigation.NavigateTo("/orders")'>
            Cancelar
        </button>
    </div>
</div>

@code {
    private List<MenuItemResponse> menuItems = new();
    private List<MenuItemResponse> selectedItems = new();
    private string? errorMessage;
    private bool isLoading = false;

    protected override async Task OnInitializedAsync()
    {
        menuItems = await OrderService.GetMenuAsync();
    }

    private void AddItem(MenuItemResponse item)
    {
        if (!selectedItems.Any(x => x.Category == item.Category))
            selectedItems.Add(item);
    }

    private void RemoveItem(MenuItemResponse item)
    {
        selectedItems.Remove(item);
    }

    private bool IsDisabled(MenuItemResponse item)
        => selectedItems.Any(x => x.Category == item.Category);

    private async Task Submit()
    {
        if (!selectedItems.Any()) return;

        isLoading = true;
        errorMessage = null;

        var request = new CreateOrderRequest(selectedItems.Select(x => x.Id).ToList());
        var (order, error) = await OrderService.CreateOrderAsync(request);

        if (order != null)
            Navigation.NavigateTo($"/orders/{order.Id}");
        else
            errorMessage = error;

        isLoading = false;
    }
}
```

---

## 8c. `OrderDetail.razor` — detalhe do pedido

### Dependências
Passo 7 concluído.

### `GoodHamburger.Blazor/Pages/OrderDetail.razor`

**Criar:**

Comportamento:
- Parâmetro de rota `{Id:guid}`
- `OnInitializedAsync` → `OrderService.GetOrderByIdAsync(Id)`
- Não encontrado: `<p class="text-danger">Pedido não encontrado.</p>`
- Card com tabela de itens (nome, preço), subtotal, desconto (valor + %), total em negrito
- Fórmula do percentual: `order.Subtotal > 0 ? order.Discount / order.Subtotal * 100 : 0`
- "Deletar Pedido" → `DeleteOrder()` → redireciona para `/orders`
- "Voltar" → navega para `/orders`

```razor
@page "/orders/{Id:guid}"
@inject OrderService OrderService
@inject NavigationManager Navigation
@using GoodHamburger.Blazor.Models
@using GoodHamburger.Blazor.Services

<div class="container mt-4">
    @if (order == null && !notFound)
    {
        <p>Carregando...</p>
    }
    else if (notFound)
    {
        <p class="text-danger">Pedido não encontrado.</p>
        <button class="btn btn-secondary" @onclick='() => Navigation.NavigateTo("/orders")'>Voltar</button>
    }
    else if (order != null)
    {
        <div class="card">
            <div class="card-header d-flex justify-content-between align-items-center">
                <h5 class="mb-0">Pedido @order.Id.ToString()[..8]</h5>
                <small class="text-muted">@order.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")</small>
            </div>
            <div class="card-body">
                <table class="table">
                    <thead>
                        <tr><th>Item</th><th>Preço</th></tr>
                    </thead>
                    <tbody>
                        @foreach (var item in order.Items)
                        {
                            <tr>
                                <td>@item.Name</td>
                                <td>R$ @item.Price.ToString("F2")</td>
                            </tr>
                        }
                    </tbody>
                </table>

                <hr />
                <p>Subtotal: R$ @order.Subtotal.ToString("F2")</p>
                <p>
                    Desconto: R$ @order.Discount.ToString("F2")
                    (@(order.Subtotal > 0 ? (order.Discount / order.Subtotal * 100).ToString("F0") : "0")%)
                </p>
                <p><strong>Total: R$ @order.Total.ToString("F2")</strong></p>
            </div>
            <div class="card-footer d-flex gap-2">
                <button class="btn btn-danger" @onclick="DeleteOrder">Deletar Pedido</button>
                <button class="btn btn-secondary" @onclick='() => Navigation.NavigateTo("/orders")'>Voltar</button>
            </div>
        </div>

        @if (errorMessage != null)
        {
            <p class="text-danger mt-2">@errorMessage</p>
        }
    }
</div>

@code {
    [Parameter] public Guid Id { get; set; }

    private OrderResponse? order;
    private string? errorMessage;
    private bool notFound = false;

    protected override async Task OnInitializedAsync()
    {
        order = await OrderService.GetOrderByIdAsync(Id);
        if (order == null)
            notFound = true;
    }

    private async Task DeleteOrder()
    {
        var (success, error) = await OrderService.DeleteOrderAsync(Id);
        if (success)
            Navigation.NavigateTo("/orders");
        else
            errorMessage = error;
    }
}
```

---

## 9a. `NavMenu.razor`

### Dependências
Passo 2 concluído.

### `GoodHamburger.Blazor/Shared/NavMenu.razor`

**Substituir** o conteúdo gerado pelo template. Links obrigatórios:
- "Good Hamburger" → `/`
- "Pedidos" → `/orders`
- "Novo Pedido" → `/orders/new`

```razor
<nav class="navbar navbar-expand-lg navbar-dark bg-dark">
    <div class="container">
        <a class="navbar-brand" href="/">Good Hamburger</a>
        <div class="navbar-nav">
            <a class="nav-link" href="/orders">Pedidos</a>
            <a class="nav-link" href="/orders/new">Novo Pedido</a>
        </div>
    </div>
</nav>
```

> O arquivo gerado pelo template usa CSS scoped e classes próprias do Blazor template. O conteúdo inteiro deve ser substituído pela navbar Bootstrap acima.

---

## 9b. `MainLayout.razor`

### Dependências
Passo 9a concluído (`NavMenu` disponível).

### `GoodHamburger.Blazor/Shared/MainLayout.razor`

**Substituir** o conteúdo gerado pelo template:

```razor
@inherits LayoutComponentBase

<NavMenu />

<main class="container mt-3">
    @Body
</main>
```

> Remover o `<div class="sidebar">` e estrutura original do template. O `MainLayout.razor.css` gerado pode ser deletado.

---

## 10. Comando `run-blazor` no Makefile

### Dependências
Passo 2 concluído (projeto existe na solution).

### `Makefile`

**Alterar** — adicionar o comando na seção "Aplicação", após o comando `run` existente:

```diff
 run: ## Roda a API (http://localhost:5000 | https://localhost:5001)
 	dotnet run --project GoodHamburger.API

+run-blazor: ## Roda o frontend Blazor
+	dotnet run --project GoodHamburger.Blazor

 test: ## Roda todos os testes com output no console
```

> Atualizar também a linha `.PHONY` no topo para incluir `run-blazor`.

---

## Resumo de arquivos

| Ação | Arquivo |
|---|---|
| Modificar | `GoodHamburger.API/Extensions/ApiServiceExtensions.cs` |
| Modificar | `GoodHamburger.API/Program.cs` |
| Modificar | `GoodHamburger.Blazor/wwwroot/index.html` |
| Modificar | `GoodHamburger.Blazor/wwwroot/appsettings.json` |
| Modificar | `GoodHamburger.Blazor/Program.cs` |
| Modificar | `GoodHamburger.Blazor/Shared/NavMenu.razor` |
| Modificar | `GoodHamburger.Blazor/Shared/MainLayout.razor` |
| Modificar | `Makefile` |
| Criar | `GoodHamburger.Blazor/Models/OrderModels.cs` |
| Criar | `GoodHamburger.Blazor/Services/OrderService.cs` |
| Criar | `GoodHamburger.Blazor/Pages/Orders.razor` |
| Criar | `GoodHamburger.Blazor/Pages/CreateOrder.razor` |
| Criar | `GoodHamburger.Blazor/Pages/OrderDetail.razor` |
| Deletar | `GoodHamburger.Blazor/Pages/Counter.razor` |
| Deletar | `GoodHamburger.Blazor/Pages/FetchData.razor` |
| Deletar (opcional) | `GoodHamburger.Blazor/Shared/MainLayout.razor.css` |

---

## Verificações antes de implementar

- [ ] Confirmar porta HTTP da API em `GoodHamburger.API/Properties/launchSettings.json` (plano usa `5149`; ajustar `appsettings.json` se diferente)
- [ ] Confirmar porta do Blazor em `GoodHamburger.Blazor/Properties/launchSettings.json` após criação do projeto (plano usa `5173`; ajustar `BlazorPolicy` no backend se diferente)
- [ ] Confirmar que `GoodHamburger.slnx` aceita o comando `dotnet sln add` (formato `.slnx` é suportado a partir do .NET 9 SDK; verificar versão do SDK instalada)
