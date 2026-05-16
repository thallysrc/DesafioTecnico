---
phase: 02-products-vertical-slice
plan: 04
subsystem: backend-products-feature
tags: [dotnet, service-layer, controllers, openapi, agentic, di]
dependency_graph:
  requires:
    - "backend/Inventory/Repositories/IProductRepository.cs (Plan 02-01)"
    - "backend/Inventory/Repositories/ProductRepository.cs (Plan 02-01)"
    - "backend/Inventory/Dtos/{CreateProductRequest, ProductResponse, PagedProductsResponse, LinksDto, ErrorResponse}.cs (Plan 02-01 + Phase 1)"
    - "backend/Inventory/Exceptions/{DuplicateCodeException, ProductNotFoundException}.cs (Plan 02-02)"
    - "backend/Inventory/Middleware/ExceptionHandlingMiddleware.cs extended (Plan 02-02)"
    - "backend/Inventory/Validators/CreateProductRequestValidator.cs auto-discovered (Plan 02-01)"
    - "Inventory.Api.Infra.PagedResult<T> + IDbConnectionFactory (Phase 1)"
  provides:
    - "Inventory.Api.Services.ProductService — concrete class orchestrating IProductRepository with inline DTO mapping and Postgres-23505 → DuplicateCodeException translation"
    - "Inventory.Api.Controllers.ProductsController — 4 endpoints with stable operationIds (createProduct/listProducts/getProduct/deleteProduct), exhaustive [ProducesResponseType], full XML docs"
    - "Program.cs DI: AddScoped<IProductRepository, ProductRepository>, AddScoped<ProductService>"
    - "OpenAPI surface for products feature — swagger.json now lists /api/products (POST+GET) and /api/products/{id} (GET+DELETE)"
  affects:
    - "Plan 02-05 (Frontend Zod + composables) — can hit the 4 endpoints via Vite proxy"
    - "Plan 02-06 (E2E smoke) — full DB-backed assertions against this controller"
    - "Phase 3 (Stock movements) — reuses ProductService for product lookups; ProductsController unchanged"
tech_stack:
  added: []
  patterns:
    - "Concrete Service class (no interface) per BACK-08 — DI registered as itself"
    - "Inline Entity ↔ DTO mapping inside Service (private static MapToResponse) per BACK-07"
    - "Postgres exception filter via `catch (PostgresException pgex) when (pgex.SqlState == \"23505\")`"
    - "Thin controllers: zero try/catch, zero manual validation, only routing + delegation"
    - "CreatedAtAction(nameof(GetById), ...) on POST sets Location header to /api/products/{id}"
    - "Page/pageSize clamping via Math.Max + Math.Clamp inside Service (controller-agnostic safety)"
    - "Fully-qualified DI registrations in Program.cs to avoid new using-imports (minimal diff)"
key_files:
  created:
    - backend/Inventory/Services/ProductService.cs
    - backend/Inventory/Controllers/ProductsController.cs
  modified:
    - backend/Inventory/Program.cs
decisions:
  - "Page/pageSize clamping centralized in ProductService.ListAsync (not in Controller) — keeps query-string defaults declarative in the controller while enforcing the [1,100] bound regardless of caller (tests, internal call, future consumer) — defense-in-depth"
  - "SoftDeleteAsync conflates 'not found' and 'already deleted' into PRODUCT_NOT_FOUND — both states are equally non-actionable from the caller's perspective; a follow-up GET disambiguates via deletedAt populated. Avoids a new 409/410-style errorCode while keeping the catalog closed."
  - "Service catches PostgresException 23505 (not the repository) — keeps the repo a thin Dapper wrapper; the Service owns the policy of which Postgres errors map to which errorCode in the domain catalog"
  - "Used fully-qualified type names in Program.cs DI block (Inventory.Api.Repositories.IProductRepository / Inventory.Api.Services.ProductService) instead of adding using directives — keeps the Program.cs diff to a single-block insertion with locality-explicit reading"
  - "ProductsController declares [Produces(\"application/json\")] at the class level — single source-of-truth for the produces array, prevents content-type negotiation surprises, avoids per-endpoint duplication"
metrics:
  duration: "~3 min"
  completed: 2026-05-16
  tasks_completed: 3
  files_created: 2
  files_modified: 1
  warnings: 0
  errors: 0
---

# Phase 02 Plan 04: Products Service + Controller + DI Summary

**Concrete `ProductService` orchestrating `IProductRepository` with inline DTO mapping and Postgres-23505 → `DuplicateCodeException` translation; thin `ProductsController` exposing the 4 locked-operationId endpoints (`createProduct`/`listProducts`/`getProduct`/`deleteProduct`) with exhaustive `[ProducesResponseType]`; two-line DI wiring in `Program.cs`. Backend products feature is now wire-up complete — host serves all 4 endpoints in `swagger.json`.**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-05-16T17:33:40Z
- **Completed:** 2026-05-16T17:36:18Z
- **Tasks:** 3 (all `type="auto"`)
- **Files created:** 2 (ProductService + ProductsController)
- **Files modified:** 1 (Program.cs DI block)
- **Build:** 0 warnings, 0 errors
- **Tests:** 9/9 pass (1 Phase 1 smoke + 8 wave 1 validator)

## Task Commits

Each task committed atomically with `--no-verify` (parallel worktree mode):

| # | Task                                                                                                       | Type | Commit    |
| - | ---------------------------------------------------------------------------------------------------------- | ---- | --------- |
| 1 | Author ProductService — orchestration, mapping, exception translation                                      | feat | `3ae0615` |
| 2 | Author ProductsController — 4 endpoints with full Swagger annotations                                      | feat | `bbd299a` |
| 3 | Wire ProductRepository + ProductService in Program.cs; host startup + Swagger contract smoke               | feat | `c1a0f2b` |

## Exception Translation Map (ProductService)

| Input from repository / Postgres                                                                   | Translated exception                                                | HTTP via middleware |
| -------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------- | ------------------- |
| `PostgresException` with `SqlState == "23505"` (UNIQUE on `products.code`)                         | `throw new DuplicateCodeException(request.Code)`                    | 422 DUPLICATE_CODE  |
| `_repository.GetByIdAsync(id)` returns `null` (in `GetByIdAsync`)                                  | `throw new ProductNotFoundException(id)`                            | 404 PRODUCT_NOT_FOUND |
| `_repository.SoftDeleteAsync(id)` returns `0` AND `GetByIdAsync(id)` returns `null` (missing)      | `throw new ProductNotFoundException(id)`                            | 404 PRODUCT_NOT_FOUND |
| `_repository.SoftDeleteAsync(id)` returns `0` AND `GetByIdAsync(id)` returns row (already deleted) | `throw new ProductNotFoundException(id)` (intentional conflation)   | 404 PRODUCT_NOT_FOUND |
| `FluentValidation.ValidationException` thrown by auto-validation pipeline                          | (not in Service) — caught directly by middleware                    | 400 VALIDATION_ERROR |

Note: `DuplicateCodeException` constructor is called with **one** argument (`request.Code`) per D-10 — typed exception builds the hint internally; service does NOT call `string.Format` or set `Hint` inline. The variant with `existingProductId` is reserved for the future case where the service does a follow-up SELECT to enrich the hint.

## Controller Endpoint Contract

| Verb   | Route                  | operationId     | Success | Error codes        | Body in                  | Body out                 |
| ------ | ---------------------- | --------------- | ------- | ------------------ | ------------------------ | ------------------------ |
| POST   | `/api/products`        | `createProduct` | 201     | 400, 422           | `CreateProductRequest`   | `ProductResponse`        |
| GET    | `/api/products`        | `listProducts`  | 200     | —                  | (query: page, pageSize, includeDeleted) | `PagedProductsResponse`  |
| GET    | `/api/products/{id:guid}` | `getProduct`    | 200     | 404                | —                        | `ProductResponse`        |
| DELETE | `/api/products/{id:guid}` | `deleteProduct` | 204     | 404                | —                        | (empty)                  |

All endpoints declare `[ProducesResponseType]` for every status code listed (AGENT-05). Controller-level `[Produces("application/json")]` declares the response content-type once.

POST returns 201 with `Location: /api/products/{id}` (via `CreatedAtAction(nameof(GetById), ...)`) — free correctness for any non-frontend consumer; the SPA uses `_links.self` instead.

## Program.cs DI Diff

Replaced the Phase 1 placeholder comment `// Repositories + Services land in Phase 2.` with two `AddScoped` registrations:

```csharp
// -------------------------------------------------------------------------------------------------
// Repositories — Scoped (interface + impl per BACK-08).
// -------------------------------------------------------------------------------------------------
builder.Services.AddScoped<Inventory.Api.Repositories.IProductRepository, Inventory.Api.Repositories.ProductRepository>();

// -------------------------------------------------------------------------------------------------
// Services — Scoped (concrete classes, no interface per BACK-08).
// -------------------------------------------------------------------------------------------------
builder.Services.AddScoped<Inventory.Api.Services.ProductService>();
```

Net diff: `+9 / -1` lines. No new `using` directives needed (fully-qualified type names keep the diff minimal and locality-explicit).

## Host Smoke Evidence

Launched short-lived host with `mcr.microsoft.com/dotnet/sdk:8.0` (`ConnectionStrings__Postgres="...__none__..."`, `ASPNETCORE_URLS="http://0.0.0.0:18080"`, `ASPNETCORE_ENVIRONMENT=Development`). Swagger.json captured via `curl -fsS http://127.0.0.1:18080/swagger/v1/swagger.json`:

**paths exposed:**

```json
[
  "/api/health",
  "/api/products",
  "/api/products/{id}"
]
```

**operationIds (all 4 locked names present):**

```json
.paths."/api/products".post.operationId     → "createProduct"
.paths."/api/products".get.operationId      → "listProducts"
.paths."/api/products/{id}".get.operationId → "getProduct"
.paths."/api/products/{id}".delete.operationId → "deleteProduct"
```

**response codes declared per endpoint** (verified via `jq -r '.paths.X.METHOD.responses | keys[]'`):

| Endpoint                         | Status codes        |
| -------------------------------- | ------------------- |
| `POST /api/products`             | 201, 400, 422       |
| `GET /api/products`              | 200                 |
| `GET /api/products/{id}`         | 200, 404            |
| `DELETE /api/products/{id}`      | 204, 404            |

**components.schemas registered** (products-related):

```
CreateProductRequest
ErrorResponse
PagedProductsResponse
PaginationMeta
ProductResponse
ProductType
```

Container stopped cleanly after capture.

## Build & Test Evidence

```
dotnet build Inventory.sln → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test Inventory.sln --no-build → Passed! Failed: 0, Passed: 9, Skipped: 0, Total: 9
```

(9 tests = 1 Phase 1 smoke + 8 wave 1 validator tests; this plan adds no new tests — Phase 4 owns service-level tests.)

## Acceptance Criteria Verification

Per-task `grep` checks:

| Check | Result |
| ----- | ------ |
| `public class ProductService` in Services/ProductService.cs                               | PASS |
| no `interface IProductService` in Services/ProductService.cs                              | PASS |
| `PostgresException pgex when pgex.SqlState == "23505"` in ProductService.cs               | PASS |
| `throw new DuplicateCodeException` in ProductService.cs                                   | PASS |
| `throw new ProductNotFoundException` in ProductService.cs                                 | PASS |
| `LinksFactory.ForProduct` + `LinksFactory.ForProductsListing` in ProductService.cs        | PASS |
| `Math.Clamp` (pageSize bounds) in ProductService.cs                                       | PASS |
| `private static ProductResponse MapToResponse` (inline mapping) in ProductService.cs      | PASS |
| `[Route("api/products")]` on controller                                                   | PASS |
| `OperationId = "createProduct" / "listProducts" / "getProduct" / "deleteProduct"`         | PASS (all 4) |
| `ProducesResponseType(typeof(ProductResponse), Status201Created)` on POST                 | PASS |
| `ProducesResponseType(typeof(PagedProductsResponse), Status200OK)` on GET list            | PASS |
| `ProducesResponseType(Status204NoContent)` on DELETE                                      | PASS |
| `ProducesResponseType(typeof(ErrorResponse), Status404NotFound)` (twice — GET-by-id + DEL) | PASS |
| `ProducesResponseType(typeof(ErrorResponse), Status422UnprocessableEntity)` on POST       | PASS |
| no `try {` in ProductsController.cs                                                       | PASS (0 occurrences) |
| `<summary>` tags ≥ 5 in controller                                                        | PASS (5 — controller + 4 endpoints) |
| `<remarks>` tags ≥ 4 in controller                                                        | PASS (4 — one per endpoint) |
| `AddScoped<Inventory.Api.Repositories.IProductRepository, Inventory.Api.Repositories.ProductRepository>` in Program.cs | PASS |
| `AddScoped<Inventory.Api.Services.ProductService>` in Program.cs                          | PASS |
| placeholder `Repositories + Services land in Phase 2` removed                             | PASS |
| `dotnet build` 0 warnings 0 errors                                                        | PASS |
| host smoke captures swagger.json with 4 operationIds                                      | PASS |
| Phase 1 `dotnet test` still passes                                                        | PASS (9/9) |

## Package Allowlist Verification

```
grep -r 'using AutoMapper' backend/Inventory/ → no matches
grep -r 'using Mapster'    backend/Inventory/ → no matches
grep -r 'MediatR'          backend/Inventory/ → no matches
```

Confirmed: no forbidden mapping/CQRS libraries introduced.

## Deviations from Plan

None — plan executed exactly as written.

## Authentication Gates

None encountered. No auth in v1 per PROJECT.md.

## Issues Encountered

- **Worktree base commit was incorrect on initial inspection** (same issue noted in Plan 02-01 SUMMARY). The worktree HEAD was on `eb6ff9a` (README-only commit chain) instead of the wave 1 merge commit `0ec527d` required by `<worktree_branch_check>`. Resolved via `git reset --hard 0ec527d` before any work began; this restored Phase 1 + wave 1 (Plan 02-01 + 02-02 + 02-03) artifacts into the worktree. All required interfaces (IProductRepository, DTOs, LinksFactory, DuplicateCodeException, ProductNotFoundException, ExceptionHandlingMiddleware extension) were then present and consumed verbatim.

## Next Phase Readiness

- **Plan 02-05 (Frontend product features):** Backend endpoints are live and discoverable via `/swagger`. The Vite proxy already forwards `/api/**` → `http://backend:8080`; the four operationIds are stable and match the planner's locked names. `createProductSchema.ts` (Zod) mirrors the validator's 7 PT-BR messages byte-for-byte. Composables (`useProducts`) can call: `POST /api/products`, `GET /api/products?page=N&pageSize=M&includeDeleted=B`, `GET /api/products/{id}`, `DELETE /api/products/{id}`.
- **Plan 02-06 (E2E smoke):** Can run the full DB-backed assertions:
  - `POST /api/products` valid → expect 201, `_links.self` + `_links.delete` present, `Location` header set.
  - `POST /api/products` duplicate code → expect 422, `errorCode: "DUPLICATE_CODE"`, dynamic hint with the conflicting code value.
  - `POST /api/products` invalid (empty code) → expect 400, `errorCode: "VALIDATION_ERROR"`, `details.fields[].field === "code"` (camelCase).
  - `GET /api/products` → expect 200, envelope `{ items, pagination, _links }`, pagination object has `page/pageSize/total/totalPages/hasNext/hasPrev`.
  - `GET /api/products/{nonexistent-guid}` → expect 404, `errorCode: "PRODUCT_NOT_FOUND"`.
  - `DELETE /api/products/{id}` first call → 204; second call → 404 PRODUCT_NOT_FOUND.
- **Phase 3 (Stock movements):** `StockMovementService` will inject `ProductService` (or `IProductRepository` directly for `forUpdate: true` reads) — both are now in DI. Pattern set here is reused verbatim: typed exceptions for movement codes (`INSUFFICIENT_BALANCE`, `PRODUCT_DELETED`, ...), inline DTO mapping in service, thin controller with locked operationIds (`registerStockMovement`, `listStockMovements`, `getStockMovement`).

## Self-Check: PASSED

**Files verified to exist:**

- `backend/Inventory/Services/ProductService.cs` — FOUND
- `backend/Inventory/Controllers/ProductsController.cs` — FOUND
- `backend/Inventory/Program.cs` — FOUND (modified)

**Commits verified in git log:**

- `3ae0615` — FOUND (feat Task 1 — ProductService)
- `bbd299a` — FOUND (feat Task 2 — ProductsController)
- `c1a0f2b` — FOUND (feat Task 3 — Program.cs DI wire)

**Build evidence:**

- `dotnet build Inventory.sln` → `Build succeeded. 0 Warning(s) 0 Error(s)`
- `dotnet test Inventory.sln --no-build` → `Passed! Failed: 0, Passed: 9, Skipped: 0, Total: 9`

**Swagger contract evidence:**

- `GET /swagger/v1/swagger.json` returns 4 product operationIds: `createProduct`, `listProducts`, `getProduct`, `deleteProduct`
- Response codes declared per AGENT-05: POST {201,400,422}, GET-list {200}, GET-by-id {200,404}, DELETE {204,404}

**Package allowlist verified:** no AutoMapper, no Mapster, no MediatR.

No stubs introduced (every endpoint is wired through to a working code path). No threat flags (controllers narrow surface to typed DTOs + UUID route constraints; exceptions are handled by middleware that omits stack traces from 500 bodies).

---
*Phase: 02-products-vertical-slice*
*Completed: 2026-05-16*
