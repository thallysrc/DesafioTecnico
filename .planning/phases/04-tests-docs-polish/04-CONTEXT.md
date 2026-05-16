# Phase 4: Tests, Docs & Polish - Context

**Gathered:** 2026-05-16 (auto mode)
**Status:** Ready for planning

<domain>
## Phase Boundary

Tests + Docs hardening of the existing Phase 2 (Products) and Phase 3 (Stock Movements) vertical slices. Phase delivers:

1. **Backend xUnit + Moq suite** covering `ProductService`, `StockMovementService`, and validators — every typed exception asserts `ErrorCode`, message substring, and non-null `Hint` (TEST-01..04).
2. **Frontend Vitest suite** covering `useProducts`/`useStockMovements` composables (mocked API layer) and `ProductForm` + `OutboundForm` (mount + basic interaction) (TEST-05..06).
3. **3 branded PDF docs** in `docs/dist/` (`01-product-decisions.pdf`, `02-architecture.pdf`, `03-business-rules.pdf`) generated via Pandoc + WeasyPrint + mermaid-filter, committed to the repo so evaluator reads them without installing toolchains (DOC-01..08).
4. **Root README polish** linking docker-compose run, Swagger/frontend URLs, and `docs/dist/` (DOC-09).

**Scope anchor:** No new business capability. No retroactive feature changes. No E2E (Playwright/Cypress) tests. No CI pipeline (TEST-07 = "CI local" only). Tracks are independent — tests and docs can run in parallel waves.

</domain>

<decisions>
## Implementation Decisions

### Backend test strategy
- **D-01:** Pure Moq of `IProductRepository` / `IStockMovementRepository` (no Testcontainers, no real Postgres). Tests run with `dotnet test` alone — no Docker required by the evaluator.
  - Rationale: faster + deterministic + matches BancoShu's test style for the evaluator.
- **D-02:** Cover every errorCode for each service:
  - `ProductService`: happy paths (Create, List, Get, Delete) + `DUPLICATE_CODE`, `PRODUCT_NOT_FOUND`, `INTERNAL_ERROR` (rethrow path).
  - `StockMovementService`: happy paths (Inbound success, Outbound success, List, Get, idempotency replay) + `MISSING_IDEMPOTENCY_KEY`, `INSUFFICIENT_BALANCE`, `PRODUCT_DELETED`, `PRODUCT_NOT_FOUND`, `INVALID_MOVEMENT_VALUES`, `MOVEMENT_NOT_FOUND` + 23505 race-window recovery.
- **D-03:** Every exception test asserts the trio `(ex.ErrorCode, ex.Message contains "…", ex.Hint is non-null)`. Single helper `AssertDomain(ex, code, messageSubstring)` keeps the trio cohesive.
- **D-04:** Validator tests use `FluentValidation.TestHelper.TestValidator` extensions (`.ShouldHaveValidationErrorFor(x => x.Field).WithErrorMessage("...")`). Cover both `CreateProductRequestValidator` (already exists) and `CreateMovementRequestValidator`.
- **D-05:** Test naming: `MethodName_StateUnderTest_ExpectedBehavior` (xUnit convention). One `[Fact]` per scenario; `[Theory]` only when parameterization is genuinely additive (e.g., negative-quantity boundary checks).

### Frontend test strategy
- **D-06:** Vitest + `@vue/test-utils` v2 + **happy-dom** (not jsdom). happy-dom is ~2× faster for our needs — we don't touch any jsdom-exclusive APIs.
- **D-07:** Add devDeps: `vitest`, `@vue/test-utils`, `happy-dom`. Add `test` script to `package.json`. Add minimal `vitest.config.ts` with happy-dom environment + path alias `@` mirroring `vite.config.ts`.
- **D-08:** Composable tests mock the Axios `api` module (`vi.mock('@/features/products/api')` and `vi.mock('@/features/stock/api')`). Assert: state transitions (loading→success / loading→error), `apiError` shape preserved, list/page updates after mutation.
- **D-09:** Component tests use **shallow mount with primitive stubs** (`BaseInput`, `BaseSelect`, `BaseSearchableSelect`, `BaseButton` as stubs). Tests verify form logic, not primitive rendering — primitives were proven by Phase 2/3 implementation. Assert: mounts without error, validation error display on bad input, submit calls composable with normalized payload, emits `created`/`registered` events.
- **D-10:** No coverage thresholds. TEST-07 explicitly only requires `dotnet test` and `npm test` to "run sem falhas" (green). Skip `@vitest/coverage-v8` and `coverlet` — keeps deps lean.

### Docs structure
- **D-11:** `docs/` layout (locked by DOC-01..08):
  ```
  docs/
  ├── README.md                          # index + install instructions for generate-pdfs.sh
  ├── 01-product-decisions.md            # vision, decisions, trade-offs, v2 roadmap
  ├── 02-architecture.md                 # 5 Mermaid diagrams + stack table + run instructions
  ├── 03-business-rules.md               # entities, enums, regras, errorCode catalog
  ├── assets/
  │   ├── pdf-style.css                  # StockEasy palette + Inter + paged media
  │   └── pandoc-template.html           # Pandoc HTML scaffold (capa, header/footer)
  ├── generate-pdfs.sh                   # Pandoc + WeasyPrint + mermaid-filter pipeline
  └── dist/
      ├── 01-product-decisions.pdf       # committed
      ├── 02-architecture.pdf            # committed
      └── 03-business-rules.pdf          # committed
  ```
- **D-12:** Source markdown is PT-BR (consistent with PT-BR conventions locked since Phase 1: identifiers in EN, user-facing text in PT-BR). Diagrams labels are PT-BR friendly but technical identifiers (file paths, class names, JSON fields) stay EN.

### Mermaid diagrams
- **D-13:** Five diagrams for `02-architecture.md` (per DOC-03):
  1. **System Context** — Evaluator/User ↔ Vue SPA ↔ Backend API ↔ Postgres (+ docker-compose envelope).
  2. **Backend Layered** — Controller → Service → Repository → Dapper → Postgres + Middleware exception flow.
  3. **Sequence: Stock-out** — User → OutboundForm → CONF-01 → API → Service (SELECT FOR UPDATE → validate balance → INSERT → UPDATE) → Postgres → Response. Includes idempotency replay path.
  4. **ER Diagram** — `products` + `stock_movements` (with `idempotency_key UNIQUE`, `deleted_at`, FKs).
  5. **Frontend Feature Flow** — `App.vue` → router → `StockMovementsPage` → tabs (`InboundForm` / `OutboundForm` / `MovementHistory`) → composables → api → Axios interceptor.
- **D-14:** Each diagram is constrained to **≤15 nodes** and fits on 1 PDF page. mermaid-filter inlines SVG; styling inherits from `pdf-style.css`.

### PDF pipeline
- **D-15:** `docs/generate-pdfs.sh` is a single Bash script that loops over the 3 source markdown files and emits 3 PDFs:
  ```bash
  for doc in 01-product-decisions 02-architecture 03-business-rules; do
    pandoc "$doc.md" \
      --filter mermaid-filter \
      --template assets/pandoc-template.html \
      --css assets/pdf-style.css \
      --pdf-engine=weasyprint \
      -o "dist/$doc.pdf"
  done
  ```
- **D-16:** Host install requirements documented at top of `docs/README.md`:
  - `pandoc` (apt/brew)
  - `python3-weasyprint` (apt) or `weasyprint` (pip)
  - `mermaid-filter` (`npm install -g mermaid-filter`)
  - Mermaid-CLI Chromium dependency (puppeteer; auto-handled by `mermaid-filter` install)
- **D-17:** PDFs are committed to `docs/dist/`. Regeneration is **opt-in** for the developer (`docs/generate-pdfs.sh`), not automatic. The evaluator never runs the script — they just `git clone` and open the PDFs.
- **D-18:** `docs/assets/pdf-style.css` uses CSS Paged Media:
  - Palette: brand-50..900 (#1863DC primary), success/warning/danger semantics from Phase 1 PROJECT.md
  - Inter via `@font-face` (locally bundled fallback) or Google Fonts CDN reference
  - Capa (cover page) with StockEasy wordmark, version, date
  - Header (running): `StockEasy — {doc title}`
  - Footer (running): page number `{n} / {total}`
  - Mermaid SVG: `max-width: 100%; page-break-inside: avoid`
- **D-19:** `docs/assets/pandoc-template.html` is a minimal Pandoc HTML5 template with a `<header class="cover">` block consumed only by WeasyPrint's `@page :first` rule (CSS-driven cover). Keeps logic in CSS, template stays simple.

### Root README polish
- **D-20:** **Extend** the existing root `README.md` (already extended by Phase 1 + Phase 3) — do NOT replace. Append/refine sections so the polished README has:
  - Quick-start: `docker-compose up`
  - URLs table: API (`http://localhost:8080`), Swagger (`/swagger`), Frontend (`http://localhost:5173`)
  - Stack one-liner: ".NET 8 + Dapper + Postgres 16 / Vue 3 + TS + Tailwind"
  - Docs section: links to `docs/dist/*.pdf` (the 3 branded PDFs) so the evaluator reads them without installing toolchains
  - Tests one-liner: `cd backend && dotnet test` / `cd frontend && npm install && npm test`
  - Link to original challenge spec (preserve evaluator's anchor)

### Track parallelization
- **D-21:** Tests track and docs track are **fully independent** (ROADMAP confirms). Planner should split into:
  - Wave A (parallel): Backend tests plan + Frontend tests plan + Docs writing plan + PDF pipeline plan
  - Wave B: Root README polish + final PDF regeneration commit + verification
- **D-22:** Tests track must complete BEFORE any final PDF regeneration (so `03-business-rules.md` errorCode catalog can reference real test coverage if desired). But this is a soft ordering — docs can be drafted in parallel and PDF regenerated in Wave B.

### Anti-scope
- **D-23:** No E2E framework (Playwright/Cypress) — out of scope. The 03-VERIFICATION.md smoke evidence + 03-HUMAN-UAT.md already cover the integration plane.
- **D-24:** No GitHub Actions CI workflow — TEST-07 says "CI local" (i.e., runs on developer machine). Adding `.github/workflows/` is scope creep for a 1-3 day evaluation deliverable.
- **D-25:** No new code-coverage tooling (coverlet, @vitest/coverage-v8). Green test runs satisfy TEST-07.
- **D-26:** No retroactive refactors to existing Phase 2/3 code. If a test reveals an existing bug, scope decision: file as a gap and re-plan (gap-closure phase), do not silently patch in this phase.

### Claude's Discretion
- Exact `pdf-style.css` typography scale (sizes, leading) — keep tasteful and consistent with Inter; not user-decided.
- Exact Mermaid theme tokens — match brand palette but no spec required.
- Test file directory structure within `Inventory.Tests/` — flat by feature is fine (e.g., `Services/ProductServiceTests.cs`, `Services/StockMovementServiceTests.cs`, `Validators/`).
- Whether to use Vitest's `globals: true` mode or explicit imports — default to globals for terser tests.
- Whether to bundle Inter as a webfont or use Google Fonts CDN — pick the simpler one that produces consistent PDFs.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase requirements
- `.planning/ROADMAP.md` §"Phase 4: Tests, Docs & Polish" — Goal + 5 success criteria
- `.planning/REQUIREMENTS.md` §Tests (TEST-01..07) + §Documentation (DOC-01..09)

### Project conventions
- `.planning/PROJECT.md` — Stack, constraints, brand palette (brand-50..900, semantic colors), Nielsen heuristics, BancoShu reference exception
- `./CLAUDE.md` — Project entry + stack + GSD workflow enforcement
- `backend/Inventory.Tests/Inventory.Tests.csproj` — xUnit 2.9 + Moq 4.20 already installed; project reference to `Inventory` already wired
- `backend/Inventory.Tests/Validators/CreateProductRequestValidatorTests.cs` — Reference validator test from Phase 2

### Phase 2 + 3 implementation surface (what the tests exercise)
- `.planning/phases/02-products-vertical-slice/02-01-SUMMARY.md` through `02-06-SUMMARY.md` — ProductService API, errorCodes, validators
- `.planning/phases/03-stock-movements-vertical-slice/03-01-SUMMARY.md` — Repository contract + 5 typed exceptions
- `.planning/phases/03-stock-movements-vertical-slice/03-02-SUMMARY.md` — Frontend types + api + composable + schemas
- `.planning/phases/03-stock-movements-vertical-slice/03-03-SUMMARY.md` — StockMovementService transaction + idempotency + errorCodes
- `.planning/phases/03-stock-movements-vertical-slice/03-04-SUMMARY.md` — Vue components (InboundForm, OutboundForm, etc.)
- `.planning/phases/03-stock-movements-vertical-slice/03-VERIFICATION.md` — Evidence of behavior the tests must reproduce
- `backend/Inventory/Exceptions/` — `DomainException` base + 5 stock exceptions + Phase 2 product exceptions
- `backend/Inventory/Services/ProductService.cs` + `StockMovementService.cs` — Implementations under test
- `backend/Inventory/Validators/` — `CreateProductRequestValidator.cs` + `CreateMovementRequestValidator.cs`
- `frontend/src/features/products/composables/useProducts.ts` — composable under test
- `frontend/src/features/stock/composables/useStockMovements.ts` — composable under test
- `frontend/src/features/products/components/ProductForm.vue` — component under test
- `frontend/src/features/stock/components/OutboundForm.vue` — component under test
- `frontend/src/features/stock/api.ts` + `frontend/src/features/products/api.ts` — API surfaces to mock

### External tooling references (for PDF pipeline)
- Pandoc User's Guide — `--filter`, `--template`, `--pdf-engine=weasyprint`
- WeasyPrint docs — CSS Paged Media (`@page`, `@page :first`, running headers/footers)
- mermaid-filter README — Pandoc filter that turns ` ```mermaid ` fenced blocks into inline SVG

### Reference repo for .NET test patterns
- `/home/thallysrc/Projects/BancoShu/` — When in doubt about xUnit + Moq + FluentValidation TestValidator usage in a Dapper/.NET 8 codebase, mirror BancoShu's `BancoShu.Tests/` structure. Same exception: BancoShu's N+1 in `TransferService.GetHistoryAsync` is anti-pattern — irrelevant to this phase.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `backend/Inventory.Tests/Inventory.Tests.csproj` — Test project already configured (xUnit 2.9 + Moq 4.20 + project ref to Inventory). No new test project needed.
- `backend/Inventory.Tests/Validators/CreateProductRequestValidatorTests.cs` — Existing Phase 2 validator test demonstrates the FluentValidation TestHelper pattern to mirror for `CreateMovementRequestValidator`.
- `backend/Inventory/Exceptions/` — All 9 typed `DomainException` subclasses already expose `ErrorCode`, `Hint`, `Message` — tests assert against these constructors as-is, no production code changes.
- Phase 3's `03-VERIFICATION.md` documents every errorCode path with curl evidence — tests can mirror those scenarios 1:1.

### Established Patterns
- **Backend identifier language:** EN code, PT-BR error messages (already enforced). Tests assert PT-BR message substrings (e.g., `ex.Message.Contains("saldo insuficiente")`).
- **Dapper repository contracts:** Pure interfaces (`IProductRepository`, `IStockMovementRepository`) make Moq trivial — no `IDbConnection` or `IDbTransaction` plumbing leaks into services beyond what Moq can handle.
- **`StockMovementService.CreateAsync` transaction wrapper:** Uses `IDbConnection`/`IDbTransaction` directly. Test strategy: extract test seams via Moq of the repository contract (`IStockMovementRepository.CreateAsync(StockMovement, IDbTransaction?, ...)`), do NOT mock IDbConnection — assert the service calls repository methods in the expected order with the expected arguments.
- **Frontend Axios interceptor (`api.ts`):** Single chokepoint normalizes errors to `ApiError`. Composable tests mock the api module so interceptor behavior is implicit (not under test in Phase 4).

### Integration Points
- `package.json` (frontend) currently has no test infra → Wave 1 frontend tests plan must add devDeps + script + config.
- `docker-compose.yml` is untouched by this phase — tests run on host, not in containers.
- Root `README.md` was last extended in Phase 3's `5d45821` commit (Phase 3 capabilities block). Phase 4 builds on top — do not regress that block.
- No `docs/` directory exists yet — Phase 4 creates it from scratch.

</code_context>

<specifics>
## Specific Ideas

- **Test discipline mirrors PROJECT.md's "limpa, testada, organizada":** evaluator must see disciplined coverage that maps 1:1 to errorCodes, not exhaustive coverage of trivial paths.
- **PDF visual identity is part of the deliverable:** the PDFs are the brand artifact the evaluator opens. Capa, headers, palette, Inter — all visible. Worth the effort even though Pandoc + WeasyPrint setup is non-trivial.
- **errorCode catalog table in `03-business-rules.md`** is the single source of truth for the agentic-API story the evaluator should notice. Format: `| Code | Category | Hint pattern | Triggered by |`.

</specifics>

<deferred>
## Deferred Ideas

- E2E browser tests (Playwright / Cypress) — out of scope. The 6-item HUMAN-UAT covers manual browser verification.
- Backend integration tests with real Postgres (Testcontainers) — defer to a v2 milestone if test depth becomes a concern.
- GitHub Actions CI — out of scope per TEST-07 ("CI local"). Add to backlog for post-submission polish.
- Code coverage thresholds (coverlet, @vitest/coverage-v8) — out of scope; TEST-07 only requires green.
- Mermaid live-reload preview server for docs authoring — nice DX but unnecessary for one-shot doc creation.
- PDF accessibility tagging (PDF/UA) — out of scope; visual fidelity is the priority.
- Docs i18n (EN version) — out of scope; PT-BR consistent with the rest of the project.

</deferred>

---

*Phase: 04-tests-docs-polish*
*Context gathered: 2026-05-16 (auto mode)*
