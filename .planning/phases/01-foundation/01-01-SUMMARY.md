---
phase: 01-foundation
plan: 01
subsystem: infrastructure
tags: [docker, postgres, dotnet, vite, schema]
requirements_completed: [INFRA-01, INFRA-02, INFRA-03, INFRA-08]
dependency_graph:
  requires: []
  provides:
    - "docker-compose runtime contract (postgres + backend + frontend on a single `up`)"
    - "Final database schema (products + stock_movements) — Phase 2/3 must not modify"
    - "SDK-only backend image + node:20-alpine frontend image for hot-reload dev"
  affects:
    - "Plan 01-02 (backend scaffold) — its source tree is bind-mounted by docker-compose"
    - "Plan 01-03 (frontend scaffold) — its package.json is consumed by frontend/Dockerfile"
    - "Plan 01-04 (smoke + healthcheck) — runs `docker compose config` and `docker compose up -d` against these artifacts"
tech_stack:
  added:
    - "postgres:16-alpine"
    - "mcr.microsoft.com/dotnet/sdk:8.0"
    - "node:20-alpine"
  patterns:
    - "Healthcheck-gated startup (depends_on: service_healthy with pg_isready)"
    - "Bind-mounted source + anonymous volume shadow for node_modules (Vite-in-Docker)"
    - "Polling file watchers (DOTNET_USE_POLLING_FILE_WATCHER, CHOKIDAR_USEPOLLING) preemptively enabled for WSL/macOS friendliness"
key_files:
  created:
    - docker-compose.yml
    - init.sql
    - backend/Dockerfile
    - backend/.dockerignore
    - frontend/Dockerfile
    - frontend/.dockerignore
  modified: []
decisions:
  - "Reconciled CONTEXT.md to backend/CLAUDE.md authority: products.code carries a full inline UNIQUE constraint (no `WHERE deleted_at IS NULL` partial-index filter). Soft-deleted product codes are NOT reusable, and PROD-06 DUPLICATE_CODE is enforced at the DB layer for free."
  - "stock_movements.idempotency_key uuid NOT NULL UNIQUE (per backend/CLAUDE.md + MOVE-02) — every movement INSERT must carry an idempotency key."
  - "Backend image is SDK-only single-stage (D-03) — hot reload requires the SDK; image size is irrelevant for a non-deployed challenge."
  - "Frontend uses anonymous volume on /app/node_modules (D-04) so host-side node_modules (potentially missing or wrong-arch) never breaks the container."
  - "Polling file watchers enabled preemptively (`DOTNET_USE_POLLING_FILE_WATCHER=1`, `CHOKIDAR_USEPOLLING=true`) — zero downside, prevents a frustrating first-boot on the evaluator's WSL/macOS host."
  - "init.sql is mounted read-only (`:ro`) — Postgres container can never accidentally rewrite the canonical schema file."
metrics:
  duration_minutes: 4
  tasks: 3
  files_created: 6
  files_modified: 0
  completed: 2026-05-16T15:30:09Z
---

# Phase 1 Plan 1: Docker Foundation + Schema Summary

Stand up the three-service docker-compose runtime (Postgres 16 + .NET 8 SDK + node:20-alpine) and author the full final database schema (`products` + `stock_movements`) so Phase 2/3 never touch `init.sql` again.

## What Was Built

- **`docker-compose.yml`** — single source of truth for the dev runtime. Three services on three ports (`5432`, `8080`, `5173`), pg_isready healthcheck gates the backend, init.sql mounted read-only, bind-mount + dotnet watch / vite for hot reload, anonymous volume shadows host node_modules.
- **`init.sql`** — full final schema. `products` with soft-delete column and full UNIQUE on `code` (no filter). `stock_movements` with `idempotency_key uuid NOT NULL UNIQUE`, type/quantity/value CHECK constraints, FK to products, composite index `(product_id, occurred_at DESC)` for history queries, secondary index on `occurred_at DESC` for generic time-window filters.
- **`backend/Dockerfile`** — SDK-only single-stage image (D-03). No multi-stage, no runtime image. CMD runs `dotnet watch`.
- **`frontend/Dockerfile`** — node:20-alpine, `COPY package*.json` + `npm install` so the image's node_modules populates the anonymous volume. CMD runs `npm run dev --host 0.0.0.0`.
- **`.dockerignore` files** — keep build context fast (excludes bin/obj, node_modules, IDE artifacts).

## Schema Reconciliation (Key Decision)

The earlier CONTEXT.md draft considered a partial unique index named `uq_products_code_active` over `code WHERE deleted_at IS NULL`. **This was rejected** in favor of the policy stated in `backend/CLAUDE.md` §"Soft Delete":

> "Re-cadastrar produto com código já existente em produto soft-deleted → DUPLICATE_CODE (UNIQUE constraint sobre code sem filtro de deleted_at previne reuso de código)."

Concretely:

- `products.code varchar(50) NOT NULL UNIQUE` — full inline UNIQUE, no partial filter.
- No `WHERE deleted_at IS NULL` anywhere in the schema.
- No index named `uq_products_code_active`.
- Effect: soft-deleted product codes are **not** reusable, and PROD-06 (`DUPLICATE_CODE 422`) is enforced at the database layer. Phase 2's `ExceptionHandlingMiddleware` only needs to catch the PostgreSQL unique-violation and map it to the canonical `ErrorResponse`.

`stock_movements.idempotency_key uuid NOT NULL UNIQUE` matches `backend/CLAUDE.md` + MOVE-02 — every movement INSERT must carry an Idempotency-Key UUID.

## Containers and Ports

| Service                | Container         | Host port → Container port | Image                                |
| ---------------------- | ----------------- | --------------------------- | ------------------------------------ |
| Postgres 16            | stockeasy-postgres | 5432 → 5432                | postgres:16-alpine                   |
| .NET 8 API (dev)       | stockeasy-api     | 8080 → 8080                 | mcr.microsoft.com/dotnet/sdk:8.0 (custom build) |
| Vue 3 SPA (vite)       | stockeasy-web     | 5173 → 5173                 | node:20-alpine (custom build)        |

Credentials: `POSTGRES_USER=stockeasy`, `POSTGRES_PASSWORD=stockeasy123`, `POSTGRES_DB=stockeasy`. Connection string injected into backend as `ConnectionStrings__Postgres=Host=postgres;Database=stockeasy;Username=stockeasy;Password=stockeasy123`.

## Tasks Executed

| # | Task                                                    | Commit  | Files                                                                                                                                                                                          |
| - | ------------------------------------------------------- | ------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1 | docker-compose.yml with three services + healthcheck    | e994618 | `docker-compose.yml`                                                                                                                                                                            |
| 2 | init.sql with full final schema                         | 0c472d0 | `init.sql`                                                                                                                                                                                      |
| 3 | backend/frontend Dockerfiles + .dockerignore            | 8c0422e | `backend/Dockerfile`, `backend/.dockerignore`, `frontend/Dockerfile`, `frontend/.dockerignore`                                                                                                  |

## Deviations from Plan

None — plan executed exactly as written. The CONTEXT.md vs `backend/CLAUDE.md` reconciliation around `products.code` UNIQUE was already resolved inside the plan body (Task 2 `<action>`), so following the plan was sufficient.

## Verification

All grep-based acceptance criteria for the three tasks passed inline during execution:

- `docker-compose.yml`: `postgres:16-alpine`, three container names, `condition: service_healthy`, `pg_isready`, `ConnectionStrings__Postgres`, `dotnet watch`, `npm run dev`, `/app/node_modules` anonymous volume, `init.sql:ro` mount.
- `init.sql`: `pgcrypto` extension, two `CREATE TABLE IF NOT EXISTS`, inline `code varchar(50) NOT NULL UNIQUE`, `deleted_at`, `idempotency_key`, `uq_stock_movements_idempotency_key`, indexes, FK, CHECKs. Confirmed **absent**: `uq_products_code_active` and `WHERE deleted_at IS NULL`.
- `backend/Dockerfile`: `mcr.microsoft.com/dotnet/sdk:8.0`, no `aspnet:8.0`, no multi-stage `AS build`, `EXPOSE 8080`, CMD `dotnet watch run`.
- `frontend/Dockerfile`: `node:20-alpine`, `npm install`, `--host 0.0.0.0`, `EXPOSE 5173`.

End-to-end smoke (`docker compose up -d` + `curl /api/health`) is Plan 01-04's job — this plan only authors the artifacts.

## Self-Check: PASSED

- FOUND: docker-compose.yml
- FOUND: init.sql
- FOUND: backend/Dockerfile
- FOUND: backend/.dockerignore
- FOUND: frontend/Dockerfile
- FOUND: frontend/.dockerignore
- FOUND commit: e994618 (Task 1 docker-compose)
- FOUND commit: 0c472d0 (Task 2 init.sql)
- FOUND commit: 8c0422e (Task 3 Dockerfiles + .dockerignore)
