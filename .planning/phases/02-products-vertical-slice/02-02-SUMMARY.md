---
phase: 02-products-vertical-slice
plan: 02
subsystem: backend-cross-cutting
tags: [exceptions, middleware, error-handling, agentic, fluentvalidation]
dependency_graph:
  requires:
    - backend/Inventory/Dtos/ErrorResponse.cs (canonical 9-field shape from Phase 1)
    - backend/Inventory/Program.cs (FluentValidation auto-validation, JsonStringEnumConverter, Swashbuckle.Annotations from Phase 1)
    - backend/Inventory/Middleware/ExceptionHandlingMiddleware.cs (Phase 1 INTERNAL_ERROR shell)
  provides:
    - "Inventory.Api.Exceptions.DomainException — abstract base with 5 canonical fields (ErrorCode, Category, Hint, Retryable, Details)"
    - "Inventory.Api.Exceptions.NotFoundException — concrete category=NOT_FOUND (HTTP 404)"
    - "Inventory.Api.Exceptions.BusinessRuleException — concrete category=BUSINESS_RULE (HTTP 422)"
    - "Inventory.Api.Exceptions.DuplicateCodeException — typed BusinessRule with dynamic hint (D-10)"
    - "Inventory.Api.Exceptions.ProductNotFoundException — typed NotFound with locked UI-SPEC hint"
    - "ExceptionHandlingMiddleware mapping: ValidationException → 400, DomainException → status from category, fallback → 500"
  affects:
    - "Plan 02-03 (validators) — FluentValidation failures auto-flow through the middleware mapping"
    - "Plan 02-04 (ProductService) — consumes typed constructors verbatim (throw new DuplicateCodeException(dto.Code))"
    - "Plan 02-06 (smoke) — byte-shape verifies the four errorCodes via curl"
    - "Phase 3 movement exceptions — extend BusinessRuleException + NotFoundException by the same pattern, no middleware changes"
tech_stack:
  added: []
  patterns:
    - "Closed errorCode catalog (SCREAMING_SNAKE_CASE), 4 codes this phase: VALIDATION_ERROR, PRODUCT_NOT_FOUND, DUPLICATE_CODE, INTERNAL_ERROR"
    - "Closed category vocabulary: VALIDATION, BUSINESS_RULE, NOT_FOUND, INTERNAL"
    - "Typed exception constructors build the hint string internally (D-10) — no string.Format at callsite"
    - "Switch-expression middleware: ValidationException → DomainException → fallback Exception"
    - "FluentValidation PropertyName lower-camelCased before going into details.fields[].field (frontend Zod expects camelCase)"
key_files:
  created:
    - backend/Inventory/Exceptions/DomainException.cs
    - backend/Inventory/Exceptions/NotFoundException.cs
    - backend/Inventory/Exceptions/BusinessRuleException.cs
    - backend/Inventory/Exceptions/DuplicateCodeException.cs
    - backend/Inventory/Exceptions/ProductNotFoundException.cs
  modified:
    - backend/Inventory/Middleware/ExceptionHandlingMiddleware.cs
decisions:
  - "Concrete (not abstract) NotFoundException + BusinessRuleException so callers can throw inline without authoring a typed subclass for low-value cases — convention is 'use the typed subclass when one exists' (D-10)"
  - "ProductNotFoundException carries a fixed hint (productId only flows through details) because the locked UI-SPEC string at line 538 deliberately omits the id — keeps the user-facing toast clean"
  - "DuplicateCodeException accepts optional existingProductId — default path emits hint with /api/products?includeDeleted=true; when service does the extra SELECT, hint links to /api/products/{id} directly"
  - "Manual ToCamelCase on FluentValidation PropertyName — JsonNamingPolicy.CamelCase doesn't touch string values inside object? Details"
  - "VALIDATION_ERROR is retryable=true (correcting fields and resubmitting works); DomainException subclasses default retryable=false (terminal state until input changes)"
metrics:
  duration: "~10 min"
  completed: 2026-05-16
  tasks_completed: 3
  files_created: 5
  files_modified: 1
  warnings: 0
  errors: 0
---

# Phase 2 Plan 02: Cross-Cutting Exception Flow Summary

**One-liner:** Closed-vocabulary exception hierarchy (`DomainException` → `NotFoundException` + `BusinessRuleException` → typed `ProductNotFoundException` + `DuplicateCodeException`) wired into `ExceptionHandlingMiddleware` switch-expression for the canonical 9-field `ErrorResponse` envelope.

## What Shipped

Three commits, six files (5 created + 1 modified), all on `worktree-agent-a2c512ecf5b57f626` branched off `e6ead96`:

| Task | Commit | Files |
|------|--------|-------|
| 1 — DomainException + NotFoundException + BusinessRuleException | `f6b87cd` | `Exceptions/DomainException.cs`, `NotFoundException.cs`, `BusinessRuleException.cs` |
| 2 — DuplicateCodeException + ProductNotFoundException | `b272697` | `Exceptions/DuplicateCodeException.cs`, `ProductNotFoundException.cs` |
| 3 — ExceptionHandlingMiddleware extension | `82d5489` | `Middleware/ExceptionHandlingMiddleware.cs` |

## Exception Inheritance Graph

```
System.Exception
  └─ DomainException (abstract)
       ├─ NotFoundException        (Category="NOT_FOUND",     HTTP 404, Retryable=false)
       │    └─ ProductNotFoundException  (ErrorCode="PRODUCT_NOT_FOUND")
       └─ BusinessRuleException    (Category="BUSINESS_RULE", HTTP 422, Retryable=false by default)
            └─ DuplicateCodeException     (ErrorCode="DUPLICATE_CODE")
```

Phase 3 extends this with `MovementNotFoundException` (NotFound) and `InsufficientBalanceException`, `ProductDeletedException`, `InvalidMovementValuesException` (BusinessRule), plus a fresh `MissingIdempotencyKeyException` likely under a yet-undefined `ValidationException` category-anchored intermediate. No middleware changes will be required.

## Hint Strings Registered (Verbatim)

| Exception | Hint (PT-BR) |
|-----------|--------------|
| `DuplicateCodeException(code)` | `Já existe um produto com código '{code}'. Use outro código ou recupere o produto via /api/products?includeDeleted=true.` |
| `DuplicateCodeException(code, existingProductId)` | `Já existe um produto com código '{code}'. Use outro código ou recupere o produto deletado em /api/products/{id}.` |
| `ProductNotFoundException(productId)` | `Produto não encontrado. Atualize a lista e tente novamente.` |
| (Middleware — VALIDATION_ERROR) | `Corrija os campos listados em 'details.fields' e tente novamente` |
| (Middleware — INTERNAL_ERROR fallback) | `Tente novamente em alguns instantes. Se persistir, contate o suporte com o traceId.` |

All strings constructed inside the exception/middleware code — never inline at the service callsite. Mirrors UI-SPEC §"Toast error messages" line 532-540.

## Middleware Switch Coverage Table

| Input | HTTP | errorCode | category | retryable | Notes |
|-------|------|-----------|----------|-----------|-------|
| `FluentValidation.ValidationException` | 400 | `VALIDATION_ERROR` | `VALIDATION` | `true` | `details.fields[]` with `field` (camelCase), `message`, `rejectedValue` |
| `DomainException` where `Category="NOT_FOUND"` | 404 | from `.ErrorCode` | `NOT_FOUND` | from `.Retryable` | covers `ProductNotFoundException` + any future `*NotFoundException` |
| `DomainException` where `Category="BUSINESS_RULE"` | 422 | from `.ErrorCode` | `BUSINESS_RULE` | from `.Retryable` | covers `DuplicateCodeException` + Phase 3 movement rules |
| `DomainException` where `Category="VALIDATION"` | 400 | from `.ErrorCode` | `VALIDATION` | from `.Retryable` | reserved for Phase 3 `MISSING_IDEMPOTENCY_KEY` |
| `DomainException` (any other category) | 500 | from `.ErrorCode` | from `.Category` | from `.Retryable` | defensive — shouldn't occur with the closed vocabulary |
| Any other `Exception` | 500 | `INTERNAL_ERROR` | `INTERNAL` | `false` | Phase 1 behavior preserved |

`TraceId` = `HttpContext.TraceIdentifier`; `Timestamp` = `DateTime.UtcNow.ToString("O")`. JSON serialized via shared `JsonOptions` with `PropertyNamingPolicy = JsonNamingPolicy.CamelCase`.

## Confirmation: Phase 1 Wiring Untouched

Grep results inside the worktree:

- `grep -q "AddFluentValidationAutoValidation\|AddFluentValidation" backend/Inventory/Program.cs` → **PASS** (BACK-15 carry-forward intact, line 44–45 of Program.cs)
- `JsonStringEnumConverter` still registered in `Program.cs` line 36
- `Swashbuckle.AspNetCore.Annotations` still enabled via `c.EnableAnnotations()` line 69
- `app.UseMiddleware<ExceptionHandlingMiddleware>()` still first in the pipeline line 85
- `ErrorResponse` record unchanged (still the 9-field canonical shape from Phase 1)

No `Program.cs` edits were necessary. No `Inventory.csproj` package additions were necessary (`FluentValidation.AspNetCore` 11.3.1 + `Swashbuckle.AspNetCore.Annotations` 6.6.2 already referenced).

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 — Bug] Fixed XML doc warning on `BusinessRuleException`**
- **Found during:** Task 1 build verification
- **Issue:** Class-level `<summary>` referenced `<paramref name="retryable"/>` but parameters belong to the constructor, not the class — produced CS1734 warning.
- **Fix:** Rewrote sentence to use `<c>retryable</c>` inline instead of `<paramref>`.
- **File modified:** `backend/Inventory/Exceptions/BusinessRuleException.cs`
- **Commit:** rolled into `f6b87cd`

**2. [Rule 1 — Bug] Fixed forward cref on `NotFoundException`**
- **Found during:** Task 1 build verification
- **Issue:** `<see cref="ProductNotFoundException"/>` in the class summary produced CS1574 (the type doesn't exist yet at Task 1; only authored in Task 2). Even after Task 2 lands, having Task 1 produce warnings was non-tolerable per success criteria "0 warnings".
- **Fix:** Replaced `<see cref="ProductNotFoundException"/>` with `<c>ProductNotFoundException</c>` (string mention).
- **File modified:** `backend/Inventory/Exceptions/NotFoundException.cs`
- **Commit:** rolled into `f6b87cd`

Otherwise: plan executed exactly as written.

## Verification Results

- Final build: `dotnet build Inventory.sln` → **Build succeeded. 0 Warning(s) 0 Error(s)** (Docker `mcr.microsoft.com/dotnet/sdk:8.0`)
- Test run: `dotnet test Inventory.sln --no-build` → **Passed! Failed: 0, Passed: 1 (SmokeTests.TestProject_Should_BuildAndRun)**
- Acceptance grep checks: all 25 grep assertions across the three tasks pass.
- BACK-15 carry-forward grep on `Program.cs`: pass.

## Self-Check: PASSED

Files verified to exist:
- `backend/Inventory/Exceptions/DomainException.cs` — FOUND
- `backend/Inventory/Exceptions/NotFoundException.cs` — FOUND
- `backend/Inventory/Exceptions/BusinessRuleException.cs` — FOUND
- `backend/Inventory/Exceptions/DuplicateCodeException.cs` — FOUND
- `backend/Inventory/Exceptions/ProductNotFoundException.cs` — FOUND
- `backend/Inventory/Middleware/ExceptionHandlingMiddleware.cs` — FOUND (modified)

Commits verified in `git log`:
- `f6b87cd` — FOUND
- `b272697` — FOUND
- `82d5489` — FOUND

No stubs created. No new untracked files outside the planned set. No threat flags (the middleware narrows error surface — does not expose stack traces; INTERNAL_ERROR body omits `details`).

## Notes for Downstream Plans

- **02-03 (Validators):** FluentValidation failures bubble to `ValidationException` automatically — no manual try/catch in controllers. Validator messages must mirror frontend Zod **exactly** per D-11.
- **02-04 (ProductService):** Write `throw new DuplicateCodeException(dto.Code)` and `throw new ProductNotFoundException(id)`. Optionally pass `existingProductId` to the duplicate exception after a follow-up `SELECT id FROM products WHERE code = @code` if the service decides the extra round-trip is worth a richer hint.
- **02-06 (Smoke):** Hit `POST /api/products` with empty body → expect `400` + `errorCode: "VALIDATION_ERROR"` + `details.fields[0].field == "code"` (camelCase). Hit duplicate-code path → `422` + `errorCode: "DUPLICATE_CODE"` + dynamic hint. Hit unknown id GET/DELETE → `404` + `errorCode: "PRODUCT_NOT_FOUND"`.
- **Phase 3:** Append `INSUFFICIENT_BALANCE`, `PRODUCT_DELETED`, `MOVEMENT_NOT_FOUND`, `MISSING_IDEMPOTENCY_KEY`, `INVALID_MOVEMENT_VALUES` as typed subclasses. Middleware needs zero changes.
