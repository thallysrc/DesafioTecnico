---
phase: 01-foundation
plan: 02
subsystem: backend
tags: [scaffold, dotnet8, dapper, swagger, cors, fluentvalidation, health, middleware]
requirements_completed: [BACK-01, BACK-02, BACK-03, BACK-04, INFRA-04, INFRA-05, INFRA-06, INFRA-07]
dependency_graph:
  requires: []
  provides:
    - Inventory.sln + Inventory/ (API) + Inventory.Tests/ projects building cleanly under .NET 8
    - Composition root in Program.cs (CORS, JSON enum-as-string, FluentValidation, Swagger w/ XML + annotations, ExceptionHandlingMiddleware, IDbConnectionFactory singleton)
    - GET /api/health endpoint pinging Postgres via Dapper SELECT 1 with graceful DB-down handling
    - ErrorResponse + HealthResponse DTOs
    - ExceptionHandlingMiddleware shell (Phase 2 extends with DomainException + ValidationException branches)
    - PagedResult<T> envelope (Phase 2 wires into list responses)
    - Folder skeleton (Controllers, Services, Repositories, Entities, Validators, Exceptions, Dtos, Infra, Middleware) with .gitkeep placeholders matching backend/CLAUDE.md
  affects:
    - Plan 01-01 (Dockerfile already targets `dotnet watch run`; this plan delivers the project it watches)
    - Plan 01-03 (frontend Vite proxy targets the /api routes shipped here)
    - Plan 01-04 (docker-compose end-to-end smoke depends on this plan's /api/health)
    - Phase 2 (ProductsController, ProductService, validators all plug into this composition root unchanged)
tech_stack:
  added:
    - .NET 8 SDK (Microsoft.NET.Sdk.Web)
    - Dapper 2.1.66
    - Npgsql 10.0.1
    - FluentValidation.AspNetCore 11.3.1
    - Swashbuckle.AspNetCore 6.6.2
    - Swashbuckle.AspNetCore.Annotations 6.6.2
    - xUnit 2.9.0 + xunit.runner.visualstudio 2.8.2
    - Moq 4.20.70
    - Microsoft.NET.Test.Sdk 17.10.0
  patterns:
    - File-scoped namespaces `Inventory.Api.{Folder}` enforced by `<RootNamespace>Inventory.Api</RootNamespace>` in csproj
    - Single-project N-tier (Inventory/) + test project (Inventory.Tests/) — NOT Clean Architecture
    - XML doc generation (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`) with `NoWarn 1591` (suppress missing-doc warnings on remaining template surfaces)
    - `IDbConnectionFactory` singleton (returns fresh `NpgsqlConnection` per call; caller owns disposal via `using`)
    - Middleware-first pipeline: `UseMiddleware<ExceptionHandlingMiddleware>` → `UseCors("Frontend")` → `MapControllers`
    - Health probe never throws (try/catch around `SELECT 1` so DB-down returns 200 + `db: "down"` instead of 500)
key_files:
  created:
    - backend/Inventory.sln
    - backend/Inventory/Inventory.csproj
    - backend/Inventory/Program.cs
    - backend/Inventory/appsettings.json
    - backend/Inventory/appsettings.Development.json
    - backend/Inventory/Properties/launchSettings.json
    - backend/Inventory/.gitignore
    - backend/.gitignore
    - backend/Inventory/Infra/IDbConnectionFactory.cs
    - backend/Inventory/Infra/DbConnectionFactory.cs
    - backend/Inventory/Infra/PagedResult.cs
    - backend/Inventory/Dtos/ErrorResponse.cs
    - backend/Inventory/Dtos/HealthResponse.cs
    - backend/Inventory/Middleware/ExceptionHandlingMiddleware.cs
    - backend/Inventory/Controllers/HealthController.cs
    - backend/Inventory.Tests/Inventory.Tests.csproj
    - backend/Inventory.Tests/SmokeTests.cs
    - backend/Inventory/Controllers/.gitkeep
    - backend/Inventory/Services/.gitkeep
    - backend/Inventory/Repositories/.gitkeep
    - backend/Inventory/Entities/.gitkeep
    - backend/Inventory/Dtos/.gitkeep
    - backend/Inventory/Validators/.gitkeep
    - backend/Inventory/Exceptions/.gitkeep
    - backend/Inventory/Infra/.gitkeep
    - backend/Inventory/Middleware/.gitkeep
  modified: []
decisions:
  - Package versions locked: Dapper 2.1.66, Npgsql 10.0.1, FluentValidation.AspNetCore 11.3.1, Swashbuckle 6.6.2 (+Annotations 6.6.2)
  - `RootNamespace` set to `Inventory.Api` so file-scoped `namespace Inventory.Api.{Folder}` matches the assembly-name convention without the project rename
  - Health endpoint design: always returns 200; sets `status: "degraded"` + `db: "down"` when Postgres ping fails (the API is up, only the dependency is impaired). This lets the App.vue health pill distinguish "API offline" (no response) from "API up but DB unreachable" (200 + degraded payload) without an extra status code
  - `ExceptionHandlingMiddleware` ships as a SHELL — Phase 1 only catches generic `Exception` → emits 500 + `INTERNAL_ERROR`. Phase 2 must extend it to map `FluentValidation.ValidationException` → 400 + `VALIDATION_ERROR` and `DomainException` subclasses (`NotFoundException` → 404, `BusinessRuleException` → 422) per backend/CLAUDE.md §"Middleware"
  - `public partial class Program { }` declared at end of Program.cs to unlock `WebApplicationFactory<Program>` in `Inventory.Tests` (Phase 4 integration tests)
  - CORS origins come from `Cors:AllowedOrigins` config array (falls back to `http://localhost:5173`); origin overridable via env var `Cors__AllowedOrigins__0` in future without code change
  - Pipeline order ExceptionHandling → CORS → MapControllers: middleware catches everything downstream (controllers + CORS errors + routing exceptions); CORS attaches headers before MVC writes the response
metrics:
  duration: ~15 minutes
  completed: 2026-05-16T15:34:00Z
  tasks_completed: 3
  files_created: 25
  commits: 3
---

# Phase 01 Plan 02: Backend Scaffold (.NET 8 + Dapper + Swagger) Summary

Bootstrapped the `backend/` .NET 8 solution end-to-end: `Inventory.sln` wires `Inventory/` (API) + `Inventory.Tests/` (xUnit) with the canonical package allowlist, XML doc generation, `Inventory.Api.{Folder}` file-scoped namespaces, CORS named policy `"Frontend"` allowing `http://localhost:5173`, JSON enum-as-string + FluentValidation auto-registration, Swagger with XML + annotations, `IDbConnectionFactory` singleton, an `ExceptionHandlingMiddleware` shell, and a single `GET /api/health` endpoint that pings Postgres via Dapper `SELECT 1` and returns `{ status, db, version, timestamp }` with graceful DB-down handling. Verified via Docker SDK container: clean build (0 warnings, 0 errors), smoke test passes, live `/api/health` and `/swagger/v1/swagger.json` both serve correctly.

## What Was Built

### Solution & projects (Task 1, commit `3711c0d`)
- `Inventory.sln` referencing both projects
- `Inventory/Inventory.csproj`: `<TargetFramework>net8.0</TargetFramework>`, `Nullable=enable`, `ImplicitUsings=enable`, `GenerateDocumentationFile=true`, `NoWarn=1591`, `RootNamespace=Inventory.Api`, `AssemblyName=Inventory`. Packages: Dapper, Npgsql, FluentValidation.AspNetCore, Swashbuckle.AspNetCore (+ Annotations)
- `Inventory.Tests/Inventory.Tests.csproj`: xUnit + Moq + Microsoft.NET.Test.Sdk + project reference to `Inventory` + `<Using Include="Xunit" />` (so `[Fact]` resolves under ImplicitUsings — Rule 3 deviation, see below)
- `Inventory.Tests/SmokeTests.cs`: one `[Fact]` asserting `Assert.True(true)` so `dotnet test` has something to run
- `appsettings.json` wires `ConnectionStrings:Postgres` + `Cors:AllowedOrigins: [ "http://localhost:5173" ]`
- `Properties/launchSettings.json` binds `http://localhost:8080`
- `.gitkeep` placeholders in `Controllers/`, `Services/`, `Repositories/`, `Entities/`, `Dtos/`, `Validators/`, `Exceptions/`, `Infra/`, `Middleware/` so the layout already mirrors backend/CLAUDE.md when Phase 2 starts

### Infrastructure + domain DTOs + health endpoint (Task 2, commit `4471e90`)
- `Infra/IDbConnectionFactory.cs` + `Infra/DbConnectionFactory.cs`: Npgsql-backed singleton reading `ConnectionStrings:Postgres` from `IConfiguration` (overridable via `ConnectionStrings__Postgres` env var per INFRA-06). Throws `InvalidOperationException` with a useful message if the connection string is missing
- `Infra/PagedResult.cs`: canonical `PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)` with computed `TotalPages`, `HasNext`, `HasPrev`. `TotalPages` guards against `PageSize <= 0` to avoid div-by-zero
- `Dtos/ErrorResponse.cs`: 9-field canonical record `(ErrorCode, Category, Message, Hint, StatusCode, Retryable, Details, TraceId, Timestamp)` with full XML docs on every parameter (drives the OpenAPI schema description)
- `Dtos/HealthResponse.cs`: 4-field record `(Status, Db, Version, Timestamp)`
- `Middleware/ExceptionHandlingMiddleware.cs`: Phase 1 SHELL catching generic `Exception` → 500 with `errorCode: "INTERNAL_ERROR"`, `category: "INTERNAL"`, `retryable: false`. Uses camelCase JSON serialization to match the controller pipeline. XML doc on the class explicitly tells Phase 2 to extend it
- `Controllers/HealthController.cs`: `[ApiController] [Route("api/health")]` with `[SwaggerOperation(OperationId = "getHealth", Tags = new[] { "Health" })]`. The action wraps the Dapper `ExecuteScalarAsync<int>("SELECT 1")` in a try/catch (the one place a controller is allowed try/catch — a probe must never throw) and returns 200 with `status: "degraded"` + `db: "down"` if PG is unreachable. Version comes from `AssemblyInformationalVersionAttribute` with a fallback to `GetName().Version`

### Composition root (Task 3, commit `4b1ab52`)
- `Program.cs`: explicitly-ordered wiring blocks separated by banner comments
  - CORS: named policy `"Frontend"` reading allowed origins from `Cors:AllowedOrigins` config (fallback `http://localhost:5173`)
  - MVC + JSON: `AddControllers().AddJsonOptions(...)` with `JsonStringEnumConverter` + camelCase property naming
  - FluentValidation: `AddFluentValidationAutoValidation()` + `AddValidatorsFromAssemblyContaining<Program>()` (zero validators ship in P1; Phase 2 validators get picked up automatically)
  - Swagger: `OpenApiInfo` with rich description, `IncludeXmlComments(includeControllerXmlComments: true)` from `bin/<config>/<tfm>/Inventory.xml`, `EnableAnnotations()` for `[SwaggerOperation]` attribute discovery
  - DI: `AddSingleton<IDbConnectionFactory, DbConnectionFactory>()` (Repositories/Services land in Phase 2 — comment in source flags this)
  - Pipeline: `UseMiddleware<ExceptionHandlingMiddleware>()` (first) → `UseCors("Frontend")` → `UseSwagger`/`UseSwaggerUI` (Development only) → `MapControllers()`
  - No `UseAuthorization` (no auth in v1), no `UseHttpsRedirection` (HTTP only on :8080 to match docker-compose)
- `public partial class Program { }` at the end of the file so Phase 4 integration tests can use `WebApplicationFactory<Program>`

## Verification

The host has no `dotnet` installed, so I ran every dotnet command inside the official `mcr.microsoft.com/dotnet/sdk:8.0` container (the same image Plan 01-01's Dockerfile uses for `dotnet watch run`), sharing a named volume for the NuGet cache between restore and build steps.

Commands run (all from `backend/`):

```bash
# restore + build
docker run --rm -v "$(pwd)":/src -v stockeasy-nuget-cache:/root/.nuget/packages \
  -w /src mcr.microsoft.com/dotnet/sdk:8.0 sh -c \
  "dotnet restore Inventory.sln --nologo && dotnet build Inventory.sln --nologo --no-restore -v minimal"
# → Build succeeded. 0 Warning(s) 0 Error(s)

# tests
docker run --rm -v "$(pwd)":/src -v stockeasy-nuget-cache:/root/.nuget/packages \
  -w /src mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet test Inventory.sln --nologo --no-build -v minimal
# → Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1

# live run smoke (DB intentionally unreachable; we test graceful degradation)
docker run --rm -d --name stockeasy-task3-smoke -v "$(pwd)":/src \
  -v stockeasy-nuget-cache:/root/.nuget/packages -w /src/Inventory \
  -e ConnectionStrings__Postgres="Host=127.0.0.1;Database=__none__;Username=__none__;Password=__none__;Timeout=2" \
  -e ASPNETCORE_URLS="http://0.0.0.0:18080" -e ASPNETCORE_ENVIRONMENT=Development \
  -p 18080:18080 mcr.microsoft.com/dotnet/sdk:8.0 \
  dotnet run --no-launch-profile --project Inventory.csproj
curl http://127.0.0.1:18080/api/health
# → {"status":"degraded","db":"down","version":"1.0.0","timestamp":"2026-05-16T15:33:44.3907100Z"}
curl http://127.0.0.1:18080/swagger/v1/swagger.json | grep operationId
# → "operationId": "getHealth"
docker stop stockeasy-task3-smoke
```

All four pieces validated:

| Check | Result |
|-------|--------|
| `dotnet restore Inventory.sln` | succeeded (both projects) |
| `dotnet build Inventory.sln` | succeeded, **0 warnings, 0 errors** |
| `dotnet test Inventory.sln` | 1 passed, 0 failed |
| `GET /api/health` (DB unreachable) | 200 OK with `{"status":"degraded","db":"down","version":"1.0.0","timestamp":"..."}` — proves the graceful-degradation contract works |
| `GET /swagger/v1/swagger.json` | served; contains `"operationId": "getHealth"` on `/api/health` GET, plus rich `summary`/`description` pulled from the XML doc comments |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added `<Using Include="Xunit" />` to Inventory.Tests.csproj**
- **Found during:** Task 3 verification (first `dotnet build` after writing all files)
- **Issue:** `Inventory.Tests/SmokeTests.cs` references `[Fact]` and `Assert.True(...)` without `using Xunit;`. With `<ImplicitUsings>enable</ImplicitUsings>`, the test project needs an `<Using Include="Xunit" />` entry in its csproj (or a `GlobalUsings.cs`). The `dotnet new xunit` template normally injects this via `GlobalUsings.cs`. Because the plan hand-authored the csproj instead of running the template (the plan's Step 1.1 was bypassed — see "Environment Notes" below), this was missing
- **Fix:** Added an `<ItemGroup><Using Include="Xunit" /></ItemGroup>` block to `Inventory.Tests.csproj`. After this, `dotnet build` reports 0 errors / 0 warnings
- **Files modified:** `backend/Inventory.Tests/Inventory.Tests.csproj`
- **Commit:** `4b1ab52`

**2. [Rule 2 - Missing critical config] Added `backend/.gitignore`**
- **Found during:** Task 3 verification (after first build produced `bin/`/`obj/` directories that `git status` then flagged as untracked)
- **Issue:** Plan only specified `backend/Inventory/.gitignore` covering the `Inventory/` project's build output. But `Inventory.Tests/` also produces `bin/`/`obj/` directories on build, and there is no root-level `.gitignore` in the repo — so test-project build artifacts would either be committed accidentally or leave `git status` permanently dirty
- **Fix:** Added `backend/.gitignore` with `**/bin/`, `**/obj/`, IDE files (`*.user`, `*.suo`, `.vs/`, `.idea/`), and a stale-XML-doc-output ignore (with negation for `Properties/` so launchSettings.json variants survive). Covers both projects uniformly without altering the project-level `.gitignore` from Task 1
- **Files modified:** new `backend/.gitignore`
- **Commit:** `4b1ab52`

## Environment Notes

The execution host has no `dotnet` binary on `PATH` (and none under `/usr/share/dotnet`, `/snap/dotnet-sdk`, `~/.dotnet`, `/opt`, `/usr/local/bin`, or other standard locations). To complete the plan, I:

1. **Hand-authored every file** the `dotnet new sln`, `dotnet new webapi --use-controllers`, and `dotnet new xunit` templates would produce, using the exact contents the plan specified (and the exact .NET 8 SDK template conventions for `Inventory.sln`).
2. **Ran all `dotnet` verification commands inside the `mcr.microsoft.com/dotnet/sdk:8.0` Docker image** — the same image Plan 01-01's Dockerfile uses. A named NuGet-cache volume kept restore artifacts available across container runs.

This is internally consistent with the plan's stack constraints (the plan specifies SDK 8.0 in `Inventory.csproj` and the dev workflow already runs inside containers per D-01..D-04), and produces byte-identical results to a host-side dotnet invocation. Plan 01-04 will exercise the same containerized build path during the `docker-compose up` end-to-end smoke.

## Self-Check

Files claimed created — all verified present on disk:

| Path | Status |
|------|--------|
| backend/Inventory.sln | FOUND |
| backend/Inventory/Inventory.csproj | FOUND |
| backend/Inventory/Program.cs | FOUND |
| backend/Inventory/appsettings.json | FOUND |
| backend/Inventory/appsettings.Development.json | FOUND |
| backend/Inventory/Properties/launchSettings.json | FOUND |
| backend/Inventory/.gitignore | FOUND |
| backend/.gitignore | FOUND |
| backend/Inventory/Infra/IDbConnectionFactory.cs | FOUND |
| backend/Inventory/Infra/DbConnectionFactory.cs | FOUND |
| backend/Inventory/Infra/PagedResult.cs | FOUND |
| backend/Inventory/Dtos/ErrorResponse.cs | FOUND |
| backend/Inventory/Dtos/HealthResponse.cs | FOUND |
| backend/Inventory/Middleware/ExceptionHandlingMiddleware.cs | FOUND |
| backend/Inventory/Controllers/HealthController.cs | FOUND |
| backend/Inventory.Tests/Inventory.Tests.csproj | FOUND |
| backend/Inventory.Tests/SmokeTests.cs | FOUND |

Commits claimed — all verified in `git log`:

| Hash | Subject | Status |
|------|---------|--------|
| 3711c0d | feat(01-02): scaffold Inventory solution + project skeleton | FOUND |
| 4471e90 | feat(01-02): author Infra, Dtos, ExceptionHandlingMiddleware shell, HealthController | FOUND |
| 4b1ab52 | feat(01-02): wire Program.cs composition root + verified build/test/run | FOUND |

## Self-Check: PASSED

## Notes for Phase 2

1. **Extend `ExceptionHandlingMiddleware`** to switch on exception type and map `FluentValidation.ValidationException` → 400 `VALIDATION_ERROR` (+ `details.fields[]`) and `DomainException` subclasses (`NotFoundException` → 404, `BusinessRuleException` → 422) — see backend/CLAUDE.md §"Middleware" for the canonical shape. The shell already serializes `ErrorResponse` with camelCase + `JsonOptions` so just wire in the new branches.
2. **Define `DomainException` / `NotFoundException` / `BusinessRuleException`** in `Exceptions/` (the folder already exists with a `.gitkeep` placeholder).
3. **Add `ProductsController` / `StockMovementsController`**, their services, repositories, validators, and entities. The composition root in `Program.cs` already auto-registers all FluentValidation validators from the assembly — just add the validator classes and they'll wire up.
4. **`PagedResult<T>`** already ships from this plan; just consume it from list endpoints.
5. **Repositories use `IDbConnectionFactory.Create()`** — singleton is registered. Don't register a second factory; use the one from `Inventory.Api.Infra`.
