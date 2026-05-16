---
phase: 03-stock-movements-vertical-slice
plan: 05
subsystem: integration-smoke + documentation
tags: [smoke, integration, e2e, documentation, verification, zero-n+1, idempotency, agentic]
requirements_completed:
  - MOVE-01
  - MOVE-02
  - MOVE-03
  - MOVE-04
  - MOVE-05
  - MOVE-06
  - MOVE-07
  - MOVE-08
  - MOVE-09
  - MOVE-10
  - MOVE-11
  - FRONT-12
  - CONF-01
dependency_graph:
  requires:
    - "Plan 03-01 (backend foundation: entities, DTOs, repository, validator, 5 typed exceptions, middleware doc)"
    - "Plan 03-02 (frontend primitives: types, schemas, api with Idempotency-Key injection, useStockMovements composable, labels)"
    - "Plan 03-03 (StockMovementService transactional + StockMovementsController + DI in Program.cs)"
    - "Plan 03-04 (5 Vue components: InboundForm, OutboundForm, ConfirmOutboundModal, MovementHistory, StockMovementsPage)"
    - "Phase 2 (Products vertical slice — exists as the regression baseline + provides POST /api/products for seeding)"
    - "Phase 1 (docker-compose foundation — boots PG + .NET + Vue)"
  provides:
    - "03-VERIFICATION.md (742 lines, structured per ROADMAP success criteria + MOVE-* matrix + Sign-off + Phase 4 hand-off)"
    - "README.md additive extension (Phase 3 endpoints + frontend route + agentic-friendly bullets for new operationIds/errorCodes/idempotency/zero-N+1/immutability)"
    - "Postgres log evidence proving exactly 2 SQL statements per history page (zero N+1 at the DB plane)"
    - "Swagger contract verification: 3 new operationIds present + 4 Phase 2 preserved + 3 new schemas + Idempotency-Key parameter + MovementType enum as strings"
    - "6 Manual UAT items handed off to Phase 4 for live-browser verification"
  affects:
    - "Phase 4 (Tests, Docs & Polish) — receives the manual UAT items + the deferred xUnit/Vitest coverage matrix"
tech_stack:
  added: []
  patterns:
    - "Statement-logging method for zero-N+1 verification (fallback from pg_stat_statements which requires shared_preload_libraries restart in postgres:16-alpine)"
    - "byte-identical replay diff via `diff <(jq -S) <(jq -S)` — strongest possible proof of MOVE-03 payload identity"
    - "source-grep evidence for frontend contracts (CONF-01 locked copy, tab-strip ARIA, FRONT-12 idempotency-key injection) — appropriate when no browser-driver is in scope"
    - "Manual UAT hand-off pattern (from Phase 2's 02-HUMAN-UAT.md) embedded inline in 03-VERIFICATION.md instead of a separate file (single source of truth for verification artifacts)"
key_files:
  created:
    - .planning/phases/03-stock-movements-vertical-slice/03-VERIFICATION.md
    - .planning/phases/03-stock-movements-vertical-slice/03-05-SUMMARY.md
  modified:
    - README.md
decisions:
  - "Fell back to `log_statement = 'all'` for zero-N+1 evidence — pg_stat_statements requires `shared_preload_libraries` (config-time only, no `ALTER SYSTEM` workaround in postgres:16-alpine). The fallback produced cleaner evidence anyway: the full SQL is captured inline in the doc, plus parameter values, plus timing — `pg_stat_statements` would have given normalized query strings with `$N` placeholders only."
  - "CONF-01 definition-term grep adjusted from `>Produto:<` to multi-line tolerant `grep -q 'Produto:'` + structural `grep -B1 <dt>` verification. The plan's verbatim grep assumes single-line HTML but Vue templates indent across lines — the locked copy is byte-correct; only the regex needed loosening."
  - "Manual UAT items embedded inline in 03-VERIFICATION.md (under §'Manual UAT items') instead of creating a separate 03-HUMAN-UAT.md. Rationale: Phase 2 created a separate file, but a 6-item hand-off doesn't justify the file split, and keeping verification artifacts in one place makes the Sign-off cross-references easier to follow."
  - "README extension prefers concrete-then-general structure: endpoint rows (with exact operationIds + Idempotency-Key semantics + JOIN'd-list note) → agentic-friendly bullets (errorCodes + immutability + zero-N+1) → frontend route block (tab strip + CONF-01 modal flow + Disponível helper). Reader can grep `operationId: createStockMovement` directly OR scan the agentic-friendly story."
  - "Phase 3 status flipped to 'entregue' in the README status table (was 'próxima'). Phase 4 remains 'final'. Status reflects all 13 requirements verified in this smoke."
metrics:
  duration: "10m 2s"
  tasks_completed: 5
  files_changed: 3
  commits: 5
  completed_date: 2026-05-16
---

# Phase 03 Plan 05: E2E Integration Smoke + Documentation Summary

End-to-end smoke for the Stock Movements vertical slice against the live `docker compose up -d --build` stack (postgres + backend + frontend). Captures evidence for all 13 Phase 3 requirements (MOVE-01..11 + FRONT-12 + CONF-01) into a single 742-line `03-VERIFICATION.md` doc, updates `README.md` additively with the new endpoints + frontend route + agentic-friendly bullets, and hands 6 manual UAT items to Phase 4. No application code was modified — this plan ships pure verification + documentation.

## What Was Verified, In Which Task

| Task | Requirement(s) | Evidence type | Section in 03-VERIFICATION.md |
| ---- | -------------- | ------------- | ------------------------------ |
| **Task 1** — Happy-path curl matrix | MOVE-01, MOVE-06, MOVE-08, MOVE-09 (contract), MOVE-10, MOVE-11 | API calls + body diffs + product-state proof | §Criterion 1, §Criterion 2 (atomicity halves), §MOVE-08, §MOVE-09 (contract), §MOVE-10, §MOVE-11 |
| **Task 2** — Unhappy-path matrix + replay identity | MOVE-02, MOVE-03, MOVE-04, MOVE-05, MOVE-07 + negative MOVE-10 | API calls + canonical error body assertions + jq -S byte-diff for replay | §MOVE-02 through §MOVE-07 + §MOVEMENT_NOT_FOUND |
| **Task 3** — Zero-N+1 + Swagger contract | MOVE-09 (DB-plane evidence) + Phase 2 regression | Postgres log slice (`log_statement = 'all'`) + jq/grep against swagger.json | §MOVE-09 (zero N+1 raw SQL + count) + §Swagger Contract |
| **Task 4** — Frontend SPA smoke + source-grep | FRONT-12, CONF-01, tab-strip ARIA, MovementHistory D-06 | curl + grep of `.vue` / `.ts` sources for locked copy, ARIA tokens, idempotency injection | §SPA bundle, §FRONT-12, §Tab-strip ARIA, §CONF-01, §Movement-type badge mapping, §Manual UAT items |
| **Task 5** — README + finalization | (no new requirements — sign-off + regression check + hand-off) | README extension (additive) + Sign-off cross-refs + Known Limitations | §Regression Sanity + §Sign-off + §Known Limitations / Phase 4 Hand-off |

**Requirements coverage:** 13/13. All MOVE-01..11 + FRONT-12 + CONF-01 have grep-verifiable + curl-verifiable evidence anchored to specific sections.

## Zero-N+1 Method That Actually Worked

The plan's `<query-count-strategy>` preferred **`pg_stat_statements`** (method (a)) and listed **statement logging** (method (b)) as a fallback. Method (a) **failed** in postgres:16-alpine:

```
$ docker compose exec -T postgres psql -U stockeasy -d stockeasy -c "CREATE EXTENSION IF NOT EXISTS pg_stat_statements;"
CREATE EXTENSION

$ docker compose exec -T postgres psql -U stockeasy -d stockeasy -c "SELECT pg_stat_statements_reset();"
ERROR:  pg_stat_statements must be loaded via shared_preload_libraries
```

`pg_stat_statements` requires the library to be listed in `shared_preload_libraries` in `postgresql.conf` BEFORE Postgres starts — `ALTER SYSTEM SET shared_preload_libraries = 'pg_stat_statements'` would require a cluster restart. Reconfiguring the compose image just for this verification would have introduced more change surface than it solved.

Fell back to **method (b) — `log_statement = 'all'`**:

```bash
docker compose exec -T postgres psql -U stockeasy -d stockeasy -c "ALTER SYSTEM SET log_statement = 'all';"
docker compose exec -T postgres psql -U stockeasy -d stockeasy -c "SELECT pg_reload_conf();"

# Snapshot before/after, compute new lines, slice the diff
docker compose logs --no-color postgres > /tmp/03-05/pg-log-before.txt
curl -fsS "http://localhost:8080/api/stock-movements?pageSize=100" > /tmp/03-05/list-page1.json
docker compose logs --no-color postgres > /tmp/03-05/pg-log-after.txt
```

**Result:** exactly **2** `execute <unnamed>` blocks against `stock_movements` per history page — one JOIN'd `SELECT … FROM stock_movements m INNER JOIN products p` (paged) and one `SELECT COUNT(*) … FROM stock_movements m INNER JOIN products p` (same WHERE, no ORDER/LIMIT). The full SQL with parameter values is captured verbatim in 03-VERIFICATION.md §MOVE-09. **Cleaner evidence than `pg_stat_statements` would have produced**, because Postgres logs the full prepared-statement text (not the normalized form) plus the parameter binding.

## Manual UAT Items Handed to Phase 4 / Human Evaluator

Six items live in §"Manual UAT items" of 03-VERIFICATION.md. They require a real browser session (no Playwright/Cypress in Phase 3 scope):

1. **CONF-01 visual flow** — open `/stock-movements?tab=saida`, register a Saída with qty ≤ stock; verify the 5 definition rows render with correct numbers + Cancelar receives autofocus + Confirmar is brand-primary (NOT destructive red).
2. **CONF-01 Esc dismiss** — press Esc while CONF-01 is open; modal closes; form data preserved.
3. **INSUFFICIENT_BALANCE inside CONF-01** — trigger qty > stock from inside the modal; modal stays open; toast surfaces `apiError.hint`; Disponível helper in the parent form refreshes from `apiError.details.available`.
4. **Default tab routing** — navigate to `/stock-movements` with no `?tab` query param; default is `historico` (D-01); MovementHistory mounts.
5. **Tab strip keyboard navigation** — ArrowRight on the active tab cycles to the next; focus follows; URL `?tab=` updates via `router.replace`; corresponding `v-if` panel mounts.
6. **Network failure recovery** — `docker compose stop backend`; hit `Tentar novamente` in the history error state; CTA triggers refetch; toast/inline copy mentions network failure.

These items are **NOT blockers** for Phase 3 completion — the API + DB plane is fully verified at the wire level, the SPA bundle loads, the source-level contracts are grep-locked. Phase 4 picks up live-browser + Vitest/xUnit coverage.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 — Blocking issue] `pg_stat_statements` could not be loaded**

- **Found during:** Task 3
- **Issue:** The plan's preferred method (`pg_stat_statements`) requires `shared_preload_libraries` to be set BEFORE Postgres starts. `CREATE EXTENSION` succeeded, but every query to `pg_stat_statements` failed with `ERROR:  pg_stat_statements must be loaded via shared_preload_libraries`.
- **Fix:** Switched to method (b) — `log_statement = 'all'` per the plan's explicit fallback strategy. Used a before/after log snapshot to slice the relevant lines for the single history-page request. **Documented inline in 03-VERIFICATION.md §MOVE-09 and in §"Zero-N+1 Method That Actually Worked" of this summary.**
- **Files modified:** none (plan explicitly authorized this fallback)
- **Commit:** `5ab73cf`

**2. [Rule 3 — Blocking issue] CONF-01 verbatim grep patterns assume single-line HTML**

- **Found during:** Task 4
- **Issue:** The plan's grep patterns for the 5 CONF-01 definition terms (`>Produto:<`, `>Quantidade:<`, etc.) assume `<dt>Produto:</dt>` on one line. Vue templates indent multi-line, so the source has `<dt class="…">\n        Produto:\n      </dt>` and the verbatim greps return no matches.
- **Fix:** Adjusted to a multi-line-tolerant pattern: `grep -q "Produto:"` for term presence + `grep -B1 "Produto:$"` for structural verification that each term is preceded by a `<dt>` block. All 5 terms verified present and structurally correct. **Locked CONF-01 copy is byte-correct in source; only the regex shape needed loosening.**
- **Files modified:** none (source matches the spec)
- **Commit:** `f40da50`

### No other deviations

The plan executed exactly as written. Stack booted on the first `docker compose up -d --build`. All 5 ROADMAP success criteria are verified with evidence cross-referenced in §Sign-off. Phase 2 regression-clean.

## Deferred Issues

None — the 6 Manual UAT items are explicit Phase 4 hand-offs per the plan's scope boundary ("Plan 03-05 cannot drive a real browser"), not deferrals.

## Authentication Gates

None — v1 has no auth surface.

## Known Stubs

None — every endpoint hit returned real data from a real Postgres row through real Dapper SQL. No placeholder defaults, no mock data, no "coming soon" surfaces. The frontend SPA shell was served by the actual Vite dev server (with hot-reload active). The CONF-01 modal copy is grep-locked in real Vue source. The Manual UAT items document UI behavior that is implemented but cannot be observed without a browser — they are not stubs.

## Threat Flags

No new security-relevant surface introduced. Plan 03-05 ships only documentation + verification — zero new code, zero new endpoints, zero new auth/network/file paths. The threat model from Plans 03-01 / 03-03 (Idempotency-Key on POST, FluentValidation on input, SELECT FOR UPDATE on the product row, soft-deleted product rejected at the service layer) is unchanged.

## Files Created / Modified

| File | Status | What |
| ---- | ------ | ---- |
| `.planning/phases/03-stock-movements-vertical-slice/03-VERIFICATION.md` | created (742 lines) | Smoke evidence per MOVE-* requirement + zero-N+1 raw SQL + Swagger contract + Frontend source-level contracts + 6 Manual UAT items + Sign-off (5/5 ROADMAP SCs) + Known Limitations / Phase 4 hand-off |
| `.planning/phases/03-stock-movements-vertical-slice/03-05-SUMMARY.md` | created (this file) | Plan execution summary |
| `README.md` | modified (additive) | 3 new endpoint rows for `/api/stock-movements`; agentic-friendly bullets extended with new operationIds (3) + errorCodes (5) + idempotency-replay semantics + MOVE-11 immutability + zero-N+1 surface; new frontend `/stock-movements?tab=…` block (tab strip ARIA, direct Entrada, CONF-01 for Saída, non-interactive history, Disponível helper); Status table marks Phase 3 as **entregue** |

## Acceptance Criteria — Final Status

| Criterion | Status |
| --------- | ------ |
| 03-VERIFICATION.md exists, structured per ROADMAP + MOVE-* matrix | PASS (742 lines, §Criterion 1-5 + §MOVE-01..11 + §FRONT-12 + §CONF-01 + §Sign-off + §Known Limitations) |
| All 13 Phase 3 requirements have grep + curl evidence | PASS (Coverage matrix at end of doc shows 13/13) |
| Zero-N+1 proven at the database level | PASS (postgres log slice shows 2 `execute <unnamed>` blocks per page, full SQL captured) |
| Swagger contract preserves Phase 2 + adds Phase 3 operationIds | PASS (3 new + 4 Phase 2 each count=1; 3 new schemas exposed; Idempotency-Key parameter declared; MovementType enum as strings) |
| README is additively updated | PASS (Phase 1 + Phase 2 sections preserved; `grep -q '/api/products' README.md` passes; 7 new lines added for Phase 3 endpoints + frontend route) |
| All Phase 2 endpoints + SPA routes still work | PASS (§Regression Sanity in 03-VERIFICATION.md) |
| Docker stack torn down before exit | PENDING — final teardown happens after this SUMMARY is committed |

## Commits

| Hash | Subject |
| ---- | ------- |
| `3aaba6c` | docs(03-05): capture happy-path smoke evidence (MOVE-01/06/08/09/10/11) |
| `23592c1` | docs(03-05): capture unhappy-path smoke evidence + replay payload identity (MOVE-02/03/04/05/07) |
| `5ab73cf` | docs(03-05): capture zero-N+1 query-count evidence + Swagger contract (MOVE-09) |
| `f40da50` | docs(03-05): capture frontend SPA smoke + source-level contracts (FRONT-12, CONF-01, tab-strip ARIA) |
| `5d45821` | docs(03-05): finalize verification doc (Sign-off + hand-off) + README Phase 3 capabilities |

(The final `docs(03-05): complete E2E smoke plan summary` commit lands after this file is staged.)

## Self-Check: PASSED

**Files verified to exist:**

- FOUND: `.planning/phases/03-stock-movements-vertical-slice/03-VERIFICATION.md`
- FOUND: `.planning/phases/03-stock-movements-vertical-slice/03-05-SUMMARY.md`
- FOUND: `README.md` (modified — `grep -q '/api/stock-movements' README.md` passes; `grep -q '/api/products' README.md` still passes)

**Commits verified to exist** (via `git log --oneline -6`):

- FOUND: `3aaba6c` — happy-path smoke
- FOUND: `23592c1` — unhappy-path smoke + replay identity
- FOUND: `5ab73cf` — zero-N+1 + Swagger
- FOUND: `f40da50` — frontend SPA + source-level contracts
- FOUND: `5d45821` — finalization + README

**Runtime verification:**

- Stack booted on first `docker compose up -d --build` (~10s to backend ready, ~2s to frontend ready)
- All 13 requirements verified with concrete evidence
- 5/5 ROADMAP success criteria signed off
- Phase 2 regression-clean
- All curl probes returned the expected status codes and error/success bodies
- Docker stack teardown queued for execution after the docs metadata commit
