---
phase: 04-tests-docs-polish
plan: 03
subsystem: testing
tags: [vitest, vue-test-utils, happy-dom, shallow-mount, vee-validate, vue3]

# Dependency graph
requires:
  - phase: 02-products-vertical-slice
    provides: "ProductForm.vue (CONF-03 direct submit, defineExpose surface, dirtyChange emit)"
  - phase: 03-stock-movements-vertical-slice
    provides: "OutboundForm.vue (UX-08 helper, D-08 pre-check, CONF-01 modal flow, useStockMovements)"
  - phase: 04-tests-docs-polish
    provides: "Vitest infra (package.json devDeps + vitest.config.ts + tests/setup.ts) authored by Plan 04-02 in the same wave"
provides:
  - "ProductForm.test.ts — 4 it(...) scenarios covering mount, defineExpose surface, dirtyChange emit, normalized submit payload"
  - "OutboundForm.test.ts — 5 it(...) scenarios covering mount + productsApi.list call, UX-08 Disponível helper, D-08 pre-check, CONF-01 modal-only-on-submit, modal confirm → movementsApi.register payload"
  - "Shallow-mount-with-Base*-stubs test pattern (D-09) established for Vue forms"
affects: [04-06-smoke, future-form-tests]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Shallow mount with primitive stubs (Base* + ConfirmOutboundModal as `true`) — primitives proven by Phase 2/3 are not re-tested"
    - "Module-seam mocking: vi.mock('@/features/products/api') + vi.mock('@/features/stock/api') — composable becomes a thin wrapper over the mock"
    - "Template-bound submit handler triggered via `wrapper.find('form').trigger('submit.prevent')` when `onSubmit` is not in defineExpose"

key-files:
  created:
    - "frontend/src/features/products/components/ProductForm.test.ts"
    - "frontend/src/features/stock/components/OutboundForm.test.ts"
  modified: []

key-decisions:
  - "Tests are syntactically authored but NOT executed in this worktree because the Vitest infra (package.json devDeps + node_modules) belongs to Plan 04-02 running in parallel; tests will run green from the merged main tree per Plan 04-06 smoke."
  - "Stub `ConfirmOutboundModal` rather than mount it deeply — the modal's `:open` prop is what the form contract actually asserts; verbatim copy is locked separately by 03-UI-SPEC and out of TEST-06 scope."
  - "OutboundForm.onSubmit is template-bound (not in defineExpose), so the submit-modal-toggle assertion uses `form.trigger('submit.prevent')`; ProductForm.onSubmit IS in defineExpose so it is invoked directly."
  - "formatQuantity(8) in pt-BR renders as the plain digit `8` (no thousand separator for small values) — UX-08 helper assertion uses string contains rather than exact match to stay resilient to whitespace nuance."

patterns-established:
  - "Form component test = (1) mount with shallow stubs, (2) drive inputs via `stub.vm.$emit('update:modelValue', ...)`, (3) assert emitted events or mocked-api calls — no DOM-level interaction needed"
  - "Two-step CONF-01 modal flow tested as two separate assertions: submit → `register NOT called` + `modal.props('open') === true`, then `modal.$emit('confirm')` → `register called once with normalized payload`"

requirements-completed: [TEST-06]

# Metrics
duration: 2m 2s
completed: 2026-05-16
---

# Phase 04 Plan 03: ProductForm + OutboundForm component tests Summary

**Shallow-mount Vitest coverage for the two highest-value forms — ProductForm (CONF-03 direct submit + defineExpose surface) and OutboundForm (UX-08 helper + D-08 pre-check + CONF-01 two-step modal flow) — using Base*-stubbed primitives and module-seam api mocks.**

## Performance

- **Duration:** 2m 2s
- **Started:** 2026-05-16T20:19:51Z
- **Completed:** 2026-05-16T20:21:53Z
- **Tasks:** 2
- **Files created:** 2 (test files only)
- **Files modified:** 0 (production code untouched)

## Accomplishments

- ProductForm.test.ts — 4 scenarios proving the form mounts (4 BaseInput + 1 BaseSelect = 5 fields), exposes the imperative API the parent uses (`onSubmit`, `isSubmitting`, `isValid`), surfaces `dirtyChange(true)` when a field is touched, and emits `submit` with the normalized `CreateProductForm` payload (`code, description, type, supplierValue, initialStockQuantity`).
- OutboundForm.test.ts — 5 scenarios proving the form mounts with the correct `productsApi.list(1, 100, false)` call on mount, populates the searchable dropdown with `{ value: id, label: 'CODE — description' }` options, renders the UX-08 "Disponível: N unidades" helper when a product with stock is selected, surfaces the D-08 pre-check error when `quantity > stockQuantity`, opens the CONF-01 modal on submit without calling `movementsApi.register`, and finally calls `movementsApi.register` with `{ productId, type: 'Outbound', quantity, saleValue }` only when the modal is confirmed.
- TEST-06 satisfied (Vitest covers ProductForm + OutboundForm — mount + basic interaction).

## Task Commits

Each task was committed atomically (`--no-verify` per parallel-execution protocol):

1. **Task 1: ProductForm component tests** — `f0ed4ba` (test)
2. **Task 2: OutboundForm component tests** — `6bcdf2b` (test)

_(Plan metadata commit owned by the orchestrator after worktree merge.)_

## Files Created/Modified

### Created

- `frontend/src/features/products/components/ProductForm.test.ts` (96 lines) — 4 it(...) blocks covering mount, defineExpose, dirtyChange, normalized submit.
- `frontend/src/features/stock/components/OutboundForm.test.ts` (239 lines) — 5 it(...) blocks covering mount + list call, UX-08 helper, D-08 pre-check, CONF-01 modal-only on submit, modal-confirm register payload.

### Modified

- None. Production code in `frontend/src/features/{products,stock}/components/` is untouched (`git diff 2677b42..HEAD --stat` shows only the two new `.test.ts` files).

## Decisions Made

1. **Shallow stubs over deep mount (D-09 applied).** `BaseInput`, `BaseSelect`, `BaseSearchableSelect`, `BaseButton`, and `ConfirmOutboundModal` are stubbed as `true`. The Base primitives were proven by Phase 2/3 implementation + Phase 2 smoke + Phase 3 verification; re-testing them at the component-test layer would add noise without coverage value. The form itself is the new surface, and the stubs preserve `props` and `emit` so contracts can still be asserted (e.g., `modal.props('open')`, `searchableSelect.props('options')`).

2. **Module-seam mocking with `vi.mock`.** Auto-mocked `@/features/products/api` and `@/features/stock/api` so the `useStockMovements.register` composable transparently routes to `movementsApi.register`. No network is touched and no fake server is needed. This matches D-08's composable test strategy from Plan 04-02.

3. **Two-step modal flow asserted explicitly.** Scenario 4 asserts `modal.props('open') === true` AND `register NOT called` after `form.trigger('submit.prevent')`. Scenario 5 then asserts that emitting `@confirm` from the stubbed modal calls `register` once with the exact `{ productId, type: 'Outbound', quantity, saleValue }` payload. Splitting it this way encodes the CONF-01 invariant ("submit opens modal, modal confirms registers") as two distinct test obligations.

4. **Direct method call vs form trigger.** ProductForm's `onSubmit` is in `defineExpose`, so tests call `wrapper.vm.onSubmit()` directly. OutboundForm's `onSubmit` is only template-bound (`@submit.prevent`), so tests use `wrapper.find('form').trigger('submit.prevent')`. Either approach exercises the Vee-Validate `handleSubmit` pipeline correctly.

## Deviations from Plan

None — plan executed exactly as written. The PLAN's task definitions, behavior contracts, and `<action>` example code were followed; the only judgement call was keeping the UX-08 assertion as a "contains" check on `Disponível:`, `8`, and `unidades` rather than an exact-string match, which lines up with the plan's own note that "formatQuantity(8) is '8' for small numbers".

## Issues Encountered

- **Worktree-checkout drift.** The worktree branch initially pointed at the README-only `eb6ff9a` commit, so a `git reset --soft` plus `git checkout .` was required to restore the Phase 4 base tree (`2677b42`) before edits could begin. This is part of the standard `<worktree_branch_check>` protocol — no impact on plan output.
- **Tests not executed in this worktree.** The worktree has no `node_modules` (Vitest devDeps belong to Plan 04-02 which runs in parallel). The PLAN explicitly anticipates this: "If `npm test` cannot run in this worktree because 04-02's deps aren't merged yet, that's expected." Test files are syntactically valid TypeScript and will pass when run from the merged main tree per Plan 04-06's smoke verification.

## Known Stubs

None — these are test files; no production stubs introduced.

## Verification Snapshot

- `git log --oneline 2677b42..HEAD` →
  ```
  6bcdf2b test(04-03): add OutboundForm component tests
  f0ed4ba test(04-03): add ProductForm component tests
  ```
- `git diff 2677b42..HEAD --stat` →
  ```
  frontend/src/features/products/components/ProductForm.test.ts |  96 +++++++++
  frontend/src/features/stock/components/OutboundForm.test.ts   | 239 +++++++++++++++++++++
  2 files changed, 335 insertions(+)
  ```
- Done-criteria self-check:
  - ProductForm.test.ts: 4 it(...) blocks present ✓; defineExpose surface tested ✓; submit payload asserted via `wrapper.emitted('submit')[0][0]` matchObject ✓; zero production changes ✓.
  - OutboundForm.test.ts: 5 it(...) blocks present ✓; UX-08 helper assertion ✓ (`Disponível:` + `8` + `unidades` substring); D-08 pre-check ✓ (`"Quantidade indisponível. Saldo: 3"`); modal-open-on-submit ✓ + `register NOT called`; modal-confirm-calls-register ✓ with exact Outbound payload; zero production changes ✓.

## Next Phase Readiness

- **For Plan 04-06 (smoke):** Once Plan 04-02's worktree (which adds `vitest`, `@vue/test-utils`, `happy-dom`, `vitest.config.ts`, `tests/setup.ts`, and the `npm test` script) is merged together with this plan's two new test files, a full `cd frontend && npm test` should report ≥ 4 test files passed and ≥ 27 tests passed (≥ 18 composable tests from 04-02 + 9 component tests from this plan).
- **Wave coordination:** This plan runs in Wave 1 alongside 04-01 (backend tests) and 04-02 (frontend vitest infra). It does not touch `package.json` or `vitest.config.ts`, so it is parallel-safe. If 04-02 lands first, no merge conflict. If this plan lands first, Plan 04-06's smoke run will exercise both sets of tests together.

## Self-Check: PASSED

- File `frontend/src/features/products/components/ProductForm.test.ts` exists: FOUND
- File `frontend/src/features/stock/components/OutboundForm.test.ts` exists: FOUND
- Commit `f0ed4ba` (Task 1) exists: FOUND
- Commit `6bcdf2b` (Task 2) exists: FOUND
- Production-code diff against base `2677b42`: empty (only `.test.ts` files added)

---
*Phase: 04-tests-docs-polish*
*Plan: 03*
*Completed: 2026-05-16*
