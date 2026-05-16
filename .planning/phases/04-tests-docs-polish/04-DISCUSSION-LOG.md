# Phase 4: Tests, Docs & Polish - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in 04-CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-16
**Phase:** 04-tests-docs-polish
**Mode:** auto (--auto flag)
**Areas auto-discussed:** Backend test strategy, Frontend test strategy, Docs structure, Mermaid diagrams, PDF pipeline, Root README, Track parallelization, Anti-scope

---

## Backend test strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Pure Moq of repositories | Fast, deterministic, no Docker needed by evaluator | ✓ |
| Testcontainers + real Postgres | Catches schema/SQL drift but adds Docker dependency to `dotnet test` | |
| Hybrid (Moq for services, real PG for repos) | Best of both but adds 2 test projects + Docker | |

**Auto-selected:** Pure Moq.
**Rationale:** Evaluator runs `dotnet test` standalone. Phase 3's `03-VERIFICATION.md` already proved SQL correctness against real Postgres.

---

## Mock pattern for transactional service

| Option | Description | Selected |
|--------|-------------|----------|
| Mock `IStockMovementRepository` only | Test seam matches production architecture, assert call order with `MockSequence` | ✓ |
| Mock `IDbConnection` + `IDbTransaction` directly | Too invasive, leaks Dapper plumbing into tests | |

**Auto-selected:** Mock repository contract.

---

## Validator test pattern

| Option | Description | Selected |
|--------|-------------|----------|
| FluentValidation TestHelper (`ShouldHaveValidationErrorFor`) | Idiomatic, declarative, used by `CreateProductRequestValidatorTests.cs` (Phase 2) | ✓ |
| Direct `validator.Validate(...)` + manual assertions | Verbose, no error-message granularity | |

**Auto-selected:** TestHelper pattern. Reuses existing Phase 2 idiom.

---

## Frontend test framework setup

| Option | Description | Selected |
|--------|-------------|----------|
| Vitest + @vue/test-utils + happy-dom | Vite-native, fast, lightweight | ✓ |
| Vitest + @vue/test-utils + jsdom | Slower, but heavier DOM emulation (we don't need this) | |
| Jest + @vue/test-utils + jsdom | Conflicts with Vite tooling | |
| Cypress component testing | Slow, browser-bound, scope creep | |

**Auto-selected:** Vitest + happy-dom.

---

## Component mount depth

| Option | Description | Selected |
|--------|-------------|----------|
| Shallow mount with primitive stubs | Tests form logic, not primitive rendering (primitives proven in Phase 2) | ✓ |
| Full mount | Slower, duplicates primitive coverage | |

**Auto-selected:** Shallow mount.

---

## Coverage tooling

| Option | Description | Selected |
|--------|-------------|----------|
| No coverage thresholds | TEST-07 only requires green | ✓ |
| coverlet (backend) + @vitest/coverage-v8 (frontend) | Adds dep + config burden, no spec requirement | |

**Auto-selected:** No coverage tooling.

---

## CI workflow

| Option | Description | Selected |
|--------|-------------|----------|
| No GitHub Actions | TEST-07 says "CI local" | ✓ |
| Add `.github/workflows/test.yml` | Scope creep | |

**Auto-selected:** No CI workflow.

---

## Docs structure

| Option | Description | Selected |
|--------|-------------|----------|
| Locked layout per DOC-01..08 | 3 markdown sources + assets/ + dist/ + generate-pdfs.sh | ✓ |
| Single combined markdown | Violates DOC-02..04 (3 separate docs required) | |

**Auto-selected:** Spec-locked layout.

---

## Mermaid diagram complexity

| Option | Description | Selected |
|--------|-------------|----------|
| ≤15 nodes each, 1 page per diagram | Readable on PDF | ✓ |
| Multi-page detailed diagrams | Overcrowded, hard to read | |

**Auto-selected:** ≤15 nodes constraint.

---

## PDF pipeline orchestration

| Option | Description | Selected |
|--------|-------------|----------|
| Single `docs/generate-pdfs.sh` loops 3 docs | Per DOC-07 spec, simpler | ✓ |
| Per-doc scripts | Verbose | |
| Pandoc Makefile | Overkill for 3 docs | |

**Auto-selected:** Single loop script.

---

## Pandoc environment

| Option | Description | Selected |
|--------|-------------|----------|
| Host-installed Pandoc + WeasyPrint + mermaid-filter | Per spec; install instructions in `docs/README.md` | ✓ |
| Docker-based generation | Extra moving piece; evaluator never runs it anyway | |

**Auto-selected:** Host install. PDFs are committed; evaluator doesn't regenerate.

---

## Root README strategy

| Option | Description | Selected |
|--------|-------------|----------|
| Extend existing README | Already polished by Phase 1/3; preserve challenge spec link | ✓ |
| Replace from scratch | Loses earlier polish + challenge spec anchor | |

**Auto-selected:** Extend existing README.

---

## Track parallelization

| Option | Description | Selected |
|--------|-------------|----------|
| Tests + Docs as parallel waves | ROADMAP confirms independence; timebox-friendly | ✓ |
| Sequential | Slower, no benefit | |

**Auto-selected:** Parallel waves.

---

## Claude's Discretion

Auto-deferred to Claude during planning:
- pdf-style.css typography scale (font sizes, line-height)
- Mermaid theme tokens (within brand palette)
- Test file directory structure inside `Inventory.Tests/`
- Vitest globals mode vs explicit imports
- Inter webfont bundling vs Google Fonts CDN

## Deferred Ideas

- E2E browser tests (Playwright/Cypress) — covered by HUMAN-UAT
- Testcontainers integration tests — v2 milestone
- GitHub Actions CI — out of scope per TEST-07
- Coverage thresholds — out of scope per TEST-07
- Mermaid live-reload preview — DX only
- PDF/UA accessibility tagging — visual fidelity is priority
- Docs EN i18n — PT-BR consistent with project
