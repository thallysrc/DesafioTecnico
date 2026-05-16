---
phase: 03-stock-movements-vertical-slice
plan: 01
subsystem: backend-foundation
tags: [backend, dapper, entities, dtos, repository, validator, exceptions, middleware, zero-n+1, agentic]
dependency-graph:
  requires:
    - phase-02 (DomainException base, BusinessRuleException, NotFoundException, ExceptionHandlingMiddleware shell, LinksFactory, PagedResult, IDbConnectionFactory)
    - init.sql stock_movements schema (id, product_id, type, quantity, sale_value, supplier_value, idempotency_key, occurred_at, created_at)
  provides:
    - StockMovement entity + StockMovementWithProduct projection (JOIN'd shape)
    - MovementType enum (Inbound=0, Outbound=1)
    - CreateMovementRequest / MovementResponse / PagedMovementsResponse DTOs
    - LinksFactory.ForMovement + LinksFactory.ForMovementsListing
    - IStockMovementRepository contract + Dapper impl (zero N+1)
    - CreateMovementRequestValidator (PT-BR, mirrors Zod schemas in 03-02)
    - 5 typed exceptions: MissingIdempotencyKey (400 VALIDATION), InsufficientBalance (422), ProductDeleted (422), InvalidMovementValues (422), MovementNotFound (404)
    - ExceptionHandlingMiddleware doc extension referencing Phase 3 additions
  affects:
    - Plan 03-03 (composes StockMovementService against IStockMovementRepository + the 5 exceptions)
    - Plan 03-05 (smoke test verifies "exactly 2 queries per history page" using the SQL captured below)
tech-stack:
  added: []
  patterns:
    - "Dapper JOIN'd SELECT with PascalCase aliases (snake_case → property setters)"
    - "Two-query pagination (items SELECT + COUNT) inside same Connection — zero N+1"
    - "Optional filter clause via `@param::type IS NULL OR column op @param` so a single SQL handles every combo"
    - "Append-only repository: InsertAsync inside caller's IDbTransaction; no Update / Delete on the interface (MOVE-11)"
    - "Idempotency lookup via dedicated GetByIdempotencyKeyAsync (JOIN'd so replay response carries productCode + productDescription)"
    - "Typed exceptions with dynamic hints constructed in-constructor from real context (D-10 from Phase 2 carried)"
    - "Category-driven auto-mapping in middleware — new exception types inherit HTTP status from DomainException.Category, no per-type switch case"
key-files:
  created:
    - backend/Inventory/Entities/MovementType.cs
    - backend/Inventory/Entities/StockMovement.cs
    - backend/Inventory/Dtos/CreateMovementRequest.cs
    - backend/Inventory/Dtos/MovementResponse.cs
    - backend/Inventory/Dtos/PagedMovementsResponse.cs
    - backend/Inventory/Repositories/IStockMovementRepository.cs
    - backend/Inventory/Repositories/StockMovementRepository.cs
    - backend/Inventory/Validators/CreateMovementRequestValidator.cs
    - backend/Inventory/Exceptions/MissingIdempotencyKeyException.cs
    - backend/Inventory/Exceptions/InsufficientBalanceException.cs
    - backend/Inventory/Exceptions/ProductDeletedException.cs
    - backend/Inventory/Exceptions/InvalidMovementValuesException.cs
    - backend/Inventory/Exceptions/MovementNotFoundException.cs
  modified:
    - backend/Inventory/Dtos/LinksDto.cs (appended ForMovement + ForMovementsListing; Phase 2 helpers preserved)
    - backend/Inventory/Middleware/ExceptionHandlingMiddleware.cs (XML `<summary>` extended; no code change to switch — auto-mapping carries the new exceptions)
decisions:
  - "Replaced unresolvable `<see cref=\"...IStockMovementRepository.ListAsync\"/>` in StockMovement.cs XML doc with `<c>IStockMovementRepository.ListAsync</c>` because Task 1 lands the entity BEFORE the interface (Task 2). Plain-text reference avoids the CS1574 -warnaserror failure while still pointing the reader at the right symbol."
  - "Idempotency check (GetByIdempotencyKeyAsync) lives in the repository, not the service, so Plan 03-03's StockMovementService can call it both pre-INSERT (fast path) and post-23505 (race recovery) without re-fetching the JOIN'd product fields — single round trip in both branches."
  - "InsertAsync requires IDbTransaction (not optional) because every Outbound insert MUST run inside the same tx as the `SELECT ... FOR UPDATE` on the product row + the balance UPDATE. Optional-tx would invite silent correctness bugs (race on stock_quantity)."
  - "Listing filter date semantics: BOTH ends inclusive (`>= startDate AND <= endDate`). Aligned with 03-CONTEXT.md / 03-UI-SPEC; deliberately diverges from half-open `[start, end)` because business users think of `startDate=2026-05-01&endDate=2026-05-31` as 'all of May'."
metrics:
  duration: "3m 52s"
  completed: "2026-05-16T19:08:33Z"
  tasks: 2
  files-created: 13
  files-modified: 2
  commits:
    - "6d59d62 — Task 1: entities, DTOs, LinksFactory extension"
    - "996106a — Task 2: repository, validator, 5 exceptions, middleware doc"
---

# Phase 3 Plan 01: Backend Foundation Summary

Backend data-shape + validation contract for stock movements: 13 new files + 2 modified files lock the JOIN'd zero-N+1 history SQL, the typed-exception hint catalog (D-10 carry-forward), and the byte-identical PT-BR validator messages the frontend Zod schemas will mirror in Plan 03-02.

## Files Created

### Entities (`backend/Inventory/Entities/`)

- **`MovementType.cs`** — Closed enum `Inbound = 0`, `Outbound = 1` matching `stock_movements.type CHECK (type IN (0, 1))`. Serialized as `"Inbound"` / `"Outbound"` via the global `JsonStringEnumConverter`.
- **`StockMovement.cs`** — Two POCOs in one file:
  - `StockMovement` — base entity (9 fields: `Id`, `ProductId`, `Type`, `Quantity`, `SaleValue?`, `SupplierValue?`, `IdempotencyKey`, `OccurredAt`, `CreatedAt`). Mutable setters for Dapper materialization.
  - `StockMovementWithProduct` — flattened JOIN target carrying `ProductCode` + `ProductDescription` snapshots so list/detail/idempotency-replay responses never trigger a second round-trip.

### DTOs (`backend/Inventory/Dtos/`)

- **`CreateMovementRequest.cs`** — record with `ProductId`, `Type`, `Quantity`, `SupplierValue?`, `SaleValue?`. Both value fields are nullable: format-level checks (≥ 0 when present) live in the validator; the Inbound/Outbound mutual-exclusion invariant is a business rule enforced by `StockMovementService` in Plan 03-03 via `InvalidMovementValuesException`.
- **`MovementResponse.cs`** — wire response with `productCode` + `productDescription` (from JOIN) and `_links` (`self` + `product`).
- **`PagedMovementsResponse.cs`** — envelope `{ Items, Pagination, Links }` reusing the existing `PaginationMeta`.

### Repository (`backend/Inventory/Repositories/`)

- **`IStockMovementRepository.cs`** — contract with exactly four methods: `InsertAsync(StockMovement, IDbTransaction)`, `GetByIdAsync(Guid)`, `GetByIdempotencyKeyAsync(Guid)`, `ListAsync(productId?, startDate?, endDate?, page, pageSize)`. No update or delete (MOVE-11 immutability).
- **`StockMovementRepository.cs`** — Dapper impl. `SelectColumns` const + `FromJoin` const reused across `GetByIdAsync`, `GetByIdempotencyKeyAsync`, `ListAsync` to guarantee one shape and one JOIN.

### Validator (`backend/Inventory/Validators/`)

- **`CreateMovementRequestValidator.cs`** — FluentValidation rules with PT-BR messages byte-identical to the locked 03-UI-SPEC mirror table:
  - `Produto é obrigatório` (ProductId required)
  - `Quantidade deve ser maior que zero` (Quantity ≥ 1)
  - `Valor do fornecedor não pode ser negativo` (SupplierValue ≥ 0 when present)
  - `Valor de venda não pode ser negativo` (SaleValue ≥ 0 when present)
  - Type enum is validated by `JsonStringEnumConverter` (invalid value → 400 from MVC), no FluentValidation rule needed.

### Exceptions (`backend/Inventory/Exceptions/`)

| File | Class | errorCode | HTTP | Category | Inherits |
|------|-------|-----------|------|----------|----------|
| `MissingIdempotencyKeyException.cs` | `MissingIdempotencyKeyException` | `MISSING_IDEMPOTENCY_KEY` | 400 | VALIDATION | `DomainException` (direct — needs VALIDATION/400, not 422) |
| `InsufficientBalanceException.cs` | `InsufficientBalanceException` | `INSUFFICIENT_BALANCE` | 422 | BUSINESS_RULE | `BusinessRuleException` |
| `ProductDeletedException.cs` | `ProductDeletedException` | `PRODUCT_DELETED` | 422 | BUSINESS_RULE | `BusinessRuleException` |
| `InvalidMovementValuesException.cs` | `InvalidMovementValuesException` | `INVALID_MOVEMENT_VALUES` | 422 | BUSINESS_RULE | `BusinessRuleException` |
| `MovementNotFoundException.cs` | `MovementNotFoundException` | `MOVEMENT_NOT_FOUND` | 404 | NOT_FOUND | `NotFoundException` |

All five build their hint **inside their constructor** from real context. Examples:

- `InsufficientBalance`: `"Reduza a quantidade para no máximo {available} ou registre uma entrada antes."` + `details: { productId, productCode, requested, available, deficit = requested - available }`
- `InvalidMovementValues`: `"Movimentos do tipo '{type}' requerem o campo '{expectedField}', e não '{providedField}'."` + `details: { type, providedField, expectedField }`
- `ProductDeleted`: `"Produto '{productCode}' foi excluído. Movimentos não podem ser registrados para produtos excluídos."`

## Files Modified

### `backend/Inventory/Dtos/LinksDto.cs`

Two new static methods appended to `LinksFactory` (Phase 2 `ForProduct` / `ForProductsListing` untouched):

- `ForMovement(Guid movementId, Guid productId)` → `{ self, product }`.
- `ForMovementsListing(page, pageSize, totalPages, productId?, startDate?, endDate?)` → `{ self, first, last, next?, prev? }`. Echoes any active filters via a `&productId=...&startDate=...&endDate=...` suffix so paginated links round-trip.

### `backend/Inventory/Middleware/ExceptionHandlingMiddleware.cs`

XML `<summary>` extended with a `<list>` item documenting that the five Phase 3 exception types flow through the existing `DomainException d => ...` switch arm and inherit their HTTP status from `Category`. **Zero per-type switch cases added** — the existing mapping (`NOT_FOUND→404`, `BUSINESS_RULE→422`, `VALIDATION→400`) already produces the correct status for every new exception.

## SQL Emitted by `StockMovementRepository.ListAsync`

This is the exact two-statement pattern Plan 03-05 (smoke test) will assert with `EXPLAIN` / `grep`:

**Statement 1 — items SELECT (JOIN'd, paginated, filtered):**
```sql
SELECT
    m.id              as Id,
    m.product_id      as ProductId,
    m.type            as Type,
    m.quantity        as Quantity,
    m.sale_value      as SaleValue,
    m.supplier_value  as SupplierValue,
    m.idempotency_key as IdempotencyKey,
    m.occurred_at     as OccurredAt,
    m.created_at      as CreatedAt,
    p.code            as ProductCode,
    p.description     as ProductDescription
FROM stock_movements m
INNER JOIN products p ON p.id = m.product_id
WHERE (@productId::uuid IS NULL OR m.product_id = @productId)
  AND (@startDate::timestamptz IS NULL OR m.occurred_at >= @startDate)
  AND (@endDate::timestamptz IS NULL OR m.occurred_at <= @endDate)
ORDER BY m.occurred_at DESC, m.id DESC
LIMIT @pageSize OFFSET @offset
```

**Statement 2 — total count (same JOIN, same WHERE, no ORDER/LIMIT):**
```sql
SELECT COUNT(*)
FROM stock_movements m
INNER JOIN products p ON p.id = m.product_id
WHERE (@productId::uuid IS NULL OR m.product_id = @productId)
  AND (@startDate::timestamptz IS NULL OR m.occurred_at >= @startDate)
  AND (@endDate::timestamptz IS NULL OR m.occurred_at <= @endDate)
```

Both share the same `Dapper` connection (`using var conn = _factory.Create();`). No per-row product fetch. No fan-out. Exactly two queries regardless of `pageSize` or filter combination — MOVE-09 (zero N+1) is satisfied at the repository plane.

The indexes already in `init.sql` cover both shapes:
- `idx_stock_movements_product_occurred (product_id, occurred_at DESC)` → product-scoped queries.
- `idx_stock_movements_occurred (occurred_at DESC)` → global time-window queries.

## Middleware Required No Per-Type Case Edits

Confirmed: the existing `DomainException d => ...` arm in `ExceptionHandlingMiddleware.HandleAsync` maps via `d.Category switch { "NOT_FOUND" => 404, "BUSINESS_RULE" => 422, "VALIDATION" => 400, _ => 500 }`. Each new exception sets its Category through its base class (or directly, for `MissingIdempotencyKeyException`), so the mapping produces the correct HTTP status without any code change.

The only middleware edit was an XML `<summary>` extension documenting this fact — `grep -q "Phase 3 additions" backend/Inventory/Middleware/ExceptionHandlingMiddleware.cs` passes.

## Verification

- `docker run --rm -v "$(pwd)/backend:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet build Inventory/Inventory.csproj -warnaserror -nologo /clp:ErrorsOnly` → **Build succeeded. 0 Warning(s) 0 Error(s).**
- All Task 1 acceptance grep checks pass (enum, entities, DTOs, LinksFactory extension + ForProduct preservation).
- All Task 2 acceptance grep checks pass (JOIN, COUNT, three `_factory.Create` uses, all five errorCodes, all four PT-BR validator messages, middleware doc string).
- No `Program.cs` edits — DI registration deliberately deferred to Plan 03-03.
- No controller, no service — deliberately deferred to Plan 03-03.
- No frontend work — Plan 03-02 owns that.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking issue] Replaced unresolvable XML cref to defer-created symbol**

- **Found during:** Task 1 build (`-warnaserror`)
- **Issue:** `StockMovement.cs` originally contained `<see cref="Inventory.Api.Repositories.IStockMovementRepository.ListAsync"/>` per the plan's verbatim source, but `IStockMovementRepository` does not exist until Task 2 lands. With `<GenerateDocumentationFile>true</GenerateDocumentationFile>` and `-warnaserror`, the build failed with `CS1574: XML comment has cref attribute 'ListAsync' that could not be resolved`.
- **Fix:** Replaced the `<see cref="..."/>` with `<c>IStockMovementRepository.ListAsync</c>` (plain code text). Reader still navigates to the same symbol; the build is green at the Task-1 boundary so the per-task commit is independently buildable.
- **Files modified:** `backend/Inventory/Entities/StockMovement.cs`
- **Commit:** `6d59d62`

## Deferred Issues

None.

## Authentication Gates

None — no auth surface in this plan.

## Self-Check: PASSED

**Files verified to exist:**

- FOUND: backend/Inventory/Entities/MovementType.cs
- FOUND: backend/Inventory/Entities/StockMovement.cs
- FOUND: backend/Inventory/Dtos/CreateMovementRequest.cs
- FOUND: backend/Inventory/Dtos/MovementResponse.cs
- FOUND: backend/Inventory/Dtos/PagedMovementsResponse.cs
- FOUND: backend/Inventory/Dtos/LinksDto.cs (modified, retains ForProduct/ForProductsListing + adds ForMovement/ForMovementsListing)
- FOUND: backend/Inventory/Repositories/IStockMovementRepository.cs
- FOUND: backend/Inventory/Repositories/StockMovementRepository.cs
- FOUND: backend/Inventory/Validators/CreateMovementRequestValidator.cs
- FOUND: backend/Inventory/Exceptions/MissingIdempotencyKeyException.cs
- FOUND: backend/Inventory/Exceptions/InsufficientBalanceException.cs
- FOUND: backend/Inventory/Exceptions/ProductDeletedException.cs
- FOUND: backend/Inventory/Exceptions/InvalidMovementValuesException.cs
- FOUND: backend/Inventory/Exceptions/MovementNotFoundException.cs
- FOUND: backend/Inventory/Middleware/ExceptionHandlingMiddleware.cs (modified)

**Commits verified to exist:**

- FOUND: 6d59d62 — feat(03-01): add stock movement entities, DTOs, and LinksFactory extension
- FOUND: 996106a — feat(03-01): add stock movement repository, validator, exceptions, middleware doc
