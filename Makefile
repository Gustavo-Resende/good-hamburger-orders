SHELL := bash
.SHELLFLAGS := -c

.DEFAULT_GOAL := help

MIGRATION_NAME ?= InitialCreate

.PHONY: help setup run run-blazor test up down migrate migration build clean reset-db wait-db

# ── Ajuda ─────────────────────────────────────────────────────────────────────

help: ## Lista todos os comandos disponíveis
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) \
		| awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-12s\033[0m %s\n", $$1, $$2}'

# ── Docker ────────────────────────────────────────────────────────────────────

up: ## Sobe os containers Docker em background
	docker compose up -d --wait

down: ## Derruba os containers Docker
	docker compose down

# ── Banco ─────────────────────────────────────────────────────────────────────

wait-db: ## Garante containers no ar e aguarda Postgres saudável (healthcheck + compose up --wait)
	@echo "Aguardando PostgreSQL..."
	@docker compose up -d --wait
	@echo "PostgreSQL pronto."

migrate: ## Aplica migrations pendentes
	dotnet ef database update \
		--project GoodHamburger.Infrastructure \
		--startup-project GoodHamburger.API

migration: ## Cria uma nova migration (uso: make migration MIGRATION_NAME=NomeDaMigration)
	dotnet ef migrations add $(MIGRATION_NAME) \
		--project GoodHamburger.Infrastructure \
		--startup-project GoodHamburger.API

# ── Aplicação ─────────────────────────────────────────────────────────────────

build: ## Compila a solution
	dotnet build

run: ## Roda a API (http://localhost:5000 | https://localhost:5001)
	dotnet run --project GoodHamburger.API

run-blazor: ## Roda o frontend Blazor
	dotnet run --project GoodHamburger.Blazor

test: ## Roda todos os testes com output no console
	dotnet test --logger "console;verbosity=normal"

clean: ## Remove todas as pastas bin/ e obj/
	find . -type d \( -name "bin" -o -name "obj" \) -not -path "./.git/*" \
		-exec rm -rf {} + 2>/dev/null; true

# ── Fluxos compostos ──────────────────────────────────────────────────────────

setup: up wait-db migrate ## Ambiente inicial: Docker → aguarda banco → migrations (use `make run` para a API)

reset-db: ## Derruba containers + volumes, recria do zero e aplica migrations
	docker compose down -v
	$(MAKE) up
	$(MAKE) wait-db
	$(MAKE) migrate