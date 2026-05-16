# Phase 1: Foundation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-16
**Phase:** 01-foundation
**Mode:** discuss (switched to auto for last question)
**Areas discussed:** Docker dev workflow, Frontend↔backend dev wiring

---

## Docker dev workflow

### Q1: How should `docker-compose up` handle dev iteration?

| Option | Description | Selected |
|--------|-------------|----------|
| Full hot reload in containers | `dotnet watch` + Vite HMR, source as volumes. Demo and dev same `up` command. | ✓ |
| Build images, no hot reload | Multi-stage Dockerfile, `--build` on every change. | |
| Hybrid: Postgres in compose, apps local | Only PG dockerized; backend/front run on host. | |

**User's choice:** Full hot reload in containers
**Notes:** Single source of truth for "how to run", reduces evaluator friction.

### Q2: Single compose file or split with override?

| Option | Description | Selected |
|--------|-------------|----------|
| Single `docker-compose.yml`, dev-tuned | One file, no prod mode. | ✓ |
| Split: base + `docker-compose.override.yml` | BancoShu pattern; prod vs dev overlays. | |

**User's choice:** Single file
**Notes:** Challenge isn't deployed — prod target is YAGNI.

### Q3: Backend Dockerfile base — SDK only or multi-stage?

| Option | Description | Selected |
|--------|-------------|----------|
| SDK-only single-stage | `sdk:8.0` + `dotnet watch run`. Smallest Dockerfile, fits hot reload. | ✓ |
| Multi-stage with runtime image | Build + `aspnet:8.0` final. Conflicts with watch. | |
| Multi-stage + dev target | `target: dev` (SDK+watch) and `target: prod` (runtime). | |

**User's choice:** SDK-only single-stage

### Q4: Frontend node_modules strategy?

| Option | Description | Selected |
|--------|-------------|----------|
| Anonymous volume shadows host | Bind-mount `./frontend`, anon volume on `/app/node_modules`. Standard Vite-in-Docker. | ✓ |
| node_modules in container, partial mount | Mount only `src/` and `public/`. | |
| Install on host, mount everything | Host `npm install` then bind-mount whole folder. Arch-mismatch risk. | |

**User's choice:** Anonymous volume

---

## Frontend↔backend dev wiring

### Q5: Vite proxy or direct call with CORS?

| Option | Description | Selected |
|--------|-------------|----------|
| Vite proxy `/api` → backend | Same-origin in dev; CORS still configured but not exercised. | ✓ |
| Axios direct call + CORS | Real CORS flow exercised in dev. | |
| Both — proxy default, env override | Future-proof, more code. | |

**User's choice:** Vite proxy

### Q6: Vite proxy target?

| Option | Description | Selected |
|--------|-------------|----------|
| Service name `http://backend:8080` | Docker DNS, clean. | ✓ |
| `http://host.docker.internal:8080` | Host loopback; Linux quirks. | |
| Env-var driven | `VITE_PROXY_TARGET` with default. | |

**User's choice:** Service name

### Q7: Axios `baseURL`?

| Option | Description | Selected |
|--------|-------------|----------|
| `/api` | Short and idiomatic; `axios.get('/products')` → `/api/products`. | ✓ |
| Empty + full paths in callers | Repetitive. | |
| Env-driven | YAGNI for challenge. | |

**User's choice:** `/api`

### Q8: Backend CORS policy permissiveness?

| Option | Description | Selected |
|--------|-------------|----------|
| Allow `http://localhost:5173` only | Named policy, `AllowAnyHeader/Method`, overridable via config. | ✓ (auto) |
| AllowAny (Development only) | Wide-open in dev. | |
| Read from `Cors:AllowedOrigins` array | Production-correct, over-engineered for one origin. | |

**User's choice:** Allow `http://localhost:5173` only — auto-selected (recommended default) after user requested auto mode.

---

## Claude's Discretion (areas not selected for discussion)

User selected only 2 of 4 presented gray areas. The remaining 2 were resolved by Claude in CONTEXT.md based on PROJECT.md decisions and BancoShu reference patterns:

- **Hello-world endpoint scope** → `GET /api/health` (pings PG via `SELECT 1`), SPA calls on mount for "✓ API conectada" indicator.
- **init.sql schema scope** → Full final schema authored in P1 (per INFRA-03). `products` + `stock_movements` with all constraints/indexes/idempotency_key UNIQUE.
- **Icon system** → `lucide-vue-next`, two icons in P1 (`Package`, `ArrowLeftRight`).
- **DB startup behavior** → Rely on `depends_on: service_healthy`, lazy connect via Npgsql pool.

## Deferred Ideas

- Multi-stage prod Dockerfile (challenge isn't deployed)
- `docker-compose.override.yml` separation
- `VITE_API_URL` for direct-call mode
- Backend healthcheck in compose
- In-app DB fail-fast probe
- Watch polling tweaks for WSL/macOS
