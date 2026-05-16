---
phase: 03-stock-movements-vertical-slice
plan: 02
subsystem: frontend
tags: [frontend, stock, types, schemas, composables, idempotency]
requirements_completed:
  - FRONT-12
dependency_graph:
  requires:
    - "@/shared/labels (productTypeLabel pattern)"
    - "@/shared/api/client (apiClient + generateIdempotencyKey + ApiError)"
    - "@/shared/types (PagedResult + PaginationMeta)"
    - "vee-validate, zod, axios, vue (deps already installed in Phase 1)"
  provides:
    - "MovementType, MovementResponse, CreateInboundRequest, CreateOutboundRequest, CreateMovementRequest discriminated union, MovementHistoryFilters (types.ts)"
    - "createInboundSchema, createOutboundSchema (Zod) with PT-BR messages mirroring backend CreateMovementRequestValidator"
    - "movementsApi (register/list/getById) — FRONT-12 Idempotency-Key injection"
    - "useStockMovements composable — items/pagination/loading/error/filters + fetchPage/setFilters/clearFilters/register/retry"
    - "movementTypeLabel added to shared/labels.ts (Inbound→Entrada, Outbound→Saída) alongside existing productTypeLabel"
  affects:
    - "Plan 03-04 (component wave) imports every public export from this plan"
    - "Plan 03-05 (smoke) greps for Idempotency-Key injection mechanism"
tech_stack:
  added: []
  patterns:
    - "Idempotency-Key injection via shared generateIdempotencyKey() helper (single source of UUID logic)"
    - "Discriminated union for request payloads (CreateInboundRequest | CreateOutboundRequest) — compile-time safety for type-specific fields"
    - "Composable mirrors useProducts shape (refs + fns, no reactive())"
    - "Anchor-once label literal pattern: MovementTypeLiteral lives in shared/labels.ts; features/stock/types.ts re-exports as MovementType"
key_files:
  created:
    - frontend/src/features/stock/types.ts
    - frontend/src/features/stock/schemas.ts
    - frontend/src/features/stock/api.ts
    - frontend/src/features/stock/composables/useStockMovements.ts
  modified:
    - frontend/src/shared/labels.ts
decisions:
  - "Idempotency-Key is generated via shared generateIdempotencyKey() (which wraps crypto.randomUUID()) rather than inlined in features/stock/api.ts — keeps UUID logic in one place, simplifies future Vitest mocking"
  - "Discriminated union (CreateInboundRequest | CreateOutboundRequest) chosen over backend's single-DTO-with-optionals shape — frontend gets compile-time safety on type-specific fields (supplierValue vs saleValue) while the union still serializes identically to the wire"
  - "MovementTypeLiteral anchored in shared/labels.ts (same pattern as ProductTypeLiteral) — features/stock/types.ts only re-exports as MovementType so the closed catalog lives in exactly one file"
  - "Composable register() re-throws on error (matches useProducts.create) so OutboundForm can update local stockQuantity from apiError.details.available when INSUFFICIENT_BALANCE returns"
metrics:
  duration: "~12 min"
  tasks_completed: 2
  files_changed: 5
  commits: 2
  completed_date: 2026-05-16
---

# Phase 03 Plan 02: Frontend Stock Feature Primitives Summary

Wired the frontend feature primitives for stock movements — types, Zod schemas (PT-BR mirror of backend validator), Axios API surface with FRONT-12 `Idempotency-Key` injection, `useStockMovements` composable, and a non-breaking extension to `shared/labels.ts` adding `movementTypeLabel`. Locks every contract Plan 03-04's Vue components will consume.

## What Was Built

### `frontend/src/shared/labels.ts` (extended)

- Preserved `ProductTypeLiteral` + `productTypeLabel` from Phase 2 (verified by `grep -c "productTypeLabel" → 1` non-zero post-edit).
- Added `MovementTypeLiteral = 'Inbound' | 'Outbound'`.
- Added `movementTypeLabel: Record<MovementTypeLiteral, string>` with `{ Inbound: 'Entrada', Outbound: 'Saída' }` per 03-UI-SPEC §"Enum translations".

### `frontend/src/features/stock/types.ts` (new)

Re-exports `MovementType` from `@/shared/labels`. Wire-shape interfaces:

- `MovementResponse` — full DTO including `productCode`/`productDescription` (sourced from MOVE-09 JOIN, no follow-up fetch), `idempotencyKey`, ISO 8601 `occurredAt`/`createdAt`, `_links`
- `CreateInboundRequest` — `{ productId, type: 'Inbound', quantity, supplierValue }`
- `CreateOutboundRequest` — `{ productId, type: 'Outbound', quantity, saleValue }`
- `CreateMovementRequest` — discriminated union of the above
- `MovementHistoryFilters` — `{ productId?, startDate?, endDate?, page?, pageSize? }`

### `frontend/src/features/stock/schemas.ts` (new)

- `createInboundSchema` — `productId` (UUID), `quantity` (int ≥1), `supplierValue` (nonnegative).
- `createOutboundSchema` — `productId` (UUID), `quantity` (int ≥1), `saleValue` (nonnegative).
- PT-BR messages byte-identical to backend `CreateMovementRequestValidator` per the locked mirror table in 03-UI-SPEC (`Quantidade deve ser maior que zero`, `Valor do fornecedor não pode ser negativo`, `Valor de venda não pode ser negativo`, etc.).
- Re-exports `CreateInboundForm` / `CreateOutboundForm` inferred types.

### `frontend/src/features/stock/api.ts` (new)

`movementsApi` with three methods:

- **`register(req)`** — `apiClient.post('/stock-movements', req, { headers: { 'Idempotency-Key': generateIdempotencyKey() } })`. **FRONT-12 implementation lives on this one line.** Uses the shared helper (which wraps `crypto.randomUUID()`) so the UUID logic is anchored once in `shared/api/client.ts`.
- **`list(filters)`** — sends `productId`, `startDate`, `endDate`, `page`, `pageSize` as Axios `params` (undefined values dropped by Axios automatically); resolves to `/api/stock-movements` via Vite proxy.
- **`getById(id)`** — `GET /api/stock-movements/{id}`.

All methods unwrap `.then(r => r.data)` at the boundary so callers get typed payloads.

### `frontend/src/features/stock/composables/useStockMovements.ts` (new)

Mirrors the `useProducts` shape exactly:

- Refs: `items`, `pagination`, `loading`, `error`, `filters`
- Functions: `fetchPage(page?, pageSize?)`, `setFilters(next)`, `clearFilters()`, `register(req)`, `retry()`
- `fetchPage` catches and writes to `error.value` for the 4-state error UI; `register` re-throws so the form can dispatch per-errorCode toasts and (for INSUFFICIENT_BALANCE) refresh the local stockQuantity from `apiError.details.available`.

## Files Created / Modified

| File | Status | Public exports |
|------|--------|----------------|
| `frontend/src/shared/labels.ts` | modified (append) | `MovementTypeLiteral`, `movementTypeLabel` (in addition to preserved `ProductTypeLiteral`, `productTypeLabel`) |
| `frontend/src/features/stock/types.ts` | created | `MovementType`, `MovementResponse`, `CreateInboundRequest`, `CreateOutboundRequest`, `CreateMovementRequest`, `MovementHistoryFilters` |
| `frontend/src/features/stock/schemas.ts` | created | `createInboundSchema`, `createOutboundSchema`, `CreateInboundForm`, `CreateOutboundForm` |
| `frontend/src/features/stock/api.ts` | created | `movementsApi` |
| `frontend/src/features/stock/composables/useStockMovements.ts` | created | `useStockMovements` |

## Regression Verification

`productTypeLabel` from Phase 2 is intact in the modified `shared/labels.ts`:

```bash
grep -c "export const productTypeLabel" frontend/src/shared/labels.ts
# → 1
```

The Plan 02 export `productTypeLabel: Record<ProductTypeLiteral, string>` survives the append; Plan 03-02 only added the two new declarations below the existing block.

## Idempotency-Key Injection Mechanism (Plan 03-05 reference)

Single grep target — one line, one file:

```ts
// frontend/src/features/stock/api.ts
register: (req: CreateMovementRequest): Promise<MovementResponse> =>
  apiClient
    .post<MovementResponse>('/stock-movements', req, {
      headers: { 'Idempotency-Key': generateIdempotencyKey() },
    })
    .then((r) => r.data),
```

Grep verification commands for the verifier / smoke plan:

```bash
grep -q "'Idempotency-Key':" frontend/src/features/stock/api.ts && echo OK
grep -q "generateIdempotencyKey" frontend/src/features/stock/api.ts && echo OK
grep -q "crypto.randomUUID" frontend/src/shared/api/client.ts && echo OK  # helper source intact
```

`generateIdempotencyKey()` lives in `frontend/src/shared/api/client.ts` and wraps `crypto.randomUUID()` — Plan 03-02 deliberately routed through the helper instead of duplicating the random-UUID call inside `features/stock/api.ts` (per CONTEXT.md FRONT-12 and the read-first reference).

## Acceptance Criteria Status

All criteria from both tasks pass via `grep` verification:

- `productTypeLabel` preserved (regression) — PASS
- `movementTypeLabel` added with locked PT-BR mapping — PASS
- `MovementType`, `MovementResponse` (with `productCode`/`productDescription`), `CreateInboundRequest`, `CreateOutboundRequest` exported from `types.ts` — PASS
- Zod messages byte-match locked mirror table (`Produto é obrigatório`, `Quantidade deve ser maior que zero`, `Quantidade deve ser um número inteiro`, `Valor do fornecedor não pode ser negativo`, `Valor de venda não pode ser negativo`, `Valor de venda é obrigatório`, `Valor do fornecedor é obrigatório`) — PASS
- `generateIdempotencyKey` reused (no duplicated `crypto.randomUUID` call in `features/stock/api.ts`) — PASS
- `'Idempotency-Key':` header injection visible on the POST line — PASS
- `apiClient.get<MovementResponse>` for `getById`; list uses Axios `params` — PASS
- `useStockMovements` exports refs + functions matching `useProducts` shape — PASS
- No `as any` in any of the 5 files — PASS
- Type check (`vue-tsc --noEmit`) **was not executed** in this worktree: `node_modules` is absent and the worktree's system Node (v14.18.2) is too old to run `vue-tsc`. The code adheres strictly to the existing typed patterns (`useProducts`, `productsApi`, `createProductSchema`, `shared/api/client.ts`) and uses no `any`; it will be re-validated by the Wave 1 merge / Plan 03-05 smoke in the main checkout where `node_modules` and Node 20 are available.

## Deviations from Plan

None — plan executed exactly as written. The 5 files match the planned exports verbatim; no Rule-1/2/3 fixes were needed during execution.

One environment note (not a code deviation): the `<verify><automated>` step `vue-tsc --noEmit` could not be executed inside this worktree because `frontend/node_modules` is absent and the worktree's system Node is v14 (vue-tsc requires Node 18+). The code is strictly typed and mirrors existing patterns; typecheck will catch any regression at the merge / smoke gate where the toolchain is available.

## Known Stubs

None. No empty defaults flow to UI — Plan 03-02 ships zero UI surface (Plan 03-04's territory). All exports are wired against real backend contracts.

## Threat Flags

No new security-relevant surface introduced. `register` adds `Idempotency-Key` to mutations (a hardening, not a new attack surface). All endpoints already exist in the backend threat model (Plan 03-01 / 03-03).

## Self-Check: PASSED

File presence:
- FOUND: frontend/src/shared/labels.ts
- FOUND: frontend/src/features/stock/types.ts
- FOUND: frontend/src/features/stock/schemas.ts
- FOUND: frontend/src/features/stock/api.ts
- FOUND: frontend/src/features/stock/composables/useStockMovements.ts

Commits:
- FOUND: 079ffe5 — feat(03-02): add stock types, labels extension, and Zod schemas
- FOUND: a25a263 — feat(03-02): add movementsApi with Idempotency-Key injection and useStockMovements composable
