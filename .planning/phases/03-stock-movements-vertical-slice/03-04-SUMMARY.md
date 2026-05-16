---
phase: 03-stock-movements-vertical-slice
plan: 04
subsystem: frontend
tags: [frontend, stock, components, vue, tabs, modal, history]
requirements_completed:
  - CONF-01
dependency_graph:
  requires:
    - "@/features/stock/types (MovementResponse, CreateInboundRequest, CreateOutboundRequest)"
    - "@/features/stock/schemas (createInboundSchema, createOutboundSchema)"
    - "@/features/stock/api (movementsApi — Plan 03-02; Idempotency-Key already wired)"
    - "@/features/stock/composables/useStockMovements (Plan 03-02)"
    - "@/features/products/api (productsApi.list, productsApi.getById — Plan 02)"
    - "@/features/products/types (ProductResponse — Plan 02)"
    - "@/shared/labels (movementTypeLabel — Plan 03-02)"
    - "@/shared/format (formatCurrency, formatDate, formatQuantity — Phase 2)"
    - "@/shared/composables/useToast (Phase 2)"
    - "@/shared/components/Base* (15 primitives from Phase 2 — composed verbatim)"
    - "vee-validate, @vee-validate/zod, zod, lucide-vue-next, vue-router (deps in package.json)"
  provides:
    - "InboundForm — direct-submit (no modal) stock-entry form composing BaseSearchableSelect + BaseInput + currency mask + useStockMovements.register"
    - "OutboundForm — Disponível helper (UX-08) + local balance pre-check (D-08) + opens CONF-01; never POSTs directly"
    - "ConfirmOutboundModal — CONF-01 verbatim copy (title + lead + 5 definition rows + Cancelar/Confirmar saída); data-autofocus on safe button"
    - "MovementHistory — 4-state region + filter bar + result-count meta + non-interactive table + BasePagination + URL state sync (productId/startDate/endDate/page)"
    - "StockMovementsPage — tab strip composition (WAI-ARIA tabs + URL ?tab= source-of-truth + v-if mounted panels)"
  affects:
    - "Plan 03-05 (smoke) — drives the full SPA flow end-to-end against backend (CONF-01 grep + Idempotency-Replay silent UX + zero-N+1 evidence)"
tech_stack:
  added: []
  patterns:
    - "Tab strip inlined per UI-SPEC (no BaseTabs primitive authored — composition lives in StockMovementsPage)"
    - "URL query is source-of-truth for tab + history filters; tab activation calls router.replace (not router.push) so back-button history isn't polluted by tab swaps"
    - "Form → modal → composable: OutboundForm opens CONF-01 on submit; modal's Confirm button triggers useStockMovements.register; INSUFFICIENT_BALANCE keeps modal open + updates local cache from apiError.details.available (race-mitigation per D-08)"
    - "Non-interactive history rows: plain <table> with BaseTable's visual styling (header bg-brand-50, row hover bg-surface) — BaseTable forces clickable rows which violates MOVE-11 (no detail surface in v1)"
    - "Movement-type badge color mapping: Inbound=success (ArrowDownToLine), Outbound=danger (ArrowUpFromLine); label via movementTypeLabel — no inline ternary"
    - "Idempotency-Replay UX is silent — SPA treats 201 (new) and 200 (replay) identically because movementsApi.register returns MovementResponse in both cases (D-07)"
key_files:
  created:
    - frontend/src/features/stock/components/InboundForm.vue
    - frontend/src/features/stock/components/OutboundForm.vue
    - frontend/src/features/stock/components/ConfirmOutboundModal.vue
    - frontend/src/features/stock/components/MovementHistory.vue
  modified:
    - frontend/src/features/stock/pages/StockMovementsPage.vue
    - frontend/src/shared/components/BaseInput.vue
decisions:
  - "OutboundForm owns BOTH the form state AND the modal mount (modalOpen ref); ConfirmOutboundModal is a pure-presentational child receiving (open, product, quantity, saleValue, isSubmitting) + emitting confirm/cancel. Rationale: keeps register() call in one place (the form's onConfirm handler) and makes race-mitigation (update local stockQuantity from apiError.details.available) trivially scoped."
  - "MovementHistory renders a plain <table> instead of BaseTable. Rationale: BaseTable forces tabindex=0 + click/Enter handlers + cursor-pointer on rows; UI-SPEC mandates non-interactive rows for v1 (MOVE-11 — no detail surface). Authoring a `clickable` prop on BaseTable would have been a Phase 2 retroactive change; cleaner to keep BaseTable's contract intact and use raw markup here. No new shared primitive authored."
  - "Tab strip lives inline in StockMovementsPage — no BaseTabs primitive. Rationale: only one tab surface in v1; UI-SPEC explicitly forbids extracting a primitive until a second use case emerges."
  - "BaseInput type union extended from text|number|email to text|number|email|date. Rationale: spec mandates native <input type=\"date\"> for filter inputs (browser-provided a11y, locale picker). Single-prop extension; no breaking change to existing usages."
  - "v-if (not v-show) on the three tab panels. Rationale: closed tabs must not run validators (Vee-Validate forms) or fetch (MovementHistory's onMounted hydrate). UI-SPEC explicitly forbids v-show here."
  - "OutboundForm cancels mid-flight close: onCancel returns early when isSubmitting is true. Rationale: prevents user from closing the modal during the POST and losing the spinner state."
metrics:
  duration: "~5 min"
  tasks_completed: 3
  files_changed: 6
  commits: 3
  completed_date: 2026-05-16
---

# Phase 03 Plan 04: Frontend Stock Components Summary

Built the five Vue 3 + TS components that complete the Stock Movements vertical slice — `InboundForm`, `OutboundForm`, `ConfirmOutboundModal` (CONF-01), `MovementHistory`, and the orchestrating `StockMovementsPage` with its tab strip. Every UX decision locked in 03-UI-SPEC is wired in; Plan 03-02's primitives (`useStockMovements`, `movementsApi`, schemas, types, `movementTypeLabel`) and Phase 2's 15 `Base*` components compose the entire surface. No new shared primitives were authored.

## What Was Built

### `frontend/src/features/stock/components/InboundForm.vue` (new)

Direct-submit Vee-Validate + Zod + currency-mask form (mirrors Phase 2 `ProductForm` pattern verbatim — Plan 02's locked template). No modal (per CONF-03 carry-forward — Entrada and Cadastro go direct without confirmation).

- Owns local `supplierValueRaw` ref for the BR-locale display string; `supplierValue` in the form values stays a JS number for Zod.
- Loads products via `productsApi.list(1, 100, false)` on mount (D-04: `includeDeleted=false` — deleted products are not movable).
- `validateOnBlur: true` per Phase 2 form contract; submit disabled while `!meta.valid || isSubmitting`.
- Submit success: `toast.success('Entrada registrada com sucesso')` (3s) + `resetForm()` + clear `supplierValueRaw`.
- Submit error: `VALIDATION_ERROR` → field-level errors via `setFieldError` + summary toast; `PRODUCT_DELETED` → toast hint + clear product select; everything else → toast `apiError.hint ?? apiError.message`.

### `frontend/src/features/stock/components/OutboundForm.vue` (new)

Same form skeleton as `InboundForm` plus the **UX-08 Disponível helper**, the **D-08 local balance pre-check**, and the **CONF-01 hand-off**.

- Owns `selectedProduct` ref derived from `values.productId` via `watch`.
- Renders the Disponível helper row verbatim per UI-SPEC §"Disponível: N unidades Helper Text Contract":
  - Hidden when no product selected (`v-if="selectedProduct"`).
  - Positive balance: `Disponível:` (muted) + bold count + ` unidades` (muted).
  - Zero balance: `text-danger` with `<AlertTriangle class="w-3 h-3 inline -mt-0.5" />` icon.
  - `aria-live="polite"` + `aria-atomic="true"` on the helper span.
- `preCheckError` computed surfaces `Quantidade indisponível. Saldo: {n}` below the input when `quantity > stockQuantity`; `submitDisabled` returns true while pre-check fails.
- Submit handler opens `ConfirmOutboundModal` (sets `modalOpen.value = true`) — **never POSTs directly**.
- `onConfirm` (called from the modal):
  - Calls `useStockMovements.register({ type: 'Outbound', ... })`.
  - Success: close modal + success toast + `resetForm()` + refresh disponível via `productsApi.getById(productId)` and update the local products cache (D-03).
  - `INSUFFICIENT_BALANCE`: modal stays open + toast `apiError.hint` + update local `selectedProduct.stockQuantity` from `apiError.details.available` (D-08 race-mitigation).
  - `PRODUCT_DELETED`: close modal + toast + clear product select.
  - `VALIDATION_ERROR`: close modal + field-level errors + summary toast.
- `onCancel` returns early when `isSubmitting` is true (cannot close mid-flight).

### `frontend/src/features/stock/components/ConfirmOutboundModal.vue` (new — CONF-01 locked copy)

Wraps `BaseModal` (centered variant) with the verbatim copy from 03-UI-SPEC §"CONF-01 — Outbound Confirmation Modal (LOCKED COPY)". Pure presentational — receives `(open, product, quantity, saleValue, isSubmitting)` props, emits `confirm` / `cancel`.

**Locked copy table (for auditors / Plan 03-05 grep):**

| Element | Copy |
|---------|------|
| Title (BaseModal `title` prop) | `Confirmar saída?` |
| Lead paragraph | `Esta operação registra uma saída de estoque. Confira o resumo abaixo antes de confirmar.` |
| Definition term — Produto | `Produto:` |
| Definition value — Produto | `{code} — {description}` |
| Definition term — Quantidade | `Quantidade:` |
| Definition value — Quantidade | `{formatQuantity(quantity)} unidades` |
| Definition term — Valor de venda | `Valor de venda:` |
| Definition value — Valor de venda | `{formatCurrency(saleValue)}` |
| Definition term — Saldo atual | `Saldo atual:` |
| Definition value — Saldo atual | `{formatQuantity(product.stockQuantity)} unidades` |
| Definition term — Saldo resultante | `Saldo resultante:` |
| Definition value — Saldo resultante | `{formatQuantity(remainingStock)} unidades` (in `font-semibold`) |
| Cancel button | `Cancelar` (secondary, `data-autofocus`) |
| Confirm button | `Confirmar saída` (primary, `bg-brand-500` — NOT destructive) |
| In-flight confirm label | `Registrando saída...` with `<Loader2 class="w-4 h-4 animate-spin" />` |

**Note on BaseModal integration:** `BaseModal` owns the `<h2>` rendered from its `title` prop (and the matching `aria-labelledby`). The locked title `Confirmar saída?` is passed verbatim via the `title` prop. The lead paragraph + definition list + footer slot render the rest. No structural deviation from the UI-SPEC — only the implementation detail of *where* the `<h2>` is emitted differs (the user-visible output is identical).

### `frontend/src/features/stock/components/MovementHistory.vue` (new)

Filter bar + result-count meta + 4-state region (loading / empty / filter-empty / error / data) + non-interactive `<table>` + `BasePagination`. Owns its own URL query sync (`productId`, `startDate`, `endDate`, `page`).

- Filter inputs decoupled from composable filters so the productId can debounce by 300ms (D-05) while dates fire immediately.
- D-10 date-pair smart default: focusing an empty date when the other end is set fills the focused input with today's ISO date (only on first focus; doesn't override user input on later focuses).
- 5 columns per UI-SPEC §"Surfaces — MovementHistory":
  1. `Data/hora` (`text-muted`) — `formatDate(occurredAt)`.
  2. `Tipo` — `BaseBadge variant=success|danger` with `ArrowDownToLine` / `ArrowUpFromLine` icon + label via `movementTypeLabel[m.type]`.
  3. `Produto` — `{productCode}` (font-medium) + dash (muted) + `{productDescription}`; `truncate max-w-xs`.
  4. `Quantidade` — `{formatQuantity(quantity)} un.` (`text-right tabular-nums`).
  5. `Valor` — D-09: Entrada → `formatCurrency(supplierValue)` with `title="Valor de fornecedor"`; Saída → `formatCurrency(saleValue)` with `title="Valor de venda"`.
- Empty state copy verbatim per UI-SPEC: `Nenhuma movimentação registrada` + body + CTA `Registrar entrada` → navigates `?tab=entrada`.
- Filter-empty state copy verbatim: `Nenhuma movimentação encontrada` + body + secondary CTA `Limpar filtros`.
- Error state via `BaseErrorState`: heading `Não foi possível carregar o histórico`, body via `apiError.hint ?? apiError.message`, trace caption `Código: {errorCode} · ID: {traceId}`, CTA `Tentar novamente`.

### `frontend/src/features/stock/pages/StockMovementsPage.vue` (rewritten — Phase 1 placeholder fully replaced)

Tab strip composed inline (no BaseTabs primitive) per UI-SPEC §"Tab Strip Composition":

- `<nav role="tablist" aria-label="Seções de movimentação" class="... border-b border-line h-12">` wrapping three `<button role="tab">` elements with roving tabindex (`activeTab === id ? 0 : -1`).
- URL query `?tab=entrada|saida|historico` is source-of-truth; default `historico` (D-01).
- `setTab(next)` calls `router.replace` + `nextTick` + `getElementById('tab-' + next).focus()` so screen readers announce the swap and keyboard users land in the right place.
- `onTabKeydown` implements the WAI-ARIA tabs pattern with automatic activation: ArrowLeft/Right/Up/Down cycle focus with wrap, Home/End jump to ends, Enter/Space activate.
- Tab content uses **`v-if` (NOT `v-show`)** so closed tabs don't run validators (Vee-Validate forms) or fetch (`MovementHistory.onMounted`).
- Browser tab title set to `Movimentação de Estoque · StockEasy` per Copywriting Contract.

### `frontend/src/shared/components/BaseInput.vue` (modified — minimal type-union extension)

Extended the `type` prop union from `'text' | 'number' | 'email'` to `'text' | 'number' | 'email' | 'date'`. Rationale: UI-SPEC mandates native `<input type="date">` for the history filter inputs (browser-provided locale picker + a11y). One-character change, no breaking impact on existing usages.

## Files Created / Modified

| File | Status | Public exports |
|------|--------|----------------|
| `frontend/src/features/stock/components/InboundForm.vue` | created | `<InboundForm />` |
| `frontend/src/features/stock/components/OutboundForm.vue` | created | `<OutboundForm />` |
| `frontend/src/features/stock/components/ConfirmOutboundModal.vue` | created | `<ConfirmOutboundModal :open :product :quantity :sale-value :is-submitting @confirm @cancel />` |
| `frontend/src/features/stock/components/MovementHistory.vue` | created | `<MovementHistory />` |
| `frontend/src/features/stock/pages/StockMovementsPage.vue` | rewritten | `<StockMovementsPage />` (already mounted on `/stock-movements` by Phase 1 router) |
| `frontend/src/shared/components/BaseInput.vue` | modified (1-char) | type union now includes `'date'` |

## Regression Boundary

**No Phase 2 components or shared primitives were touched** beyond the one-character extension to `BaseInput`'s type union. The 15 `Base*` primitives keep their public contracts:

```bash
# Sanity: every Base* component still exists and is untouched (except BaseInput type union).
git diff 9f18d4e..HEAD --stat frontend/src/shared/components/
# → only BaseInput.vue appears (+1/-1 line)
```

No new shared primitives, no new composables, no new types, no router changes. Plan 03-02's primitive surface (`movementsApi`, `useStockMovements`, schemas, types, `movementTypeLabel`) is consumed verbatim — zero re-exports or wrappers.

## Verification Status

The plan's `<verify>` block calls for `vue-tsc --noEmit` and `npm run build`. **Neither could be executed inside this worktree** — the same environment limitation Plan 03-02 documented:

- `frontend/node_modules/` is absent (worktree-scoped checkout).
- System Node is `v14.18.2` (vue-tsc requires Node 18+).

What was done in lieu of toolchain execution:

1. **Strict typing**: every component is fully typed (`<script setup lang="ts">`, `defineProps<Props>()` generic syntax, no `any`, no `as any`).
2. **Pattern adherence**: forms mirror Phase 2's `ProductForm` byte-for-byte for Vee-Validate setup + currency mask helpers; modal mirrors `DeleteProductModal`'s shape; 4-state region mirrors `ProductsPage`'s `viewState` pattern.
3. **Grep verification** (all acceptance criteria from the plan):
   - **Task 1 (3 components)**: 22/22 grep criteria pass — `Registrar entrada`, `Entrada registrada com sucesso`, `type: 'Inbound'`, `Disponível:`, `aria-live="polite"`, `Quantidade indisponível. Saldo:`, `type: 'Outbound'`, `INSUFFICIENT_BALANCE`, `Confirmar saída?`, `Esta operação registra uma saída de estoque`, all 5 definition terms, `data-autofocus` on secondary (Cancelar), `Registrando saída...`, no `variant="destructive"` on the confirm, no `as any`.
   - **Task 2 (MovementHistory)**: all 13 effective criteria pass — column headers (`Data/hora`, `Tipo`, `Produto`, `Quantidade`, `Valor`), `movementTypeLabel[m.type]`, `ArrowDownToLine`/`ArrowUpFromLine`, badge variant mapping via `:variant="m.type === 'Inbound' ? 'success' : 'danger'"`, all locked empty/filter-empty/error copies, `Valor de fornecedor`/`Valor de venda` tooltips, `300` (debounce), `router.replace` (URL sync), no `as any`.
   - **Task 3 (StockMovementsPage)**: all 13 criteria pass — `role="tablist"`, `role="tab"` (single template-loop instance counted as ≥ 1 — UI-SPEC explicitly allows the loop emit), all 3 tab labels, `:aria-selected="activeTab`, full keyboard contract token set, `router.replace`, `v-if` on entrada panel, `v-else-if` on saida panel, no `v-show`, no `RouterLink`, page title `Movimentação de Estoque`, tablist aria-label `Seções de movimentação`.
4. **Toolchain re-validation will happen** at the wave merge / Plan 03-05 smoke gate where `node_modules` + Node 20 are available. The verifier (`gsd-verifier`) running in the main checkout will re-run `npm run build` end-to-end before the phase closes.

The badge `variant.*success` / `variant.*danger` acceptance criterion uses the relaxed form per UI-SPEC because the implementation binds the variant dynamically (`:variant="m.type === 'Inbound' ? 'success' : 'danger'"`) rather than statically — both literals appear on the line, satisfying both alternates in the regex.

## Deviations from Plan

### Rule 2 (Auto-add missing critical functionality)

**BaseInput type union extended to include `'date'`**
- **Found during:** Task 2 (MovementHistory filter inputs)
- **Issue:** UI-SPEC §"MovementHistory — filter bar" mandates native `<input type="date">` for the date filters (locale-aware picker + browser a11y). `BaseInput`'s `type` prop union was `text | number | email` — `type="date"` would either silently fall back to `text` (TS-strict reject) or be a covert deviation.
- **Fix:** Added `'date'` to the union. One-character change. No impact on existing BaseInput usages (no `type='date'` callers existed before this plan).
- **Files modified:** `frontend/src/shared/components/BaseInput.vue` (1 line, +1/-1)
- **Commit:** `f60529e` (bundled with MovementHistory)

### Rule 3 (Auto-fix blocking issue) — informational

**MovementHistory uses a raw `<table>` instead of `BaseTable`**
- **Found during:** Task 2
- **Issue:** `BaseTable` (Phase 2) forces clickable rows: `tabindex="0"`, click handler, Enter/Space handlers, `cursor-pointer`. UI-SPEC §"Surfaces — MovementHistory" mandates **non-interactive rows** (MOVE-11 — no detail surface in v1, movements are immutable historical records).
- **Fix:** Render a plain `<table>` with the same visual styling that `BaseTable` applies (`bg-brand-50` header, `border-line` row separators, `hover:bg-surface`, `text-sm text-left border-collapse`). No new shared primitive authored — avoids retrofitting `BaseTable` with a `clickable` prop (Phase 2 contract stays intact).
- **Files modified:** `frontend/src/features/stock/components/MovementHistory.vue` (within the new file — no prior file existed)
- **Commit:** `f60529e`

### No other deviations

Plan executed exactly as written. CONF-01 copy is verbatim. Tab strip ARIA contract honored verbatim. `v-if` (not `v-show`) on tab panels. No new Base primitives, no router changes, no new types, no new composables.

## Idempotency-Replay UX (D-07 confirmation)

The SPA's response handling treats 200 (replay) identically to 201 (new) for toasts because `movementsApi.register` (Plan 03-02) returns a `MovementResponse` in **both** cases — the resolved promise shape is identical, so the form's `await register(...)` branch fires `toast.success` either way. No "you already submitted" banner; no replay disclosure surface. Wired by the abstraction Plan 03-02 already locked.

## Acceptance Criteria Status

| Criterion | Status |
|-----------|--------|
| InboundForm: `Registrar entrada` button + success toast + `type: 'Inbound'` payload | PASS |
| OutboundForm: Disponível helper + `aria-live="polite"` + pre-check error + opens CONF-01 + INSUFFICIENT_BALANCE branch | PASS |
| ConfirmOutboundModal: CONF-01 verbatim copy (title, lead, 5 definition rows, in-flight label, primary not destructive, data-autofocus on Cancelar) | PASS |
| MovementHistory: 5 columns + movement-type badge mapping + 3 empty/error states + filter bar with 300ms debounce + URL sync + D-09 tooltips | PASS |
| StockMovementsPage: `role="tablist"` + 3 tabs + `aria-selected` + keyboard contract + `router.replace` URL sync + `v-if` panels (not `v-show`) + no RouterLink | PASS |
| No new Base* primitives authored | PASS (BaseInput change is a 1-char type-union extension, not a new component) |
| No `as any` anywhere | PASS |
| `vue-tsc --noEmit` + `npm run build` | NOT EXECUTED in worktree (node_modules absent + Node 14 too old); re-validated at wave merge / Plan 03-05 smoke |

## Known Stubs

None. All UI surfaces are wired to real data sources:
- `InboundForm` / `OutboundForm` → `useStockMovements.register` → `movementsApi.register` (backend `POST /api/stock-movements` with Idempotency-Key).
- `MovementHistory` → `useStockMovements.fetchPage/setFilters/clearFilters` → `movementsApi.list` (backend `GET /api/stock-movements`).
- Product selects → `productsApi.list(1, 100, false)` (backend `GET /api/products`).
- Disponível helper → `selectedProduct.stockQuantity` from the cached product, refreshed via `productsApi.getById` after successful Outbound (D-03).

No empty defaults flow to UI; no placeholder text for "coming soon" features; no TODO/FIXME comments.

## Threat Flags

No new security-relevant surface introduced beyond what Plan 03-02 already wired (Idempotency-Key injection on POSTs). All endpoints consumed already exist in the backend threat model (Plans 03-01 / 03-03). The history filters are simple GET query params that the backend validates via FluentValidation (Plan 03-01 territory).

## Self-Check: PASSED

File presence:
- FOUND: frontend/src/features/stock/components/InboundForm.vue
- FOUND: frontend/src/features/stock/components/OutboundForm.vue
- FOUND: frontend/src/features/stock/components/ConfirmOutboundModal.vue
- FOUND: frontend/src/features/stock/components/MovementHistory.vue
- FOUND: frontend/src/features/stock/pages/StockMovementsPage.vue
- FOUND: frontend/src/shared/components/BaseInput.vue (modified)

Commits (all confirmed via `git log --oneline`):
- FOUND: d528c17 — feat(03-04): add InboundForm, OutboundForm, and CONF-01 modal
- FOUND: f60529e — feat(03-04): add MovementHistory with 4-state table, filters, URL sync
- FOUND: 86bbd02 — feat(03-04): rewrite StockMovementsPage with tab strip composition

## Phase 4 Hand-off Notes

- Movement detail drawer is intentionally deferred (MOVE-11 — rows are non-interactive in v1). If a detail surface is added post-v1, `BaseTable` could be extended with a `clickable` prop and `MovementHistory` migrated; the row data already contains the full `MovementResponse` payload.
- Sticky tab bar is deferred per UI-SPEC §"Don'ts" — re-evaluate if user feedback says scroll-out is a problem.
- Phase 4 Vitest specs can mount each component and assert against the locked copy strings using `findByText(...)` — every PT-BR surface in this plan is byte-locked in the SUMMARY tables above for grep / test stability.
- `frontend/src/shared/components/BaseInput.vue` type union extension is forward-compatible — future plans needing `<input type="time">` or `<input type="datetime-local">` can extend the same union without breaking changes.
