# Phase 2: Products Vertical Slice - Context

**Gathered:** 2026-05-16
**Status:** Ready for planning
**Mode:** Auto (autonomous chain) — recommended defaults selected for residual gray areas

<domain>
## Phase Boundary

End-to-end Products CRUD (create / list paginated / detail / soft-delete) via the Vue UI, talking to the .NET API. **Establishes all cross-cutting conventions** for the project: exception flow (`DomainException` + middleware mapping), FluentValidation pipeline + per-DTO validators, agentic OpenAPI plumbing (operationIds, ProducesResponseType, XML docs, `ErrorResponse` canonical envelope, dynamic hints, `_links`, pagination envelope), Vue UX foundation (4-state lists, BR formatting, Nielsen heuristics, Vee-Validate + Zod with mirrored messages, toast/modal/confirm primitives in `src/shared/`), brand-aware visual identity. Phase 3 will reuse all of this verbatim.

**Out of scope:** Stock movements (Phase 3), tests (Phase 4), branded PDFs (Phase 4), product code editing (immutable), hard delete, product images, multi-warehouse, audit log UI.

</domain>

<decisions>
## Implementation Decisions

### Locked by PROJECT.md + backend/CLAUDE.md + frontend/CLAUDE.md
The 48 requirements (BACK-05..16, PROD-01..07, AGENT-01..11, FRONT-05..08, FRONT-09..11 reuse, UX-01..13, CONF-02..03) are fully specified upstream. Every requirement ID becomes an executable acceptance criterion in PLAN.md. The planner MUST treat each requirement as a locked decision. Notable points the planner cannot relitigate:

- **Backend layout:** `Controllers/`, `Services/` (concrete classes, no interface), `Repositories/` (interface + impl), `Entities/` (POCO with setters), `DTOs/` (`record` types), `Validators/`, `Exceptions/`, `Middleware/`, `Infra/` — populated this phase.
- **Mapping:** manual inline in Service. NO AutoMapper / Mapster.
- **Dapper queries:** SQL inline with snake_case aliases mapped to PascalCase. No query builder.
- **Transactions:** explicit `BeginTransaction()` in Service for any multi-step. Single-step writes use connection's implicit transaction.
- **Exception flow:** `DomainException(errorCode, category, hint, retryable, details)` base + `NotFoundException` + `BusinessRuleException`. `ExceptionHandlingMiddleware` maps to canonical `ErrorResponse` `{ errorCode, category, message, hint, statusCode, retryable, details, traceId, timestamp }`. Categories closed: `VALIDATION`, `BUSINESS_RULE`, `NOT_FOUND`, `INTERNAL`. FluentValidation's `ValidationException` is caught and converted to a `ErrorResponse` with `errorCode: VALIDATION_ERROR` + `details: { field: [messages] }`.
- **Error catalog (closed set introduced this phase):** `VALIDATION_ERROR`, `PRODUCT_NOT_FOUND`, `DUPLICATE_CODE`, `INTERNAL_ERROR`. Phase 3 adds the movement codes (`MISSING_IDEMPOTENCY_KEY`, `MOVEMENT_NOT_FOUND`, `PRODUCT_DELETED`, `INSUFFICIENT_BALANCE`, `INVALID_MOVEMENT_VALUES`).
- **Hints are dynamic** (AGENT-08): the Service constructs the hint string with real context. Example: `DUPLICATE_CODE` → `"Já existe um produto com código '{code}'. Use outro código ou recupere o produto deletado em /api/products/{id}."`. Never hardcoded generic strings.
- **`_links` on every resource response:** at minimum `self`; add related actions where applicable (`delete` on product detail when not soft-deleted, `restore` placeholder for future).
- **Pagination envelope (AGENT-10):** `{ items, pagination: { page, pageSize, total, totalPages, hasNext, hasPrev }, _links: { self, first, last, next?, prev? } }`. Applies to `GET /api/products`.
- **Enum serialization:** `JsonStringEnumConverter` global, `JsonNamingPolicy.CamelCase`. Enum values in API are English (`Electronic`, `Appliance`, `Furniture`); UI translates via `src/shared/labels.ts` (`productTypeLabel`).
- **Frontend stack patterns:** Axios instance singleton with interceptor that normalizes responses to `ApiError` (`{ errorCode, category, message, hint, statusCode, retryable, details, traceId }`). Composables (`useProducts`) only, no Pinia. Vee-Validate + Zod, schemas mirror FluentValidation messages.
- **UX heuristics:** 4-state lists (skeleton / empty+CTA / error+hint+retry / data); toast green-3s on success; toast red-5s on error consuming `apiError.hint ?? apiError.message`; modals with `Cancelar` left + primary right, `Esc` closes, focus trap, focus returns to trigger.
- **BR formatting (UX-04):** `formatCurrency` → `Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })`; `formatDate` → `dd/mm/yyyy HH:mm` (no seconds in UI); `formatQuantity` → `Intl.NumberFormat('pt-BR')` with no decimals. Centralized in `src/shared/format.ts`.
- **Confirmations (CONF-02, CONF-03):** soft-delete shows confirmation modal explaining histórico is preserved; create + update submit directly (no fricção). Modal copy is locked here:
  - Title: `Excluir produto?`
  - Body: `Esta ação marca o produto como excluído. O histórico de movimentações permanece visível.`
  - Confirm button: `Excluir` (destructive `bg-danger`)
  - Cancel button: `Cancelar`

### Auto-resolved gray areas (auto mode, recommended defaults)

- **D-01 (Product detail view UI):** Detail is shown in a **read-only side drawer** (slide-in from right) launched when user clicks a row in `ProductList`. Drawer reuses `BaseModal` primitive in `right-pane` variant. Fields displayed: `code`, `description`, `type` (translated), `supplierValue` (formatted currency), `stockQuantity` (formatted quantity), `createdAt`/`updatedAt` (formatted date), and `deletedAt` if present (in red). Drawer also exposes the "Excluir produto" button (hidden if `deletedAt` is set). This is preferred over a dedicated route because the URL hierarchy stays flat (`/products` only) and the user keeps the list context. Reuses Nielsen #3 (controle e liberdade: easy to close).
- **D-02 (Searchable dropdown library):** **Custom `BaseSearchableSelect`** component in `src/shared/components/`. No vendor library (consistent with D14 "Tailwind sem UI library"). Implementation uses Vue's native `<input>` + filtered `<ul>` with keyboard nav (↑↓ Enter Esc). Phase 2 doesn't need it (no movement form yet), but the component is authored here so Phase 3 reuses it without delay.
- **D-03 (Toast & Modal primitives):** Custom `BaseToast` + `useToast`, `BaseModal` + `useConfirm` in `src/shared/components/` and `composables/`. Toasts auto-dismiss (3s success, 5s error), max 3 stacked, dismiss on click. Modal uses portal/teleport to `body`, traps focus, returns focus on close.
- **D-04 (Default sort on `GET /api/products`):** `ORDER BY created_at DESC, id DESC` server-side. Stable cursor for pagination. No client-side sort controls in Phase 2 — added if backlog requests it.
- **D-05 (`includeDeleted=true` UX):** Toggle switch above the list ("Mostrar excluídos"). Default off. When enabled, deleted rows render with `opacity-60` and a `Excluído` badge (warning yellow). Deleted rows show `deletedAt` in the side drawer.
- **D-06 (Form validation behavior):** `validateOnBlur: true` per FRONT-07. Inline error message under each input with `aria-describedby`. Submit button disabled while `meta.valid === false` (UX-09). On submit failure, focus moves to first invalid field.
- **D-07 (Empty state CTA):** When `/api/products` returns 0 items AND no filter active, show illustration (simple inline SVG — no external asset) + heading `Nenhum produto cadastrado` + primary button `Cadastrar primeiro produto` that opens the create form (drawer or page route — see D-08).
- **D-08 (Create form placement):** Create form opens in a **slide-in drawer from the right** (same `BaseModal` right-pane variant as detail view). Buttons: `Cancelar` (left, secondary) + `Cadastrar` (right, primary). On success: toast `Produto cadastrado com sucesso`, drawer closes, list refreshes (re-fetch first page).
- **D-09 (Error retry behavior on list):** Error state shows `apiError.hint` + retry button. Retry re-invokes the composable's `fetch()` without page reload. Network errors (no API response) map to a generic hint `Não foi possível conectar. Verifique sua conexão e tente novamente.`
- **D-10 (Hint catalog discipline):** Each `DomainException` subclass constructor takes context (e.g., `new DuplicateCodeException(code)`) and builds the hint inside the constructor. Hints are never `string.Format` calls in the consumer — always typed constructors. Service code reads: `throw new DuplicateCodeException(dto.Code);`.
- **D-11 (Validator messages):** FluentValidation messages are PT-BR and exactly mirror the Zod schema messages on the frontend. Source-of-truth pair: backend `Validators/CreateProductRequestValidator.cs` ↔ frontend `features/products/schemas/createProductSchema.ts`. Example: `"Código é obrigatório"`, `"Valor do fornecedor deve ser maior ou igual a zero"`.
- **D-12 (`_links` rel names):** Resource self link is `self`. Action links use rel names matching the operation verb in past tense or imperative: `delete` (idempotent action), `restore` (future). Pagination links use `first`, `last`, `next`, `prev`, `self`.

### Claude's Discretion
- Exact CSS for the side drawer (width, slide-in animation duration, backdrop opacity)
- Skeleton loading layout (rows, columns) — match the data table structure
- Toast positioning (top-right vs bottom-right) — default to bottom-right per common UX convention
- Specific lucide icons for actions (likely `Plus` for create, `Trash2` for delete, `Pencil` for edit-placeholder)
- Database column order in `init.sql` (already authored in Phase 1; this phase only reads/writes)
- Whether to add `restore` endpoint scaffolding now (default: NO, defer to backlog)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project-level rules
- `.planning/PROJECT.md` — 31 key decisions (D1–D31), out-of-scope, constraints, evolution rules
- `.planning/REQUIREMENTS.md` §BACK-05..16, PROD-01..07, AGENT-01..11, FRONT-05..08, FRONT-09..11, UX-01..13, CONF-02, CONF-03 — Phase 2 scope (48 reqs)
- `.planning/ROADMAP.md` §"Phase 2: Products Vertical Slice" — goal + 5 success criteria
- `.planning/phases/01-foundation/01-CONTEXT.md` — Phase 1 decisions D-01..D-09 (Docker, Vite proxy, CORS, schema policy, hello-world endpoint) are still authoritative

### Backend conventions (read in full)
- `backend/CLAUDE.md` — solution layout, namespace rules, package allowlist (`FluentValidation.AspNetCore`, `Swashbuckle.AspNetCore.Annotations`, `Dapper`, `Npgsql` only; NO AutoMapper/Mapster/MediatR), Dapper aliasing, exception flow, hint catalog, error response shape, OpenAPI requirements
- `/home/thallysrc/Projects/BancoShu/` — reference repo for: Dapper repository patterns, FluentValidation registration, ExceptionHandlingMiddleware implementation, `DomainException` hierarchy, `ErrorResponse` shape, Swagger XML doc inclusion, agentic patterns. **Exception:** the N+1 in `TransferService.GetHistoryAsync` is anti-pattern — do not copy

### Frontend conventions (read in full)
- `frontend/CLAUDE.md` — palette tokens, typography hierarchy, sidebar contract, src layout (feature-based), composables-only state, Axios interceptor shape, Vee-Validate + Zod mirroring, `BaseToast`/`BaseModal`/`useToast`/`useConfirm` primitives, BR formatting helpers, Nielsen heuristics (#1–#10), accessibility minimums

### Phase 1 artifacts (carry-forward)
- `.planning/phases/01-foundation/01-UI-SPEC.md` — locked design tokens (palette, typography, spacing, sidebar layout, wordmark, health pill state machine) — extends here for forms, tables, toasts, modals
- `init.sql` (repo root) — products + stock_movements schema, authoritative for both phases

### Existing implementation references
- `backend/Inventory/Middleware/ExceptionHandlingMiddleware.cs` — Phase 1 shell to be extended in Phase 2 (catch `DomainException`, `FluentValidation.ValidationException`, fallback `Exception`)
- `backend/Inventory/Dtos/ErrorResponse.cs` — Phase 1 stub to be filled with full canonical shape
- `frontend/src/shared/api/client.ts` — Phase 1 stub with `baseURL: '/api'` to be extended with interceptor that normalizes `ApiError`

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets (created in Phase 1)
- `backend/Inventory/Infra/IDbConnectionFactory.cs` + `DbConnectionFactory.cs` — Npgsql connection factory, inject into repositories
- `backend/Inventory/Middleware/ExceptionHandlingMiddleware.cs` — shell catches `Exception` and returns 500 `ErrorResponse`; extend to catch `DomainException` and `ValidationException` with proper mapping
- `backend/Inventory/Dtos/ErrorResponse.cs` — stub `record` type, extend to the full canonical 9-field shape
- `backend/Inventory/Controllers/HealthController.cs` — reference for `[ApiController]` + `[Route("api/[controller]")]` + `[SwaggerOperation(OperationId = "...")]` + `[ProducesResponseType]` patterns
- `backend/Inventory/Program.cs` — composition root with FluentValidation pipeline + CORS + Swagger already registered
- `frontend/src/shared/api/client.ts` — Axios singleton (extend with response interceptor)
- `frontend/src/shared/components/AppShell.vue` — sidebar layout, wordmark, nav items, HealthPill (no changes needed in P2)
- `frontend/src/shared/components/HealthPill.vue` — uses `lucide-vue-next` icons (reuse pattern for action icons)
- `frontend/src/features/products/pages/ProductsPage.vue` — `Em breve` placeholder, rewrite this phase
- Tailwind config with full `brand-50..900` + semantic tokens, Inter font loaded

### Established Patterns
- Backend file-scoped namespaces `Inventory.Api.{Folder}` enforced
- XML doc generation enabled in `Inventory.csproj` — every public type/member needs `<summary>`
- `<Nullable>enable</Nullable>` + `<ImplicitUsings>enable</ImplicitUsings>` — use nullable annotations everywhere
- Vue 3 Composition API + `<script setup>` only, TS strict
- Feature-based frontend folders (`features/products/`, `features/stock/`)
- Identifiers in English, user-facing strings in PT-BR

### Integration Points
- New backend endpoints under `Controllers/ProductsController.cs` reachable via Vite proxy `http://backend:8080/api/products/**`
- Frontend composable `useProducts` consumes Axios singleton; errors normalize to `ApiError` via interceptor
- FluentValidation auto-validation hooks into the pipeline already registered in Phase 1's `Program.cs`
- `JsonStringEnumConverter` already registered globally in Phase 1's `Program.cs`
- Swagger UI at `:8080/swagger` picks up new endpoints automatically once `[SwaggerOperation]` is added

</code_context>

<specifics>
## Specific Ideas

- "Agentic-friendly desde o MVP" — the differentiator. Every endpoint must be machine-readable from `/swagger`: stable `operationId`, every status code declared, every field with XML doc, enums as strings, error responses with full canonical shape and dynamic hints. The evaluator will see this in Swagger.
- "Validação espelhada front/back" — FluentValidation and Zod messages must be **identical PT-BR strings**, not just equivalent. Source-of-truth: backend Validator authored first, frontend schema mirrors verbatim.
- "Modal de confirmação no soft-delete" (CONF-02) is the **only** confirmation in Phase 2. Create and entrada submit directly (CONF-03).
- "BR formatting central" (UX-04, UX-05) — never `.toLocaleString()` inline; always through `src/shared/format.ts` / `src/shared/labels.ts`.
- Side drawer (D-01, D-08) instead of separate `/products/:id` route — preserves the `/products` URL space and keeps the sidebar/header layout stable.

</specifics>

<deferred>
## Deferred Ideas

- **Product update/edit** — spec doesn't require it. Only create + soft-delete. Add to v2 backlog.
- **Product restore endpoint** — `_links.restore` rel name reserved, but no implementation in v1.
- **Server-side sort controls** — default `created_at DESC`. Client-side sort UI deferred.
- **Free-text search / filter by type** — list is just paginated. Search/filter deferred to v2.
- **Bulk soft-delete** — single delete only.
- **Audit log of who deleted what** — no auth, no actor tracking.
- **Pagination cursor/keyset** — offset pagination is sufficient for the dataset size; cursor pagination is v2.
- **Image upload on products** — not in spec.
- **Inline edit** — explicitly out of scope; only create is supported.

</deferred>

---

*Phase: 02-products-vertical-slice*
*Context gathered: 2026-05-16 (auto mode within autonomous chain)*
