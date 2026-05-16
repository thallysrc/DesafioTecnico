<!-- GSD:project-start source:PROJECT.md -->
## Project

**StockEasy**

**StockEasy** é uma aplicação fullstack para gestão de produtos e movimentações de estoque (entradas e saídas), entregue como parte de um desafio técnico de processo seletivo. Backend em **ASP.NET Core Web API (.NET 8 + Dapper)** e frontend em **Vue 3 + TypeScript + Tailwind** consumindo a API, com regras de negócio como validação de saldo insuficiente e histórico imutável.

A API é projetada **agentic-friendly** desde o MVP — operationIds estáveis, errorCodes estruturados, hints dinâmicos de recuperação, idempotência em mutations sensíveis. Em v2 (futuro), um chat com LLM consumirá essa API como tools.

Visualmente, o produto é parte da família **RoboteAsy** (paleta azul corporativa).

**Core Value:** Demonstrar competência fullstack através de uma implementação **limpa, testada, organizada, e agentic-ready** que cumpre integralmente o spec do desafio — porque o objetivo final é passar na avaliação técnica.

### Constraints

- **Backend stack**: ASP.NET Core Web API (.NET 8 LTS), C# 12, Nullable enable — exigência do spec + decisão LTS
- **Frontend stack**: Vue 3 + Vite + Composition API + TypeScript strict — exigência do spec (Vue.js) + decisão pela versão moderna
- **Database**: PostgreSQL 16 — mesma versão do BancoShu, ampla compatibilidade
- **ORM**: Dapper — micro ORM, SQL raw, sem migrations (`init.sql` versionado)
- **Validation**: FluentValidation (backend) + Zod via Vee-Validate (frontend)
- **HTTP client**: Axios com interceptor único pra normalizar erros
- **UI styling**: Tailwind CSS sem biblioteca de componentes
- **Tipografia**: Inter (Google Fonts) — fallback `system-ui, sans-serif`
- **Paleta brand**: derivada do RoboteAsy (`#1863DC` primary, `#0056A7` primary-dark, escala brand-50..900) + semantic (`success #009C34`, `warning #FCB900`, `danger #CF2E2E`)
- **Timeline**: 1-3 dias — granularidade coarse, fases verticais (back+front juntos), foco MVP
- **Entrega**: PR no fork do GitHub — commits limpos, atômicos, hist coerente
- **Dois lados sincronizados**: validações em backend (autoridade) E frontend (UX preventiva) — critério de avaliação
- **Histórico imutável**: movimentações registradas não editam/excluem — auditoria
- **Soft delete em produtos**: `deleted_at` timestamptz NULL; movimentos seguem imutáveis
- **Idempotency obrigatória**: header `Idempotency-Key` (UUID v4) required em `POST /api/stock-movements`
- **Paginação padrão**: `?page=1&pageSize=30`, max `pageSize=100`
- **Zero N+1**: JOIN ou batch fetch em toda listagem com dados relacionados
- **Idiomas**: identifiers em código em **inglês**; mensagens, hints e labels ao usuário em **português** (BR locale)
<!-- GSD:project-end -->

<!-- GSD:stack-start source:STACK.md -->
## Technology Stack

Technology stack not yet documented. Will populate after codebase mapping or first phase.
<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->
## Conventions

Conventions not yet established. Will populate as patterns emerge during development.
<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->
## Architecture

Architecture not yet mapped. Follow existing patterns found in the codebase.
<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->
## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, or `.github/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->
## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:
- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->



<!-- GSD:profile-start -->
## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
