# StockEasy

Aplicação fullstack para gestão de produtos e movimentações de estoque, entregue como desafio técnico.

> A **API é agentic-friendly desde o MVP** — operationIds estáveis, errorCodes estruturados, hints dinâmicos, idempotência em mutations. Em v2, um chat com LLM consumirá esta API como tools.

## Stack

| Camada | Tecnologia |
|--------|------------|
| Backend | ASP.NET Core Web API (.NET 8 LTS) + Dapper + PostgreSQL 16 |
| Frontend | Vue 3 + Vite + TypeScript estrito + Tailwind CSS |
| Validação | FluentValidation (backend) · Vee-Validate + Zod (frontend) |
| Docs API | Swashbuckle.AspNetCore + Annotations (Swagger UI) |
| Container | Docker Compose (PG + .NET SDK + Node 20) |

A challenge spec original está preservada em [`README.challenge-spec.md`](README.challenge-spec.md).

## Como rodar (uma linha)

Pré-requisitos: `docker` + `docker compose` instalados.

```bash
docker compose up
```

Na primeira execução, o `docker compose` constrói as imagens (.NET SDK e Node), sobe o Postgres com o `init.sql` aplicado automaticamente, e starta o backend em modo hot reload (`dotnet watch run`) + frontend em modo dev (`vite`). Em ~30–60 segundos, três URLs estão disponíveis:

| Serviço | URL |
|---------|-----|
| Frontend (SPA Vue) | <http://localhost:5173> |
| Backend Swagger UI | <http://localhost:8080/swagger> |
| Health check | <http://localhost:8080/api/health> |
| Postgres (psql) | `psql -h localhost -p 5432 -U stockeasy -d stockeasy` (senha `stockeasy123`) |

Hot reload está ativo nos dois lados: editar `backend/Inventory/**/*.cs` rebuilda o assembly no container; editar `frontend/src/**` re-renderiza no navegador.

Para parar tudo:

```bash
docker compose down            # mantém o volume do Postgres
docker compose down -v         # remove o volume também (reaplica o init.sql na próxima subida)
```

## Estrutura do repositório

```
DesafioTecnico/
├── docker-compose.yml       # orquestra postgres + backend + frontend
├── init.sql                 # schema final (products + stock_movements + indexes)
├── README.md                # este arquivo
├── README.challenge-spec.md # spec original do desafio (preservado)
├── backend/
│   ├── Dockerfile           # SDK-only, dotnet watch
│   ├── Inventory.sln
│   ├── Inventory/           # API: Controllers/, Services/, Repositories/, Dtos/, Infra/, ...
│   └── Inventory.Tests/     # xUnit + Moq
├── frontend/
│   ├── Dockerfile           # node:20-alpine, vite dev server
│   ├── package.json
│   ├── vite.config.ts       # proxy /api → http://backend:8080
│   ├── tailwind.config.js   # paleta brand-50..900 + Inter
│   └── src/
│       ├── App.vue          # sidebar shell + <router-view/>
│       ├── shared/          # Axios client, AppShell, HealthPill
│       └── features/
│           ├── products/    # produtos (Phase 2)
│           └── stock/       # movimentações (Phase 3)
└── .planning/               # planejamento GSD (fases, contexto, decisions log)
```

A documentação detalhada (decisões de produto, arquitetura com diagramas Mermaid, regras de negócio + catálogo de errorCodes) chega na entrega final em `docs/dist/` (PDFs branded). Esta fase entrega a fundação para rodar.

## Status

| Fase | Entrega | Status |
|------|---------|--------|
| 1. Foundation | docker-compose end-to-end com health check | em entrega |
| 2. Products Vertical Slice | CRUD de produtos com soft delete + validação | próxima |
| 3. Stock Movements Vertical Slice | entradas/saídas com idempotência + saldo | depois |
| 4. Tests, Docs & Polish | xUnit + Vitest + 3 PDFs branded em `docs/dist/` | final |

## Variáveis de configuração

Todas estão no `docker-compose.yml` — não há `.env` a configurar para rodar localmente.

| Variável | Valor default | Descrição |
|----------|---------------|-----------|
| `ConnectionStrings__Postgres` | `Host=postgres;Database=stockeasy;Username=stockeasy;Password=stockeasy123` | Connection string da API; sobrescreve `appsettings.json`. |
| `ASPNETCORE_ENVIRONMENT` | `Development` | Habilita Swagger UI em `/swagger`. |
| `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` | `stockeasy` / `stockeasy123` / `stockeasy` | Credenciais do container Postgres. |

## Verificação rápida (smoke)

Com a stack rodando:

```bash
# Backend reachable + DB connected
curl -sS http://localhost:8080/api/health
# → {"status":"ok","db":"up","version":"...","timestamp":"..."}

# Swagger document
curl -sS http://localhost:8080/swagger/v1/swagger.json | head -c 200

# Frontend SPA shell
curl -sS http://localhost:5173/ | grep -E '<title>|app'
```

## Notas para o avaliador

- **Sem deploy externo, sem login.** Toda a aplicação roda local via `docker compose up`. Não há credenciais a configurar.
- **Hot reload é o modo dev e o modo demo.** A mesma compose serve para iterar código e para o avaliador rodar — não há `compose.prod.yml`.
- **Identifiers em inglês, mensagens ao usuário em português (BR).** Decisão consciente; documentada em `.planning/PROJECT.md` (D7).
- **Repositório de referência:** o backend espelha padrões de `/home/thallysrc/Projects/BancoShu/` (Dapper, FluentValidation, exception flow, DI). Exceção deliberada: não copiamos o N+1 em `TransferService.GetHistoryAsync`.
