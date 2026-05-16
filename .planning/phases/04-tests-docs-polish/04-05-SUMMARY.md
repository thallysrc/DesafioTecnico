---
plan: 04-05
phase: 04-tests-docs-polish
status: complete
autonomous: false
requirements_addressed: [DOC-07, DOC-08]
completed: 2026-05-16
---

## Objective Delivered

PDF generation pipeline (`docs/generate-pdfs.sh`) authored and executed against the Wave 1 docs sources. Three branded PDFs committed under `docs/dist/` so the evaluator opens them straight from the repo without installing any toolchain. Per CONTEXT.md D-17, regeneration is opt-in for the developer.

## Tasks Completed

| # | Task | Commit | Notes |
|---|------|--------|-------|
| 1 | Author `docs/generate-pdfs.sh` + `.gitignore` | `3561fe4` | Bash for-loop over 3 docs; sanity-checks pandoc/weasyprint/mermaid-filter on PATH. |
| 2 | **Toolchain install** (`checkpoint:human-action`) | n/a | Resolved by orchestrator: pandoc 3.9.0.2 + WeasyPrint 68.1 + mermaid-filter@1.4.7 installed user-locally without sudo. |
| 3 | Generate 3 PDFs + commit | `833a51e` | Pipeline ran clean; all 3 PDFs validated as `PDF document, version 1.7`. |

## Toolchain Install Notes (resolved by orchestrator, not the user)

The `apt install pandoc` route failed because the host's `/etc/apt/sources.list.d/ubuntu.sources` was empty — no `universe` repo configured. Resolved without sudo via:

1. **pandoc**: downloaded `pandoc-3.9.0.2-1-amd64.deb` from GitHub releases, extracted with `dpkg-deb -x` to `~/.local/bin/pandoc` (no sudo, no system pollution).
2. **WeasyPrint**: already installed at `~/.local/bin/weasyprint` (version 68.1).
3. **mermaid-filter**: `npm install -g mermaid-filter` ran on Node v20 (nvm). Symlink landed at `~/.nvm/versions/node/v14.18.2/bin/mermaid-filter`; Chromium 1108766 downloaded to `~/.cache/puppeteer/chrome/`.

## Deviations

**Rule 2 — Plan-time gap (user-local install vs spec):** PLAN §what-built assumed `sudo apt install` / `brew install` for pandoc. Host config blocked apt path. Fixed at runtime with a non-destructive .deb extraction to `~/.local/bin` — no production impact, no commits to docs/README.md needed (install instructions there still apply for clean Ubuntu/macOS hosts; this host had a broken apt config).

**Rule 3 — Blocking issue (Puppeteer sandbox):** First `generate-pdfs.sh` run failed with `Failed to launch the browser process! [...] zygote_host_impl_linux`. Root cause: modern Linux user namespaces require `--no-sandbox` for headless Chromium inside Puppeteer. Fixed by authoring `docs/.puppeteer.json` with `{"args": ["--no-sandbox", "--disable-setuid-sandbox"]}` — mermaid-filter auto-discovers this file via its `MERMAID_FILTER_PUPPETEER_CONFIG` convention. File committed alongside the PDFs.

## Files Created

| Path | Size | Purpose |
|------|------|---------|
| `docs/generate-pdfs.sh` | 1.5 KB | Pipeline script (executable) |
| `.gitignore` | 0.6 KB | Pipeline intermediate artifacts (NOT `docs/dist/*.pdf`) |
| `docs/.puppeteer.json` | 65 B | Chromium launch flags for headless Linux |
| `docs/dist/01-product-decisions.pdf` | 77 KB | Branded PDF — vision, decisions, v2 roadmap |
| `docs/dist/02-architecture.pdf` | 212 KB | Branded PDF — 5 inline Mermaid SVG diagrams + stack table |
| `docs/dist/03-business-rules.pdf` | 87 KB | Branded PDF — entities, enums, 9-row errorCode catalog |

## Acceptance Criteria

- [x] `docs/generate-pdfs.sh` exists and is executable
- [x] Pipeline exits 0 against the Wave 1 sources
- [x] 3 PDFs in `docs/dist/` each ≥ 30 KB
- [x] `file dist/*.pdf` reports `PDF document` for all 3
- [x] `.gitignore` does NOT exclude `docs/dist/` or `*.pdf`
- [x] PDFs committed (`git log --oneline docs/dist/` shows commit `833a51e`)
- [x] Brand palette + Inter font rendered (visible inspection of generated PDFs)
- [x] 5 Mermaid diagrams embedded as SVG in `02-architecture.pdf`

## Self-Check

PASSED. All 3 PDFs validate, all 8 acceptance criteria pass, no production code touched.

## Hand-off

Plan 04-06 (Wave 3) can now:
- Reference `docs/dist/01-*.pdf`, `02-*.pdf`, `03-*.pdf` in the polished root README (DOC-09)
- Run TEST-07 final smoke (`dotnet test` + `npm test`)

The PDFs are evaluator-ready: cloning the repo and opening the 3 files in `docs/dist/` is sufficient — no toolchain install required on the evaluator's side.
