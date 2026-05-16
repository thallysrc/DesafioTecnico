# StockEasy

## What This Is

**StockEasy** é uma aplicação fullstack para gestão de produtos e movimentações de estoque (entradas e saídas), entregue como parte de um desafio técnico de processo seletivo. Backend em **ASP.NET Core Web API (.NET 8 + Dapper)** e frontend em **Vue 3 + TypeScript + Tailwind** consumindo a API, com regras de negócio como validação de saldo insuficiente e histórico imutável.

A API é projetada **agentic-friendly** desde o MVP — operationIds estáveis, errorCodes estruturados, hints dinâmicos de recuperação, idempotência em mutations sensíveis. Em v2 (futuro), um chat com LLM consumirá essa API como tools.

Visualmente, o produto é parte da família **RoboteAsy** (paleta azul corporativa).

## Core Value

Demonstrar competência fullstack através de uma implementação **limpa, testada, organizada, e agentic-ready** que cumpre integralmente o spec do desafio — porque o objetivo final é passar na avaliação técnica.

## Requirements

### Validated

<!-- Shipped and confirmed valuable. -->

**Validated in Phase 1: Foundation (2026-05-16)** — scaffolding only, no business capability yet:
- Stack & arquitetura wired: .NET 8 + Dapper + PostgreSQL 16 (single-project N-tier `Inventory/` + `Inventory.Tests/`), Vue 3 + Vite + TS strict + Tailwind (feature-based `src/features/{products,stock}/`)
- Cross-cutting plumbing live but inactive: `ExceptionHandlingMiddleware` shell, FluentValidation pipeline registered, Swagger + `JsonStringEnumConverter`, CORS named policy `"Frontend"`, Axios `baseURL: '/api'` + Vite proxy → `http://backend:8080`
- Brand identity rendering: paleta `brand-50..900` (#1863DC), Inter via Google Fonts, wordmark `Stock<span class="text-brand-500">Easy</span>`, sidebar light com `lucide-vue-next`
- Schema autoritativo em `init.sql`: `products` (com `deleted_at`, `code UNIQUE` sem filtro — anti-reuso) + `stock_movements` (com `idempotency_key NOT NULL UNIQUE`) — não precisa tocar em Phase 2/3
- Infraestrutura: `docker-compose up` boots PG + .NET API + Vue SPA com hot reload; `GET /api/health` prova end-to-end; root README guia o avaliador

### Active

<!-- Current scope. Building toward these. Detalhamento granular vive em REQUIREMENTS.md. -->

**Domínio — Produto:**
- [ ] Cadastrar produto (código único, descrição, tipo, valor fornecedor, qtd inicial)
- [ ] Listar produtos com paginação (default 30/página, max 100)
- [ ] Detalhar produto por ID
- [ ] Soft delete de produto (`DELETE /api/products/{id}` marca `deleted_at`)
- [ ] Listagem filtra deletados por padrão; `?includeDeleted=true` retorna todos

**Domínio — Movimentação de Estoque:**
- [ ] Registrar entrada (`Inbound`) — aumenta saldo, atualiza supplier_value
- [ ] Registrar saída (`Outbound`) — valida saldo suficiente, registra sale_value
- [ ] Listar histórico paginado com filtros (productId, startDate, endDate)
- [ ] Idempotency-Key **required** em POST de movimentos
- [ ] Movimentos com produto soft-deleted são rejeitados (`PRODUCT_DELETED`)
- [ ] Histórico de produto deletado permanece acessível

**Backend — Arquitetura & qualidade:**
- [ ] Arquitetura single-project N-tier (estilo BancoShu): `Inventory/` + `Inventory.Tests/`
- [ ] PostgreSQL 16 com Dapper (sem EF Core), `init.sql` versionado
- [ ] FluentValidation cobre 100% do input validation
- [ ] Exception flow centralizado: `DomainException` → `ExceptionHandlingMiddleware`
- [ ] **Zero N+1 queries** — JOIN ou batch fetch (`WHERE id IN @ids`)
- [ ] Transações explícitas em operações multi-step (movimentos)
- [ ] `SELECT ... FOR UPDATE` em read-modify-write de saldo

**API — Agentic-friendly:**
- [ ] Enums serializados como string (`JsonStringEnumConverter`)
- [ ] OperationIds estáveis camelCase verb-noun (`createProduct`, `registerStockMovement`)
- [ ] XML docs em controllers/endpoints/DTOs/cada campo → OpenAPI rico
- [ ] `ErrorResponse` canônico: `{ errorCode, category, message, hint, statusCode, retryable, details, traceId, timestamp }`
- [ ] Categorias fechadas: `VALIDATION`, `BUSINESS_RULE`, `NOT_FOUND`, `INTERNAL`
- [ ] Hints **dinâmicos por contexto** (Service constrói com dados reais)
- [ ] `_links` em responses de recursos (self + related actions)
- [ ] Pacote `Swashbuckle.AspNetCore.Annotations`
- [ ] Swagger UI exposto em `/swagger`

**Frontend — Stack & UX:**
- [ ] Vue 3 + TypeScript strict + Vite + Tailwind
- [ ] Composables only (sem Pinia)
- [ ] Axios com interceptor que normaliza erros pra `ApiError`
- [ ] Vee-Validate + Zod (schemas espelham FluentValidation)
- [ ] Feature-based: `src/features/products/` + `src/features/stock/`
- [ ] Paleta brand-* derivada do RoboteAsy (`#1863DC` primary)
- [ ] Fonte Inter via Google Fonts
- [ ] Wordmark `Stock<span class="text-brand-500">Easy</span>`
- [ ] Sidebar light (`bg-white`, item ativo `bg-brand-50 text-brand-700`)

**Frontend — Heurísticas de Nielsen aplicadas:**
- [ ] **#1 Visibilidade**: 4 estados em toda lista/form (loading skeleton / empty / error / success)
- [ ] **#2 Mundo real**: formatação BR via `Intl` (`R$`, dd/mm/yyyy, separador milhar) + labels traduzidos centralizados em `src/shared/labels.ts`
- [ ] **#3 Controle e liberdade**: modais com Cancel sempre, Esc fecha, warn em sair com unsaved changes
- [ ] **#4 Consistência**: primário direita, destrutivo `bg-danger`, labels acima de inputs
- [ ] **#5 Prevenção**: validação inline `onBlur`, submit desabilitado com erros, saldo visível no form de saída
- [ ] **#6 Reconhecimento**: searchable dropdown pra produtos com `code — description`
- [ ] **#8 Minimalista**: 1 ação primária por tela, whitespace generoso
- [ ] **#9 Recuperação**: toast de erro usa `apiError.hint` (mapping API → UI helper text)
- [ ] **#10 A11y básico**: semantic HTML, labels associados, focus ring `focus:ring-2`, contraste, cor não como único sinal

**Frontend — Confirmações:**
- [ ] Modal de confirmação em **saída de estoque** (resumo qty/valor/saldo resultante)
- [ ] Modal de confirmação em **soft delete de produto** (alerta que histórico será mantido)
- [ ] Entrada e cadastro vão direto sem confirmação (sem fricção)

**Infraestrutura:**
- [ ] `docker-compose.yml` na raiz subindo PG 16 + API .NET + Vue (3 services)
- [ ] `init.sql` montado no Postgres container
- [ ] Variáveis de ambiente para connection strings
- [ ] CORS configurado pro frontend chamar backend

**Testes:**
- [ ] xUnit + Moq no backend cobrindo Services (regras de negócio), Validators, Repositories críticos
- [ ] Vitest + Vue Test Utils no frontend cobrindo composables + forms críticos
- [ ] Cobrir cenários de erro agentic (errorCode + hint + details corretos)

**Documentação:**
- [ ] `docs/` na raiz com 3 arquivos markdown:
  - `01-product-decisions.md` — decisões estratégicas, trade-offs, roadmap futuro
  - `02-architecture.md` — diagramas Mermaid (system context, layered, sequence, ER, feature flow), stack, como rodar
  - `03-business-rules.md` — entidades, enums, regras enforced, catálogo de errorCodes
- [ ] Pipeline Pandoc + WeasyPrint + mermaid-filter gera PDFs com CSS branded (paleta StockEasy, Inter, capa, header/footer)
- [ ] PDFs commitados em `docs/dist/` (avaliador vê direto)
- [ ] Script `docs/generate-pdfs.sh` regenera localmente (install: pandoc + weasyprint + mermaid-cli + mermaid-filter)

### Out of Scope

<!-- Explicit boundaries. Includes reasoning to prevent re-adding. -->

- **Autenticação / login** — não pedido no spec, fora do escopo para manter foco no MVP em 1-3 dias
- **Chat com LLM dentro do app** — planejado para milestone v2; v1 entrega API agent-ready, sem UI de chat
- **Biblioteca de UI pronta (PrimeVue/Vuetify/Element)** — escolha consciente por Tailwind + componentes próprios
- **Logo gráfico custom** — wordmark `Stock<accent>Easy</accent>` em Inter é suficiente pro MVP
- **Deploy em cloud** — entrega é via PR no GitHub, rodar local com docker-compose basta
- **Vue 2 / .NET 6 / EF Core** — stack travada é Vue 3 + .NET 8 + Dapper
- **SQL Server / MySQL** — banco escolhido é PostgreSQL 16
- **Tipos de produto além de Electronic/Appliance/Furniture** — spec define enum fechado
- **Edição/exclusão de movimentações já registradas** — histórico é imutável (auditoria)
- **Hard delete de produtos** — soft delete via `deleted_at`, histórico preserva integridade referencial
- **Atalhos de teclado custom (Ctrl+K palette etc)** — overkill pro prazo
- **Auto-save de drafts de formulário** — overkill pro MVP
- **i18n multi-language** — só PT-BR no UI; identifiers em código são EN
- **Dark mode toggle** — light mode only
- **AutoMapper / Mapster** — mapping manual inline
- **MediatR / CQRS / Vertical Slices** — arquitetura single-project N-tier
- **Generic Repository (`IRepository<T>`)** — cada repo é específico do agregado
- **Pinia / Vuex** — composables only

## Context

- **Desafio técnico** de processo seletivo. Avaliação é via PR no fork do repositório (`/home/thallysrc/Projects/DesafioTecnico/`).
- **Critérios de avaliação explícitos** (do README do desafio):
  - Organização do código (separação clara backend/frontend)
  - Boas práticas (service layers, DTOs, controllers)
  - Validações no backend E no frontend
  - Clareza na estrutura de componentes Vue
  - UX/UI básica mas funcional
  - Tratamento de erros amigáveis
- **Diferenciais conscientes além do spec**: testes (xUnit + Vitest), docker-compose, API agentic-friendly, soft delete, Nielsen heuristics aplicadas, documentação em PDF.
- **Prazo curto**: 1-3 dias para entrega — força roadmap coarse e foco no essencial.
- **Estrutura no fork**:
  ```
  DesafioTecnico/
  ├── .planning/         # GSD (este diretório)
  ├── backend/           # Inventory.sln + Inventory/ + Inventory.Tests/
  ├── frontend/          # Vue 3 SPA
  ├── docs/              # 3 markdown docs + PDFs gerados
  ├── docker-compose.yml
  ├── init.sql
  └── README.md          # spec do desafio (já existe)
  ```
- **Projeto de referência**: `/home/thallysrc/Projects/BancoShu/` — quando em dúvida sobre QUALQUER pattern .NET não coberto nos CLAUDE.md, copiar do BancoShu. Exceção: a query N+1 em `TransferService.GetHistoryAsync` é anti-pattern — NÃO copiar.

## Constraints

- **Backend stack**: ASP.NET Core Web API (.NET 8 LTS), C# 12, Nullable enable — exigência do spec + decisão LTS
- **Frontend stack**: Vue 3 + Vite + Composition API + TypeScript strict — exigência do spec (Vue.js) + decisão pela versão moderna
- **Database**: PostgreSQL 16 — mesma versão do BancoShu, ampla compatibilidade
- **ORM**: Dapper — micro ORM, SQL raw, sem migrations (`init.sql` versionado)
- **Validation**: FluentValidation (backend) + Zod via Vee-Validate (frontend)
- **HTTP client**: Axios com interceptor único pra normalizar erros
- **UI styling**: Tailwind CSS sem biblioteca de componentes
- **Tipografia**: Inter (Google Fonts) — fallback `system-ui, sans-serif`
- **Paleta brand**: derivada do RoboteAsy (`#1863DC` primary, `#0056A7` primary-dark, escala brand-50..900) + semantic (`success #009C34`, `warning #FCB900`, `danger #CF2E2E`)
- **Timeline**: 1-3 dias — granularidade coarse, fases verticais (back+front juntos), foco MVP
- **Entrega**: PR no fork do GitHub — commits limpos, atômicos, hist coerente
- **Dois lados sincronizados**: validações em backend (autoridade) E frontend (UX preventiva) — critério de avaliação
- **Histórico imutável**: movimentações registradas não editam/excluem — auditoria
- **Soft delete em produtos**: `deleted_at` timestamptz NULL; movimentos seguem imutáveis
- **Idempotency obrigatória**: header `Idempotency-Key` (UUID v4) required em `POST /api/stock-movements`
- **Paginação padrão**: `?page=1&pageSize=30`, max `pageSize=100`
- **Zero N+1**: JOIN ou batch fetch em toda listagem com dados relacionados
- **Idiomas**: identifiers em código em **inglês**; mensagens, hints e labels ao usuário em **português** (BR locale)

## Key Decisions

| # | Decision | Rationale | Outcome |
|---|----------|-----------|---------|
| D1 | Desenvolver dentro do fork `DesafioTecnico/` | É onde o PR será aberto — evita migração de código depois | — Pending |
| D2 | Estrutura `backend/` + `frontend/` + `docs/` + `.planning/` na raiz | Separação clara exigida pelo spec; `.planning/` coordena fases verticais | — Pending |
| D3 | .NET 8 (LTS) sobre .NET 9 | Estabilidade, suporte longo, preferido em avaliações profissionais | — Pending |
| D4 | Vue 3 + Vite + Composition API + TypeScript strict | Stack moderna 2025, TS casa com DTOs tipados da API | — Pending |
| D5 | PostgreSQL 16 via Dapper (não EF Core) | Convenção BancoShu de referência, sem migrations, schema em `init.sql` | — Pending |
| D6 | Arquitetura: projeto único N-tier estilo BancoShu | NÃO Clean Architecture (4 projetos). Pasta única `Inventory/` + `Inventory.Tests/` | — Pending |
| D7 | Identifiers em inglês, mensagens em PT-BR | `Product`, `StockMovement`, `ProductType`, `MovementType`. Mensagens consistentes com BancoShu, avaliadores brasileiros | — Pending |
| D8 | Mapping DTO ↔ Entity: manual inline | Sem AutoMapper/Mapster. Dapper usa aliases SQL para snake_case ↔ PascalCase | — Pending |
| D9 | FluentValidation cobre 100% input validation | Sem DataAnnotations. Validators dedicados por DTO em `Validators/` | — Pending |
| D10 | Exception flow: DomainException base + ExceptionHandlingMiddleware | Throw → switch type → HTTP status + ErrorResponse | — Pending |
| D11 | Frontend: Composables only (sem Pinia) | Estado CRUD simples — composables em `features/<dominio>/composables/` bastam | — Pending |
| D12 | Axios + Vee-Validate + Zod | HTTP via Axios; validação espelha FluentValidation | — Pending |
| D13 | Vue: feature-based folders | `src/features/products/` + `src/features/stock/` com components/composables/types dentro | — Pending |
| D14 | Tailwind sem UI library | Mostra capacidade de construir UI do zero | — Pending |
| D15 | Testes + Docker compose como diferenciais | Únicos extras fora do spec — maturidade sem inflar escopo | — Pending |
| D16 | Sem auth (out of scope) | Não pedido no spec, atrasaria entrega | — Pending |
| D17 | Histórico de movimentos imutável | Movimentações não editam/excluem — auditoria | — Pending |
| **D18** | **API agentic-friendly desde o MVP (não chat)** | OpenAPI rico + errorCodes estruturados + hints dinâmicos + idempotência. Chat com LLM fica como v2/milestone. Cabe no prazo + vira diferencial silencioso | — Pending |
| D19 | ErrorResponse canônico com `hint` + `details` + `traceId` + `timestamp` | Auto-recuperação por LLM; debug e auditoria por humanos | — Pending |
| D20 | Hints dinâmicos por contexto (não estáticos) | Service constrói hint com dados reais (`"Reduza para no máximo 3 unidades"`) | — Pending |
| D21 | Idempotency-Key required em stock-movements | Espelha BancoShu (`transfers`). Coluna `idempotency_key uuid UNIQUE NULL` na tabela | — Pending |
| D22 | Paginação `?page=&pageSize=` default 30 max 100 | Em TODA listagem. Envelope `{ items, pagination, _links }` | — Pending |
| D23 | Enums serializados como string no JSON | `JsonStringEnumConverter` global. `"Electronic"` em vez de `0` — LLM e humanos leem melhor | — Pending |
| D24 | Zero N+1 queries | JOIN ou batch fetch. BancoShu tem um caso em `TransferService.GetHistoryAsync` — NÃO copiar | — Pending |
| D25 | Soft delete em produtos via `deleted_at` | Listagens filtram `WHERE deleted_at IS NULL` default. Histórico de produto deletado preservado | — Pending |
| D26 | Confirmações de UI: somente saída de estoque + soft delete | Entrada e cadastro vão direto (sem fricção) | — Pending |
| D27 | Aplicar 10 heurísticas de Nielsen | Critério explícito do desafio ("UX/UI básica mas funcional"). Loading/empty/error/success, formatação BR, helper text consumindo hint da API | — Pending |
| D28 | Paleta brand derivada de RoboteAsy | StockEasy é sibling visual. Primary `#1863DC`. Inter font. Wordmark `Stock<brand>Easy</brand>` | — Pending |
| D29 | Sidebar light | `bg-white` + item ativo `bg-brand-50 text-brand-700`. Coerente com B2B corporate | — Pending |
| D30 | Documentação em `docs/` com 3 markdowns + PDFs branded | Pandoc + WeasyPrint + mermaid-filter. Diagramas Mermaid renderizam como SVG inline. CSS Paged Media com paleta StockEasy | — Pending |
| D31 | PDFs commitados em `docs/dist/` | Avaliador vê direto sem instalar nada. Regeneração local opcional | — Pending |

## Reference Projects

- **BancoShu**: `/home/thallysrc/Projects/BancoShu/` — espelho de arquitetura para o backend (Dapper, FluentValidation, exception flow, DI setup).
- **RoboteAsy**: `https://roboteasy.tech/` — referência visual para paleta de cores e tipografia do frontend.

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions (next ID)
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state (users, feedback, metrics)

**Milestone v2 (planejado, pós-entrega do desafio):**
- Chat com LLM dentro do app consumindo a API como tools
- Server-side (.NET) ou local — decisão pendente
- Tools: leituras (listar produtos, ver histórico, saldo), cadastro de produto, entrada de estoque, saída de estoque (com confirmação)

---
*Last updated: 2026-05-16 — Phase 1 (Foundation) complete; scaffolding validated end-to-end via docker-compose smoke*
