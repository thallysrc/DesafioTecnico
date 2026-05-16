# Phase 3: Stock Movements Vertical Slice - Context

**Gathered:** 2026-05-16
**Status:** Ready for planning
**Mode:** Auto (autonomous chain) — recommended defaults selected for residual gray areas

<domain>
## Phase Boundary

End-to-end Stock Movements (Inbound / Outbound) via the Vue UI talking to the .NET API. Idempotency-Key required on every movement POST. Outbound validates available balance with dynamic hint, opens a confirmation modal with full resumo before committing, and decrements stock atomically. Inbound updates `supplier_value` on the product. Movement history is paginated, filterable by `productId`/`startDate`/`endDate`, and the listing emits **exactly two queries** (one JOIN'd SELECT + one COUNT) regardless of page size. Movements of soft-deleted products are rejected on insert but remain visible in the history. Movements are immutable (no UPDATE / no DELETE endpoint).

**All Phase 2 conventions are reused verbatim:** exception flow, validators, agentic OpenAPI plumbing, Nielsen heuristics, 4-state lists, BR formatting, base primitives, validation message mirroring (Zod ↔ FluentValidation).

**Out of scope:** Edit/delete movements (immutable), bulk operations, multi-warehouse, transfer-between-warehouses, kardex full report, audit log UI, low-stock alerts, recurring movements.

</domain>

<decisions>
## Implementation Decisions

### Locked by PROJECT.md + backend/CLAUDE.md + frontend/CLAUDE.md + Phase 2 SUMMARYs
The 13 Phase 3 requirements (MOVE-01..11, FRONT-12, CONF-01) are fully specified upstream. Notable points:

- **`POST /api/stock-movements`** receives `{ productId, type ('Inbound'|'Outbound'), quantity, supplierValue?, saleValue? }` plus `Idempotency-Key` header. Returns 201 with the created movement and `_links.self` + `_links.product`.
- **Idempotency (MOVE-02, MOVE-03):** `Idempotency-Key` header required (UUID v4 format). Absent → 400 `MISSING_IDEMPOTENCY_KEY`. Replay (same key) → 200 with `Idempotency-Replay: true` response header + the SAME movement payload. Implementation: backend persists `idempotency_key uuid UNIQUE` on `stock_movements`; the service catches the unique-violation, fetches the existing movement, returns it. MOVE-02 uniqueness is database-enforced (no race window).
- **Atomicity (MOVE-01, BACK-09, BACK-10):** Service uses explicit `BeginTransaction()`. Sequence: `SELECT id, stock_quantity, supplier_value, deleted_at FROM products WHERE id=@id FOR UPDATE` → check `deleted_at` → check balance for Outbound → INSERT movement → UPDATE product (`stock_quantity` ± qty, `supplier_value` = new value for Inbound) → COMMIT. Rollback on any error.
- **Soft-deleted product rejection (MOVE-05):** When `products.deleted_at IS NOT NULL` at the locked-row read time, throw `ProductDeletedException(productId, code)` → 422 `PRODUCT_DELETED` with hint `"Produto '{code}' foi excluído. Movimentos não podem ser registrados para produtos excluídos."`.
- **Insufficient balance (MOVE-04):** When `Outbound` with `quantity > stockQuantity`, throw `InsufficientBalanceException(productId, code, requested, available)` with dynamic hint `"Reduza a quantidade para no máximo {available} ou registre uma entrada antes."` and `details: { requested, available, deficit }`.
- **Inverted values (MOVE-07):** `Inbound` requires `supplierValue` (and `saleValue` MUST be null/absent); `Outbound` requires `saleValue` (and `supplierValue` MUST be null/absent). Mismatch → 422 `INVALID_MOVEMENT_VALUES` with hint pointing at the correct field.
- **History listing (MOVE-08, MOVE-09):** `GET /api/stock-movements?productId=&startDate=&endDate=&page=&pageSize=` with the standard envelope `{ items, pagination, _links }`. Each item includes `productCode` and `productDescription` resolved via a single JOIN (zero N+1). Filter by date is inclusive on both ends; `productId` is optional.
- **History query budget (MOVE-09):** EXACTLY two SQL queries per page — one `SELECT ... JOIN products ... ORDER BY occurred_at DESC LIMIT @pageSize OFFSET @offset` + one `SELECT COUNT(*) FROM stock_movements WHERE ...`. Verified in execute-phase via `psql` query log or explicit test of `IDbConnectionFactory` query count.
- **Movement detail (MOVE-10):** `GET /api/stock-movements/{id}` returns the same shape as list items + `_links.self` + `_links.product`.
- **Immutability (MOVE-11):** NO UPDATE or DELETE endpoint on `stock-movements`. Confirmed by absence in `ProductsController.cs` style.
- **Soft-deleted products in history:** Filter by `productId` still works after soft-delete. Movement rows survive.

### Frontend layout (StockMovementsPage tabs)
- **Page structure:** `StockMovementsPage.vue` renders a tab strip (Entrada / Saída / Histórico) at the top. Active tab content swaps below.
- **Entrada tab:** `InboundForm.vue` — `BaseSearchableSelect` for product (`code — description`), quantity input (numeric), `supplierValue` (BR currency input, same comma-decimal mask as Phase 2). Submit direct (no confirmation — CONF-03). Success: toast green `Entrada registrada com sucesso`, form clears, page refreshes the history tab badge count if any indicator.
- **Saída tab:** `OutboundForm.vue` — same product searchable select. **Above the quantity input** show "Disponível: N unidades" (UX-08) updated on product selection. Submit → opens `ConfirmOutboundModal` (CONF-01) with resumo: `Produto: P001 — Notebook` / `Quantidade: 3` / `Valor de venda: R$ 1.500,00` / `Saldo atual: 10 → Saldo resultante: 7`. Confirm → POST → toast `Saída registrada` + balance display updates. Cancel → modal closes, form stays filled (Nielsen #3).
- **Histórico tab:** `MovementHistory.vue` — paginated table with columns `Data/hora`, `Tipo` (badge: Entrada green / Saída red), `Produto (code — description)`, `Quantidade`, `Valor` (formatted currency, source field depends on type), `Saldo após` (computed in service from `stock_quantity_after` if persisted, or omitted in v1). 4-state pattern. Filter controls above the table: product searchable select (optional), date range pickers (optional), Limpar filtros button.
- **`crypto.randomUUID()` Idempotency-Key (FRONT-12):** Generated inside the `useStockMovements.create()` composable on every submit; added to Axios request headers via `axiosInstance.post('/stock-movements', body, { headers: { 'Idempotency-Key': crypto.randomUUID() } })`. Frontend does NOT retry on transient failure — single submit, single key. Submit-button disabled while submission in flight to prevent double-click double-key.

### Backend error codes added in this phase (extending Phase 2's catalog)
- `MISSING_IDEMPOTENCY_KEY` (400) — header absent on movement POST
- `INSUFFICIENT_BALANCE` (422) — outbound quantity exceeds stock
- `PRODUCT_DELETED` (422) — movement attempted on soft-deleted product
- `INVALID_MOVEMENT_VALUES` (422) — wrong value field for movement type
- `MOVEMENT_NOT_FOUND` (404) — `GET /api/stock-movements/{id}` for non-existent ID

### Auto-resolved gray areas (auto mode, recommended defaults)

- **D-01 (Movement page UI structure):** **Tab strip** at the top (Entrada / Saída / Histórico). Active tab via Vue Router query param `?tab=entrada|saida|historico` (deep-linkable; defaults to `historico`). Tabs use brand-50 active background + brand-700 text per UI-SPEC. Sticky tab bar so it doesn't scroll out when history is long.
- **D-02 (Confirm Outbound modal copy — CONF-01):** Title `Confirmar saída?`. Body summary block (definition list):
  ```
  Produto:           {code} — {description}
  Quantidade:        {qty} {unit}
  Valor de venda:    R$ {saleValue formatted}
  Saldo atual:       {currentStock} unidades
  Saldo resultante:  {currentStock - qty} unidades
  ```
  Buttons: `Cancelar` (left, secondary, `data-autofocus`) + `Confirmar saída` (right, primary `brand-500` — NOT destructive, since it's a normal business operation).
- **D-03 (Disponível: N unidades display — UX-08):** Shown immediately when a product is selected in the Outbound form, formatted as `Disponível: 10 unidades` using `formatQuantity()` from Phase 2's `format.ts`. Below the field, helper text in caption size. Updates on product change. After successful Outbound submit, the display refreshes by re-fetching the product (cheap single-record GET) — OR the response payload includes `stockQuantityAfter` and the form derives the new display. Default: re-fetch on submit success (simpler).
- **D-04 (Searchable product dropdown behavior):** Use the `BaseSearchableSelect` authored in Phase 2. Fetches products on mount via `/api/products?pageSize=100`. If more than 100, client-side filter operates on the loaded set + shows "Carregue mais resultados ajustando a busca" hint when filter returns 0 for partial-match. Excluded soft-deleted products by default; toggle "incluir excluídos" reuses Phase 2 toggle (but defaults OFF for movement forms — you cannot move a deleted product, so showing them as selectable options confuses).
- **D-05 (History filters UX):** Filters above the table. `BaseSearchableSelect` for product (optional). `BaseInput type="date"` for start/end (optional). Apply on change (debounced 300ms for product select, immediate for dates). "Limpar filtros" link-button. Filter state persisted in URL query so refresh preserves view.
- **D-06 (Movement type badge in history):** `Entrada` = success-green (`bg-success-50 text-success-700`); `Saída` = danger-red (`bg-danger-50 text-danger-700`). Reuses `BaseBadge` from Phase 2.
- **D-07 (Idempotency-Replay UX):** Frontend does NOT surface "replay" to users — replay is invisible (silent success), since users would never intentionally re-submit. Replay happens if a user double-clicks or browser retries; the second response is `Idempotency-Replay: true` with status 200, and the SPA still shows the success toast. NO special "already submitted" message — just success.
- **D-08 (Outbound balance check timing):** Frontend pre-checks balance on Outbound submit (uses the cached `stockQuantity` from product fetch). If pre-check fails locally, shows inline error `Quantidade indisponível. Saldo: N` and disables submit. This avoids round-trip for the obvious case. Backend is still authoritative — if its DB-locked read sees less stock than the frontend believed (race), the 422 INSUFFICIENT_BALANCE response is surfaced via toast.
- **D-09 (Movement value field placement in history):** Single `Valor` column that shows `supplier_value` for Entrada rows and `sale_value` for Saída rows. Tooltip on hover clarifies which is which. Right-aligned currency formatting.
- **D-10 (Date range default):** Empty by default. If user activates one of the date fields, the other defaults to today on first focus. Cleared on "Limpar filtros".
- **D-11 (Idempotency-Key persistence):** Backend stores the `idempotency_key` on the movement row (already in `init.sql` per Phase 1 schema with `NOT NULL UNIQUE` constraint). On replay, the existing row is returned via that key lookup. The persistence is permanent (no TTL) — sufficient for the challenge scope.

### Claude's Discretion
- Exact tab strip CSS (height, border-bottom, active-indicator style)
- Whether to add a hidden "Saldo após" virtual column to history (default: NO — adds query complexity; can be derived if needed)
- Skeleton row count in the 4-state loading state of history (default: 5 rows)
- Confirm modal animation (reuses BaseModal centered variant defaults from Phase 2 UI-SPEC)
- Whether to compute `stockQuantityAfter` server-side on each movement insert (default: NO for v1 — derivable from the products row at read time; no historical "balance at time of movement" column needed)
- Exact polling/refetch behavior of the Disponível display after movement (default: re-fetch product on submit success)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project-level rules
- `.planning/PROJECT.md` — 31 key decisions (D1–D31), out-of-scope, constraints
- `.planning/REQUIREMENTS.md` §MOVE-01..11, FRONT-12, CONF-01 — Phase 3 scope (13 reqs)
- `.planning/ROADMAP.md` §"Phase 3: Stock Movements Vertical Slice" — goal + 5 success criteria

### Backend conventions (carry-forward)
- `backend/CLAUDE.md` — solution layout, namespace rules, package allowlist, Dapper aliasing, exception flow, error response shape, OpenAPI requirements, transaction patterns
- `backend/Inventory/Services/ProductService.cs` — Phase 2 reference for service pattern (concrete class, manual mapping, exception translation Postgres-23505 → typed exception)
- `backend/Inventory/Controllers/ProductsController.cs` — Phase 2 reference for controller pattern ([SwaggerOperation], [ProducesResponseType] coverage, XML docs, _links emission)
- `backend/Inventory/Repositories/ProductRepository.cs` — Phase 2 reference for Dapper inline SQL with snake_case → PascalCase aliases
- `backend/Inventory/Validators/CreateProductRequestValidator.cs` — Phase 2 reference for FluentValidation PT-BR messages
- `backend/Inventory/Exceptions/DomainException.cs` + concrete subclasses — pattern for new exceptions (D-10: typed constructors with hint built inside)
- `backend/Inventory/Middleware/ExceptionHandlingMiddleware.cs` — extend the switch with `InsufficientBalanceException`, `ProductDeletedException`, `InvalidMovementValuesException`, `MissingIdempotencyKeyException`, `MovementNotFoundException`
- `/home/thallysrc/Projects/BancoShu/BancoShu/Services/TransferService.cs` — reference for transactional multi-step service. **DO NOT copy the N+1 in `GetHistoryAsync`** — for our `GetMovementHistoryAsync` use JOIN'd SELECT instead

### Frontend conventions (carry-forward)
- `frontend/CLAUDE.md` — palette, primitives inventory, accessibility, brand
- `frontend/src/shared/components/Base*` — 15 primitives authored in Phase 2 (reuse all of them)
- `frontend/src/shared/composables/useToast.ts`, `useConfirm.ts`, `usePagination.ts` — composables (reuse)
- `frontend/src/shared/format.ts` — `formatCurrency`, `formatDate`, `formatQuantity` (reuse)
- `frontend/src/shared/labels.ts` — extend with `movementTypeLabel` (Inbound→Entrada, Outbound→Saída)
- `frontend/src/shared/api/client.ts` — Axios singleton with `ApiError` interceptor (reuse)
- `frontend/src/features/products/composables/useProducts.ts` — reference pattern for composable; new file `frontend/src/features/stock/composables/useStockMovements.ts`
- `frontend/src/features/products/components/ProductForm.vue` — reference for Vee-Validate + Zod schema + comma-decimal currency mask; mirror in InboundForm/OutboundForm
- `frontend/src/features/products/components/DeleteProductModal.vue` — reference for modal pattern (BaseModal + autofocus on safe button + locked copy)

### Phase 2 artifacts (read for context)
- `.planning/phases/02-products-vertical-slice/02-CONTEXT.md` — D-01..D-12 from Phase 2, still authoritative
- `.planning/phases/02-products-vertical-slice/02-UI-SPEC.md` — base primitives + validator message mirror table + tab patterns
- `.planning/phases/02-products-vertical-slice/02-04-SUMMARY.md`, `02-05-SUMMARY.md` — what backend/frontend Phase 2 delivered

### Schema (carry-forward)
- `init.sql` — `stock_movements` table already authored in Phase 1 with `idempotency_key uuid NOT NULL UNIQUE`, `product_id FK`, `type` (CHECK enum), `quantity`, `supplier_value` (NULL), `sale_value` (NULL), `occurred_at`, indexes on `(product_id, occurred_at DESC)` and `(occurred_at DESC)`

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets (from Phase 1 + Phase 2)
- **Backend infra:** `IDbConnectionFactory`, `ExceptionHandlingMiddleware`, `ErrorResponse`, `DomainException` hierarchy, Swagger XML + Annotations setup, FluentValidation pipeline
- **Backend Products references:** `ProductsController` for controller pattern, `ProductService` for service pattern, `ProductRepository` for Dapper pattern, `CreateProductRequestValidator` for validator pattern
- **Frontend primitives:** 15 `Base*` components (no new primitives needed unless something specific to history table — likely all covered)
- **Frontend composables:** `useToast`, `useConfirm`, `usePagination` — `usePagination` for the history table
- **Frontend utilities:** `format.ts`, `labels.ts` (extend with `movementTypeLabel`), `api/client.ts`
- **Frontend Products feature** as a reference template for `features/stock/`

### Established Patterns (must enforce)
- Backend file-scoped namespaces `Inventory.Api.{Folder}`, XML doc generation enabled
- Single-project N-tier — no Clean Architecture
- Identifiers English, user-facing strings PT-BR
- Service mapping manual inline
- Validators in PT-BR with messages byte-identical to Zod
- `_links` rel names: resource `self`/`product` (for movements); pagination `self`/`first`/`last`/`next`/`prev`
- Tab strip pattern (new in Phase 3 — additional UI-SPEC contract authored before plan)

### Integration Points
- New endpoints `POST/GET /api/stock-movements` and `GET /api/stock-movements/{id}` reachable via Vite proxy
- Frontend composable `useStockMovements` consumes Axios singleton
- `useStockMovements.create()` adds `Idempotency-Key: ${crypto.randomUUID()}` header
- `ExceptionHandlingMiddleware` switch extended for 5 new exception types
- Swagger UI picks up new operationIds: `createStockMovement`, `listStockMovements`, `getStockMovement`
- `JsonStringEnumConverter` already serializes `MovementType` enum (Inbound/Outbound) as strings

</code_context>

<specifics>
## Specific Ideas

- "Zero N+1" (D24, MOVE-09) is the most visible technical bar — verify with `EXPLAIN ANALYZE` or query count assertion that history listing is exactly 2 queries per request
- "Histórico imutável" (D17, MOVE-11) — no UPDATE/DELETE endpoint; this is a deliberate audit-trail decision the evaluator will notice
- "Idempotency obrigatória" (D21, MOVE-02) — header-required design is uncommon and signals operational discipline; the unique constraint enforces it at the DB layer
- Confirm modal on Saída with **full resumo** (CONF-01) — including "saldo resultante" — gives users the calculation Nielsen #5 (prevention) demands

</specifics>

<deferred>
## Deferred Ideas

- **Movement edit/delete** — explicitly out of scope (audit-trail immutability)
- **Bulk movements** — single movement per request
- **Transfer-between-warehouses** — single warehouse in v1
- **Kardex full report** — history listing is sufficient for v1
- **Low-stock alerts** — out of scope
- **Recurring/scheduled movements** — out of scope
- **`stockQuantityAfter` historical column** — derivable from current products row; v1 doesn't need it
- **Idempotency-Key TTL/expiration** — permanent storage in v1 (sufficient for challenge scope)
- **Replay UX disclosure** — silent replay; no "you already did this" message
- **Excel/CSV export of history** — out of scope

</deferred>

---

*Phase: 03-stock-movements-vertical-slice*
*Context gathered: 2026-05-16 (auto mode within autonomous chain)*
