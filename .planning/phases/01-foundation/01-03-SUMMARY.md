---
phase: 01-foundation
plan: 03
subsystem: frontend
tags:
  - vue3
  - vite
  - tailwind
  - typescript-strict
  - scaffolding
  - sidebar-shell
  - health-pill
dependency_graph:
  requires:
    - "Phase 1 CONTEXT.md decisions D-06/D-07 (Vite proxy literal target) and D-08 (Axios baseURL '/api')"
    - "Phase 1 UI-SPEC.md (sidebar layout, wordmark, health-pill state mapping, copy contract)"
    - "frontend/CLAUDE.md (stack, palette, sidebar contract, folder structure)"
  provides:
    - "Working Vite dev server on :5173 with hot reload (INFRA-08)"
    - "Tailwind brand-* palette (50..900) + semantic tokens + Inter font (FRONT-02, FRONT-03)"
    - "TypeScript strict mode passing across the entire src/ tree (FRONT-01)"
    - "Feature-based folder layout: src/features/products/, src/features/stock/, src/shared/ (FRONT-04)"
    - "Vue Router 4 routes /products + /stock-movements + root redirect (FRONT-09, FRONT-10)"
    - "Sidebar AppShell with wordmark Stock<span text-brand-500>Easy</span>, lucide nav, health pill (FRONT-11)"
    - "Single Axios instance with baseURL '/api' + ApiError type pre-declared for Phase 2 (D-08)"
    - "Vite dev-server proxy /api → http://backend:8080 (literal, no env-var indirection — D-06/D-07)"
    - "Three-state HealthPill (checking/connected/offline) consuming /api/health with 5s timeout"
  affects:
    - "Plan 04 (Wave 2) docker-compose smoke will validate the proxy + CORS path end-to-end"
    - "Phase 2 will build forms/tables/modals on top of the shared/api + AppShell foundation"
tech_stack:
  added:
    - vue@^3.5.13
    - vue-router@^4.4.5
    - vite@^5.4.10
    - "@vitejs/plugin-vue@^5.1.4"
    - typescript@~5.6.3
    - vue-tsc@^2.1.10
    - tailwindcss@^3.4.14
    - postcss@^8.4.49
    - autoprefixer@^10.4.20
    - axios@^1.7.7
    - lucide-vue-next@^0.460.0
    - eslint@^9.14.0 (flat config)
    - eslint-plugin-vue@^9.30.0
    - "@vue/eslint-config-typescript@^14.1.3"
    - prettier@^3.3.3
  patterns:
    - "Composition API + <script setup lang='ts'> in every .vue file"
    - "Lazy-loaded route components via () => import('@/features/.../pages/X.vue')"
    - "Single Axios instance pattern (shared/api/client.ts) with interceptor stub for Phase 2"
    - "AbortController teardown on HealthPill unmount (clean fetch cancellation)"
    - "Active sidebar item via route.path.startsWith(basePath) (preserves highlight on nested routes)"
key_files:
  created:
    - frontend/package.json
    - frontend/package-lock.json
    - frontend/tsconfig.json
    - frontend/tsconfig.node.json
    - frontend/vite.config.ts
    - frontend/tailwind.config.js
    - frontend/postcss.config.js
    - frontend/index.html
    - frontend/.gitignore
    - frontend/.prettierrc
    - frontend/env.d.ts
    - frontend/eslint.config.js
    - frontend/public/favicon.svg
    - frontend/src/main.ts
    - frontend/src/App.vue
    - frontend/src/style.css
    - frontend/src/router/index.ts
    - frontend/src/shared/api/client.ts
    - frontend/src/shared/api/health.ts
    - frontend/src/shared/components/AppShell.vue
    - frontend/src/shared/components/HealthPill.vue
    - frontend/src/features/products/pages/ProductsPage.vue
    - frontend/src/features/stock/pages/StockMovementsPage.vue
    - frontend/src/features/products/.gitkeep
    - frontend/src/features/stock/.gitkeep
  modified: []
decisions:
  - "Pinned dependency versions: Vue 3.5.x, Vite 5.4.x, Tailwind 3.4.x, axios 1.7.x, lucide-vue-next 0.460.x — locked for reproducibility"
  - "Vite proxy target is the literal string 'http://backend:8080' per D-07 (no env-var indirection, no fallback). The only supported run path is docker compose up."
  - "Axios baseURL '/api' per D-08; calls written as apiClient.get('/health') resolve to /api/health via Vite proxy"
  - "HealthPill state mapping locked by UI-SPEC: Connected requires HTTP 2xx AND body.status === 'ok' AND body.db === 'up'; ANY other outcome (degraded API, db down, non-2xx, network error, 5s timeout) → offline"
  - "fetchHealth uses 5s timeout overriding the 10s default on apiClient (UI-SPEC behavior rule)"
  - "Feature folders products/ and stock/ contain only pages + .gitkeep in Phase 1; composables/components/api/schemas land in Phase 2/3"
  - "Inter font loaded via Google Fonts with weights 400;500;600;700 per UI-SPEC Typography section"
  - "Migrated ESLint config from legacy .eslintrc.cjs to flat eslint.config.js (ESLint 9 + @vue/eslint-config-typescript 14 require flat config)"
  - "Switched local Node from v14.18.2 to v20.20.0 via nvm — vue-tsc/Vite 5/eslint 9 all need Node 18+"
metrics:
  duration_minutes: ~12
  tasks_completed: 3
  completed_date: "2026-05-16"
---

# Phase 1 Plan 03: Frontend Scaffold (Vite + Vue 3 + TS strict + Tailwind) Summary

Bootstrapped the StockEasy SPA frontend: feature-based Vue 3 + Vite 5 + TypeScript strict scaffold with Tailwind brand palette, Inter font, Vue Router 4 (`/products` + `/stock-movements` + root redirect), single Axios instance routed through a Vite dev-server proxy whose target is the **literal** `http://backend:8080` (D-06/D-07 — no env-var indirection), and a sidebar `AppShell` carrying the `Stock<span class="text-brand-500">Easy</span>` wordmark plus a three-state `HealthPill` that calls `/api/health` on mount.

## What Was Built

### Task 1 — Project bootstrap (`edbc1ac`)
- `package.json` with pinned versions: Vue 3.5.13, Vite 5.4.10, Tailwind 3.4.14, axios 1.7.7, lucide-vue-next 0.460.0, vue-router 4.4.5, TypeScript 5.6.3, ESLint 9.14.0, Prettier 3.3.3. Scripts: `dev`, `build` (vue-tsc + vite build), `preview`, `type-check`, `lint` (`--max-warnings 0`), `format`.
- `tsconfig.json` with `strict: true`, `noUnusedLocals`, `noUnusedParameters`, `verbatimModuleSyntax`, path alias `@/* → src/*`.
- `tsconfig.node.json` for `vite.config.ts` typechecking under Node.
- `vite.config.ts` — `host: '0.0.0.0'`, `port: 5173`, `strictPort: true`, proxy `'/api' → 'http://backend:8080'` (literal target; **no `process.env.BACKEND_HOST`, no fallback** per D-07).
- `tailwind.config.js` with full `brand-50..900` palette (`#1863DC` primary) + semantic tokens (ink, muted, line, surface, success, warning, danger) + `font-sans: ['Inter', ...]`.
- `postcss.config.js` (Tailwind + Autoprefixer).
- `index.html` with Inter Google Fonts preconnect + stylesheet (weights 400/500/600/700) + `<title>StockEasy</title>` + `lang="pt-BR"` + `<div id="app">`.
- `public/favicon.svg` — stylized "SE" mark on a #1863DC rounded square (recognizable at 16px).
- `.gitignore`, `.prettierrc`, `env.d.ts` (Vue SFC shim + Vite client types).

### Task 2 — Vue source tree (`7db5b97`)
- `src/main.ts` — bootstrap (`createApp(App).use(router).mount('#app')`) + `style.css` import.
- `src/style.css` — Tailwind layers + body base (`font-sans text-ink bg-white`) + global focus-visible ring (`ring-2 ring-brand-500 ring-offset-1`).
- `src/router/index.ts` — Vue Router 4 with `/` → `/products` redirect, lazy-loaded `/products` + `/stock-movements`, catch-all `(.*) → /products`, `afterEach` updates `document.title` from `route.meta.title` (`"<route> — StockEasy"`).
- `src/shared/api/client.ts` — single Axios instance `apiClient` with `baseURL: '/api'`, 10s default timeout, `Content-Type: application/json`. Exposes the `ApiError` interface for Phase 2 to expand. Phase 1 interceptor normalizes network/non-`errorCode` failures to a fallback `ApiError`. Exports `generateIdempotencyKey()` using `crypto.randomUUID()` for Phase 3's movement POSTs.
- `src/shared/api/health.ts` — `fetchHealth(signal?)` typed wrapper around `apiClient.get<HealthResponse>('/health', { signal, timeout: 5_000 })`. The 5s timeout overrides the 10s default per UI-SPEC behavior rule.
- `src/shared/components/AppShell.vue` — `<div class="flex h-screen bg-white">` containing `<aside class="w-60 border-r border-line p-4 flex flex-col bg-white">` with the `<h1>` wordmark `Stock<span class="text-brand-500">Easy</span>`, `<nav aria-label="Navegação principal">` of `RouterLink`s (Package → Produtos, ArrowLeftRight → Movimentação de Estoque), active item gets `bg-brand-50 text-brand-700` via `route.path.startsWith(basePath)`, `<HealthPill>` pinned to bottom via `mt-auto`. Slot in `<main class="flex-1 p-6 overflow-auto">` for `<RouterView>`.
- `src/shared/components/HealthPill.vue` — Three states: `checking` (Loader2 spinning + "Verificando API…" on `bg-surface`), `connected` (Check + "API conectada" on `bg-success/10`), `offline` (XCircle + "API offline" on `bg-danger/10`). State derived from `health.status === 'ok' && health.db === 'up' ? 'connected' : 'offline'`; any throw → `offline`. `AbortController` aborts the pending request on `onBeforeUnmount`. Each state container has `role="status" aria-live="polite"` and icons have `aria-hidden="true"`.
- `src/App.vue` — `<AppShell><RouterView /></AppShell>`.
- `src/features/products/pages/ProductsPage.vue` + `src/features/stock/pages/StockMovementsPage.vue` — Phase 1 "Em breve" placeholders with `<h2 text-lg font-semibold text-ink>` page title + `<p text-sm text-muted>` body. No forms, no tables, no lists — those land in Phases 2/3.
- `.gitkeep` files in `src/features/products/` and `src/features/stock/` to commit the empty Phase 2/3 sub-folders.

### Task 3 — Verification + flat ESLint migration (`f30f352`)
- `npm run type-check` (vue-tsc --noEmit) passes under strict mode.
- `npm run build` produces `dist/index.html` + hashed `dist/assets/*.js` + CSS (~140 kB JS gzipped to ~54 kB).
- `npm run lint` passes with zero warnings (`--max-warnings 0`).
- Vite dev server serves on `http://localhost:5173` and returns the SPA index for `/`, `/products`, and `/stock-movements` (history fallback) — each response contains `id="app"`, `<title>StockEasy</title>`, and the Inter preconnect link.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Switched local Node from v14.18.2 → v20.20.0 via nvm**
- **Found during:** Task 2 (first `npm run type-check`).
- **Issue:** vue-tsc's bundled `@vue/language-core` uses `??=` (logical-nullish-assignment), which is a syntax error in Node 14. Vite 5, ESLint 9, and vue-tsc 2 all require Node 18+.
- **Fix:** `nvm use 20` for all subsequent npm scripts. Re-ran `npm install` so `package-lock.json` is consistent with the Node 20 toolchain (e.g., rollup native binaries).
- **Files modified:** `frontend/package-lock.json` (regenerated under Node 20).
- **Commit:** included in `7db5b97`.

**2. [Rule 3 - Blocking] Migrated ESLint config from legacy `.eslintrc.cjs` to flat `eslint.config.js`**
- **Found during:** Task 3 (`npm run lint`).
- **Issue:** ESLint 9 defaults to flat config and `@vue/eslint-config-typescript@14` only ships as flat config (uses `configureVueProject` top-level which `.eslintrc.*` rejects). Forcing legacy mode via `ESLINT_USE_FLAT_CONFIG=false` also failed because the bundled config uses flat-only top-level keys.
- **Fix:** Wrote `eslint.config.js` using `defineConfigWithVueTs(...)` with `js.configs.recommended`, `pluginVue.configs['flat/recommended']`, `vueTsConfigs.recommended`, and the project's custom rule overrides (`vue/multi-word-component-names: off`, `@typescript-eslint/no-unused-vars` with `argsIgnorePattern: '^_'`, `no-console` warn allowing warn/error). Deleted `.eslintrc.cjs`.
- **Files modified:** `frontend/eslint.config.js` (new), `frontend/.eslintrc.cjs` (deleted).
- **Commit:** `f30f352`.

**3. [Rule 1 - Bug] `@typescript-eslint/no-empty-object-type` error on Vue SFC shim**
- **Found during:** Task 3 (`npm run lint`).
- **Issue:** Modern `@typescript-eslint` forbids the `{}` empty-object type, but Vue's standard SFC shim is `DefineComponent<{}, {}, any>`. Adding `object` or `unknown` breaks Vue's type resolution.
- **Fix:** Added `eslint-disable-next-line @typescript-eslint/no-empty-object-type` to the existing disable comment on the shim line in `env.d.ts`. Preserves Vue's expected typing while satisfying the rule.
- **Files modified:** `frontend/env.d.ts`.
- **Commit:** `f30f352`.

**4. [Rule 1 - Bug] Auto-fixed Vue formatting warnings**
- **Found during:** Task 3 (`npm run lint --max-warnings 0`).
- **Issue:** `eslint-plugin-vue` `vue3-recommended` enforces `vue/singleline-html-element-content-newline` and `vue/max-attributes-per-line` — the original templates had inline `<h2>Produtos</h2>` and `<Loader2 class="..." aria-hidden="true" />`.
- **Fix:** Ran `eslint --fix`. Headings, paragraphs, and multi-attribute lucide icon elements were broken onto separate lines. The wordmark `Stock<span class="text-brand-500">Easy</span>` was deliberately preserved (verified via `grep`) since the `<h1>` opening tag and content are formatted independently from the inline `<span>`.
- **Files modified:** `frontend/src/features/products/pages/ProductsPage.vue`, `frontend/src/features/stock/pages/StockMovementsPage.vue`, `frontend/src/shared/components/AppShell.vue`, `frontend/src/shared/components/HealthPill.vue`.
- **Commit:** `f30f352`.

No architectural changes were needed; no Rule 4 (ask-the-user) checkpoints were hit.

## Locked Contract Highlights

- **Vite proxy target is the literal `'http://backend:8080'`** per D-06/D-07. Verified with both a positive grep (`target: 'http://backend:8080'`) and a negative grep (`! grep -q 'process.env.BACKEND_HOST'`). The canonical run path is `docker compose up`; running `npm run dev` directly on the host means the proxy cannot resolve `backend`, but the SPA shell itself loads fine and is the only thing this host-smoke validates. Full proxy + CORS smoke runs inside docker-compose in Plan 04.
- **Axios baseURL = `'/api'`** per D-08 (no `import.meta.env.VITE_API_URL`, no env-driven baseURL).
- **HealthPill state mapping** locked by UI-SPEC §"Health-Status Pill": `connected` requires HTTP 2xx AND `body.status === 'ok'` AND `body.db === 'up'`. Anything else — including 2xx with degraded API or `db === 'down'`, non-2xx, network errors, or the 5s timeout — resolves to `offline`. This is *better-UX behavior*: dependency outages surface to the user, not just transport-level failures.
- **HealthPill 5s timeout** overrides the apiClient 10s default via the per-request `timeout: 5_000` option on `apiClient.get`.
- **Wordmark** lives only in the sidebar `<h1>`; page titles inside route placeholders are `<h2>` per UI-SPEC accessibility floor (single H1 per page).
- **Feature folders** `products/` and `stock/` contain only `pages/` + `.gitkeep` in Phase 1. Composables, components, API modules, and Zod schemas land in Phase 2 (products) and Phase 3 (movements).
- **Inter font** loaded with weights 400/500/600/700 via Google Fonts preconnect — matches UI-SPEC §"Typography" exactly.

## Verification Evidence

| Check | Result |
|-------|--------|
| `npm run type-check` (vue-tsc --noEmit) | PASS, zero TS errors under strict mode |
| `npm run build` | PASS, `dist/index.html` + `dist/assets/*.js` produced (139 kB main bundle, gzipped 54 kB) |
| `npm run lint` (`--max-warnings 0`) | PASS, zero errors, zero warnings |
| Vite dev server smoke (`curl localhost:5173/`, `/products`, `/stock-movements`) | PASS, all return SPA index with `id="app"`, `<title>StockEasy</title>`, Inter preconnect |
| `vite.config.ts` literal proxy target | PASS, `target: 'http://backend:8080'` present; `process.env.BACKEND_HOST` absent |
| `tailwind.config.js` brand palette | PASS, `#1863DC` present, full `brand-50..900` defined |
| Wordmark in AppShell.vue | PASS, `Stock<span class="text-brand-500">Easy</span>` literal preserved |

## Scope Boundary Honored

- Zero files touched in Plan 01 territory (Dockerfile, docker-compose.yml, init.sql).
- Zero files touched in Plan 02 territory (`backend/`).
- No Phase 2 components authored: no `ProductForm`, `ProductList`, `BaseButton`, `BaseModal`, `BaseTable`, `useToast`, no Vee-Validate, no Zod, no actual product/movement API calls.
- No Phase 3/4 dependencies introduced (no Pinia, Vuex, dayjs, lodash, jQuery, shadcn-vue, Element Plus, PrimeVue, Vuetify, Vitest, @vue/test-utils).

## Self-Check: PASSED

- `frontend/package.json`: FOUND
- `frontend/package-lock.json`: FOUND
- `frontend/tsconfig.json`: FOUND
- `frontend/tsconfig.node.json`: FOUND
- `frontend/vite.config.ts`: FOUND
- `frontend/tailwind.config.js`: FOUND
- `frontend/postcss.config.js`: FOUND
- `frontend/index.html`: FOUND
- `frontend/.gitignore`: FOUND
- `frontend/eslint.config.js`: FOUND
- `frontend/.prettierrc`: FOUND
- `frontend/env.d.ts`: FOUND
- `frontend/public/favicon.svg`: FOUND
- `frontend/src/main.ts`: FOUND
- `frontend/src/App.vue`: FOUND
- `frontend/src/style.css`: FOUND
- `frontend/src/router/index.ts`: FOUND
- `frontend/src/shared/api/client.ts`: FOUND
- `frontend/src/shared/api/health.ts`: FOUND
- `frontend/src/shared/components/AppShell.vue`: FOUND
- `frontend/src/shared/components/HealthPill.vue`: FOUND
- `frontend/src/features/products/pages/ProductsPage.vue`: FOUND
- `frontend/src/features/stock/pages/StockMovementsPage.vue`: FOUND
- `frontend/src/features/products/.gitkeep`: FOUND
- `frontend/src/features/stock/.gitkeep`: FOUND
- Commit `edbc1ac` (Task 1): FOUND
- Commit `7db5b97` (Task 2): FOUND
- Commit `f30f352` (Task 3): FOUND
