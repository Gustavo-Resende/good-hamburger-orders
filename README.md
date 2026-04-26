# Good Hamburger — Orders API

API REST para criar e gerir pedidos de hambúrguer com cálculo automático de descontos por combinação de categorias, persistência em PostgreSQL e arquitetura em camadas (Domain → Application → Infrastructure → API). Inclui frontend em **Blazor WebAssembly** consumindo a API.

---

## Para quem vai avaliar o código

```bash
make setup       # sobe Docker, aguarda banco, aplica migrations
make run         # sobe a API (terminal 1)
make run-blazor  # sobe o frontend (terminal 2)
make test        # roda todos os testes
```

| Interface | URL |
|---|---|
| Swagger (API) | http://localhost:5149/swagger |
| Frontend Blazor | http://localhost:5192 |
| pgAdmin | http://localhost:5050 — `admin@goodhamburger.com` / `admin` |

---

## Pré-requisitos

| Ferramenta | Verificar |
|---|---|
| [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) | `dotnet --version` → 9.x ou superior |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | `docker --version` |
| GNU Make + Bash | `make --version` — no Windows use Git Bash ou WSL |
| EF Core CLI | `dotnet tool install --global dotnet-ef` |

> O projeto usa `.slnx` (formato novo de solution), que requer SDK 9 ou superior. A versão do SDK está fixada em `global.json` na raiz.

---

## Rodando o projeto

### Com Make

```bash
# Terminal 1 — backend
make setup   # sobe Docker → aguarda banco → aplica migrations
make run     # sobe a API

# Terminal 2 — frontend
make run-blazor
```

### Sem Make

```bash
# Banco
docker compose up -d --wait
dotnet ef database update --project GoodHamburger.Infrastructure --startup-project GoodHamburger.API

# API (terminal 1)
dotnet run --project GoodHamburger.API

# Blazor (terminal 2)
dotnet run --project GoodHamburger.Blazor
```

---

## Serviços Docker

| Serviço | Porta | Acesso |
|---|---|---|
| PostgreSQL | 5432 | user `postgres` / senha `postgres` / banco `goodhamburger` |
| pgAdmin | 5050 | http://localhost:5050 — email `admin@goodhamburger.com` / senha `admin` |

---

## Comandos Make

| Comando | O que faz |
|---|---|
| `make help` | Lista todos os comandos disponíveis |
| `make setup` | Fluxo completo: Docker → banco → migrations |
| `make run` | Sobe a API |
| `make run-blazor` | Sobe o frontend Blazor |
| `make test` | Roda todos os testes |
| `make up` | Sobe os containers Docker |
| `make down` | Derruba os containers (mantém dados) |
| `make migrate` | Aplica migrations pendentes |
| `make migration MIGRATION_NAME=Nome` | Cria uma nova migration |
| `make build` | Compila a solution |
| `make clean` | Remove pastas `bin/` e `obj/` |
| `make reset-db` | Derruba containers + apaga dados + recria tudo do zero |

---

## Stack

| Camada | Tecnologias |
|---|---|
| Runtime | .NET 8, C# |
| API | ASP.NET Core, Swagger (Swashbuckle), Ardalis.Result |
| Application | MediatR (CQRS), DTOs, Handlers |
| Domain | Entidades ricas, regras de pedido e desconto |
| Dados | EF Core 8, Npgsql, PostgreSQL 16 |
| Frontend | Blazor WebAssembly, Bootstrap 5 |
| Testes | xUnit, FluentAssertions, NSubstitute |

---

## Estrutura do projeto

```
GoodHamburger.Domain             # entidades, enums, erros de domínio
GoodHamburger.Application        # interfaces, MediatR, casos de uso
GoodHamburger.Infrastructure     # EF Core, repositórios, seed do menu
GoodHamburger.API                # controllers, pipeline HTTP, Swagger
GoodHamburger.Blazor             # frontend Blazor WebAssembly
GoodHamburger.Domain.Tests       # testes das regras de domínio
GoodHamburger.Application.Tests  # testes dos handlers com mocks
```

---

## Decisões de arquitetura

### Clean Architecture + DDD

O projeto está dividido em quatro camadas com dependências que fluem para dentro — Domain não conhece nada, Application conhece só o Domain, Infrastructure implementa as interfaces definidas pela Application, API orquestra tudo. Essa separação garante que regras de negócio nunca dependam de detalhes de infraestrutura como banco ou framework.

O domínio usa o modelo rico — `Order` é o Aggregate Root e concentra toda a lógica de validação, adição de itens e cálculo de desconto. `OrderItem` é criado exclusivamente pelo `Order` via construtor `internal`, impedindo que outras camadas montem itens inconsistentes.

### CQRS com MediatR

Commands alteram estado, Queries apenas leem. Cada caso de uso tem um arquivo próprio com Command/Query e seu Handler correspondente. O MediatR desacopla a API dos handlers — o controller só conhece `IMediator`, não os handlers diretamente.

### Result Pattern (Ardalis.Result)

Em vez de lançar exceptions para controle de fluxo, todos os handlers retornam `Result<T>`. Erros esperados (pedido não encontrado, item duplicado, lista vazia) são representados como `Result.NotFound()` ou `Result.Invalid()`. O `[TranslateResultToActionResult]` converte automaticamente para os status HTTP corretos. Exceptions genuínas são capturadas pelo `GlobalExceptionHandler` e retornam HTTP 500 sem expor detalhes internos.

### Cardápio in-memory com GUIDs determinísticos

O cardápio é fixo por definição do desafio. Implementado como lista estática com GUIDs fixos em `MenuSeed`, exposto via `MenuService` registrado como Singleton. GUIDs determinísticos garantem consistência entre execuções e facilitam testes — é possível referenciar um item do cardápio pelo GUID conhecido sem consultar o banco.

### Shadow Property no OrderItem

`OrderItem` não expõe `Id` público — a PK da tabela `OrderItems` é gerenciada exclusivamente pelo EF Core via shadow property. Essa decisão mantém o domínio limpo: `OrderItem` não existe fora de um `Order` e nunca será buscado diretamente por ID, então expor esse detalhe de infraestrutura no domínio seria contaminação desnecessária.

### Snapshot de preço

O preço dos itens é copiado para o `OrderItem` no momento da criação do pedido. Alterações futuras no cardápio não afetam pedidos já realizados — comportamento correto para qualquer sistema de pedidos.

### Tabela separada para OrderItems (vs Owned Entity)

EF Core suporta Owned Entities para mapear coleções sem tabela própria, mas a abordagem tem limitações conhecidas em coleções — queries menos previsíveis, dificuldade com cascata, comportamento inconsistente entre versões. A decisão foi usar tabela separada com FK explícita e `OnDelete(Cascade)`, mais previsível e extensível.

### Blazor WebAssembly

Escolhido por rodar inteiramente no browser como SPA, demonstrando separação clara entre frontend e backend. O Blazor consome a API via `HttpClient` igual a qualquer outro cliente. A URL da API é configurada via `wwwroot/appsettings.json`, sem hardcode no código.

---

## O que ficou fora

**Autenticação e autorização** — fora do escopo do desafio. A estrutura de camadas facilita adição futura via middleware no pipeline da API.

**Paginação no `GET /orders`** — não solicitada. O Specification Pattern já está em uso no projeto e suporta paginação com alteração mínima no handler.

**GraphQL** — listado como diferencial na vaga, não implementado por limitação de prazo. A camada de Application com handlers independentes facilita a exposição via Hot Chocolate sem alterar regras de negócio.

**Testes de integração** — apenas testes unitários de Domain e Application foram implementados, cobrindo as regras de negócio e os handlers com mocks. Testes de integração com banco real ficariam em um projeto separado `GoodHamburger.Integration.Tests`.

---

## Configuração

A connection string de desenvolvimento está em `GoodHamburger.API/appsettings.Development.json` já configurada para o banco local do Docker. A URL da API consumida pelo Blazor está em `GoodHamburger.Blazor/wwwroot/appsettings.json`. Nenhuma configuração adicional é necessária para rodar localmente.
