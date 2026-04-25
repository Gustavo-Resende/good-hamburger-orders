# Good Hamburger Orders API

## Stack

- .NET 8 / ASP.NET Core
- PostgreSQL 16 + Docker
- EF Core (Code First, Migrations, Fluent API)
- MediatR, Ardalis.Result, Ardalis.Result.AspNetCore, Ardalis.GuardClauses, Ardalis.Repository

## Testes

- xUnit, FluentAssertions, NSubstitute

## Namespace raiz

`GoodHamburger`  
Padrão: `GoodHamburger.<Camada>.<Feature>`  
Exemplos: `GoodHamburger.Domain.Orders`, `GoodHamburger.Application.Orders.Commands`

## Arquitetura

Clean Architecture + DDD + CQRS  
Camadas: `Domain` → `Application` → `Infrastructure` → `API`

## Estrutura do Projeto

```
src/
 ├── Domain/
 ├── Application/
 ├── Infrastructure/
 └── API/
tests/
 ├── Domain.Tests/
 └── Application.Tests/
```

---

# Cardápio (fixo, in-memory)

| Id (GUID fixo) | Nome | Preço | Categoria |
|---|---|---|---|
| 11111111-1111-1111-1111-111111111111 | X-Burger | 5.00 | Sandwich |
| 22222222-2222-2222-2222-222222222222 | X-Egg | 4.50 | Sandwich |
| 33333333-3333-3333-3333-333333333333 | X-Bacon | 7.00 | Sandwich |
| 44444444-4444-4444-4444-444444444444 | Batata Frita | 2.00 | Side |
| 55555555-5555-5555-5555-555555555555 | Refrigerante | 2.50 | Drink |

---

# Regras de Negócio

- Pedido pode ter no máximo: 1 Sandwich, 1 Side, 1 Drink
- Item duplicado por categoria → erro
- Pedido não pode ser criado vazio
- Preço é snapshot no momento da criação

## Descontos

| Combinação | Desconto |
|---|---|
| Sandwich + Side + Drink | 20% |
| Sandwich + Drink | 15% |
| Sandwich + Side | 10% |
| Outros | 0% |

Sanduíche é obrigatório para qualquer desconto.

---

# Mensagens de Erro (constantes)

```csharp
public static class OrderErrors
{
    public const string EmptyOrder = "Pedido não pode ser criado sem itens.";
    public const string DuplicateCategory = "Já existe um item da categoria '{0}' neste pedido.";
}

public static class MenuErrors
{
    public const string ItemNotFound = "Item de cardápio não encontrado: '{0}'.";
}

public static class OrderQueryErrors
{
    public const string OrderNotFound = "Pedido não encontrado.";
}
```

---

# Domain Layer

## Estrutura

```
Domain/
 ├── Orders/
 │    ├── Order.cs
 │    ├── OrderItem.cs
 │    └── OrderErrors.cs
 ├── Menu/
 │    ├── MenuItem.cs
 │    └── MenuErrors.cs
 └── Common/
      ├── BaseEntity.cs
      └── ItemCategory.cs
```

## Enum

```csharp
public enum ItemCategory { Sandwich = 1, Side = 2, Drink = 3 }
```

## BaseEntity

```csharp
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    protected void SetUpdated() => UpdatedAt = DateTime.UtcNow;
}
```

Usado apenas em `Order`. Não aplicar em `OrderItem` nem `MenuItem`.

## MenuItem

```csharp
public class MenuItem
{
    public Guid Id { get; }
    public string Name { get; }
    public decimal Price { get; }
    public ItemCategory Category { get; }

    public MenuItem(Guid id, string name, decimal price, ItemCategory category)
    {
        Id = id; Name = name; Price = price; Category = category;
    }
}
```

## OrderItem

```csharp
public class OrderItem
{
    public Guid MenuItemId { get; private set; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }  // snapshot
    public ItemCategory Category { get; private set; }

    internal OrderItem(Guid menuItemId, string name, decimal price, ItemCategory category)
    {
        MenuItemId = menuItemId; Name = name; Price = price; Category = category;
    }
}
```

- Construtor `internal` — só `Order` cria `OrderItem`
- Sem `Id` público — PK gerenciada pelo EF via shadow property

## Order

```csharp
public class Order : BaseEntity
{
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public decimal Subtotal { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Total { get; private set; }

    private Order() { }  // para EF Core

    public static Result<Order> Create(List<MenuItem> menuItems) { ... }
    public Result AddItem(MenuItem menuItem) { ... }
    public Result ReplaceItems(List<MenuItem> menuItems) { ... }
    private void Recalculate() { ... }
}
```

### Create

```
1. Validar lista vazia → Result.Invalid(OrderErrors.EmptyOrder)
2. new Order()
3. Para cada menuItem → AddItem(menuItem); se Invalid propagar
4. Retornar Result.Success(order)
```

### AddItem

```
1. Guard.Against.Null(menuItem)
2. Se já existe item com mesma Category → Result.Invalid(DuplicateCategory formatado)
3. _items.Add(new OrderItem(menuItem.Id, menuItem.Name, menuItem.Price, menuItem.Category))
4. Recalculate()
5. SetUpdated()
6. Retornar Result.Success()
```

### ReplaceItems

```
1. Validar lista vazia → Result.Invalid(OrderErrors.EmptyOrder)
2. _items.Clear()
3. Para cada menuItem → AddItem(menuItem); se Invalid propagar
4. Retornar Result.Success()
```

### Recalculate

```
1. Subtotal = _items.Sum(x => x.Price)
2. bool hasSandwich = _items.Any(x => x.Category == Sandwich)
   bool hasSide     = _items.Any(x => x.Category == Side)
   bool hasDrink    = _items.Any(x => x.Category == Drink)
3. decimal pct = (hasSandwich, hasSide, hasDrink) switch
   {
       (true, true, true)  => 0.20m,
       (true, false, true) => 0.15m,
       (true, true, false) => 0.10m,
       _                   => 0.00m
   };
4. Discount = Subtotal * pct
5. Total = Subtotal - Discount
```

---

# Application Layer

## Estrutura

```
Application/
 ├── Orders/
 │    ├── Commands/
 │    │    ├── CreateOrder.cs    (Command + Handler)
 │    │    ├── UpdateOrder.cs    (Command + Handler)
 │    │    └── DeleteOrder.cs    (Command + Handler)
 │    ├── Queries/
 │    │    ├── GetOrderById.cs   (Query + Handler)
 │    │    └── GetOrders.cs      (Query + Handler)
 │    ├── DTOs/
 │    │    └── OrderDtos.cs
 │    └── Extensions/
 │         └── OrderMappings.cs
 ├── Interfaces/
 │    ├── Repositories/
 │    │    └── IOrderRepository.cs
 │    └── Services/
 │         └── IMenuService.cs
 ├── DependencyInjection/
 │    └── MediatRConfig.cs
 └── Common/
      └── GlobalUsings.cs
```

## DTOs

```csharp
public record CreateOrderRequest(List<Guid> MenuItemIds);
public record UpdateOrderRequest(List<Guid> MenuItemIds);

public record OrderItemDto(Guid MenuItemId, string Name, decimal Price, string Category);

public record CreateOrderResponse(Guid Id, List<OrderItemDto> Items,
    decimal Subtotal, decimal Discount, decimal Total, DateTime CreatedAt);

public record GetOrderByIdResponse(Guid Id, List<OrderItemDto> Items,
    decimal Subtotal, decimal Discount, decimal Total,
    DateTime CreatedAt, DateTime? UpdatedAt);

public record GetOrdersResponse(List<GetOrderByIdResponse> Orders);

public record UpdateOrderResponse(Guid Id, List<OrderItemDto> Items,
    decimal Subtotal, decimal Discount, decimal Total, DateTime? UpdatedAt);

public record MenuItemResponse(Guid Id, string Name, decimal Price, string Category);
public record GetMenuResponse(List<MenuItemResponse> Items);
```

## Interfaces

```csharp
public interface IOrderRepository : IRepository<Order> { }

public interface IMenuService
{
    MenuItem? GetById(Guid id);
    List<MenuItem> GetAll();
}
```

## Mappings

```csharp
// OrderMappings.cs — métodos de extensão estáticos
public static CreateOrderResponse ToCreateResponse(this Order order)
public static GetOrderByIdResponse ToGetByIdResponse(this Order order)
public static UpdateOrderResponse ToUpdateResponse(this Order order)
public static OrderItemDto ToDto(this OrderItem item)
public static MenuItemResponse ToResponse(this MenuItem item)
```

## Fluxo dos Handlers

### CreateOrderHandler

```
1. Validar MenuItemIds não vazio → Result.Invalid(EmptyOrder)
2. Para cada id → IMenuService.GetById(id)
   → null: Result.NotFound(ItemNotFound formatado)
3. Order.Create(menuItems) → se Invalid, propagar
4. IOrderRepository.AddAsync(order)
5. Result.Success(order.ToCreateResponse())
```

### GetOrderByIdHandler

```
1. IOrderRepository.GetByIdAsync(id)
   → null: Result.NotFound(OrderNotFound)
2. Result.Success(order.ToGetByIdResponse())
```

### GetOrdersHandler

```
1. IOrderRepository.ListAsync()
2. Result.Success(new GetOrdersResponse(orders.Select(x => x.ToGetByIdResponse()).ToList()))
```

### UpdateOrderHandler

```
1. IOrderRepository.GetByIdAsync(id)
   → null: Result.NotFound(OrderNotFound)
2. Validar MenuItemIds não vazio → Result.Invalid(EmptyOrder)
3. Para cada id → IMenuService.GetById(id)
   → null: Result.NotFound(ItemNotFound formatado)
4. order.ReplaceItems(menuItems) → se Invalid, propagar
5. IOrderRepository.UpdateAsync(order)
6. Result.Success(order.ToUpdateResponse())
```

### DeleteOrderHandler

```
1. IOrderRepository.GetByIdAsync(id)
   → null: Result.NotFound(OrderNotFound)
2. IOrderRepository.DeleteAsync(order)
3. Result.Success()
```

## MediatRConfig

```csharp
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(MediatRConfig).Assembly));
    return services;
}
```

## GlobalUsings

```csharp
global using MediatR;
global using Ardalis.Result;
global using Ardalis.GuardClauses;
```

---

# Infrastructure Layer

## Estrutura

```
Infrastructure/
 ├── Data/
 │    └── AppDbContext.cs
 ├── Configurations/
 │    ├── OrderConfiguration.cs
 │    └── OrderItemConfiguration.cs
 ├── Repositories/
 │    └── OrderRepository.cs
 ├── Services/
 │    └── MenuService.cs
 ├── Seed/
 │    └── MenuSeed.cs
 ├── Migrations/
 └── DependencyInjection/
      └── InfrastructureConfig.cs
```

## AppDbContext

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
```

## Schema do Banco

```
Orders
 ├── Id          uuid, PK
 ├── Subtotal    numeric(18,2), NOT NULL
 ├── Discount    numeric(18,2), NOT NULL
 ├── Total       numeric(18,2), NOT NULL
 ├── CreatedAt   timestamptz, NOT NULL
 └── UpdatedAt   timestamptz, nullable

OrderItems
 ├── Id          uuid, PK (shadow property — sem Id no domínio)
 ├── OrderId     uuid, FK → Orders.Id, CASCADE DELETE
 ├── MenuItemId  uuid, NOT NULL
 ├── Name        text, NOT NULL
 ├── Price       numeric(18,2), NOT NULL
 └── Category    int, NOT NULL
```

⚠️ PostgreSQL usa `numeric(18,2)`, não `decimal(18,2)`.

## OrderConfiguration

```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Subtotal).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Discount).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Total).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

## OrderItemConfiguration

```csharp
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.Property<Guid>("Id");
        builder.HasKey("Id");

        builder.Property(x => x.MenuItemId).IsRequired();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Price).HasColumnType("numeric(18,2)").IsRequired();
        builder.Property(x => x.Category).IsRequired();
    }
}
```

## OrderRepository

```csharp
public class OrderRepository : EfRepository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext dbContext) : base(dbContext) { }
}
```

## MenuSeed

```csharp
public static class MenuSeed
{
    public static readonly List<MenuItem> Items = new()
    {
        new MenuItem(Guid.Parse("11111111-1111-1111-1111-111111111111"), "X-Burger", 5.00m, ItemCategory.Sandwich),
        new MenuItem(Guid.Parse("22222222-2222-2222-2222-222222222222"), "X-Egg", 4.50m, ItemCategory.Sandwich),
        new MenuItem(Guid.Parse("33333333-3333-3333-3333-333333333333"), "X-Bacon", 7.00m, ItemCategory.Sandwich),
        new MenuItem(Guid.Parse("44444444-4444-4444-4444-444444444444"), "Batata Frita", 2.00m, ItemCategory.Side),
        new MenuItem(Guid.Parse("55555555-5555-5555-5555-555555555555"), "Refrigerante", 2.50m, ItemCategory.Drink),
    };
}
```

## MenuService

```csharp
public class MenuService : IMenuService
{
    private readonly List<MenuItem> _items = MenuSeed.Items;
    public MenuItem? GetById(Guid id) => _items.FirstOrDefault(x => x.Id == id);
    public List<MenuItem> GetAll() => _items;
}
```

Registrar como **Singleton**.

## InfrastructureConfig

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

    services.AddScoped<IOrderRepository, OrderRepository>();
    services.AddSingleton<IMenuService, MenuService>();

    return services;
}
```

## docker-compose.yml

```yaml
services:
  postgres:
    image: postgres:16
    container_name: good-hamburger-db
    environment:
      POSTGRES_DB: goodhamburger
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
```

## Connection String (não commitar credenciais reais — usar user-secrets)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=goodhamburger;Username=postgres;Password=postgres"
  }
}
```

---

# API Layer

## Estrutura

```
API/
 ├── Controllers/
 │    ├── OrdersController.cs
 │    └── MenuController.cs
 ├── ExceptionHandling/
 │    └── GlobalExceptionHandler.cs
 └── Extensions/
      └── ApiServiceExtensions.cs
```

## Padrão dos Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    public OrdersController(IMediator mediator) => _mediator = mediator;
}
```

## Endpoints

```
POST   /api/orders       → CreateOrderCommand   → 201 / 400 / 422
GET    /api/orders        → GetOrdersQuery       → 200
GET    /api/orders/{id}   → GetOrderByIdQuery    → 200 / 404
PUT    /api/orders/{id}   → UpdateOrderCommand   → 200 / 404 / 422
DELETE /api/orders/{id}   → DeleteOrderCommand   → 204 / 404
GET    /api/menu          → IMenuService.GetAll  → 200
```

## Conversão Result → HTTP

| Result | HTTP |
|---|---|
| `Result.Success()` | 200 |
| `Result.Created()` | 201 |
| `Result.NotFound()` | 404 |
| `Result.Invalid()` | 422 |
| `Result.Error()` | 400 |

- `[TranslateResultToActionResult]` → mapeamento em runtime
- `[ProducesResponseType]` → documentação Swagger (usar juntos, papéis distintos)

## Exemplo de action

```csharp
[HttpPost]
[TranslateResultToActionResult]
[ProducesResponseType(typeof(CreateOrderResponse), 201)]
[ProducesResponseType(400)]
[ProducesResponseType(422)]
public async Task<Result<CreateOrderResponse>> Create([FromBody] CreateOrderRequest request)
{
    var command = new CreateOrderCommand(request.MenuItemIds);
    return await _mediator.Send(command);
}
```

## ApiServiceExtensions

```csharp
public static IServiceCollection AddApiServices(this IServiceCollection services)
{
    services.AddControllers(options => options.AddDefaultResultConvention());
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();
    services.AddExceptionHandler<GlobalExceptionHandler>();
    return services;
}
```

## GlobalExceptionHandler

- Implementa `IExceptionHandler` (nativo .NET 8)
- Retorna HTTP 500 sem expor stack interna
- Ativado com `app.UseExceptionHandler()` sem argumento

## Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
```

---

# Tests

## Estrutura

```
tests/
 ├── Domain.Tests/
 │    └── Orders/
 │         ├── OrderTests.cs
 │         └── DiscountTests.cs
 └── Application.Tests/
      └── Orders/
           ├── CreateOrderHandlerTests.cs
           └── GetOrderByIdHandlerTests.cs
```

## Domain.Tests — casos obrigatórios

```
OrderTests:
 ✓ Criar pedido vazio → Result.Invalid
 ✓ Criar pedido com 1 item → sucesso
 ✓ Adicionar dois itens da mesma categoria → Result.Invalid
 ✓ Subtotal calculado corretamente
 ✓ ReplaceItems substitui todos os itens

DiscountTests:
 ✓ Sandwich + Side + Drink → 20%
 ✓ Sandwich + Drink        → 15%
 ✓ Sandwich + Side         → 10%
 ✓ Sem Sandwich            → 0%
 ✓ Total = Subtotal - Discount
```

## Application.Tests — casos obrigatórios

```
CreateOrderHandlerTests:
 ✓ MenuItemId não encontrado → Result.NotFound
 ✓ Lista vazia de ids → Result.Invalid
 ✓ Pedido válido → AddAsync chamado + Result.Success
 ✓ Duplicata de categoria propaga Result.Invalid do domínio

GetOrderByIdHandlerTests:
 ✓ Id inexistente → Result.NotFound
 ✓ Id existente → Result.Success com dados corretos
```

## Padrão de mock

```csharp
var repository = Substitute.For<IOrderRepository>();
var menuService = Substitute.For<IMenuService>();
```

---

# GoodHamburger.Blazor — Frontend

## Stack

- .NET 8 / Blazor WebAssembly
- Bootstrap 5 (via CDN no index.html)
- HttpClient para consumo da API

## Namespace raiz

`GoodHamburger.Blazor`

## Projeto na solution

Adicionar ao `GoodHamburger.sln`:
```bash
dotnet new blazorwasm -n GoodHamburger.Blazor -o GoodHamburger.Blazor -f net8.0
dotnet sln add GoodHamburger.Blazor
```

---

# Estrutura

```
GoodHamburger.Blazor/
 ├── Pages/
 │    ├── Orders.razor          ← listagem de pedidos
 │    ├── CreateOrder.razor     ← criar pedido
 │    └── OrderDetail.razor     ← detalhe + deletar
 │
 ├── Services/
 │    └── OrderService.cs       ← chamadas HTTP para a API
 │
 ├── Models/
 │    └── OrderModels.cs        ← DTOs do frontend
 │
 ├── Shared/
 │    ├── MainLayout.razor      ← layout base
 │    └── NavMenu.razor         ← navegação
 │
 ├── wwwroot/
 │    ├── index.html            ← entry point — adicionar Bootstrap aqui
 │    └── appsettings.json      ← URL da API
 │
 └── Program.cs
```

---

# Configuração

## wwwroot/appsettings.json

```json
{
  "ApiBaseUrl": "http://localhost:5149"
}
```

## Program.cs

```csharp
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

## wwwroot/index.html — adicionar Bootstrap no <head>

```html
<link rel="stylesheet"
      href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" />
```

---

# Models

## OrderModels.cs

```csharp
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

# OrderService

## Services/OrderService.cs

```csharp
public class OrderService
{
    private readonly HttpClient _http;

    public OrderService(HttpClient http)
    {
        _http = http;
    }

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

        var errorContent = await response.Content.ReadAsStringAsync();
        return (null, $"Erro ao criar pedido: {response.StatusCode}");
    }

    public async Task<(bool Success, string? Error)> DeleteOrderAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/orders/{id}");

        if (response.IsSuccessStatusCode)
            return (true, null);

        return (false, $"Erro ao deletar pedido: {response.StatusCode}");
    }
}
```

---

# Pages

## Orders.razor — listagem

**Rota:** `/orders`

**Comportamento:**
- Ao carregar: busca todos os pedidos via `OrderService.GetAllOrdersAsync()`
- Exibe tabela com: Id (truncado para 8 chars), Total, Desconto, Data de criação
- Botão "Ver" por linha → navega para `/orders/{id}`
- Botão "Deletar" por linha → chama `DeleteOrderAsync`, exibe erro em texto se falhar, atualiza a lista
- Botão "Novo Pedido" no topo → navega para `/orders/new`
- Estado de carregando: `<p>Carregando...</p>`
- Erro: `<p class="text-danger">@errorMessage</p>`

```razor
@page "/orders"
@inject OrderService OrderService
@inject NavigationManager Navigation

@* UI com Bootstrap:
   - container > h1 + botão "Novo Pedido" no topo
   - table table-striped com colunas: Id, Total, Desconto, Data, Ações
   - botões: btn btn-sm btn-outline-primary (Ver) e btn btn-sm btn-danger (Deletar)
*@

@code {
    private List<OrderResponse> orders = new();
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        orders = await OrderService.GetAllOrdersAsync();
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

## CreateOrder.razor — criar pedido

**Rota:** `/orders/new`

**Comportamento:**
- Ao carregar: busca cardápio via `OrderService.GetMenuAsync()`
- Exibe os itens do cardápio agrupados por categoria (Sanduíches, Acompanhamentos, Bebidas)
- Cada item tem botão "Adicionar" → item entra na lista de selecionados
- Lista de selecionados exibe nome e preço de cada item adicionado
- Botão "Remover" por item na lista de selecionados
- Regra visual: se já existe um item da mesma categoria na lista, o botão "Adicionar" dos outros da mesma categoria fica desabilitado (`disabled`)
- Botão "Criar Pedido" → chama `CreateOrderAsync` com os Ids selecionados
- Sucesso → redireciona para `/orders/{id}` do pedido criado
- Erro → exibe `<p class="text-danger">@errorMessage</p>`
- Botão "Cancelar" → volta para `/orders`

```razor
@page "/orders/new"
@inject OrderService OrderService
@inject NavigationManager Navigation

@* UI com Bootstrap:
   - duas colunas: cardápio à esquerda, itens selecionados à direita
   - cardápio: cards por categoria com btn btn-sm btn-outline-success (Adicionar)
   - selecionados: lista com btn btn-sm btn-danger (Remover) por item
   - rodapé: btn btn-primary (Criar Pedido) e btn btn-secondary (Cancelar)
*@

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

## OrderDetail.razor — detalhe

**Rota:** `/orders/{Id:guid}`

**Comportamento:**
- Ao carregar: busca pedido pelo Id via `OrderService.GetOrderByIdAsync(Id)`
- Se não encontrar: exibe `<p class="text-danger">Pedido não encontrado.</p>`
- Exibe: lista de itens com nome e preço, subtotal, desconto, total
- Desconto exibido como: valor em R$ e percentual (ex: "R$ 1,90 (20%)")
- Botão "Deletar Pedido" → chama `DeleteOrderAsync`, redireciona para `/orders`
- Botão "Voltar" → navega para `/orders`
- Erro: `<p class="text-danger">@errorMessage</p>`

```razor
@page "/orders/{Id:guid}"
@inject OrderService OrderService
@inject NavigationManager Navigation

@* UI com Bootstrap:
   - card com card-body
   - table com itens do pedido
   - rodapé do card: subtotal, desconto (valor + %), total em negrito
   - botões: btn btn-danger (Deletar) e btn btn-secondary (Voltar)
*@

@code {
    [Parameter] public Guid Id { get; set; }

    private OrderResponse? order;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        order = await OrderService.GetOrderByIdAsync(Id);
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

# NavMenu.razor

Links de navegação:
- "Good Hamburger" → `/`
- "Pedidos" → `/orders`
- "Novo Pedido" → `/orders/new`

---

# MainLayout.razor

Layout padrão Bootstrap:
- Navbar no topo com o nome da aplicação e links
- Área de conteúdo centralizada com `container`

---

# CORS no Backend

O backend precisa aceitar requisições do Blazor (porta diferente).

Adicionar no `GoodHamburger.API`:

## ApiServiceExtensions.cs — adicionar CORS

```csharp
services.AddCors(options =>
{
    options.AddPolicy("BlazorPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:7173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

## Program.cs — ativar CORS antes do MapControllers

```csharp
app.UseCors("BlazorPolicy");
app.MapControllers();
```

---

# Makefile — adicionar comando

```makefile
run-blazor: ## Roda o frontend Blazor
	dotnet run --project GoodHamburger.Blazor
```

---

# Fluxo das telas

```
/ (home)
    └── /orders (listagem)
         ├── Botão "Novo Pedido" → /orders/new
         │    └── Criar com sucesso → /orders/{id}
         │                               └── Deletar → /orders
         └── Botão "Ver" → /orders/{id}
              └── Deletar → /orders
```

---

