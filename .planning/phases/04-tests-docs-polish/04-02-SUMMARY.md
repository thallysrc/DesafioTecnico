---
phase: 04-tests-docs-polish
plan: 02
subsystem: frontend-tests
tags: [vitest, composables, frontend, tests]
requires:
  - 03-02 (useStockMovements composable)
  - 02-04 (useProducts composable)
provides:
  - vitest-infra (devDeps + vitest.config.ts + tests/setup.ts + `npm test` script)
  - useProducts-test-coverage (9 scenarios, mocked productsApi)
  - useStockMovements-test-coverage (9 scenarios, mocked movementsApi)
affects:
  - frontend/package.json (test scripts + 3 devDeps)
  - frontend/package-lock.json (380 transitive deps added)
tech-stack:
  added:
    - vitest@4.1.6
    - "@vue/test-utils@2.4.10"
    - happy-dom@20.9.0
  patterns:
    - "vi.mock('../api') seam isolates composables from Axios"
    - "Test fixtures via small `makeApiError` / `makeProduct` / `makeMovement` / `makePage` helpers (kept local per file)"
    - "Co-located *.test.ts next to production code (vitest.config.ts include: src/**/*.test.ts)"
key-files:
  created:
    - frontend/vitest.config.ts
    - frontend/tests/setup.ts
    - frontend/src/features/products/composables/useProducts.test.ts
    - frontend/src/features/stock/composables/useStockMovements.test.ts
  modified:
    - frontend/package.json
    - frontend/package-lock.json
decisions:
  - "Used Node 20 (nvm) for the Vitest install (vitest@4 minimum requirement). Node 14 from default shell is incompatible — evaluator must `nvm use 20` or have Node ≥ 18 on PATH"
  - "Adopted Vitest's `globals: true` flag per CONTEXT.md 'Claude's Discretion' so test bodies can use describe/it/expect/vi without imports if desired (we still import explicitly for IDE friendliness)"
  - "Test fixtures kept inline per file rather than in a shared `tests/fixtures/` module — under 2 files of duplication; promoting now would be premature abstraction"
  - "softDelete page-fallback test re-mocks productsApi.list per call with mockResolvedValueOnce + queues responses; mirrors what the production composable does (one call per side-effect)"
  - "Retry test for useStockMovements forces pagination.page=2 via a custom mock payload (not just makePage) since makePage hardcodes page=1"
metrics:
  completed: 2026-05-16
  duration: ~10 min execution (npm install dominated wall-clock at 2m)
  tasks: 3
  files_created: 4
  files_modified: 2
---

# Phase 4 Plan 02: Vitest Infra + Composable Tests Summary

**One-liner:** Stood up frontend Vitest 4.1 + happy-dom + @vue/test-utils 2.4 infra and added 18 unit tests covering `useProducts` and `useStockMovements` behavior with mocked api modules — satisfies TEST-05 with zero changes to production composables.

## What Shipped

### Infra (Task 1)

- `frontend/package.json` gained three devDeps and two scripts:
  - `vitest@^4.1.6` (resolved to 4.1.6)
  - `@vue/test-utils@^2.4.10` (resolved to 2.4.10)
  - `happy-dom@^20.9.0` (resolved to 20.9.0)
  - `"test": "vitest run"` (CI-local single-pass per TEST-07 / D-10)
  - `"test:watch": "vitest"` (DX convenience)
- `frontend/vitest.config.ts` mirrors `vite.config.ts`'s `@` alias, uses happy-dom env (D-06), enables `globals: true` (Claude's Discretion), points at `./tests/setup.ts`, and includes `src/**/*.test.ts` for co-location.
- `frontend/tests/setup.ts` is a deliberately empty placeholder — composable + component tests mock their own api modules so no global stub is needed yet.

### useProducts coverage (Task 2 — 9 scenarios)

`frontend/src/features/products/composables/useProducts.test.ts`:

1. starts with empty state and `loading=false`
2. `fetchPage` populates items + pagination + clears error on success
3. `fetchPage` rejection writes `error.value` (errorCode + hint preserved) and does NOT throw
4. `setShowDeleted(true)` flips flag + refetches page 1 with `includeDeleted=true`
5. `create` re-throws `DUPLICATE_CODE` and does NOT refetch
6. `create` success refetches page 1 with `(1, 30, false)` and returns the created product
7. `softDelete` with remaining items preserves current page
8. `softDelete` on the last item of page > 1 falls back to `page - 1` (useProducts.ts lines 70–78)
9. `retry()` re-uses `pagination.value.page`

### useStockMovements coverage (Task 3 — 9 scenarios)

`frontend/src/features/stock/composables/useStockMovements.test.ts`:

1. starts with empty filters and zero items
2. `fetchPage` forwards current filters (`productId`, `startDate`, `endDate`, `page`, `pageSize`) merged into the single object passed to `movementsApi.list`
3. `fetchPage` rejection writes `error.value` (errorCode preserved)
4. `setFilters` maps empty strings → `undefined` and resets to page 1
5. `clearFilters` resets filters to `{}` and refetches page 1
6. `register` success refetches page 1 and returns the created movement
7. `register` re-throws `INSUFFICIENT_BALANCE` with `details.available` + `deficit` preserved (D-08 contract for OutboundForm consumers)
8. `register` does NOT refetch on failure (`movementsApi.list` never called)
9. `retry()` re-runs `fetchPage` with current `pagination.page=2` and filters `productId='x'` preserved

## Versions Pinned (npm resolution)

| Package          | Range          | Installed |
| ---------------- | -------------- | --------- |
| vitest           | `^4.1.6`       | 4.1.6     |
| @vue/test-utils  | `^2.4.10`      | 2.4.10    |
| happy-dom        | `^20.9.0`      | 20.9.0    |

`npm install --save-dev vitest @vue/test-utils happy-dom` added 380 transitive packages to `package-lock.json` (vitest brings in `tinypool`, `tinyspy`, `vite-node`, etc.). Two moderate audit warnings from transitive deps; not actionable for this plan.

## Final `npm test` Output

```
> stockeasy-web@0.0.0 test
> vitest run

 RUN  v4.1.6 /home/thallysrc/Projects/DesafioTecnico/.claude/worktrees/agent-a5b8c94afc73e2df9/frontend

 Test Files  2 passed (2)
      Tests  18 passed (18)
   Start at  17:23:39
   Duration  468ms
```

| Metric      | Value |
| ----------- | ----- |
| Test Files  | 2 passed |
| Tests       | 18 passed |
| Duration    | 468ms |
| Failures    | 0 |

## Production Composables Untouched

```bash
$ git diff 2677b42 -- frontend/src/features/products/composables/useProducts.ts \
                       frontend/src/features/stock/composables/useStockMovements.ts
(empty)
```

Per D-26 (no retroactive refactors), neither composable was modified during this plan.

## Commits

| Task | Hash    | Type   | Description |
| ---- | ------- | ------ | ----------- |
| 1    | 98bcd78 | chore  | Vitest infra (devDeps + config + setup + scripts) |
| 2    | d925a54 | test   | useProducts composable coverage (9 scenarios) |
| 3    | 12b516b | test   | useStockMovements composable coverage (9 scenarios) |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Node version pinned to 20 (nvm) — Node 14 from system default is incompatible with Vitest 4**

- **Found during:** Task 1 (`npm install` for Vitest)
- **Issue:** The default shell's `PATH` resolved to `~/.nvm/versions/node/v14.18.2/bin/node`. Vitest 4 requires Node ≥ 18.
- **Fix:** Re-exported `PATH="$HOME/.nvm/versions/node/v20.20.0/bin:$PATH"` for every Bash invocation that ran `npm` / `npx`. No changes to repo files — purely a local environment switch the evaluator will replicate via their own `nvm use 20` (Node 20 LTS).
- **Files modified:** none
- **Commit:** N/A

**Auth gates:** none.

### Architectural Deviations

None — plan executed exactly as written.

## Known Stubs

None. All tests assert against real composable contracts; no placeholder data.

## Threat Flags

None — this plan adds test code only; no new network endpoints, auth paths, or trust-boundary changes.

## Evaluator Reproducibility

```bash
cd frontend
nvm use 20   # (or any Node ≥ 18 on PATH)
npm install
npm test
# → Test Files 2 passed (2)  Tests 18 passed (18)
```

## Self-Check: PASSED

- frontend/vitest.config.ts — FOUND
- frontend/tests/setup.ts — FOUND
- frontend/src/features/products/composables/useProducts.test.ts — FOUND
- frontend/src/features/stock/composables/useStockMovements.test.ts — FOUND
- Commit 98bcd78 — FOUND
- Commit d925a54 — FOUND
- Commit 12b516b — FOUND
- `npm test` exit code 0; 2 files / 18 tests passed
- `git diff` against base for `useProducts.ts` + `useStockMovements.ts` is empty (production composables untouched per D-26)
