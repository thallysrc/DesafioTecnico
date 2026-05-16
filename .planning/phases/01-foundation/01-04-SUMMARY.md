---
phase: 01-foundation
plan: 04
subsystem: integration
tags:
  - docker-compose
  - smoke
  - readme
  - doc-09
  - integration
requirements_completed: [DOC-09]
dependency_graph:
  requires:
    - "Plan 01-01 (docker-compose.yml + init.sql + Dockerfiles)"
    - "Plan 01-02 (backend solution + /api/health + Swagger + CORS named policy 'Frontend')"
    - "Plan 01-03 (frontend SPA shell + Vite proxy /api → backend:8080)"
  provides:
    - "End-to-end smoke evidence: docker compose up + curl gates + schema introspection + clean shutdown"
    - "Root README.md as the evaluator's run-guide (DOC-09)"
    - "Original challenge spec preserved as README.challenge-spec.md (file rename, content verbatim)"
  affects:
    - "Phase 1 close-out — all 5 ROADMAP success criteria are demonstrable from this commit"
    - "Phase 4 README extension — adds PDF doc pointers on top of this base"
tech_stack:
  added: []
  patterns:
    - "Integration-validation plan: no source-code changes; reveals/proves Wave 1 contracts via the real runtime"
    - "README forward-references docs/dist/ for Phase 4 without claiming it already exists"
key_files:
  created:
    - README.challenge-spec.md
  modified:
    - README.md
decisions:
  - "Renamed original README.md → README.challenge-spec.md (git rename, preserves history) to satisfy both CONTEXT.md spirit ('preserve original') and DOC-09 ('root README explains docker-compose')."
  - "New README forward-references docs/ for Phase 4 without claiming the PDFs ship yet."
  - "Task 1 (smoke) is verification-only — no source files committed. The smoke output is the artifact, captured here for audit."
  - "Swagger operationId grep in the verify block is `\"operationId\":\"getHealth\"` (no space) but ASP.NET serializes JSON with a space (`\"operationId\": \"getHealth\"`); used a space-tolerant grep during execution. This is a verify-pattern typo, not a behavior bug — the contract (a `getHealth` operationId exists in the OpenAPI document) is satisfied."
metrics:
  duration_minutes: 4
  tasks_completed: 2
  files_created: 1
  files_modified: 1
  commits: 1
  completed: 2026-05-16T15:45:00Z
---

# Phase 01 Plan 04: End-to-End Smoke + Root README (DOC-09) Summary

Closed Phase 1 with a real end-to-end docker-compose smoke (config → build → boot → curl gates → schema introspection → clean teardown) and authored the root `README.md` that walks the evaluator through `docker compose up` and the three URLs they need. Original challenge spec preserved as `README.challenge-spec.md`. All 5 ROADMAP Phase 1 success criteria are now demonstrable from one command.

## What Was Built

### Task 1 — End-to-end docker-compose smoke (verification-only, no commit)

Ran the full smoke from a cold state (no containers, no volume). Every gate from the plan's `<verify>` block passed. Source code in Plans 01/02/03 needed zero fixes — Wave 1 landed correctly.

| Gate | Result |
|------|--------|
| `docker compose config` parses without errors | PASS — 109-line resolved config, all three services present |
| `docker compose up -d --build` succeeds | PASS — built + booted in **~36s total** (images cached from earlier dev work; cold first build on a clean machine is closer to 2–3 minutes) |
| Postgres reaches `healthy` | PASS — health-gated start fired before backend launched |
| `GET http://localhost:8080/api/health` returns 200 with `"db":"up"` | PASS — backend first-response **in ~4s** after `docker compose up` completed |
| Swagger UI at `/swagger/index.html` returns HTML containing `Swagger UI` | PASS |
| `/swagger/v1/swagger.json` contains `"operationId": "getHealth"` | PASS (with space — see Deviation #1 below) |
| `GET http://localhost:5173/` returns the SPA shell (id="app", `<title>StockEasy</title>`) | PASS |
| `GET http://localhost:5173/api/health` (Vite proxy → backend over Docker DNS) returns `"db":"up"` | PASS |
| CORS header `Access-Control-Allow-Origin: http://localhost:5173` attached to `/api/health` for `Origin: http://localhost:5173` | PASS |
| `psql \d products` shows `code` (UNIQUE) + `deleted_at` columns | PASS — `code varchar(50) NOT NULL UNIQUE`, full UNIQUE (no partial filter, per backend/CLAUDE.md soft-delete reuse-prevention policy) |
| `psql \d stock_movements` shows `idempotency_key` (UNIQUE) | PASS — `uq_stock_movements_idempotency_key` unique constraint present |
| All expected indexes present | PASS — `idx_stock_movements_occurred`, `idx_stock_movements_product_occurred`, `products_code_key`, `products_pkey`, `stock_movements_pkey`, `uq_stock_movements_idempotency_key` |
| `docker compose down -v --remove-orphans` cleans up | PASS — all three containers stopped + removed, volume removed, network removed |

### Task 2 — Root README.md as the evaluator's run-guide (commit `ec4fd63`)

- Renamed `README.md` → `README.challenge-spec.md` via `git mv` (history preserved, content verbatim — this is the original challenge brief).
- Authored the new `README.md` as the evaluator-facing run guide:
  - **Title:** `# StockEasy` (H1, matches frontend `<title>` and wordmark)
  - **Stack table:** Backend (.NET 8 + Dapper + PG 16), Frontend (Vue 3 + Vite + TS strict + Tailwind), Validation (FluentValidation + Vee-Validate + Zod), Docs API (Swashbuckle + Annotations), Container (Docker Compose)
  - **Run section:** `docker compose up` one-liner + table of URLs (frontend :5173, swagger :8080/swagger, health :8080/api/health, psql :5432) + hot-reload note + `docker compose down` variants
  - **Repo structure:** ASCII tree from `docker-compose.yml` + `init.sql` through `backend/`, `frontend/`, and `.planning/` — accurate as of Phase 1 close
  - **Status table:** four phases with current/next/depois/final markers (matches `.planning/ROADMAP.md`)
  - **Env vars table:** documents the three compose-injected variables (`ConnectionStrings__Postgres`, `ASPNETCORE_ENVIRONMENT`, PG credentials) — no `.env` needed
  - **Smoke commands:** three `curl` snippets the evaluator can run after `docker compose up` to verify everything is up
  - **Notes for the evaluator:** no deploy, no login, hot reload is both dev and demo, EN identifiers + PT-BR UI strings (D7), BancoShu reference + the deliberately-not-copied N+1
  - **Forward-reference:** mentions `docs/dist/` PDFs for Phase 4 without claiming they exist yet
- Verify block (`test -f` + 12 grep assertions) ALL PASSED.

## Smoke Performance Notes

- **Build phase ~36s.** Images were already cached locally from earlier dev work; a clean-machine cold first build (image pulls + dotnet restore inside the SDK container + npm install inside the node image) would be closer to 2–3 minutes. Subsequent `docker compose up` (no rebuild) takes <10s.
- **Backend first-response: ~4s.** The 90-second timeout the plan budgeted is very comfortable — `dotnet watch run` started, ASP.NET bound to :8080, and the Dapper `SELECT 1` health probe succeeded against the (already-healthy) Postgres container within four seconds of `docker compose up` returning. If the SDK image were not cached, this would be longer because the first invocation does an in-container `dotnet restore`/`build` before the host can be bound.
- **All five ROADMAP Phase 1 success criteria demonstrable:**
  1. `docker compose up` boots three services and they stay healthy — PROVEN
  2. `init.sql` ran and created tables with constraints + indexes — PROVEN via `\d` + `pg_indexes`
  3. Swagger UI at `:8080/swagger` works; SPA shell at `:5173` works — PROVEN
  4. Axios reaches backend through Vite proxy without CORS errors — PROVEN via `curl :5173/api/health` returning the same JSON as `curl :8080/api/health`
  5. Backend solution builds with two projects, namespace convention, XML doc generation — PROVEN by the build step inside `docker compose up --build` succeeding and Swagger surfacing the rich `summary`/`description` from XML docs on the health endpoint

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Swagger operationId grep pattern in plan's `<verify>` block doesn't match actual JSON formatting**
- **Found during:** Task 1, gate #4 (`grep -q '"operationId":"getHealth"'` — no space after the colon).
- **Issue:** ASP.NET Core's default JSON serializer formats the swagger document with a space between `"operationId":` and `"getHealth"`, so the no-space grep returned exit-code 1. The contract (a `getHealth` operationId exists in `/swagger/v1/swagger.json`) was satisfied; only the regex used to assert it was overly strict.
- **Fix:** Used a space-tolerant pattern (`'"operationId" *: *"getHealth"'`) during execution to confirm the contract held. Did **not** modify the plan or the Swashbuckle serializer settings — this is a verify-pattern typo, not a behavior bug, and tightening either side (forcing no-space JSON output, or rewriting the plan's grep) would be over-engineering for a one-off issue. The plan's `<verify>` block already lives in a closed plan; this note exists so a future re-runner of the plan knows why a naive `grep -q '"operationId":"getHealth"'` would falsely fail.
- **Files modified:** none (no source/config change required).
- **Commit:** none (verification-only).

### Auth Gates

None.

### Architectural Changes (Rule 4)

None.

## README Structure Decision

**Why `README.challenge-spec.md` rather than `docs/CHALLENGE.md` or git-history-only preservation?**

Three constraints had to be balanced:

1. **CONTEXT.md says** "README.md (root) — original challenge spec; do not edit in P1, will be rewritten in P4." Spirit: preserve the original verbatim, don't lose it.
2. **DOC-09 requires** a root README explaining `docker compose up` + URLs + pointer to `docs/`. Mapped to Phase 1, not Phase 4 (it's needed for the evaluator to run the project from the very first commit they read).
3. **Discoverability:** a stranger cloning the repo expects `README.md` to tell them what the project is and how to run it — not the recruiter's brief.

The chosen resolution — `git mv README.md README.challenge-spec.md` followed by a fresh authoring of the new `README.md` — satisfies all three:

- The original spec content is preserved **byte-for-byte** in the new file (git rename, no diff).
- The new `README.md` is the evaluator-facing run guide (DOC-09).
- The new `README.md` links to `README.challenge-spec.md` so anyone curious about the original brief can find it in one click.

Phase 4 will expand `README.md` with PDF doc pointers (`docs/dist/01-product-decisions.pdf`, etc.). The forward-reference is already in place.

## Self-Check: PASSED

- FOUND: README.md
- FOUND: README.challenge-spec.md
- FOUND: .planning/phases/01-foundation/01-04-SUMMARY.md
- FOUND commit: ec4fd63 (Task 2)
- README.md contains: `# StockEasy`, `docker compose up`, `http://localhost:5173`, `http://localhost:8080/swagger`, `http://localhost:8080/api/health`, `init.sql`, `.planning/`, `README.challenge-spec.md`, `Vue 3`, `.NET 8`, `Dapper`, `PostgreSQL 16`, `docs/dist/`
- Smoke teardown verified: `docker ps -a` filter `name=stockeasy` returned 0 rows; volume `desafiotecnico_postgres_data` removed; network `desafiotecnico_default` removed

## Notes for Phase 4

1. `README.md` already forward-references `docs/dist/`. Phase 4 should:
   - Add a "Documentação" section with three bulleted links (`docs/dist/01-product-decisions.pdf`, `docs/dist/02-architecture.pdf`, `docs/dist/03-business-rules.pdf`) once the pipeline is in place.
   - Optionally add a test status badge or coverage badge (xUnit + Vitest) — the README intentionally omits all badges in Phase 1.
2. The smoke script in this plan's `<verify>` block can be promoted to `scripts/smoke.sh` if Phase 4 wants a one-command verification entry. Not committed in Phase 1 because nothing references it yet.
3. README intentionally omits a LICENSE section because no LICENSE file is in scope for the challenge. If Phase 4 decides to add one, the README can grow a "License" section pointing at it.
