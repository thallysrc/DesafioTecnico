---
phase: 04-tests-docs-polish
plan: 04
subsystem: docs
tags: [pandoc, weasyprint, mermaid, css-paged-media, markdown, pt-br]

# Dependency graph
requires:
  - phase: 02-products-vertical-slice
    provides: ProductService + exception catalog + agentic OpenAPI conventions (referenced in business rules)
  - phase: 03-stock-movements-vertical-slice
    provides: StockMovementService transaction flow + idempotency + 5 typed exceptions (referenced in 02-architecture sequence diagram + 03-business-rules catalog + canonical INSUFFICIENT_BALANCE example)
provides:
  - docs/ directory + README index
  - 01-product-decisions.md (12 D{N} estratégicas + roadmap v2)
  - 02-architecture.md (5 Mermaid diagrams + stack table + run/test instructions)
  - 03-business-rules.md (entities + enums + 12 regras + 9-row errorCode catalog + canonical ErrorResponse + idempotency flow)
  - docs/assets/pdf-style.css (CSS Paged Media for WeasyPrint, brand palette, Inter, @page rules, cover gradient)
  - docs/assets/pandoc-template.html (HTML5 scaffold with $title$ / $body$ / <header class="cover">)
  - YAML front matter (title + date + lang) on 01/02/03 source markdowns for cover-page consumption
affects: [04-05 (PDF pipeline reads these sources), 04-06 (root README polish links to docs/dist/)]

# Tech tracking
tech-stack:
  added:
    - "Mermaid v10 syntax (sequenceDiagram + flowchart + erDiagram) — fenced ```mermaid blocks; rendered to inline SVG by mermaid-filter in Plan 04-05"
    - "Pandoc HTML5 template variables ($title$, $lang$, $date$, $body$, $for(css)$...$endfor$) — read from YAML front matter"
    - "CSS Paged Media (@page, @page :first, @top-left / @top-right / @bottom-center, string-set / string()) — WeasyPrint-flavored CSS"
  patterns:
    - "Single source of truth for errorCode catalog: docs/03-business-rules.md mirrors backend/Inventory/Exceptions/*.cs exactly (9 codes — vocabulary fechado, no invention)"
    - "PT-BR prose + EN identifiers (D-12 / D7): section headings in PT-BR, code names + slugs + class names in EN"
    - "Mermaid diagrams: ≤15 nodes each (D-14), fits 1 PDF page after SVG rendering"
    - "Cover-page-via-CSS (D-19): minimal HTML template injects <header class='cover'>, all visual identity lives in pdf-style.css @page :first rule"

key-files:
  created:
    - docs/README.md
    - docs/01-product-decisions.md
    - docs/02-architecture.md
    - docs/03-business-rules.md
    - docs/assets/pdf-style.css
    - docs/assets/pandoc-template.html
  modified: []

key-decisions:
  - "Cover page driven by CSS, not HTML (D-19 honored verbatim): pandoc-template.html keeps only the structural <header class='cover'> block; all colors, gradients, typography, and pagination overrides live in pdf-style.css @page :first — keeps HTML template trivially small and editable"
  - "Inter via Google Fonts CDN (not @font-face bundled) — simpler reproducibility; the regeneration script in Plan 04-05 will fetch fonts on first run; PDFs are committed, so the evaluator never executes the fetch"
  - "Canonical ErrorResponse example uses real production data from Phase 3 verification (P3-SMOKE-001, requested 999999, available 55, deficit 999944) instead of a synthetic example — agentic-friendly story is grounded in evidence"
  - "Idempotency flow documented as 4 explicit paths (fast-path, slow-path, race recovery, byte-identical guarantee) instead of a generic 'idempotent' bullet — cross-references Plan 03-03 SUMMARY's 'Two Paths, Same Output' section by name"

patterns-established:
  - "errorCode catalog as table: each row carries HTTP status + Category + Exception class + Hint pattern (dynamic) + Triggering condition — the contract LLMs and humans both consume"
  - "Diagram-as-source: 5 Mermaid diagrams in 02-architecture.md are the canonical visualization; future phases reference them by section name"
  - "YAML front matter (title + date + lang) on every doc markdown — consumed by Pandoc template variables; cover page is generated automatically without per-doc template tweaks"

requirements-completed: [DOC-01, DOC-02, DOC-03, DOC-04, DOC-05, DOC-06]

# Metrics
duration: ~20min
completed: 2026-05-16
---

# Phase 4 Plan 4: docs/ Markdown Sources + Assets Summary

**Three PT-BR markdowns (decisions, architecture with 5 Mermaid diagrams, business rules with 9-row errorCode catalog) plus the branded CSS Paged Media stylesheet and the minimal Pandoc HTML template — the authoring half of the docs pipeline; PDF generation lands in Plan 04-05.**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-05-16T20:05Z
- **Completed:** 2026-05-16T20:26Z
- **Tasks:** 3 (all autonomous, no checkpoints)
- **Files created:** 6
- **Files modified:** 0 (no production code touched)

## Accomplishments

- `docs/` directory created with the locked file tree from CONTEXT.md D-11 (`README.md` + 3 markdown sources + `assets/` with stylesheet + template)
- 5 Mermaid diagrams authored in `02-architecture.md` (System Context, Backend Layered, Sequence Outbound, ER, Frontend Feature Flow) — each ≤15 nodes per D-14
- Full 9-row `errorCode` catalog table in `03-business-rules.md` — every slug pulled verbatim from `backend/Inventory/Exceptions/*.cs` constructors (no invention)
- Brand identity captured in `pdf-style.css`: paleta `brand-50..900` (#1863DC primary), Inter via Google Fonts, capa cheia via `@page :first` + linear gradient, running header (`StockEasy` left + doc title right via `string-set: doctitle content()`), footer paginação `{page} / {total}`
- Pandoc HTML5 template scaffold ready: `$title$`, `$lang$`, `$date$`, `$body$`, `$for(css)$` template variables wired; `<header class="cover">` block consumed by `@page :first` CSS rule
- YAML front matter (title + date + lang) prepended to all 3 source markdowns — cover page renders automatically when Plan 04-05 runs Pandoc

## File Tree Under docs/

```
docs/
├── README.md                          (53 lines)
├── 01-product-decisions.md            (125 lines)
├── 02-architecture.md                 (191 lines)
├── 03-business-rules.md               (150 lines)
└── assets/
    ├── pdf-style.css                  (266 lines)
    └── pandoc-template.html           (19 lines)
```

Total: **6 files, 804 lines**.

No `docs/dist/` artifacts yet (committed PDFs come from Plan 04-05). No `generate-pdfs.sh` yet (also Plan 04-05).

## Task Commits

Each task was committed atomically with `--no-verify` (parallel agent flag):

1. **Task 1: README + 01-product-decisions.md** — `ef1e16c` (docs)
2. **Task 2: 02-architecture.md + pdf-style.css + pandoc-template.html** — `2a5c16f` (docs)
3. **Task 3: 03-business-rules.md + YAML front matter on 01/02** — `2add943` (docs)

## Files Created/Modified

### Created

- `docs/README.md` — index document with toolchain install commands (Ubuntu/Debian + macOS variants of `pandoc`, `weasyprint`, `mermaid-filter`) and pointers to `dist/` PDFs (the script `generate-pdfs.sh` is forward-referenced for Plan 04-05)
- `docs/01-product-decisions.md` — 12 strategic decisions (D3, D5, D6, D7, D9, D10, D18, D21, D24, D25, D27, D30) extracted from PROJECT.md Key Decisions table with expanded rationale; trade-offs section with 7 bullets; full v2 roadmap (CHAT-01..05); evaluator-facing sumário
- `docs/02-architecture.md` — Stack table (8 rows) + 5 Mermaid diagrams + Como Executar (mirrors root README run instructions) + Testes (xUnit + Vitest one-liners)
- `docs/03-business-rules.md` — Entity tables (Product + StockMovement); enum tables (ProductType + MovementType with PT-BR labels from `frontend/src/shared/labels.ts`); 12 regras enforced bullets; full 9-row `errorCode` catalog; canonical INSUFFICIENT_BALANCE ErrorResponse from Phase 3 verification (real production data); 4-step idempotency flow (fast-path + slow-path + race recovery + byte-identical guarantee)
- `docs/assets/pdf-style.css` — CSS Paged Media for WeasyPrint: imports Inter from Google Fonts; defines palette tokens (`--brand-50..900`, `--ink`, `--muted`, `--line`, `--surface`, `--success`, `--warning`, `--danger`); base typography (Inter 11pt body, brand-colored h1/h2/h3); code blocks with brand-500 left border; tables with brand-50 header; Mermaid SVG `page-break-inside: avoid`; `@page` running header/footer; `@page :first` cover-page override; `header.cover` full-bleed gradient block with wordmark
- `docs/assets/pandoc-template.html` — 19-line minimal HTML5 template; injects `<header class="cover">` with `Stock<span class="accent">Easy</span>` wordmark + `$title$` + `$date$` meta line; passes through `$body$` unchanged

### Modified

None — no production code (backend/Inventory or frontend/src) touched in this plan.

## Decisions Made

- **Cover page driven by CSS, not HTML** (D-19 honored verbatim): the Pandoc HTML template stays trivial (19 lines); all visual identity (gradient, wordmark sizing, meta line, padding) lives in `pdf-style.css` under `header.cover { ... }` and `@page :first { ... }`. Keeps the HTML template editable without touching styling.
- **Inter via Google Fonts CDN, not @font-face bundling**: simpler reproducibility on the regeneration host (Plan 04-05); PDFs are committed (D-31), so the evaluator never executes the font fetch.
- **Canonical ErrorResponse example uses real Phase 3 verification data** (`P3-SMOKE-001` / requested 999999 / available 55 / deficit 999944) instead of a synthetic example — the agentic-friendly story is grounded in actual evidence.
- **Idempotency flow documented as 4 explicit paths** (fast-path before transaction, slow-path with `SELECT ... FOR UPDATE`, race-window recovery via Postgres `SqlState 23505`, byte-identical body guarantee) — cross-references `03-03-SUMMARY.md` §"Idempotency Replay — Two Paths, Same Output" by file name so the evaluator (or a future agent) can drill down.

## Deviations from Plan

None — plan executed exactly as written. All `<done>` criteria and the `<verification>` automated checks passed on first run for each task.

## Issues Encountered

- **Worktree base state**: the soft reset against the orchestrator base (`2677b42`) initially showed all files as deleted because the worktree had not been checked out. Resolved with `git checkout HEAD -- .` to materialize the working tree before authoring. No content was lost; no production code touched.

## User Setup Required

None — no external service configuration required. The docs assets in this plan are pure sources; the toolchain install (`pandoc` + `weasyprint` + `mermaid-filter`) is needed only to regenerate PDFs, which Plan 04-05 handles. The evaluator never installs anything.

## Verification (mirrors PLAN <verification>)

- ✅ All 6 files exist under `docs/` (README + 3 markdown sources + 2 assets)
- ✅ Exactly **5** ```mermaid``` fenced blocks in `02-architecture.md` (`grep -c '^\`\`\`mermaid' docs/02-architecture.md` → `5`)
- ✅ All **9** errorCode slugs present in `03-business-rules.md` catalog (`VALIDATION_ERROR`, `MISSING_IDEMPOTENCY_KEY`, `PRODUCT_NOT_FOUND`, `MOVEMENT_NOT_FOUND`, `DUPLICATE_CODE`, `PRODUCT_DELETED`, `INSUFFICIENT_BALANCE`, `INVALID_MOVEMENT_VALUES`, `INTERNAL_ERROR`)
- ✅ YAML front matter (`title` + `date` + `lang`) prepended to `01-product-decisions.md`, `02-architecture.md`, `03-business-rules.md` (head -5 of each starts with `---`)
- ✅ `pdf-style.css` has `@page`, `@page :first`, `--brand-500: #1863DC`, Inter import, running header/footer rules
- ✅ `pandoc-template.html` has `<header class="cover">`, `$title$`, `$body$` template variables

## Next Phase Readiness

Plan 04-05 (PDF pipeline) consumes everything authored here:

- Read source markdowns: `docs/01-product-decisions.md`, `docs/02-architecture.md`, `docs/03-business-rules.md` (with YAML front matter)
- Apply template: `docs/assets/pandoc-template.html`
- Apply stylesheet: `docs/assets/pdf-style.css`
- Apply filter: `mermaid-filter` (converts ```mermaid``` blocks to inline SVG)
- PDF engine: WeasyPrint (consumes the CSS Paged Media rules)
- Output: `docs/dist/01-product-decisions.pdf`, `02-architecture.pdf`, `03-business-rules.pdf` (committed per D-31)

No blockers. Source authoring contract is complete and PDF generation can begin in parallel with Plans 04-01/02/03 (test tracks) per CONTEXT.md D-21 (tests and docs are independent).

## Self-Check

Verified manually with:

```bash
find docs -type f | sort
# docs/01-product-decisions.md
# docs/02-architecture.md
# docs/03-business-rules.md
# docs/README.md
# docs/assets/pandoc-template.html
# docs/assets/pdf-style.css

git log --oneline | head -3
# 2add943 docs(04-04): add 03-business-rules.md + YAML front matter for cover page
# 2a5c16f docs(04-04): add 02-architecture.md + pdf-style.css + pandoc-template.html
# ef1e16c docs(04-04): add docs README index + 01-product-decisions.md
```

All 6 files present. All 3 task commits present.

## Self-Check: PASSED

---
*Phase: 04-tests-docs-polish*
*Plan: 04-04*
*Completed: 2026-05-16*
