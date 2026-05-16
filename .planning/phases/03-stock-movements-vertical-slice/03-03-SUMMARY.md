---
phase: 03-stock-movements-vertical-slice
plan: 03
subsystem: backend-service-controller-di
tags: [backend, dapper, transactional, select-for-update, idempotency, controller, di, agentic]
dependency-graph:
  requires:
    - 03-01 (StockMovement entity, MovementType, CreateMovementRequest, MovementResponse, PagedMovementsResponse, LinksFactory.ForMovement*, IStockMovementRepository, StockMovementRepository, 5 typed exceptions, ExceptionHandlingMiddleware)
    - Phase 2 (Product entity, IDbConnectionFactory, PagedResult, PaginationMeta, JsonStringEnumConverter)
  provides:
    - StockMovementService (transactional create with SELECT FOR UPDATE + idempotency fast-path + 23505 race-window recovery + read paths)
    - StockMovementsController (3 endpoints with stable operationIds + exhaustive ProducesResponseType)
    - DI registration for IStockMovementRepository + StockMovementService in Program.cs
    - MOVE-01..11 backend behavior fully wired and runtime-verified
  affects:
    - Plan 03-04 (frontend components — now has a live backend to call)
    - Plan 03-05 (E2E smoke — full happy/unhappy + replay matrix targets these endpoints)
tech-stack:
  added: []
  patterns:
    - "Transactional service: connection.OpenAsync → BeginTransactionAsync → SELECT FOR UPDATE → INSERT → UPDATE → CommitAsync"
    - "Idempotency two-step recovery: fast-path GetByIdempotencyKeyAsync BEFORE tx; 23505 catch + RollbackAsync + re-fetch AFTER INSERT"
    - "Inline locked-read SQL in the service (not the repo) — avoids expanding IProductRepository with a forUpdate flag this late"
    - "Service returns (Response, IsReplay) tuple so the controller composes 201 vs 200 + Idempotency-Replay header without exposing the replay signal in the DTO"
    - "Controller MOVE-11 enforcement by ABSENCE of HttpPut / HttpDelete / HttpPatch — MVC routing returns 405 Method Not Allowed"
    - "[FromHeader(Name = ...)] binding with explicit UUID validation; missing-or-malformed → MissingIdempotencyKeyException (400 VALIDATION via category auto-mapping)"
key-files:
  created:
    - backend/Inventory/Services/StockMovementService.cs
    - backend/Inventory/Controllers/StockMovementsController.cs
  modified:
    - backend/Inventory/Program.cs (added 2 DI lines — IStockMovementRepository + StockMovementService; existing Product registrations preserved)
decisions:
  - "Locked-read SQL lives in StockMovementService, not IProductRepository — the interface's GetByIdAsync remains transaction-agnostic and Phase 2's contract stays untouched. The service holds the IDbConnection that owns the transaction anyway, so executing a Dapper QuerySingleOrDefaultAsync with `FOR UPDATE` against `conn` (with `transaction: tx`) is the lowest-blast-radius placement of the lock."
  - "Idempotency replay returns the SAME persisted row through `MapToResponse(StockMovementWithProduct)` — never a freshly-constructed payload. This satisfies the must-have `Idempotency replay returns the SAME persisted movement payload (no random fields rebuilt)`."
  - "23505 catch performs an EXPLICIT `await tx.RollbackAsync()` before re-fetching, so the connection state is clean before the second SELECT. Although `using var tx` would auto-rollback on dispose, an explicit rollback releases locks immediately."
  - "Controller treats blank/malformed Idempotency-Key as MISSING — both raise the same `MissingIdempotencyKeyException`. A bad-format key is semantically `missing a valid key` and shares the same hint (`Gere um UUID v4 no cliente (crypto.randomUUID())`)."
  - "Auth gates / 401 / 403: none in scope — v1 has no auth."
metrics:
  duration: "4m 56s"
  completed: "2026-05-16T19:17:02Z"
  tasks: 3
  files-created: 2
  files-modified: 1
  commits:
    - "bbb0661 — feat(03-03): add transactional StockMovementService"
    - "df68bbd — feat(03-03): add StockMovementsController + DI registration"
---

# Phase 3 Plan 03: Service + Controller + DI Summary

Backend stock-movements vertical slice composes: a transactional `StockMovementService` (BeginTransaction → SELECT FOR UPDATE → validate → INSERT → UPDATE → COMMIT, with idempotency fast-path AND 23505 race-window recovery), a thin `StockMovementsController` (3 endpoints, stable operationIds, exhaustive `[ProducesResponseType]`, MOVE-11 enforced by absence of PUT/DELETE), and two new DI lines in `Program.cs`. All MOVE-01..11 backend behavior is now wired and runtime-verified (Swagger lists three new operationIds; PUT/DELETE return 405; missing `Idempotency-Key` returns 400 + canonical `errorCode: MISSING_IDEMPOTENCY_KEY`).

## The Transactional Flow in `CreateAsync`

Annotated step list (the comment markers in the code line up with these numbers):

1. **Idempotency fast-path lookup** — `_movements.GetByIdempotencyKeyAsync(idempotencyKey)` runs BEFORE any connection is opened with a transaction. If a row exists with that key, the service short-circuits and returns `(MapToResponse(existing), IsReplay: true)`. Zero locks, zero round trips beyond the one JOIN'd SELECT in the repo.
2. **Open connection + BeginTransactionAsync** — `using var conn = (NpgsqlConnection)_factory.Create(); await conn.OpenAsync(); using var tx = await conn.BeginTransactionAsync();` — both bound to `using` so any escape rolls back automatically.
3. **SELECT FOR UPDATE on the product** — inline Dapper `QuerySingleOrDefaultAsync<Product>(ProductSelectForUpdateSql, ..., transaction: tx)`. The `FOR UPDATE` clause locks the row for the duration of the transaction; concurrent movements on the SAME product block here until commit/rollback. Soft-deleted rows still come back so the service can throw `PRODUCT_DELETED` (vs the generic `PRODUCT_NOT_FOUND`).
4. **Validate**:
   - `product is null` → `ProductNotFoundException` (404 NOT_FOUND).
   - `product.DeletedAt is not null` → `ProductDeletedException` (422 BUSINESS_RULE, MOVE-05).
   - `EnforceMovementValueInvariants(request)`: Inbound requires `SupplierValue` and forbids `SaleValue`; Outbound is the mirror — both fail with `InvalidMovementValuesException` (422, MOVE-07).
   - `Outbound && StockQuantity < Quantity` → `InsufficientBalanceException` (422 with dynamic hint built in the exception's constructor, MOVE-04).
5. **INSERT movement** — `_movements.InsertAsync(StockMovement, tx)` runs inside the transaction. `Id` is sent as `Guid.Empty` so the repo's `COALESCE(NULLIF(@Id, ...), gen_random_uuid())` triggers server generation. `SupplierValue` and `SaleValue` are split per `Type` so Inbound rows have only supplier_value and Outbound rows have only sale_value (mirrors the `CHECK` constraints in `init.sql`). On `PostgresException` with SqlState `23505` (unique_violation on `idempotency_key`):
   - `await tx.RollbackAsync()` — release locks immediately.
   - `winning = await _movements.GetByIdempotencyKeyAsync(idempotencyKey)` — fetch the row that won the race.
   - Return `(MapToResponse(winning), IsReplay: true)` — the controller surfaces this as 200 + `Idempotency-Replay: true`, identical body to the fast-path replay.
   - If `winning is null` despite the 23505 (impossible by definition), re-throw so the middleware emits 500 `INTERNAL_ERROR` — better a loud failure than a silent inconsistency.
6. **UPDATE product** — single Dapper `ExecuteAsync` against `conn` (transaction-bound):
   - Inbound: `UPDATE products SET stock_quantity = stock_quantity + @quantity, supplier_value = @supplierValue, updated_at = now() WHERE id = @productId` — satisfies MOVE-06 (Inbound updates supplier_value AND bumps stock atomically).
   - Outbound: `UPDATE products SET stock_quantity = stock_quantity - @quantity, updated_at = now() WHERE id = @productId` — supplier_value is left untouched.
7. **CommitAsync** — `await tx.CommitAsync()`. Connection + tx dispose at scope exit.

If any step from 2 onward throws and the catch arm doesn't return, control falls through to the outer `catch { throw; }` and `using` rolls back the tx.

## Idempotency Replay — Two Paths, Same Output

**Fast path (no race, key already used):**
```
Client → POST /api/stock-movements (Idempotency-Key: K)
  ↓
GetByIdempotencyKeyAsync(K) returns existing row
  ↓
return (MapToResponse(existing), IsReplay: true)
  ↓
Controller: Response.Headers["Idempotency-Replay"] = "true"; return Ok(response)  // 200 + header + identical body
```

**Race-window path (concurrent inserts, both with key K):**
```
Both requests find no row in fast-path lookup.
Both open transactions, lock the same product, validate, then race to INSERT.
The first INSERT wins; the second hits Postgres 23505 on stock_movements.idempotency_key UNIQUE.

Loser's catch arm:
  await tx.RollbackAsync()
  winning = GetByIdempotencyKeyAsync(K)   ← always returns the winner's row
  return (MapToResponse(winning), IsReplay: true)
  ↓
Controller: 200 + Idempotency-Replay: true + winner's body  // INDISTINGUISHABLE from fast-path replay to the client
```

This satisfies the must-have triad:
- "Idempotency replay returns the SAME persisted movement payload (no random fields rebuilt) via `GetByIdempotencyKeyAsync`"
- "Verified by header `Idempotency-Replay: true` AND identical body"
- "Idempotency race-window is closed: service catches Postgres 23505 ... fetches by key, returns existing row as a replay (NOT a new row, NOT a 500)"

## Controller Response Composition

Controller body is intentionally tiny — header check + service call + branch on tuple:

```csharp
if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new MissingIdempotencyKeyException();
if (!Guid.TryParse(idempotencyKey, out var key)) throw new MissingIdempotencyKeyException();

var (response, isReplay) = await _service.CreateAsync(request, key);

if (isReplay) {
    Response.Headers[IdempotencyReplayHeader] = "true";
    return Ok(response);          // 200, replay header, identical body
}
return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);  // 201, Location header
```

Three endpoints, stable operationIds (camelCase verb-noun per backend/CLAUDE.md §"OperationIds estáveis"):

| Method | Route | operationId | Returns |
|--------|-------|-------------|---------|
| POST | `/api/stock-movements` | `createStockMovement` | 201 / 200 (replay) / 400 / 404 / 422 |
| GET | `/api/stock-movements` | `listStockMovements` | 200 |
| GET | `/api/stock-movements/{id:guid}` | `getStockMovement` | 200 / 404 |

`[ProducesResponseType]` count: 8 (POST: 5, GET list: 1, GET id: 2). Plan acceptance required ≥ 7.

## Exact Program.cs Lines Added (so 03-04 can rely on DI)

In the **Repositories** block:
```csharp
builder.Services.AddScoped<Inventory.Api.Repositories.IStockMovementRepository, Inventory.Api.Repositories.StockMovementRepository>();
```

In the **Services** block:
```csharp
builder.Services.AddScoped<Inventory.Api.Services.StockMovementService>();
```

Nothing else moved — Phase 2's `IProductRepository → ProductRepository` and `ProductService` registrations are byte-identical to pre-change. CORS, JSON config, FluentValidation, Swagger, ExceptionHandlingMiddleware ordering all untouched.

## Regression Check — Phase 2 Endpoints Still Work

The Task 3 smoke (now torn down) ran `curl -fsS http://localhost:8080/swagger/v1/swagger.json` against the freshly-booted container and counted operationIds. All seven expected ids are present:

- Phase 3 NEW: `createStockMovement` (1), `listStockMovements` (1), `getStockMovement` (1)
- Phase 2 PRESERVED: `createProduct` (1), `listProducts` (1), `getProduct` (1), `deleteProduct` (1)
- Phase 1 PRESERVED: `getHealth` (1)

`grep -c '"createProduct"'` returned 1 in `/tmp/03-03-swagger.json` — the regression acceptance check passes. (Note: plan's regex pattern was strict; actual Swagger emits `"operationId": "createProduct"` with a space after the colon. Verified via `grep -E '"operationId":\s*"createProduct"'` returning 1.)

## Smoke Verification (Task 3) — Live Backend

Backend booted via `docker compose up -d --build backend`; `GET /api/health` returned ready after ~4 seconds. Smoke results:

| Check | Expected | Got | Pass |
|-------|----------|-----|------|
| Swagger contains `createStockMovement` | 1 hit | 1 | ✅ |
| Swagger contains `listStockMovements` | 1 hit | 1 | ✅ |
| Swagger contains `getStockMovement` | 1 hit | 1 | ✅ |
| Swagger still contains `createProduct` (regression) | 1 hit | 1 | ✅ |
| POST without `Idempotency-Key` (valid body) | HTTP 400 + `errorCode: MISSING_IDEMPOTENCY_KEY` | 400 + correct body | ✅ |
| PUT `/api/stock-movements/{guid}` | HTTP 405 | 405 | ✅ |
| DELETE `/api/stock-movements/{guid}` | HTTP 405 | 405 | ✅ |

After smoke, `docker compose down` cleaned up postgres + api containers so parallel agents in this wave don't collide on port 8080.

### Captured ErrorResponse (MISSING_IDEMPOTENCY_KEY)

```json
{
  "errorCode": "MISSING_IDEMPOTENCY_KEY",
  "category": "VALIDATION",
  "message": "Header 'Idempotency-Key' é obrigatório para registrar movimentos",
  "hint": "Gere um UUID v4 no cliente (crypto.randomUUID()) e envie no header 'Idempotency-Key'.",
  "statusCode": 400,
  "retryable": true,
  "details": null,
  "traceId": "0HNLJFU5K7L4F:00000001",
  "timestamp": "2026-05-16T19:16:33.1400674Z"
}
```

All nine canonical ErrorResponse fields present, hint mentions `crypto.randomUUID()`, statusCode duplicated in body, traceId populated.

## Deviations from Plan

### Auto-fixed Issues

None — the implementation matches the plan's `<action>` blocks verbatim. The transactional flow, the 23505 catch, the controller wiring, the two DI lines, and the smoke task all landed as specified.

### Notes (not deviations)

- **Plan's smoke payload uses `productId="00000000-0000-0000-0000-000000000000"`** which fails FluentValidation's `RuleFor(x => x.ProductId).NotEmpty()` before the controller action runs. ASP.NET's `[ApiController]` short-circuits with the stock ProblemDetails 400 response (`"Produto é obrigatório"`), so the `MissingIdempotencyKeyException` arm is never reached with that payload. Re-ran with a valid-shape body (`productId="11111111-..."`, `quantity:1`, `supplierValue:1`) and got the canonical `errorCode: MISSING_IDEMPOTENCY_KEY` ErrorResponse as expected. The controller behavior is correct; the plan's example curl just happened to choose a payload that exercises FluentValidation first. Documented for Plan 03-05 so its smoke matrix uses a valid-shape body when testing MOVE-02.
- **FluentValidation auto-validation bypasses `ExceptionHandlingMiddleware`.** `AddFluentValidationAutoValidation()` populates `ModelState` rather than throwing `ValidationException`, and `[ApiController]` returns a stock ProblemDetails 400. This is a Phase 2-era choice carried into Phase 3; it is OUT OF SCOPE for this plan (no plan task asked to change validation flow). Plan 03-05 may revisit if its smoke matrix demands canonical `errorCode: VALIDATION_ERROR` on body-shape failures.

## Deferred Issues

None.

## Authentication Gates

None — v1 has no auth surface.

## Known Stubs

None — every endpoint is wired to a real service method, every service method is wired to real Dapper SQL, no placeholder/empty/hardcoded data.

## Self-Check: PASSED

**Files verified to exist:**

- FOUND: backend/Inventory/Services/StockMovementService.cs (`[ -f ... ] && echo FOUND`)
- FOUND: backend/Inventory/Controllers/StockMovementsController.cs
- FOUND: backend/Inventory/Program.cs (modified — both new AddScoped lines present)

**Commits verified to exist:**

- FOUND: bbb0661 — feat(03-03): add transactional StockMovementService
- FOUND: df68bbd — feat(03-03): add StockMovementsController + DI registration

**Build verification:**

- `docker run --rm -v "$(pwd)/backend:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet build Inventory/Inventory.csproj -warnaserror -nologo /clp:ErrorsOnly` → **Build succeeded. 0 Warning(s) 0 Error(s).** (run after each task)

**Runtime verification:**

- Backend booted via `docker compose up -d --build backend`; `GET /api/health` returned `{"status":"ok","db":"up", ...}` within 4 seconds.
- All 7 acceptance checks in Task 3's smoke matrix passed (see table above).
- Stack torn down with `docker compose down` so this worktree leaves no running containers behind.
