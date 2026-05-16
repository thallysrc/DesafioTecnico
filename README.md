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
| 1. Foundation | docker-compose end-to-end com health check | entregue |
| 2. Products Vertical Slice | CRUD de produtos com soft delete + validação | entregue |
| 3. Stock Movements Vertical Slice | entradas/saídas com idempotência + saldo | entregue |
| 4. Tests, Docs & Polish | xUnit + Vitest + 3 PDFs branded em `docs/dist/` | final |

## Endpoints (v1)

Após `docker compose up`, a API expõe:

| Método | Caminho | Descrição |
|--------|---------|-----------|
| GET | `http://localhost:8080/api/health` | Health probe (status + Postgres connectivity). |
| POST | `http://localhost:8080/api/products` | Cadastra novo produto (`operationId: createProduct`). |
| GET | `http://localhost:8080/api/products` | Lista paginada (params `page`, `pageSize`, `includeDeleted`) — `operationId: listProducts`. |
| GET | `http://localhost:8080/api/products/{id}` | Detalha produto (retorna mesmo se soft-deletado) — `operationId: getProduct`. |
| DELETE | `http://localhost:8080/api/products/{id}` | Soft-delete (preserva histórico) — `operationId: deleteProduct`. |
| POST | `http://localhost:8080/api/stock-movements` | Registra movimentação de estoque (Entrada ou Saída). **Requer header `Idempotency-Key: <UUID v4>`** — replay com mesma chave retorna `200 OK` + header `Idempotency-Replay: true` + body byte-idêntico ao da primeira resposta. `operationId: createStockMovement`. |
| GET | `http://localhost:8080/api/stock-movements` | Histórico paginado com filtros opcionais `productId`, `startDate`, `endDate`, `page`, `pageSize` (padrão 30, máx 100). Cada item já carrega `productCode` + `productDescription` via JOIN (zero N+1 — exatamente 2 statements SQL por página). `operationId: listStockMovements`. |
| GET | `http://localhost:8080/api/stock-movements/{id}` | Detalha movimento. `operationId: getStockMovement`. |

## Agentic-friendly API

Toda resposta segue o padrão pensado pra consumo por LLM:

- **`operationId` estável camelCase verb-noun** — `createProduct`, `listProducts`, `getProduct`, `deleteProduct`, `createStockMovement`, `listStockMovements`, `getStockMovement`. Estáveis entre versões — vira tool name no chat v2.
- **`errorCode` SCREAMING_SNAKE_CASE com vocabulário fechado** — `VALIDATION_ERROR`, `DUPLICATE_CODE`, `PRODUCT_NOT_FOUND`, `INTERNAL_ERROR` + Phase 3: `MISSING_IDEMPOTENCY_KEY`, `INSUFFICIENT_BALANCE`, `PRODUCT_DELETED`, `INVALID_MOVEMENT_VALUES`, `MOVEMENT_NOT_FOUND`.
- **Idempotência obrigatória em mutações de movimento** — `POST /api/stock-movements` exige header `Idempotency-Key` (UUID v4). Replay com a mesma chave retorna `200 OK` + header `Idempotency-Replay: true` + body byte-idêntico ao da primeira resposta — sem efeito colateral duplicado, mesmo sob race-condition (recovery via `pg_unique_violation` SqlState `23505`).
- **Movimentações são imutáveis** (MOVE-11) — `PUT /api/stock-movements/{id}` e `DELETE /api/stock-movements/{id}` retornam `405 Method Not Allowed`. Histórico é auditoria.
- **Zero N+1 em histórico** — `GET /api/stock-movements?pageSize=100` emite **exatamente 2 statements SQL** (um JOIN'd SELECT + um COUNT), independentemente do `pageSize`. Items já carregam `productCode` + `productDescription` via JOIN.
- **`hint` dinâmica em PT-BR**, construída com dados reais do erro (não genérica). Ex.: para código duplicado, a hint cita o código conflitante.
- **HATEOAS `_links`** em respostas de recurso (`self`, `delete` quando aplicável) e em listagens (`self`, `first`, `last`, `next`, `prev`).
- **Enums serializados como string** (`"Electronic"`, não `0`) via `JsonStringEnumConverter` global.
- **Soft-delete** com `deleted_at` — produtos deletados ficam recuperáveis via `GET /api/products/{id}` e listáveis com `?includeDeleted=true`. Códigos NÃO são reutilizáveis (anti-reuso por UNIQUE total).
- **OpenAPI rica** em `http://localhost:8080/swagger` — XML doc em cada campo, `[ProducesResponseType]` para cada status code (201/200/204/400/404/422).

Exemplo de erro canônico:

```json
{
  "errorCode": "DUPLICATE_CODE",
  "category": "BUSINESS_RULE",
  "message": "Já existe um produto com código 'P001'.",
  "hint": "Já existe um produto com código 'P001'. Use outro código ou recupere o produto via /api/products?includeDeleted=true.",
  "statusCode": 422,
  "retryable": false,
  "details": { "code": "P001" },
  "traceId": "...",
  "timestamp": "2026-05-16T..."
}
```

## Frontend (v1)

`http://localhost:5173/products`:

- Lista paginada com 4 estados (loading skeleton / empty CTA / error com retry / dados).
- Cadastro em **side drawer** com Vee-Validate + Zod (mensagens PT-BR espelhadas byte-for-byte do FluentValidation no backend).
- Detalhe + **soft-delete** via modal de confirmação (CONF-02, copy locked: "Excluir produto?" — "Esta ação marca o produto como excluído. O histórico de movimentações permanece visível.").
- Toggle **"Mostrar excluídos"** reabre produtos arquivados (badge `Excluído`).
- Formatação BR de moeda (`R$ 1.234,56`), data (`dd/mm/yyyy HH:mm`) e quantidade.
- Toasts consomem `apiError.hint ?? apiError.message` (D-09) — erros de rede surgem como `"Não foi possível conectar. Verifique sua conexão e tente novamente."`.

`http://localhost:5173/stock-movements?tab=entrada|saida|historico` (default `historico`):

- **Tab strip composta inline** com WAI-ARIA tabs (`role="tablist"` + roving `tabindex` + Arrow/Home/End/Enter/Space) e URL `?tab=` como fonte da verdade — navegação por teclado conforme spec. Painéis fecham com `v-if` (não `v-show`) para garantir que validators e fetches só rodem na aba ativa.
- **Entrada** vai direto (sem confirmação) — atualiza `stock_quantity` e `supplier_value` na mesma transação (MOVE-01 + MOVE-06).
- **Saída** abre **modal de confirmação CONF-01** com resumo completo (Produto / Quantidade / Valor de venda / Saldo atual / **Saldo resultante**) — autofocus em "Cancelar" (botão seguro), Confirmar é brand-primary (NÃO destrutivo).
- **Histórico** mostra tabela paginada com 4 estados (loading / empty / filter-empty / error / dados) + filtros `productId` (300 ms debounce), `startDate`, `endDate` sincronizados com a URL. Tabela é **não-interativa** (sem linha clicável) — movimentos são imutáveis (MOVE-11; sem `PUT`, `DELETE`, `PATCH`).
- **Idempotência transparente**: cada `POST /api/stock-movements` envia `Idempotency-Key` (UUID v4 gerado pelo cliente via `crypto.randomUUID()`) — se o servidor retornar `200 OK` + `Idempotency-Replay: true`, a UI trata como sucesso normal (D-07; sem banner de "você já enviou").
- **Helper Disponível** em tempo real no formulário de Saída — surface do saldo do produto selecionado, com `aria-live="polite"` para anunciar mudanças. Se a API responder `INSUFFICIENT_BALANCE`, o helper se atualiza a partir de `apiError.details.available` (D-08 race-mitigation).

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
