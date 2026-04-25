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

## Decisões e o que ficou fora

**Blazor WebAssembly** foi escolhido por rodar inteiramente no browser e demonstrar claramente a separação entre frontend e backend — o Blazor consome a API via HTTP igual a qualquer outro cliente.

**Autenticação** está fora do escopo do desafio.

**Paginação no `GET /orders`** não foi solicitada. A estrutura com Specification Pattern já está preparada para suportar.

**GraphQL** listado como diferencial na vaga — não implementado por limitação de prazo. A API REST cobre todos os requisitos do desafio.

---

## Configuração

A connection string de desenvolvimento está em `GoodHamburger.API/appsettings.Development.json` já configurada para o banco local do Docker. A URL da API consumida pelo Blazor está em `GoodHamburger.Blazor/wwwroot/appsettings.json`. Nenhuma configuração adicional é necessária para rodar localmente.