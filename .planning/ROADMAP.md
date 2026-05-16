# Roadmap: StockEasy

**Defined:** 2026-05-16
**Granularity:** coarse
**Mode:** yolo
**Parallelization:** enabled
**Coverage:** 97/97 v1 requirements mapped

## Overview

StockEasy is a fullstack technical challenge with a 1-3 day deadline. The roadmap decomposes 97 v1 requirements into **4 coarse phases organized as vertical slices** (backend + frontend together for each feature) so the product is demoable after each phase.

Phase 1 establishes scaffolding and gets a "hello world" running end-to-end through docker-compose. Phase 2 delivers the **Products** feature end-to-end and — because it's the first vertical slice — also establishes the cross-cutting backend conventions (exception flow, FluentValidation, agentic API patterns) and frontend UX foundation (4 states, formatting, Nielsen heuristics). Phase 3 reuses those patterns to deliver **Stock Movements** with idempotency and the outbound confirmation modal. Phase 4 hardens with tests and produces branded PDF documentation for the evaluator.

The API is **agentic-friendly from day one** (operationIds, structured errorCodes, dynamic hints, idempotency-key, _links) — a silent differentiator the evaluator will see in `/swagger`.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3, 4): Planned milestone work for v1.0 delivery
- Decimal phases (e.g., 2.1) reserved for urgent insertions if needed

**Parallelization opportunities:**
- Phase 1 is foundational — sequential gate before anything else
- Within Phase 2 and Phase 3, backend and frontend tracks can run in parallel (the API contract is fixed by the conventions in `backend/CLAUDE.md` and `frontend/CLAUDE.md`)
- Phase 4 sub-tracks (tests vs docs) are fully independent and can run in parallel

- [ ] **Phase 1: Foundation** - Scaffold both apps, docker-compose up runs PG + API + Vue end-to-end with "hello world"
- [ ] **Phase 2: Products Vertical Slice** - End-to-end Products CRUD with agentic API conventions and UX foundation established
- [ ] **Phase 3: Stock Movements Vertical Slice** - End-to-end Inbound/Outbound with idempotency, saldo validation, and confirmation modal
- [ ] **Phase 4: Tests, Docs & Polish** - xUnit + Vitest test suites + 3 branded PDF docs committed in docs/dist/

## Phase Details

### Phase 1: Foundation
**Goal**: A developer clones the repo, runs `docker-compose up`, and sees PostgreSQL + .NET API (Swagger reachable) + Vue SPA (sidebar rendered) talking to each other end-to-end — without any business logic yet.
**Depends on**: Nothing (first phase)
**Requirements**: INFRA-01, INFRA-02, INFRA-03, INFRA-04, INFRA-05, INFRA-06, INFRA-07, INFRA-08, BACK-01, BACK-02, BACK-03, BACK-04, FRONT-01, FRONT-02, FRONT-03, FRONT-04, FRONT-09, FRONT-10, FRONT-11, DOC-09
**Success Criteria** (what must be TRUE):
  1. `docker-compose up` boots three services (postgres:16-alpine, .NET API on :8080, Vue dev server on :5173) and they stay healthy
  2. `init.sql` runs automatically on first boot and creates `products` and `stock_movements` tables with all constraints and indexes
  3. Visiting `http://localhost:8080/swagger` shows Swagger UI; visiting `http://localhost:5173` shows the StockEasy SPA shell with sidebar (light theme, brand-50 active item, wordmark `Stock<accent>Easy</accent>`) and routes `/products` and `/stock-movements` resolvable
  4. Frontend Axios client reaches the backend without CORS errors; backend reads its connection string from `IConfiguration` (overridable by env var) and connects to Postgres on startup
  5. Backend solution `Inventory.sln` builds with `Inventory/` and `Inventory.Tests/` projects, namespaces follow `Inventory.Api.{Folder}`, XML doc generation enabled in `.csproj`
**Plans**: 4 plans
Plans:
- [x] 01-01-PLAN.md — Infra: docker-compose.yml + init.sql + Dockerfiles (INFRA-01..03, INFRA-08)
- [x] 01-02-PLAN.md — Backend scaffold: Inventory.sln + Program.cs + IDbConnectionFactory + ExceptionHandlingMiddleware + GET /api/health (BACK-01..04, INFRA-04..07)
- [x] 01-03-PLAN.md — Frontend scaffold: Vite + Vue 3 + TS strict + Tailwind + AppShell + HealthPill + Axios + Vite proxy (FRONT-01..04, FRONT-09..11, INFRA-08)
- [ ] 01-04-PLAN.md — E2E integration smoke + root README.md (DOC-09)
**UI hint**: yes

### Phase 2: Products Vertical Slice
**Goal**: A user can cadastrar, listar (paginado), detalhar e soft-deletar produtos pela UI, com validação espelhada front/back e errorResponses agentic. Along the way, all cross-cutting conventions (exception flow, FluentValidation pipeline, agentic OpenAPI plumbing, Nielsen heuristics, 4-state lists, BR formatting) are established and ready to be reused by Phase 3.
**Depends on**: Phase 1
**Requirements**: BACK-05, BACK-06, BACK-07, BACK-08, BACK-09, BACK-10, BACK-11, BACK-12, BACK-13, BACK-14, BACK-15, BACK-16, PROD-01, PROD-02, PROD-03, PROD-04, PROD-05, PROD-06, PROD-07, AGENT-01, AGENT-02, AGENT-03, AGENT-04, AGENT-05, AGENT-06, AGENT-07, AGENT-08, AGENT-09, AGENT-10, AGENT-11, FRONT-05, FRONT-06, FRONT-07, FRONT-08, UX-01, UX-02, UX-03, UX-04, UX-05, UX-06, UX-07, UX-08, UX-09, UX-10, UX-11, UX-12, UX-13, CONF-02, CONF-03
**Success Criteria** (what must be TRUE):
  1. User can submit `ProductForm` to create a product with `code`, `description`, `type` (dropdown Eletrônico/Eletrodoméstico/Móvel), `supplierValue`, `initialStockQuantity` — invalid inputs show inline errors `onBlur` (PT messages identical to FluentValidation), submit is disabled with errors, success toast appears
  2. `/products` list renders one of 4 states at all times (skeleton loading / empty state with CTA / error state showing `apiError.hint` + retry / paginated table) with BR-formatted currency (R$), date (dd/mm/yyyy), and quantity (separador milhar); deleted products appear only when `?includeDeleted=true`
  3. User can soft-delete a product through a confirmation modal that explains the histórico will be preserved; deleted product is filtered from default listing but still retrievable via `GET /api/products/{id}` with `deletedAt` populated
  4. Every API response — success or error — follows the canonical agentic envelope: paginated lists return `{ items, pagination, _links }`; errors return `ErrorResponse` with `errorCode` from the fixed catalog, dynamic `hint` constructed from real context, `traceId`, and `details`; the frontend toast consumes `apiError.hint ?? apiError.message`
  5. Hitting `/swagger` displays rich OpenAPI docs: every endpoint has stable `operationId` (`createProduct`, `listProducts`, `getProduct`, `deleteProduct`), every DTO field has a description, every status code is declared via `[ProducesResponseType]`, enums serialize as strings (`"Electronic"` not `0`)
**Plans**: TBD
**UI hint**: yes

### Phase 3: Stock Movements Vertical Slice
**Goal**: A user can registrar entradas e saídas de estoque pela UI, com validação de saldo insuficiente, idempotência obrigatória, e modal de confirmação na saída. Histórico paginado com filtros funciona e zero N+1 queries são emitidas. All conventions from Phase 2 are reused.
**Depends on**: Phase 2
**Requirements**: MOVE-01, MOVE-02, MOVE-03, MOVE-04, MOVE-05, MOVE-06, MOVE-07, MOVE-08, MOVE-09, MOVE-10, MOVE-11, FRONT-12, CONF-01
**Success Criteria** (what must be TRUE):
  1. User can register an `Inbound` movement via UI form (product searchable dropdown `code — description`, quantity, supplierValue) — backend atomically increments `stock_quantity` AND updates `supplier_value` on the product inside a single transaction, and a success toast confirms
  2. User can register an `Outbound` movement via UI form that shows "Disponível: N unidades" above the quantity field; submitting opens a confirmation modal with full resumo (produto, quantidade, valor de venda, saldo atual → saldo resultante); confirming decrements stock atomically
  3. Attempting to outbound more than available shows a friendly toast with the API's dynamic hint (e.g., "Reduza a quantidade para no máximo 3 ou registre uma entrada antes") — error is `422 INSUFFICIENT_BALANCE` with `details` containing requested/available/deficit
  4. Idempotency works end-to-end: frontend `crypto.randomUUID()` generates an `Idempotency-Key` header per submit; submitting twice with the same key returns the same movement with `Idempotency-Replay: true` header (status 200, not 201); omitting the header returns `400 MISSING_IDEMPOTENCY_KEY`
  5. `/stock-movements` history is paginated, filterable by `productId`, `startDate`, `endDate`, and the listing endpoint emits **exactly two SQL queries** (one JOIN'd SELECT + one COUNT) regardless of page size — no N+1; movements of soft-deleted products remain visible in the history
**Plans**: TBD
**UI hint**: yes

### Phase 4: Tests, Docs & Polish
**Goal**: A reviewer can run `dotnet test` and `npm test` and see green suites covering business rules and forms; they can open `docs/dist/01-product-decisions.pdf`, `02-architecture.pdf`, and `03-business-rules.pdf` (branded with StockEasy palette and Inter font) without installing anything; the root README points them to docker-compose and the docs folder.
**Depends on**: Phase 3
**Requirements**: TEST-01, TEST-02, TEST-03, TEST-04, TEST-05, TEST-06, TEST-07, DOC-01, DOC-02, DOC-03, DOC-04, DOC-05, DOC-06, DOC-07, DOC-08
**Success Criteria** (what must be TRUE):
  1. `dotnet test` runs `Inventory.Tests` (xUnit + Moq) and reports green coverage of `ProductService`, `StockMovementService` (happy path + every errorCode: `DUPLICATE_CODE`, `INSUFFICIENT_BALANCE`, `PRODUCT_DELETED`, `PRODUCT_NOT_FOUND`, `MISSING_IDEMPOTENCY_KEY`, idempotency replay), and validators — every exception test asserts `ex.ErrorCode`, message substring, and non-null `ex.Hint`
  2. `npm test` runs Vitest and reports green coverage of `useProducts`, `useStockMovements` composables (mocked API layer) and `ProductForm` + `OutboundForm` mounting and basic interaction
  3. `docs/` contains `README.md` index plus three markdown sources (`01-product-decisions.md` with strategic decisions + v2 roadmap, `02-architecture.md` with 5 Mermaid diagrams + stack table + run instructions, `03-business-rules.md` with entities, enums, regras enforced, and full errorCode catalog table)
  4. Running `docs/generate-pdfs.sh` (Pandoc + WeasyPrint + mermaid-filter) regenerates the three PDFs in `docs/dist/` with branded CSS (StockEasy palette, Inter font, capa, header/footer, pagination); the three PDFs are committed to the repo
  5. Root `README.md` explains how to run with `docker-compose up`, lists Swagger and frontend URLs, and links to `docs/dist/` so the evaluator can read the documentation without installing toolchains
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4

**Parallelization within phases:**
- Phase 2 & 3: backend track and frontend track can run in parallel (separate sub-agents) since the API contract is locked by conventions in `backend/CLAUDE.md`
- Phase 4: tests track and docs track are fully independent

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation | 0/TBD | Not started | - |
| 2. Products Vertical Slice | 0/TBD | Not started | - |
| 3. Stock Movements Vertical Slice | 0/TBD | Not started | - |
| 4. Tests, Docs & Polish | 0/TBD | Not started | - |

---

## Coverage Summary

**v1 requirements total:** 97
**Mapped to phases:** 97 ✓
**Unmapped:** 0
**Orphans:** none
**Duplicates:** none

### Per-Phase Counts

| Phase | Requirement Count | Categories Touched |
|-------|-------------------|--------------------|
| Phase 1 | 20 | INFRA (8), BACK (4), FRONT (6), DOC (1), structural |
| Phase 2 | 48 | BACK (12), PROD (7), AGENT (11), FRONT (4), UX (13), CONF (2) — bulk because cross-cutting conventions land here |
| Phase 3 | 13 | MOVE (11), FRONT (1), CONF (1) |
| Phase 4 | 16 | TEST (7), DOC (8) + the index/PDF assets |

Phase 2 is intentionally heavy: it's the first vertical slice, so all the **cross-cutting conventions** (exception flow, FluentValidation, agentic OpenAPI plumbing, UX heuristics, BR formatting, Nielsen) are established once and reused by Phase 3 nearly verbatim. This is the natural shape — fighting it (splitting AGENT-* off into its own phase, for example) would create artificial boundaries.

---
*Roadmap created: 2026-05-16*
