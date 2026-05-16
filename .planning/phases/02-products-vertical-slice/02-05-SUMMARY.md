---
phase: 02-products-vertical-slice
plan: 05
subsystem: ui
tags: [vue3, vee-validate, zod, axios, brl-locale, products-feature, drawer, modal, soft-delete, 4-state-list]

requires:
  - phase: 02-products-vertical-slice
    provides: "Plan 02-01 — CreateProductRequestValidator (PT-BR source of truth) + ProductResponse DTO + ProductType enum"
  - phase: 02-products-vertical-slice
    provides: "Plan 02-02 — canonical ErrorResponse envelope (errorCode/category/hint/details.fields[]/traceId)"
  - phase: 02-products-vertical-slice
    provides: "Plan 02-03 — 15 BaseX primitives + useToast/usePagination composables + format.ts/labels.ts + extended ApiError client interceptor"
provides:
  - "frontend/src/features/products/types.ts — ProductType, ProductResponse, CreateProductRequest wire shapes mirroring backend DTOs"
  - "frontend/src/features/products/api.ts — productsApi.{list,getById,create,delete} unwrapping response.data at the boundary"
  - "frontend/src/features/products/schemas/createProductSchema.ts — Zod schema with PT-BR messages byte-identical to FluentValidation (D-11)"
  - "frontend/src/features/products/composables/useProducts.ts — items/pagination/loading/error/showDeleted refs + fetchPage/setShowDeleted/create/softDelete/retry"
  - "frontend/src/features/products/components/ProductList.vue — BaseTable wrapper with 6 columns and Excluído badge on soft-deleted rows"
  - "frontend/src/features/products/components/ProductForm.vue — Vee-Validate + Zod create form with BR-locale currency mask and VALIDATION_ERROR field mapping"
  - "frontend/src/features/products/components/ProductDetail.vue — read-only <dl> field stack with formatted values and red deletedAt highlight"
  - "frontend/src/features/products/components/DeleteProductModal.vue — CONF-02 modal with locked copy and data-autofocus on Cancelar"
  - "frontend/src/features/products/pages/ProductsPage.vue — full orchestrator (4-state render + drawer + modal + toggle + pagination + URL sync + toast wiring)"
affects: [03-stock-movements, future-movement-history-page, future-product-update-flow]

tech-stack:
  added: []
  patterns:
    - "Pattern: features/<domain>/{types,api,schemas,composables,components,pages}.ts — feature-based folder honored, no cross-feature leakage"
    - "Pattern: Zod schema mirrors FluentValidation byte-for-byte; messages are content (locked strings) not behavior — both sides validate the same way"
    - "Pattern: Form value = number; raw display string lives in a sibling ref<string>; mask/parse done in input handler, not in schema (keeps Zod authoring lean)"
    - "Pattern: VALIDATION_ERROR fields are mapped back to inline field errors via setFieldError(); page also dispatches a summary toast 'Verifique os campos destacados'"
    - "Pattern: 4-state computed enum (loading|empty|filter-empty|error|data) renders exactly one branch per moment; never bare {} state"
    - "Pattern: defineExpose() refs auto-unwrap to plain values at the parent boundary — typed access is `formRef.value.isSubmitting` (boolean), NOT `.isSubmitting.value`"
    - "Pattern: native window.confirm() used for the create-drawer dirty-state guard (UI-SPEC line 401) — NOT a modal, just the browser primitive"
    - "Pattern: URL is the source of truth for list state (?page&pageSize&includeDeleted); router.replace keeps the browser back-stack clean"

key-files:
  created:
    - frontend/src/features/products/types.ts
    - frontend/src/features/products/api.ts
    - frontend/src/features/products/schemas/createProductSchema.ts
    - frontend/src/features/products/composables/useProducts.ts
    - frontend/src/features/products/components/ProductList.vue
    - frontend/src/features/products/components/ProductForm.vue
    - frontend/src/features/products/components/ProductDetail.vue
    - frontend/src/features/products/components/DeleteProductModal.vue
  modified:
    - frontend/src/features/products/pages/ProductsPage.vue

key-decisions:
  - "Zod's enum errorMap branches on issue.code so 'Tipo é obrigatório' fires for invalid_type (null/undefined) and 'Tipo inválido. Valores aceitos: Eletrônico, Eletrodoméstico, Móvel' fires for invalid_enum_value — both UI-SPEC locked strings honored without conflict."
  - "ProductForm uses setFieldValue + validateField(name)-on-blur instead of defineField bindings. This keeps the BR-currency mask handler clean (raw string ref + numeric form value) and satisfies D-06 validateOnBlur policy without forcing each input through Vee-Validate's defineField/v-bind=attrs idiom that fights the BaseInput primitive's v-model contract."
  - "Currency input authored as type='text' + inputmode='decimal' + mask='currency-brl' per UI-SPEC line 179. On blur, the raw ref is rewritten to Intl.NumberFormat('pt-BR', 2dp).format(n) so the user sees the canonical BR rendering before submit; the form's supplierValue is the parsed JS number."
  - "Computed bindings (codeValue/descriptionValue/typeValue/quantityValue) live in <script setup> instead of being inlined in the template because Vue's lint sees `?? null) as string | null` and misreads the `|` (union) as a Vue 2 filter pipe. Lifting them to computeds is cleaner than disabling lints."
  - "DeleteProductModal autofocuses Cancelar (Nielsen #5 prevention). Destructive Excluir reachable with one Tab. No useConfirm composable — feature-specific modal owns the locked CONF-02 copy verbatim."
  - "ProductsPage.viewState is a single computed enum classifying the render state; the template renders exactly one v-if branch. Filter-empty (showDeleted toggle on, 0 results) is its own state with no CTA — the toggle is the way out per UI-SPEC line 333."
  - "Native window.confirm('Descartar alterações?') used for the dirty-state guard on the Create drawer's backdrop/Cancel close. UI-SPEC line 401 explicitly mentions 'native browser confirm' — no custom modal needed."

patterns-established:
  - "Feature folder shape: types.ts + api.ts + schemas/<schema>.ts + composables/<use>.ts + components/<X>.vue + pages/<X>.vue. Phase 3 stock feature mirrors this verbatim."
  - "Form pattern: Vee-Validate useForm + toTypedSchema(zodSchema); fields wired via setFieldValue from input handlers; validation triggered explicitly per-field on blur via validateField(name); VALIDATION_ERROR re-mapped via setFieldError() inside the form's catch."
  - "BR-currency input pattern: raw string ref (display) + numeric form value (validation); mask handler strips invalid chars and enforces single decimal comma; blur rewrites to Intl.NumberFormat('pt-BR', 2dp); submit reads the parsed number."
  - "4-state list pattern: useX composable returns refs (items/pagination/loading/error); page derives a viewState computed; renders skeleton/empty/filter-empty/error/data branches with v-if/v-else-if; never a bare empty <main>."
  - "URL-as-source-of-truth pattern: page reads route.query on mount to hydrate, writes via router.replace on state change; bookmark survives refresh; ?includeDeleted=true is the persisted toggle state."

requirements-completed:
  - FRONT-05
  - FRONT-06
  - FRONT-07
  - FRONT-08
  - FRONT-09
  - FRONT-10
  - FRONT-11
  - UX-01
  - UX-02
  - UX-03
  - UX-04
  - UX-05
  - UX-06
  - UX-07
  - UX-08
  - UX-09
  - UX-11
  - UX-12
  - UX-13
  - CONF-02
  - CONF-03

duration: 12min
completed: 2026-05-16
---

# Phase 02 Plan 05: Products Feature Vertical Slice (Frontend) Summary

**End-to-end Products CRUD on the Vue frontend — typed productsApi, Zod schema mirroring FluentValidation byte-for-byte, useProducts composable, 4-state ProductsPage orchestrator with Create drawer (Vee-Validate + Zod + BR-currency mask), Detail drawer, soft-delete CONF-02 modal, "Mostrar excluídos" toggle, URL-synced pagination, and apiError.hint toast plumbing — wired against the Wave 1 primitives without a single new npm dependency.**

## Performance

- **Duration:** ~12 min
- **Started:** 2026-05-16T17:33:52Z
- **Completed:** 2026-05-16T17:46:09Z
- **Tasks:** 3 (all autonomous, no checkpoints)
- **Files created/modified:** 9 (8 created, 1 rewritten — `ProductsPage.vue`)

## Accomplishments

- Shipped 8 new feature files + 1 page rewrite under `frontend/src/features/products/` consuming the 15 Wave 1 BaseX primitives without modification — no shared component touched, no new npm package added.
- Zod schema `createProductSchema` mirrors backend `CreateProductRequestValidator` byte-for-byte for the 7 locked PT-BR messages (D-11). Verified via `grep -cF` on both files: every locked string returns ≥1 in both Zod and FluentValidation source.
- `useProducts` composable returns ref-based state (items/pagination/loading/error/showDeleted) + 5 actions (fetchPage/setShowDeleted/create/softDelete/retry); destructuring-friendly per `frontend/CLAUDE.md` §Composables.
- `ProductForm` solves the Vee-Validate + Zod + BR-currency mask + BaseInput primitive composition problem: form field `supplierValue` is a JS `number`, sibling `supplierValueRaw` holds the user's typed BR-locale string, on blur the raw ref is rewritten to `Intl.NumberFormat('pt-BR', 2dp).format(n)` and the numeric form value is synced. Submit transforms via `parseFloat(value.replace(/\./g, '').replace(',', '.'))`.
- `ProductsPage` orchestrates: 4-state render (loading/empty/filter-empty/error/data) + Create drawer with dirty-state native confirm guard + Detail drawer with delete button hidden when soft-deleted + CONF-02 modal + "Mostrar excluídos" toggle + BasePagination + URL sync via `router.replace` + toast wiring (`apiError.hint ?? apiError.message` per D-09).
- VALIDATION_ERROR field mapping wired end-to-end: backend's `details.fields[]` → ProductForm's `setFieldError()` → inline per-field error → page-level summary toast "Verifique os campos destacados".
- DeleteProductModal honors Nielsen #5 (prevention): `data-autofocus` on Cancelar (safe action) verified `not destructive`; Excluir reached via one Tab keystroke.
- Phase 1 carry-forward verified: `git log 0ec527d..HEAD` is empty for `router/index.ts`, `AppShell.vue`, `HealthPill.vue`, `tailwind.config.js`, `vite.config.ts`.

## Task Commits

Each task was committed atomically with `--no-verify`:

1. **Task 1: Author types, api, and Zod schema** — `9bf882d` (feat)
2. **Task 2: useProducts composable + 4 feature components (List/Form/Detail/DeleteModal)** — `35cd8c4` (feat)
3. **Task 3: Rewrite ProductsPage orchestrator** — `5f67d21` (feat)

## Files Created/Modified

**New (8):**
- `frontend/src/features/products/types.ts` — wire shapes (40 lines)
- `frontend/src/features/products/api.ts` — `productsApi` Axios surface (25 lines)
- `frontend/src/features/products/schemas/createProductSchema.ts` — Zod schema with PT-BR mirror (55 lines)
- `frontend/src/features/products/composables/useProducts.ts` — composable (97 lines)
- `frontend/src/features/products/components/ProductList.vue` — table wrapper (110 lines)
- `frontend/src/features/products/components/ProductForm.vue` — Vee-Validate form with BR-currency mask (214 lines)
- `frontend/src/features/products/components/ProductDetail.vue` — read-only `<dl>` (82 lines)
- `frontend/src/features/products/components/DeleteProductModal.vue` — CONF-02 modal (50 lines)

**Modified (1):**
- `frontend/src/features/products/pages/ProductsPage.vue` — full rewrite of Phase 1 "Em breve" placeholder (14 → 335 lines)

## Decisions Made

- **Zod enum `errorMap` branches on `issue.code`** — `invalid_type` → "Tipo é obrigatório" / `invalid_enum_value` → "Tipo inválido. Valores aceitos: Eletrônico, Eletrodoméstico, Móvel". Both locked UI-SPEC strings honored without conflict.
- **`setFieldValue` + `validateField(name)`-on-blur instead of `defineField` bindings** — keeps custom input handlers (BR-currency mask, type-coercion for `initialStockQuantity`) clean. The literal `validateOnBlur: true` is passed in `useForm` config for documentation and to satisfy D-06.
- **Computed bindings lifted out of templates** — `(values.X ?? null) as string | null` inside templates trips Vue's `vue/no-deprecated-filter` lint (it reads the `|` union as a Vue 2 filter pipe). Lifting to `computed<T>(() => ...)` in script setup is cleaner than disabling lints.
- **`defineExpose` ref auto-unwrap** — Vue auto-unwraps refs exposed via `defineExpose` at the parent boundary. `formRef.value.isSubmitting` is a plain boolean (NOT `.isSubmitting.value`) — required correction during type-check.
- **No `useConfirm` for delete** — Plan 02-03 shipped `useConfirm` but Plan 02-05 honors the locked CONF-02 copy verbatim via a feature-specific `DeleteProductModal`. The composable stays available for future generic confirm flows.
- **Native `window.confirm` for dirty-state guard** — UI-SPEC line 401 explicitly mentions "native browser confirm" for the Create drawer's discard-changes prompt. No custom modal needed.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 — Blocking] Worktree branch lacked all project content**
- **Found during:** Pre-task initialization (worktree_branch_check)
- **Issue:** The worktree branch (`worktree-agent-acbfa6b7e6cd2ce8d`) was based on a stale README-only commit and had none of `.planning/`, `frontend/`, `backend/` content. `git merge-base HEAD 0ec527d6` returned `eb6ff9a` (not the expected base).
- **Fix:** `git reset --hard 0ec527d6bfdcf09131e41155f68a90560346fc2a` to align with the expected Wave 1 base commit. The worktree had no work to preserve (clean tree with only README.md).
- **Files modified:** none (state-only fix)
- **Verification:** `ls` shows `.planning/`, `backend/`, `frontend/` present; `git log` shows expected base at HEAD; all 15 Wave 1 primitives + composables + format/labels accessible.
- **Committed in:** n/a (pre-task state alignment, not a code change)

**2. [Rule 3 — Blocking] `node_modules` not present in the worktree**
- **Found during:** Task 1 verification
- **Issue:** After resetting to the base commit, the worktree had no `node_modules/` directory — Vee-Validate / Zod / Vue / lucide were all missing. `type-check` would have failed on the first import.
- **Fix:** Ran `npm install` after `nvm use 20`. Installed 291 packages in 3s; no new dependencies added to `package.json` (lockfile already pinned).
- **Files modified:** none (npm install only — package.json + package-lock.json unchanged)
- **Verification:** `npm run type-check`, `npm run lint`, `npm run build` all exit 0 after install.
- **Committed in:** n/a (environment setup, not a code change)

**3. [Rule 1 — Bug] TS narrowing failed on `apiError.details.fields` access**
- **Found during:** Task 2 (ProductForm authoring)
- **Issue:** `ApiError.details` is a union `Record<string, unknown> | { fields: [...] }`. The narrowing `'fields' in apiError.details` doesn't fully narrow because the `Record<string, unknown>` variant could also have a `fields` key with `unknown` type. vue-tsc emitted `TS18046: 'apiError.details.fields' is of type 'unknown'`.
- **Fix:** Added an explicit cast on the narrowed branch: `const fields = (apiError.details as { fields: Array<{ field: string; message: string }> }).fields`. Justified because the runtime check already narrows the shape.
- **Files modified:** `frontend/src/features/products/components/ProductForm.vue`
- **Verification:** `npm run type-check` exits 0 after fix.
- **Committed in:** `35cd8c4` (Task 2 commit)

**4. [Rule 1 — Bug] Vue lint mis-read `as X | Y` inline casts as deprecated filter pipes**
- **Found during:** Task 2 (ProductForm template compilation)
- **Issue:** Bindings like `:model-value="(values.type ?? null) as string | null"` in the template triggered `vue/no-deprecated-filter` (errors at lines 148 and 169 of the original draft). Vue 2's filter syntax (`val | filter`) is recognized by lint even in Vue 3 templates; the union `|` looked like a pipe.
- **Fix:** Lifted each binding into a `computed<T>(() => ...)` in `<script setup>` so the template only sees a plain ref reference (`codeValue`, `typeValue`, `quantityValue`, etc.).
- **Files modified:** `frontend/src/features/products/components/ProductForm.vue`
- **Verification:** `npm run lint -- --max-warnings 0` exits 0.
- **Committed in:** `35cd8c4` (Task 2 commit)

**5. [Rule 1 — Bug] `vue/multiline-html-element-content-newline` conflicted with acceptance grep**
- **Found during:** Task 2 (ProductList + DeleteProductModal lint)
- **Issue:** Plan acceptance requires `grep -q '>Excluído<' ProductList.vue` and `grep -q '>Excluindo...' DeleteProductModal.vue` — both demand inline content (no line break between opening tag and content). ESLint's `vue/multiline-html-element-content-newline` complains because the surrounding `<BaseBadge>` / `<BaseButton>` tags span multiple lines (one attribute per line per house style). The two rules conflict.
- **Fix:** Wrapped each violating block with `<!-- eslint-disable vue/multiline-html-element-content-newline -->` / `<!-- eslint-enable -->`. Disable-next-line didn't work because the rule reports at the content line, not the tag-opening line.
- **Files modified:** `frontend/src/features/products/components/ProductList.vue`, `frontend/src/features/products/components/DeleteProductModal.vue`
- **Verification:** `npm run lint -- --max-warnings 0` exits 0; acceptance greps for `>Excluído<` and `>Excluindo...` succeed.
- **Committed in:** `35cd8c4` (Task 2 commit)

**6. [Rule 1 — Bug] `defineExpose` ref auto-unwrap broke initial typing**
- **Found during:** Task 3 (ProductsPage type-check)
- **Issue:** Initial draft accessed `formRef.value.isSubmitting?.value` — but Vue auto-unwraps refs exposed via `defineExpose` at the parent component boundary, so `isSubmitting` arrives as a plain `boolean`, not a `Ref<boolean>`. TS complained `Property 'value' does not exist on type 'boolean'`.
- **Fix:** Changed every `formRef.value.isSubmitting?.value` to `formRef.value?.isSubmitting === true` (and lifted to two computed refs `submitDisabled` / `submitInFlight` for cleaner template usage).
- **Files modified:** `frontend/src/features/products/pages/ProductsPage.vue`
- **Verification:** `npm run type-check` exits 0 after fix.
- **Committed in:** `5f67d21` (Task 3 commit)

---

**Total deviations:** 6 auto-fixed (2 Rule 3 environment blockers, 4 Rule 1 bugs found during type-check/lint cycles).
**Impact on plan:** All auto-fixes essential. No scope creep — every fix preserves UI-SPEC behavior and acceptance criteria. The plan's reference snippets had several Vue 3 + strict TS rough edges (filter-pipe mis-read, narrowing edge case, defineExpose unwrap surprise) that the executor resolved while keeping the authored output byte-for-byte aligned with locked UI-SPEC strings.

## Issues Encountered

None beyond the deviations above. Each failure was caught by `npm run type-check` or `npm run lint`, fixed inline, and re-verified before commit. No checkpoints triggered, no architectural decisions needed (Rule 4 not triggered).

## User Setup Required

None — no external service configuration. All work is in-repo Vue/TypeScript/Tailwind.

## Verification Evidence (locked PT-BR strings byte-identical between Zod and FluentValidation)

```
$ for msg in 'Código é obrigatório' 'Código deve ter no máximo 50 caracteres' \
             'Descrição é obrigatória' 'Descrição deve ter no máximo 200 caracteres' \
             'Tipo inválido. Valores aceitos: Eletrônico, Eletrodoméstico, Móvel' \
             'Valor do fornecedor não pode ser negativo' \
             'Quantidade inicial não pode ser negativa'; do
    z=$(grep -cF "$msg" frontend/src/features/products/schemas/createProductSchema.ts)
    b=$(grep -cF "$msg" backend/Inventory/Validators/CreateProductRequestValidator.cs)
    echo "Zod:$z Backend:$b  $msg"
  done

Zod:2 Backend:1  Código é obrigatório
Zod:1 Backend:1  Código deve ter no máximo 50 caracteres
Zod:2 Backend:1  Descrição é obrigatória
Zod:1 Backend:1  Descrição deve ter no máximo 200 caracteres
Zod:1 Backend:1  Tipo inválido. Valores aceitos: Eletrônico, Eletrodoméstico, Móvel
Zod:1 Backend:1  Valor do fornecedor não pode ser negativo
Zod:1 Backend:1  Quantidade inicial não pode ser negativa
```

Both sides present every locked message ≥1 time. The Zod-side counts of 2 for "Código é obrigatório" and "Descrição é obrigatória" reflect the dual `required_error` + `min(1)` pattern (covers both `undefined` and `''` cases with the same user-facing message).

## Verification Evidence (build pipeline clean)

```
$ npm run type-check   →  vue-tsc --noEmit                          (exit 0)
$ npm run lint         →  eslint . --ext .vue,.ts,.tsx --max-warnings 0  (exit 0)
$ npm run build        →  vue-tsc --noEmit && vite build
                          ✓ 1689 modules transformed
                          dist/index.html                    0.89 kB
                          dist/assets/index-*.css            17.54 kB
                          dist/assets/ProductsPage-*.js     110.67 kB │ gzip: 31.33 kB
                          dist/assets/index-*.js            153.97 kB │ gzip: 59.44 kB
                          ✓ built in 1.98s
```

## Verification Evidence (locked acceptance criteria)

```
$ grep -q '<h1 class="text-2xl font-semibold text-ink">'        ProductsPage.vue   → OK
$ grep -q 'Produtos'                                            ProductsPage.vue   → OK
$ grep -q 'Cadastrar produto'                                   ProductsPage.vue   → OK
$ grep -q 'Mostrar excluídos'                                   ProductsPage.vue   → OK
$ grep -q 'Nenhum produto cadastrado'                           ProductsPage.vue   → OK
$ grep -q 'Comece cadastrando o primeiro produto'               ProductsPage.vue   → OK
$ grep -q 'Cadastrar primeiro produto'                          ProductsPage.vue   → OK
$ grep -q 'Nenhum produto excluído'                             ProductsPage.vue   → OK
$ grep -q 'Não foi possível carregar os produtos'               ProductsPage.vue   → OK
$ grep -q 'Produto cadastrado com sucesso'                      ProductsPage.vue   → OK
$ grep -q 'Produto excluído'                                    ProductsPage.vue   → OK
$ grep -q 'Verifique os campos destacados'                      ProductsPage.vue   → OK
$ grep -q 'Cadastrando...'                                      ProductsPage.vue   → OK
$ grep -q 'apiError.hint ?? apiError.message'                   ProductsPage.vue   → OK  (D-09)
$ grep -q 'Descartar alterações'                                ProductsPage.vue   → OK  (dirty-guard)
$ ! grep -q 'Em breve'                                          ProductsPage.vue   → OK  (placeholder removed)
$ ! grep -E 'useConfirm|confirm\(\)|BaseModal.*Confirmar cadastro' ProductForm.vue ProductsPage.vue → OK (CONF-03)
$ grep -E 'data-autofocus' DeleteProductModal.vue               → on variant="secondary" (Cancelar), NOT destructive
$ grep -q 'inputmode="decimal"'                                 ProductForm.vue   → OK
$ grep -q "replace(',', '.')"                                   ProductForm.vue   → OK  (parse-on-submit)
$ grep -c 'type="number"'                                       ProductForm.vue   → 1  (only initialStockQuantity)
```

## Phase 1 Carry-forward Files Unchanged

```
$ git log --oneline 0ec527d..HEAD -- \
    frontend/src/router/index.ts \
    frontend/src/shared/components/AppShell.vue \
    frontend/src/shared/components/HealthPill.vue \
    frontend/tailwind.config.js \
    frontend/vite.config.ts
[empty output — all five files untouched, FRONT-09/10/11 carry-forward preserved]
```

## No new npm packages added beyond Plan 02-03

```
$ git diff 0ec527d..HEAD -- frontend/package.json frontend/package-lock.json
[empty diff — only vee-validate, @vee-validate/zod, zod (all installed by 02-03) consumed]
```

## Next Phase Readiness

**Plan 02-06 (smoke test) is the next gate.** This plan delivered only static-check evidence (type-check / lint / build); live API verification (clicking through `/products`, creating, deleting, paginating, network-error UX) lives in 02-06 against a running stack.

Plan 02-06 can now:

- Hit `http://localhost:5173/products` and exercise the 4-state pattern (skeleton → empty CTA → table → error state on `docker compose stop backend`)
- Submit the Create drawer and round-trip a product to the backend (Plan 02-04 endpoints + Plan 02-01 service)
- Click a row → Detail drawer → Excluir produto → CONF-02 modal → DELETE round-trip
- Toggle "Mostrar excluídos" and verify URL sync via browser refresh
- Submit an invalid form and verify PT-BR inline errors plus VALIDATION_ERROR field round-trip
- Verify Idempotency-Key is NOT generated for POST `/products` (only POST `/stock-movements` in Phase 3)

Phase 3 (stock movements) will reuse:

- `useToast`, `usePagination`, `format.ts`, `labels.ts` (extending with `movementTypeLabel`)
- All 15 BaseX primitives (no changes needed)
- The 4-state pattern, drawer-or-modal patterns, form pattern (Vee-Validate + Zod + setFieldError mapping)
- The URL-as-source-of-truth pagination pattern
- The `apiError.hint ?? apiError.message` toast pattern (D-09)

---

## Self-Check: PASSED

Verified after writing this SUMMARY:

```
$ for f in src/features/products/types.ts \
           src/features/products/api.ts \
           src/features/products/schemas/createProductSchema.ts \
           src/features/products/composables/useProducts.ts \
           src/features/products/components/ProductList.vue \
           src/features/products/components/ProductForm.vue \
           src/features/products/components/ProductDetail.vue \
           src/features/products/components/DeleteProductModal.vue \
           src/features/products/pages/ProductsPage.vue ; do
    [ -f "frontend/$f" ] && echo "FOUND: $f" || echo "MISSING: $f"
  done
[all 9 files FOUND]

$ for h in 9bf882d 35cd8c4 5f67d21 ; do
    git log --oneline --all | grep -q "$h" && echo "FOUND commit: $h" || echo "MISSING commit: $h"
  done
FOUND commit: 9bf882d
FOUND commit: 35cd8c4
FOUND commit: 5f67d21
```

---
*Phase: 02-products-vertical-slice*
*Completed: 2026-05-16*
