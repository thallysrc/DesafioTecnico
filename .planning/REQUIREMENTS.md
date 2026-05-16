# Requirements: StockEasy

**Defined:** 2026-05-16
**Core Value:** Demonstrar competência fullstack através de uma implementação limpa, testada, organizada e agentic-ready que cumpre integralmente o spec do desafio.

## v1 Requirements

Requisitos para entrega do desafio técnico. Cada um mapeia para uma fase do roadmap.

### Infrastructure (`INFRA-*`)

- [ ] **INFRA-01**: `docker-compose.yml` sobe PostgreSQL 16 + Backend .NET + Frontend Vue com `docker-compose up`
- [ ] **INFRA-02**: Container Postgres usa `postgres:16-alpine`, monta `init.sql` na inicialização
- [ ] **INFRA-03**: `init.sql` cria tabelas `products` (com `deleted_at`) e `stock_movements` (com `idempotency_key UNIQUE`), índices e CHECK constraints
- [ ] **INFRA-04**: Backend `.csproj` configura `<GenerateDocumentationFile>true</GenerateDocumentationFile>` para XML docs
- [ ] **INFRA-05**: Backend tem CORS habilitado para origem do frontend (`http://localhost:5173`)
- [ ] **INFRA-06**: Backend lê connection string via `IConfiguration` (`ConnectionStrings:Postgres`), sobrescritível por env var
- [ ] **INFRA-07**: Swagger UI exposto em `http://localhost:8080/swagger` em ambiente Development
- [ ] **INFRA-08**: Frontend Vite serve em `http://localhost:5173` com hot reload no container

### Backend Architecture (`BACK-*`)

- [ ] **BACK-01**: Solution `Inventory.sln` com projetos `Inventory/` (API) e `Inventory.Tests/` (testes)
- [ ] **BACK-02**: Estrutura de pastas: `Controllers/`, `Services/`, `Repositories/`, `Entities/`, `DTOs/`, `Validators/`, `Exceptions/`, `Middleware/`, `Infra/`
- [ ] **BACK-03**: Namespaces seguem `Inventory.Api.{Folder}` (file-scoped)
- [ ] **BACK-04**: `IDbConnectionFactory` Singleton com `Npgsql` provider
- [ ] **BACK-05**: Repositórios usam Dapper com SQL inline e aliases (snake_case → PascalCase)
- [ ] **BACK-06**: Entities são classes POCO com setters; DTOs são `record` types
- [ ] **BACK-07**: Mapping Entity ↔ DTO é manual inline no Service (sem AutoMapper/Mapster)
- [ ] **BACK-08**: Services são classes concretas (sem interface); Repositories têm interface + impl
- [ ] **BACK-09**: Transações explícitas via `BeginTransaction()` no Service para operações multi-step
- [ ] **BACK-10**: `SELECT ... FOR UPDATE` em read-modify-write de `stock_quantity`
- [ ] **BACK-11**: Zero N+1 queries — listagens com dados relacionados usam JOIN ou batch fetch
- [ ] **BACK-12**: `DomainException` base com `ErrorCode`, `Category`, `Hint`, `Retryable`, `Details`
- [ ] **BACK-13**: `NotFoundException` e `BusinessRuleException` herdam de `DomainException`
- [ ] **BACK-14**: `ExceptionHandlingMiddleware` mapeia exceptions para `ErrorResponse` canônico
- [ ] **BACK-15**: FluentValidation registrado e auto-validation habilitada (sem DataAnnotations)
- [ ] **BACK-16**: Validators específicos por DTO em `Validators/` (mensagens em PT-BR)

### Domínio — Produtos (`PROD-*`)

- [ ] **PROD-01**: `POST /api/products` cadastra produto com `code` (único), `description`, `type` (enum), `supplierValue` (≥ 0), `initialStockQuantity` (≥ 0)
- [ ] **PROD-02**: `GET /api/products?page=1&pageSize=30` lista produtos ativos paginados, default 30, max 100
- [ ] **PROD-03**: `GET /api/products?includeDeleted=true` inclui produtos soft-deletados
- [ ] **PROD-04**: `GET /api/products/{id}` detalha produto por ID, retorna mesmo se deletado (com `deletedAt`)
- [ ] **PROD-05**: `DELETE /api/products/{id}` faz soft delete (`UPDATE products SET deleted_at = now()`), retorna 204
- [ ] **PROD-06**: Tentativa de criar produto com `code` já existente retorna 422 com `errorCode: DUPLICATE_CODE` + hint
- [ ] **PROD-07**: Validator rejeita `supplierValue < 0` e `initialStockQuantity < 0` com mensagens específicas

### Domínio — Movimentação de Estoque (`MOVE-*`)

- [ ] **MOVE-01**: `POST /api/stock-movements` registra movimento (Inbound ou Outbound), incrementa/decrementa `stock_quantity` do produto atomicamente
- [ ] **MOVE-02**: Header `Idempotency-Key` (UUID) é **required** — ausente retorna 400 com `errorCode: MISSING_IDEMPOTENCY_KEY`
- [ ] **MOVE-03**: Repeat com mesmo `Idempotency-Key` retorna o movimento existente com header `Idempotency-Replay: true` e status 200
- [ ] **MOVE-04**: Movimento `Outbound` com `quantity > stockQuantity` retorna 422 com `errorCode: INSUFFICIENT_BALANCE`, hint dinâmica com saldo real
- [ ] **MOVE-05**: Movimento de produto soft-deletado retorna 422 com `errorCode: PRODUCT_DELETED`
- [ ] **MOVE-06**: Movimento `Inbound` atualiza `supplier_value` do produto com o valor enviado
- [ ] **MOVE-07**: Movimento `Outbound` exige `saleValue` no body; `Inbound` exige `supplierValue`. Inversão retorna 422 com `errorCode: INVALID_MOVEMENT_VALUES`
- [ ] **MOVE-08**: `GET /api/stock-movements?productId=X&startDate=Y&endDate=Z&page=1&pageSize=30` lista histórico paginado
- [ ] **MOVE-09**: Listagem de movimentos faz JOIN com `products` para retornar `productCode` e `productDescription` (zero N+1)
- [ ] **MOVE-10**: `GET /api/stock-movements/{id}` detalha movimento individual
- [ ] **MOVE-11**: Movimentos registrados são imutáveis (sem endpoint de UPDATE ou DELETE)

### Agentic API (`AGENT-*`)

- [ ] **AGENT-01**: Enums serializados como string no JSON via `JsonStringEnumConverter` global
- [ ] **AGENT-02**: Todo endpoint tem `[SwaggerOperation(OperationId = "verbNoun")]` (camelCase verb-noun)
- [ ] **AGENT-03**: Todo controller e endpoint tem XML `<summary>` e `<remarks>` quando aplicável
- [ ] **AGENT-04**: Todo campo de cada DTO tem XML `<summary>` (vira description no OpenAPI)
- [ ] **AGENT-05**: Todo endpoint declara `[ProducesResponseType]` para todos os status codes que retorna
- [ ] **AGENT-06**: `ErrorResponse` canônico com: `errorCode`, `category`, `message`, `hint`, `statusCode`, `retryable`, `details`, `traceId`, `timestamp`
- [ ] **AGENT-07**: `errorCode` segue catálogo fechado (SCREAMING_SNAKE_CASE): `VALIDATION_ERROR`, `MISSING_IDEMPOTENCY_KEY`, `PRODUCT_NOT_FOUND`, `MOVEMENT_NOT_FOUND`, `DUPLICATE_CODE`, `PRODUCT_DELETED`, `INSUFFICIENT_BALANCE`, `INVALID_MOVEMENT_VALUES`, `INTERNAL_ERROR`
- [ ] **AGENT-08**: `hint` é **dinâmica** construída no Service com dados reais do contexto (não hardcoded genérica)
- [ ] **AGENT-09**: Responses de recursos incluem `_links` (self + related actions) quando aplicável
- [ ] **AGENT-10**: Listagens retornam envelope com `items`, `pagination` (page/pageSize/total/totalPages/hasNext/hasPrev), `_links` (self/next/first/last)
- [ ] **AGENT-11**: Pacote `Swashbuckle.AspNetCore.Annotations` configurado para enriquecer OpenAPI

### Frontend Stack (`FRONT-*`)

- [ ] **FRONT-01**: Projeto Vue 3 + Vite + TypeScript strict (tsconfig com `strict: true`)
- [ ] **FRONT-02**: Tailwind configurado com paleta `brand-*` (`#1863DC` primary), `ink`/`muted`/`line`/`surface`/`success`/`warning`/`danger`
- [ ] **FRONT-03**: Inter font carregada via Google Fonts no `index.html`
- [ ] **FRONT-04**: Estrutura feature-based: `src/features/products/`, `src/features/stock/`, `src/shared/`
- [ ] **FRONT-05**: Axios instance único em `src/shared/api/client.ts` com interceptor normalizando erros para `ApiError`
- [ ] **FRONT-06**: Composables (`useProducts`, `useStockMovements`) em `features/<feat>/composables/`, sem Pinia
- [ ] **FRONT-07**: Forms usam Vee-Validate + Zod com `validateOnBlur: true`
- [ ] **FRONT-08**: Schemas Zod espelham regras do FluentValidation com mesmas mensagens PT
- [ ] **FRONT-09**: Vue Router com SPA layout: sidebar light fixa + `<router-view/>`
- [ ] **FRONT-10**: Rotas: `/products` e `/stock-movements`; redirect de `/` para `/products`
- [ ] **FRONT-11**: Wordmark `Stock<span class="text-brand-500">Easy</span>` em sidebar/header
- [ ] **FRONT-12**: API client gera `Idempotency-Key` automaticamente em `POST /api/stock-movements` via `crypto.randomUUID()`

### Frontend UX — Heurísticas de Nielsen (`UX-*`)

- [ ] **UX-01** (Visibilidade): Toda lista renderiza um de 4 estados (loading skeleton / empty state com CTA / error com hint+retry / success-with-data)
- [ ] **UX-02** (Visibilidade): Form submetendo mostra botão "Salvando..." disabled + spinner + `aria-busy="true"`
- [ ] **UX-03** (Visibilidade): Sucesso de operação dispara toast verde 3s com mensagem específica
- [ ] **UX-04** (Mundo real): `src/shared/format.ts` centraliza `formatCurrency` (R$), `formatDate` (dd/mm/yyyy HH:mm), `formatQuantity` (separador milhar) via `Intl`
- [ ] **UX-05** (Mundo real): `src/shared/labels.ts` centraliza traduções enum API → PT (`productTypeLabel`, `movementTypeLabel`)
- [ ] **UX-06** (Controle): Modais têm botão "Cancelar" à esquerda, `Esc` fecha, foco retorna ao trigger
- [ ] **UX-07** (Consistência): Botão primário sempre à direita, paleta padronizada (brand/danger/success), labels acima de inputs
- [ ] **UX-08** (Prevenção): Form de saída mostra "Disponível: N unidades" acima do campo quantidade
- [ ] **UX-09** (Prevenção): Submit desabilitado enquanto `meta.valid === false`
- [ ] **UX-10** (Reconhecimento): Seleção de produto na movimentação é searchable dropdown com `code — description`
- [ ] **UX-11** (Recuperação): Toast de erro consome `apiError.hint ?? apiError.message`
- [ ] **UX-12** (A11y): Inputs têm `<label for>`, erros usam `aria-describedby`, focus ring `focus:ring-2 focus:ring-brand-500` em todos elementos focáveis
- [ ] **UX-13** (A11y): Modais movem foco ao primeiro elemento, `Esc` fecha, semantic HTML (`<button>`, não `<div onclick>`)

### Confirmation Modals (`CONF-*`)

- [ ] **CONF-01**: Modal de confirmação em **saída de estoque** com resumo: produto, quantidade, valor de venda, saldo atual → saldo resultante
- [ ] **CONF-02**: Modal de confirmação em **soft delete de produto** explicando que histórico será mantido
- [ ] **CONF-03**: Cadastro de produto e entrada de estoque submetem direto (sem modal de confirmação)

### Tests (`TEST-*`)

- [ ] **TEST-01**: `Inventory.Tests` (xUnit + Moq) cobre `ProductService` (happy path + cada errorCode possível)
- [ ] **TEST-02**: `Inventory.Tests` cobre `StockMovementService` (entrada, saída, saldo insuficiente, produto deletado, idempotency replay)
- [ ] **TEST-03**: Tests assertam `ex.ErrorCode`, `ex.Message` (substring) e `ex.Hint` (não-null) em exceptions
- [ ] **TEST-04**: `Inventory.Tests` cobre Validators (`CreateProductValidator`, `CreateMovementValidator`)
- [ ] **TEST-05**: Frontend Vitest cobre `useProducts` e `useStockMovements` (mock da API layer)
- [ ] **TEST-06**: Frontend Vitest cobre `ProductForm` e `OutboundForm` (montagem + interação básica)
- [ ] **TEST-07**: `dotnet test` e `npm test` rodam sem falhas em CI local

### Documentation (`DOC-*`)

- [ ] **DOC-01**: Pasta `docs/` na raiz com `README.md` (índice)
- [ ] **DOC-02**: `docs/01-product-decisions.md` documenta visão do produto, decisões estratégicas (stack, agentic API, soft delete), trade-offs, roadmap v2 (chat LLM)
- [ ] **DOC-03**: `docs/02-architecture.md` contém 5 diagramas Mermaid (System Context, Backend Layered, Sequence stock-out, ER Diagram, Frontend feature flow) + tabela de stack + instruções de execução
- [ ] **DOC-04**: `docs/03-business-rules.md` documenta entidades, enums, regras enforced, catálogo de errorCodes com tabela
- [ ] **DOC-05**: `docs/assets/pdf-style.css` aplica paleta StockEasy, Inter font, CSS Paged Media (capa, header/footer, paginação)
- [ ] **DOC-06**: `docs/assets/pandoc-template.html` é o scaffold HTML que Pandoc usa
- [ ] **DOC-07**: `docs/generate-pdfs.sh` roda Pandoc + WeasyPrint + mermaid-filter e gera os 3 PDFs em `docs/dist/`
- [ ] **DOC-08**: PDFs commitados em `docs/dist/01-product-decisions.pdf`, `02-architecture.pdf`, `03-business-rules.pdf`
- [ ] **DOC-09**: `README.md` raiz do projeto explica como rodar com docker-compose, lista URLs (Swagger, frontend), aponta para `docs/`

## v2 Requirements

Deferred to future release.

### LLM Chat (`CHAT-*`)

- **CHAT-01**: Endpoint `POST /api/chat` recebe mensagem, chama LLM com tools definidas da API
- **CHAT-02**: LLM consome operationIds existentes como tool names
- **CHAT-03**: UI de chat no frontend (drawer ou page dedicada)
- **CHAT-04**: Confirmação humana antes de executar mutations (`Outbound`, `deleteProduct`)
- **CHAT-05**: Tools disponíveis: `listProducts`, `getProduct`, `createProduct`, `registerStockMovement`, `listStockMovements`

## Out of Scope

| Feature | Razão |
|---------|-------|
| Autenticação / Login (JWT) | Não pedido no spec, fora do prazo de 1-3 dias |
| Chat com LLM dentro do app v1 | Planejado pra v2 — API já é agent-ready |
| Biblioteca de UI pronta (PrimeVue/Vuetify) | Tailwind + componentes próprios é escolha consciente |
| Hard delete de produtos | Sempre soft delete via `deleted_at` |
| Modificar/deletar movimentos | Histórico imutável (auditoria) |
| Tipos de produto extras | Spec define enum fechado |
| Deploy em cloud | Entrega via PR; rodar local com docker-compose basta |
| Dark mode toggle | Light mode only no MVP |
| i18n multi-language | Só PT-BR no UI |
| Auto-save drafts | Overkill pro prazo |
| Atalhos de teclado custom | Overkill pro prazo |
| AutoMapper / Mapster | Mapping manual inline |
| MediatR / CQRS | Arquitetura single-project N-tier |
| Pinia / Vuex | Composables only |
| EF Core / Migrations | Dapper + `init.sql` |

## Traceability

Será populado durante criação do roadmap (cada REQ-ID mapeado para uma fase).

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01..08 | TBD | Pending |
| BACK-01..16 | TBD | Pending |
| PROD-01..07 | TBD | Pending |
| MOVE-01..11 | TBD | Pending |
| AGENT-01..11 | TBD | Pending |
| FRONT-01..12 | TBD | Pending |
| UX-01..13 | TBD | Pending |
| CONF-01..03 | TBD | Pending |
| TEST-01..07 | TBD | Pending |
| DOC-01..09 | TBD | Pending |

**Coverage:**
- v1 requirements total: **86**
- Mapped to phases: **0** (pending — roadmap creation)
- Unmapped: 86 ⚠️ (esperado nesta fase)

---
*Requirements defined: 2026-05-16*
*Last updated: 2026-05-16 after rule consolidation*
