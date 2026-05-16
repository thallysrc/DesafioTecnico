# Phase 1: Foundation - Context

**Gathered:** 2026-05-16
**Status:** Ready for planning

<domain>
## Phase Boundary

Scaffold both apps so `docker-compose up` boots Postgres + .NET API (Swagger reachable at `:8080/swagger`) + Vue SPA shell (sidebar, brand wordmark, routes `/products` and `/stock-movements` resolvable) talking end-to-end. **No business logic** — no Product/StockMovement domain code, no controllers beyond what's needed to prove connectivity. INFRA-01..08, BACK-01..04, FRONT-01..04, FRONT-09..11, DOC-09 are in scope; everything else is deferred.

</domain>

<decisions>
## Implementation Decisions

### Docker dev workflow
- **D-01:** Full hot reload in containers — `docker-compose up` runs `dotnet watch run` (backend) and `vite` dev server (frontend) against source mounted as bind volumes. Editing C# or Vue files live-reloads inside the containers. Demo and dev are the same `up` command.
- **D-02:** Single `docker-compose.yml` (dev-tuned). No `docker-compose.override.yml` split, no "prod" mode — this is a non-deployed challenge. Evaluator runs one command.
- **D-03:** Backend Dockerfile is **SDK-only single-stage** (`mcr.microsoft.com/dotnet/sdk:8.0`) running `dotnet watch run`. No multi-stage, no runtime image. Image size is irrelevant; hot reload requires the SDK.
- **D-04:** Frontend Dockerfile uses `node:20-alpine` running `npm run dev`. `node_modules` is shadowed by an **anonymous volume** so host's `node_modules` (potentially missing or wrong-arch) doesn't break the container. Standard Vite-in-Docker pattern.
- **D-05:** Postgres uses `depends_on: condition: service_healthy` (BancoShu pattern) with `pg_isready` healthcheck so backend only starts after DB is accepting connections.

### Frontend↔backend dev wiring
- **D-06:** **Vite proxy** — `vite.config.ts` proxies `/api/**` to `http://backend:8080` (compose service name via Docker DNS). Browser sees same-origin. CORS still configured on backend (per INFRA-05) but the dev flow doesn't depend on it.
- **D-07:** Vite proxy target is the **service name `backend`** (no `host.docker.internal`, no env-var indirection). Cleanest, no host coupling.
- **D-08:** Axios `baseURL = '/api'`. Calls written as `axios.get('/products')` resolve to `/api/products` → Vite proxy → `backend:8080/api/products`. Backend controllers use `[Route("api/[controller]")]`. No env-driven baseURL — YAGNI for the challenge.
- **D-09:** Backend CORS — named policy `"Frontend"` registered in `Program.cs` that allows **only `http://localhost:5173`** with `AllowAnyHeader` + `AllowAnyMethod`. Origin overridable via `Cors:AllowedOrigins` config for future. Tight and correct; satisfies INFRA-05 properly rather than bypassing with `AllowAny`.

### Claude's Discretion
Areas the user did not select for discussion — Claude resolves these during planning, anchored on PROJECT.md decisions and BancoShu patterns:
- **Hello-world endpoint scope**: provide `GET /api/health` that pings Postgres (simple `SELECT 1` via Dapper) and returns `{ status, db, version, timestamp }`. SPA calls it on mount in App.vue to render a discreet "✓ API conectada" indicator. Cheap, proves criteria #1, #3, #4 of the roadmap explicitly.
- **init.sql schema scope**: author the **full final schema in Phase 1** (per INFRA-03) — `products` with `deleted_at timestamptz NULL`, `stock_movements` with `idempotency_key uuid UNIQUE NULL`, all CHECK constraints, indexes on `products.code` UNIQUE WHERE `deleted_at IS NULL`, `stock_movements(product_id, occurred_at DESC)`. Avoids touching `init.sql` in P2/P3.
- **Icon system for sidebar**: `lucide-vue-next` (tree-shakeable, ~1KB per icon, minimal aesthetic that matches RoboteAsy palette). Two icons needed in P1: `Package` for Products, `ArrowLeftRight` for Stock Movements.
- **DB startup behavior**: rely on `depends_on: service_healthy` (D-05) for ordering; backend opens connection lazily on first request. `Npgsql` connection pool handles transient failures. No in-app fail-fast probe needed for Phase 1.
- **Port strategy**: `5432:5432` (PG), `8080:8080` (API), `5173:5173` (Vite). Matches BancoShu where applicable.
- **Container names**: `stockeasy-postgres`, `stockeasy-api`, `stockeasy-web` (clarity in `docker ps`).
- **Postgres credentials in dev**: `POSTGRES_USER=stockeasy`, `POSTGRES_PASSWORD=stockeasy123`, `POSTGRES_DB=stockeasy`. Connection string assembled in compose env.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project-level rules (must follow)
- `.planning/PROJECT.md` — 31 key decisions (D1–D31), out-of-scope list, constraints
- `.planning/REQUIREMENTS.md` §INFRA-01..08, BACK-01..04, FRONT-01..04, FRONT-09..11, DOC-09 — scope of Phase 1
- `.planning/ROADMAP.md` §"Phase 1: Foundation" — goal + 5 success criteria

### Backend conventions
- `backend/CLAUDE.md` — full backend stack, solution layout, namespace rules, package allowlist, exception/middleware patterns
- `/home/thallysrc/Projects/BancoShu/` — reference repository. Copy patterns for: `docker-compose.yml`, `init.sql`, Dapper `IDbConnectionFactory`, `ExceptionHandlingMiddleware`, FluentValidation registration, Swagger setup, Dockerfile shape. **Exception:** the N+1 in `TransferService.GetHistoryAsync` is anti-pattern — do not copy.

### Frontend conventions
- `frontend/CLAUDE.md` — full frontend stack, brand palette tokens, typography, sidebar layout, wordmark spec
- `frontend/CLAUDE.md` §"Brand & Visual Identity" — `Stock<span class="text-brand-500">Easy</span>` wordmark, sidebar light mode (`bg-white`, active item `bg-brand-50 text-brand-700`)

### Repo root
- `README.md` (root) — original challenge spec; do not edit in P1, will be rewritten in P4

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- **None yet** — `backend/` and `frontend/` contain only their `CLAUDE.md`. Phase 1 creates everything.
- **BancoShu** (`/home/thallysrc/Projects/BancoShu/`) provides copy-paste templates for: `Dockerfile`, `docker-compose.yml`, `init.sql` shape, `Program.cs` skeleton, `IDbConnectionFactory`, `ExceptionHandlingMiddleware`, Swashbuckle config with XML doc inclusion.

### Established Patterns (from CLAUDE.md files, must enforce starting P1)
- **Backend**: file-scoped namespaces `Inventory.Api.{Folder}`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<GenerateDocumentationFile>true</GenerateDocumentationFile>` set in `.csproj` from day one (INFRA-04).
- **Backend**: single-project N-tier — NOT Clean Architecture, NOT 4 projects. One `Inventory/` + one `Inventory.Tests/`.
- **Frontend**: feature-based `src/features/products/`, `src/features/stock/` (folders can be empty placeholders in P1), shared utilities in `src/shared/`.
- **Frontend**: Composition API + `<script setup>`, no Pinia (composables only), Inter via Google Fonts in `index.html`.
- **Both**: identifiers in English, user-facing strings in PT-BR.

### Integration Points
- **Vite proxy → backend service** at `http://backend:8080/api/**` (D-06, D-07)
- **Axios baseURL `/api`** in `src/shared/api/client.ts` (D-08)
- **Backend CORS policy `"Frontend"`** allowing `http://localhost:5173` (D-09, INFRA-05)
- **Connection string** read from `ConnectionStrings:Postgres` config key (INFRA-06), set in compose as `ConnectionStrings__Postgres=Host=postgres;Database=stockeasy;Username=stockeasy;Password=stockeasy123`
- **`init.sql`** mounted at `/docker-entrypoint-initdb.d/init.sql` on postgres container (INFRA-02)
- **Swagger** at `:8080/swagger` in Development only (INFRA-07)

</code_context>

<specifics>
## Specific Ideas

- "Demo and dev are the same `docker-compose up`" — evaluator should not have to learn two run modes. Hot reload in container = friction-free dev that's also the demo path.
- Mirror BancoShu's `docker-compose.yml` shape (healthcheck, depends_on, volume mount of init.sql) but adapt for hot reload: SDK image + `dotnet watch run`, bind-mount source.
- Sidebar shell must already render the `Stock<span class="text-brand-500">Easy</span>` wordmark in P1 — visual identity from day one.

</specifics>

<deferred>
## Deferred Ideas

- **Multi-stage prod Dockerfile** — not needed; challenge isn't deployed. Could be added in v2 but out of scope for v1 delivery.
- **`docker-compose.override.yml` separation** — same reason; revisit only if a prod target is ever added.
- **`VITE_API_URL` env var for direct-call mode** — not needed; Vite proxy covers dev, and there's no prod frontend.
- **Healthcheck on backend container** — depends_on order is sufficient for P1; revisit if startup races appear.
- **In-app DB fail-fast probe** — relying on `depends_on: service_healthy` + lazy connection. Add only if observed flakiness.
- **Container hot-reload polling intervals / `CHOKIDAR_USEPOLLING`** — only enable if filesystem-watch is unreliable on the dev host (WSL/macOS may need it). Note in README if so.

</deferred>

---

*Phase: 01-foundation*
*Context gathered: 2026-05-16*
