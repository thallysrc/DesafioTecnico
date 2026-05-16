# Backend Conventions — Inventory API (StockEasy)

Regras que devem ser seguidas **religiosamente** ao escrever código no `backend/`.
Convenções derivadas do projeto de referência `BancoShu` (`/home/thallysrc/Projects/BancoShu/`). Quando em dúvida, abrir o BancoShu e copiar o padrão. **Exceção**: a query N+1 em `TransferService.GetHistoryAsync` é anti-pattern — NÃO copiar.

---

## Stack

| Camada | Tecnologia |
|--------|------------|
| Framework | ASP.NET Core Web API on **.NET 8** (LTS) |
| Linguagem | C# 12, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>` |
| DB | PostgreSQL 16 |
| ORM | **Dapper** (NÃO usar Entity Framework Core) |
| DB driver | `Npgsql` 10.x |
| Validação | **FluentValidation.AspNetCore** (cobre todo input — NÃO usar DataAnnotations) |
| API docs | `Swashbuckle.AspNetCore` + `Swashbuckle.AspNetCore.Annotations` (Swagger rico) |
| Tests | xUnit + Moq |

**Pacotes adicionais aprovados**: nenhum além desta lista sem decisão explícita. Em particular: nada de AutoMapper, Mapster, MediatR, Polly, Serilog, FluentResults.

---

## Solution & Project Structure

```
backend/
├── Inventory.sln
├── Inventory/
│   ├── Inventory.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   ├── Entities/
│   ├── DTOs/
│   ├── Validators/
│   ├── Exceptions/
│   ├── Middleware/
│   └── Infra/
└── Inventory.Tests/
    ├── Inventory.Tests.csproj
    ├── Services/
    ├── Repositories/
    └── Validators/
```

**Single project — NÃO** dividir em `Inventory.Domain`, `Inventory.Application`, `Inventory.Infrastructure`. Pasta única com responsabilidades separadas. Avaliadores valorizam clareza > over-engineering.

---

## Namespacing

Padrão: **`Inventory.Api.{Folder}`** (com segmento `.Api.` mesmo o projeto chamando `Inventory`).

```csharp
namespace Inventory.Api.Controllers;
namespace Inventory.Api.Services;
namespace Inventory.Api.Repositories;
namespace Inventory.Api.Entities;
namespace Inventory.Api.Dtos;        // "Dtos" (minúsculo após 'D') igual BancoShu
namespace Inventory.Api.Validators;
namespace Inventory.Api.Exceptions;
namespace Inventory.Api.Middleware;
namespace Inventory.Api.Infra;       // "Infra" curto, não "Infrastructure"
```

Tests: `namespace Inventory.Tests.Services;` etc.

File-scoped namespaces (`namespace X;` com `;`, sem chaves). Sempre.

---

## Naming

| Tipo | Convenção | Exemplo |
|------|-----------|---------|
| Classes / structs / records | PascalCase | `Product`, `StockMovement` |
| Interfaces | PascalCase com `I` prefix | `IProductRepository` |
| Métodos / propriedades públicas | PascalCase | `CreateAsync`, `Balance` |
| Métodos assíncronos | sufixo `Async` | `GetByIdAsync` |
| Parâmetros / locais | camelCase | `productId`, `request` |
| Campos privados | `_camelCase` | `_repository`, `_factory` |
| Constantes | PascalCase | `MaxAttempts` |
| Enums | PascalCase singular | `ProductType`, `MovementType` |
| Members de enum | PascalCase | `Electronic`, `Inbound` |
| SQL: tabelas / colunas | snake_case | `products`, `stock_movements`, `created_at` |
| Rotas HTTP | kebab-case plural | `/api/products`, `/api/stock-movements` |
| `operationId` (OpenAPI) | camelCase verb-noun | `createProduct`, `registerStockMovement` |
| `errorCode` (HTTP body) | SCREAMING_SNAKE_CASE | `INSUFFICIENT_BALANCE`, `NOT_FOUND` |

**Identifiers em inglês.** Mensagens e hints ao usuário em **português** (avaliadores brasileiros).

---

## Domain Names (fixos para este projeto)

```csharp
public class Product { ... }
public class StockMovement { ... }

public enum ProductType { Electronic, Appliance, Furniture }
public enum MovementType { Inbound, Outbound }
```

Rotas e operationIds:

| Método | Rota | operationId | Descrição |
|--------|------|-------------|-----------|
| POST | `/api/products` | `createProduct` | Cadastrar produto |
| GET | `/api/products` | `listProducts` | Listar com paginação |
| GET | `/api/products/{id}` | `getProduct` | Detalhar produto |
| DELETE | `/api/products/{id}` | `deleteProduct` | Soft delete (`deleted_at`) |
| POST | `/api/stock-movements` | `registerStockMovement` | Registrar entrada ou saída |
| GET | `/api/stock-movements` | `listStockMovements` | Listar histórico com filtros |
| GET | `/api/stock-movements/{id}` | `getStockMovement` | Detalhar movimento |

---

## Entities (`Entities/`)

POCO **classe**, propriedades mutáveis com `{ get; set; }`. Dapper precisa de setters pra materializar.

```csharp
namespace Inventory.Api.Entities;

public class Product
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ProductType Type { get; set; }
    public decimal SupplierValue { get; set; }
    public int StockQuantity { get; set; }
    public DateTime? DeletedAt { get; set; }   // soft delete — NULL = ativo
}

public class StockMovement
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public MovementType Type { get; set; }
    public int Quantity { get; set; }
    public decimal? SaleValue { get; set; }       // null em Inbound
    public decimal? SupplierValue { get; set; }   // null em Outbound
    public Guid IdempotencyKey { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

- Use `Guid` para IDs (não `int`/`long`).
- `decimal` para valores monetários (NUNCA `double`/`float`).
- `DateTime.UtcNow` para timestamps; armazenar em UTC.
- Inicialize strings com `string.Empty` pra calar o nullable analyzer.
- **Sem lógica de negócio em entities** — só dados. Lógica vive em Services.

---

## DTOs (`Dtos/`)

`record` types com parâmetros posicionais. Imutáveis por construção. XML docs em cada campo (vira description no OpenAPI).

```csharp
namespace Inventory.Api.Dtos;

/// <summary>Cadastro de novo produto no inventário</summary>
public record CreateProductRequest(
    /// <summary>Código único do produto (até 50 caracteres, identifica o produto no sistema)</summary>
    string Code,
    /// <summary>Descrição livre do produto (até 200 caracteres)</summary>
    string Description,
    /// <summary>Categoria: Electronic, Appliance ou Furniture</summary>
    ProductType Type,
    /// <summary>Valor pago ao fornecedor por unidade (BRL, não-negativo)</summary>
    decimal SupplierValue,
    /// <summary>Quantidade inicial em estoque (≥ 0)</summary>
    int InitialStockQuantity
);

/// <summary>Produto cadastrado no inventário</summary>
public record ProductResponse(
    Guid Id,
    string Code,
    string Description,
    ProductType Type,
    decimal SupplierValue,
    int StockQuantity,
    DateTime? DeletedAt,
    Dictionary<string, string>? _links
);
```

- Request DTOs: sufixo `Request`.
- Response DTOs: sufixo `Response` (ou nome do recurso quando é listagem).
- Listas: criar tipos específicos (ex: `MovementHistoryItem`) em vez de reusar entities.
- XML docs em cada campo: vira `description` no schema OpenAPI → LLM lê.

---

## Mapping Entity ↔ DTO

**Manual inline no Service.** SEM AutoMapper/Mapster/`.ToDto()` extensions.

```csharp
// Dentro do Service:
var product = await _repository.GetByIdAsync(id);
return new ProductResponse(
    product.Id,
    product.Code,
    product.Description,
    product.Type,
    product.SupplierValue,
    product.StockQuantity,
    product.DeletedAt,
    _links: new Dictionary<string, string>
    {
        ["self"] = $"/api/products/{product.Id}",
        ["movements"] = $"/api/stock-movements?productId={product.Id}"
    }
);
```

Pra snake_case ↔ PascalCase no Dapper, use **aliases SQL**:

```sql
SELECT id, supplier_value as SupplierValue, stock_quantity as StockQuantity, deleted_at as DeletedAt
FROM products WHERE id = @id
```

Não usar `DefaultTypeMap.MatchNamesWithUnderscores` — aliases explícitos > convenção implícita.

---

## Controllers (`Controllers/`)

**Thin.** Apenas roteamento + delegação ao Service. Zero lógica de negócio.

```csharp
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Inventory.Api.Dtos;
using Inventory.Api.Services;

namespace Inventory.Api.Controllers;

/// <summary>Gerenciamento de produtos do inventário</summary>
[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _service;
    public ProductsController(ProductService service) => _service = service;

    /// <summary>Cadastra um novo produto</summary>
    /// <remarks>O código deve ser único. Quantidade inicial e valor do fornecedor devem ser não-negativos.</remarks>
    [HttpPost]
    [SwaggerOperation(OperationId = "createProduct", Tags = new[] { "Products" })]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var response = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>Detalha produto por ID</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(OperationId = "getProduct")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var response = await _service.GetByIdAsync(id);
        return Ok(response);
    }
}
```

Regras:
- `[ApiController]` + `[Route("api/<recurso-plural-kebab>")]` na classe.
- XML doc `<summary>` em controller (vira tag description) e em cada action (vira summary do endpoint).
- `[SwaggerOperation(OperationId = "verbNoun")]` em **todo endpoint**.
- `[ProducesResponseType]` para **cada status code** que o endpoint pode retornar (200/201/400/404/422/500).
- Construtor com expression body (`public X(Y y) => _y = y;`) quando trivial.
- Métodos `async Task<IActionResult>`, retornando `Ok(...)` / `CreatedAtAction(...)` / `NoContent()`.
- **NUNCA** try/catch em controller. Exceptions sobem pro middleware.
- **NUNCA** validar input manualmente — FluentValidation faz isso.

---

## Services (`Services/`)

**Classe concreta, SEM interface.** Constructor injection. Joga `DomainException` com hints dinâmicos.

```csharp
namespace Inventory.Api.Services;

public class StockMovementService
{
    private readonly IDbConnectionFactory _factory;
    private readonly IProductRepository _products;
    private readonly IStockMovementRepository _movements;

    public StockMovementService(
        IDbConnectionFactory factory,
        IProductRepository products,
        IStockMovementRepository movements)
    {
        _factory = factory;
        _products = products;
        _movements = movements;
    }

    public async Task<MovementResponse> RegisterAsync(CreateMovementRequest request, Guid idempotencyKey)
    {
        using var conn = _factory.Create();
        conn.Open();

        // Idempotency check ANTES de iniciar transação
        var existing = await _movements.GetByIdempotencyKeyAsync(idempotencyKey, conn);
        if (existing is not null)
            return MapToResponse(existing, isReplay: true);

        using var tx = conn.BeginTransaction();

        var product = await _products.GetByIdAsync(request.ProductId, tx, forUpdate: true);

        if (product is null)
            throw new NotFoundException(
                errorCode: "PRODUCT_NOT_FOUND",
                message: "Produto não encontrado",
                hint: "Verifique o productId. Use GET /api/products para listar produtos disponíveis",
                details: new { request.ProductId });

        if (product.DeletedAt is not null)
            throw new BusinessRuleException(
                errorCode: "PRODUCT_DELETED",
                message: $"Produto '{product.Code}' foi removido e não aceita novas movimentações",
                hint: "Cadastre um novo produto ou use outro produto ativo",
                details: new { productId = product.Id, productCode = product.Code, deletedAt = product.DeletedAt });

        if (request.Type == MovementType.Outbound && product.StockQuantity < request.Quantity)
            throw new BusinessRuleException(
                errorCode: "INSUFFICIENT_BALANCE",
                message: $"Saldo insuficiente: solicitado {request.Quantity} unidades, disponível {product.StockQuantity}",
                hint: $"Reduza a quantidade para no máximo {product.StockQuantity} ou registre uma entrada de estoque antes da saída",
                details: new
                {
                    productId = product.Id,
                    productCode = product.Code,
                    requested = request.Quantity,
                    available = product.StockQuantity,
                    deficit = request.Quantity - product.StockQuantity
                });

        // ... mutação saldo + insert movimento
        tx.Commit();
        return MapToResponse(newMovement, isReplay: false);
    }
}
```

Regras:
- Classe concreta, registrada no DI como ela mesma (sem interface).
- Quando precisar de transação: usar `IDbConnectionFactory.Create()` + `BeginTransaction()` direto no Service e passar `IDbTransaction tx` pros repositories.
- Validações de negócio → `throw new BusinessRuleException(errorCode, message, hint, details)`.
- Recurso não encontrado → `throw new NotFoundException(errorCode, message, hint, details)`.
- **Sempre 4 argumentos** ao lançar exception: `errorCode` (slug estável), `message` (PT humano), `hint` (PT acionável com dados reais), `details` (objeto estruturado).
- Idempotency check **antes** de abrir transação.

---

## Repositories (`Repositories/`)

Interface + implementação Dapper. SQL inline como strings. **Zero N+1 queries.**

```csharp
namespace Inventory.Api.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, IDbTransaction? tx = null, bool forUpdate = false);
    Task<PagedResult<Product>> ListAsync(int page, int pageSize, bool includeDeleted);
    Task CreateAsync(Product product, IDbTransaction? tx = null);
    Task UpdateStockAsync(Guid id, int newQuantity, IDbTransaction tx);
    Task SoftDeleteAsync(Guid id);
}

public class ProductRepository : IProductRepository
{
    private readonly IDbConnectionFactory _factory;
    public ProductRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<Product?> GetByIdAsync(Guid id, IDbTransaction? tx = null, bool forUpdate = false)
    {
        var sql = @"
            SELECT id, code, description, type,
                   supplier_value as SupplierValue,
                   stock_quantity as StockQuantity,
                   deleted_at as DeletedAt
            FROM products
            WHERE id = @id";

        if (forUpdate) sql += " FOR UPDATE";

        if (tx is not null)
            return await tx.Connection!.QuerySingleOrDefaultAsync<Product>(sql, new { id }, tx);

        using var conn = _factory.Create();
        return await conn.QuerySingleOrDefaultAsync<Product>(sql, new { id });
    }

    public async Task<PagedResult<Product>> ListAsync(int page, int pageSize, bool includeDeleted)
    {
        var filter = includeDeleted ? "" : "WHERE deleted_at IS NULL";
        var offset = (page - 1) * pageSize;

        using var conn = _factory.Create();

        var items = await conn.QueryAsync<Product>($@"
            SELECT id, code, description, type,
                   supplier_value as SupplierValue,
                   stock_quantity as StockQuantity,
                   deleted_at as DeletedAt
            FROM products {filter}
            ORDER BY code
            LIMIT @pageSize OFFSET @offset",
            new { pageSize, offset });

        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM products {filter}");

        return new PagedResult<Product>(items.ToList(), page, pageSize, total);
    }

    public async Task SoftDeleteAsync(Guid id)
    {
        using var conn = _factory.Create();
        await conn.ExecuteAsync(
            "UPDATE products SET deleted_at = now() WHERE id = @id AND deleted_at IS NULL",
            new { id });
    }
}
```

**Listagens com dados relacionados — SEM N+1:**

```csharp
// ❌ NUNCA fazer isso (N+1):
foreach (var m in movements)
    var p = await _products.GetByIdAsync(m.ProductId);

// ✅ JOIN inline:
var sql = @"
    SELECT m.id, m.product_id as ProductId, m.type, m.quantity,
           m.sale_value as SaleValue, m.supplier_value as SupplierValue,
           m.idempotency_key as IdempotencyKey, m.created_at as CreatedAt,
           p.code as ProductCode, p.description as ProductDescription
    FROM stock_movements m
    INNER JOIN products p ON p.id = m.product_id
    WHERE (@productId::uuid IS NULL OR m.product_id = @productId)
      AND m.created_at >= @startDate AND m.created_at < @endDate
    ORDER BY m.created_at DESC
    LIMIT @pageSize OFFSET @offset";
```

Para resultados multi-tabela com Dapper, usar **multi-mapping** (`QueryAsync<A, B, Result>(sql, (a, b) => ..., splitOn: "...")`) ou DTO direto que combine campos.

Regras:
- Sempre interface + classe.
- Métodos aceitam `IDbTransaction? tx = null` quando podem ser usados dentro de transação compartilhada.
- `forUpdate: true` faz `SELECT ... FOR UPDATE` (necessário pra read-modify-write em movimentações).
- SQL inline. NÃO criar abstração tipo `SqlBuilder`.
- Aliases SQL pra snake_case → PascalCase.
- Dapper queries: `ExecuteAsync` (DML), `QuerySingleOrDefaultAsync<T>` (1 ou 0), `QuerySingleAsync<T>` (exatamente 1), `QueryAsync<T>` (lista), `ExecuteScalarAsync<T>` (1 valor).

---

## Validators (`Validators/`)

FluentValidation. Uma classe por DTO de request. Mensagens em **português**.

```csharp
using FluentValidation;
using Inventory.Api.Dtos;

namespace Inventory.Api.Validators;

public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Código é obrigatório")
            .MaximumLength(50).WithMessage("Código deve ter no máximo 50 caracteres");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Descrição é obrigatória")
            .MaximumLength(200);

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo inválido. Valores aceitos: Electronic, Appliance, Furniture");

        RuleFor(x => x.SupplierValue)
            .GreaterThanOrEqualTo(0).WithMessage("Valor do fornecedor não pode ser negativo");

        RuleFor(x => x.InitialStockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantidade inicial não pode ser negativa");
    }
}

public class PaginationValidator : AbstractValidator<PaginationRequest>
{
    public PaginationValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Página deve ser ≥ 1");
        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("pageSize deve ser ≥ 1")
            .LessThanOrEqualTo(100).WithMessage("pageSize deve ser ≤ 100");
    }
}
```

Registrar no `Program.cs`:
```csharp
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

Falhas viram `ValidationException` que o middleware traduz pra `400` com `errorCode: "VALIDATION_ERROR"` + lista de `fields[]` no `details`.

---

## Exceptions (`Exceptions/`)

Hierarquia rica — cada exception carrega contexto que vira `ErrorResponse` no middleware.

```csharp
namespace Inventory.Api.Exceptions;

public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    public string Category { get; }
    public string? Hint { get; }
    public bool Retryable { get; }
    public object? Details { get; }

    protected DomainException(
        string message,
        string errorCode,
        string category,
        string? hint = null,
        bool retryable = false,
        object? details = null) : base(message)
    {
        ErrorCode = errorCode;
        Category = category;
        Hint = hint;
        Retryable = retryable;
        Details = details;
    }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string errorCode, string message, string? hint = null, object? details = null)
        : base(message, errorCode, "NOT_FOUND", hint, retryable: false, details) { }
}

public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string errorCode, string message, string? hint = null, object? details = null, bool retryable = false)
        : base(message, errorCode, "BUSINESS_RULE", hint, retryable, details) { }
}
```

**Catálogo fechado de errorCodes:**

| errorCode | Category | HTTP | Quando |
|-----------|----------|------|--------|
| `VALIDATION_ERROR` | VALIDATION | 400 | FluentValidation falhou |
| `MISSING_IDEMPOTENCY_KEY` | VALIDATION | 400 | Header `Idempotency-Key` ausente em POST `/api/stock-movements` |
| `PRODUCT_NOT_FOUND` | NOT_FOUND | 404 | productId não existe |
| `MOVEMENT_NOT_FOUND` | NOT_FOUND | 404 | movementId não existe |
| `DUPLICATE_CODE` | BUSINESS_RULE | 422 | INSERT em `products.code` quebrou UNIQUE constraint |
| `PRODUCT_DELETED` | BUSINESS_RULE | 422 | Tentar movimentar produto com `deleted_at != null` |
| `INSUFFICIENT_BALANCE` | BUSINESS_RULE | 422 | Outbound com quantity > stock |
| `INVALID_MOVEMENT_VALUES` | BUSINESS_RULE | 422 | Outbound sem `saleValue`, ou Inbound sem `supplierValue` |
| `INTERNAL_ERROR` | INTERNAL | 500 | Exception não tratada |

**Adicionar novos `errorCode` requer atualização desta tabela.**

---

## Middleware (`Middleware/`)

`ExceptionHandlingMiddleware` registrado **primeiro** no pipeline. Mapeia tudo para `ErrorResponse` canônico.

```csharp
using System.Net;
using System.Text.Json;
using FluentValidation;
using Inventory.Api.Dtos;
using Inventory.Api.Exceptions;

namespace Inventory.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try { await _next(context); }
        catch (Exception ex) { await HandleAsync(context, ex); }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        _logger.LogError(ex, "Erro tratado: {Type} - {Message}", ex.GetType().Name, ex.Message);

        var traceId = ctx.TraceIdentifier;
        var timestamp = DateTime.UtcNow.ToString("O");

        ErrorResponse response = ex switch
        {
            ValidationException v => new ErrorResponse(
                ErrorCode: "VALIDATION_ERROR",
                Category: "VALIDATION",
                Message: "Um ou mais campos estão inválidos",
                Hint: "Corrija os campos listados em 'details.fields' e tente novamente",
                StatusCode: 400,
                Retryable: true,
                Details: new { fields = v.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage, rejectedValue = e.AttemptedValue }) },
                TraceId: traceId,
                Timestamp: timestamp),

            DomainException d => new ErrorResponse(
                ErrorCode: d.ErrorCode,
                Category: d.Category,
                Message: d.Message,
                Hint: d.Hint,
                StatusCode: d.Category switch { "NOT_FOUND" => 404, "BUSINESS_RULE" => 422, _ => 400 },
                Retryable: d.Retryable,
                Details: d.Details,
                TraceId: traceId,
                Timestamp: timestamp),

            _ => new ErrorResponse(
                ErrorCode: "INTERNAL_ERROR",
                Category: "INTERNAL",
                Message: "Ocorreu um erro interno no servidor",
                Hint: "Tente novamente em alguns instantes. Se persistir, contate o suporte com o traceId",
                StatusCode: 500,
                Retryable: false,
                Details: null,
                TraceId: traceId,
                Timestamp: timestamp)
        };

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = response.StatusCode;
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

`ErrorResponse` em `Dtos/`:

```csharp
public record ErrorResponse(
    string ErrorCode,
    string Category,
    string Message,
    string? Hint,
    int StatusCode,
    bool Retryable,
    object? Details,
    string TraceId,
    string Timestamp
);
```

---

## Infrastructure (`Infra/`)

```csharp
namespace Inventory.Api.Infra;

public interface IDbConnectionFactory
{
    IDbConnection Create();
}

public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _config;
    public DbConnectionFactory(IConfiguration config) => _config = config;
    public IDbConnection Create() => new NpgsqlConnection(_config.GetConnectionString("Postgres"));
}

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}
```

Connection string vem do `appsettings.json` chave `ConnectionStrings:Postgres`. Em docker-compose, sobrescrever via env var `ConnectionStrings__Postgres`.

---

## DI Registration (`Program.cs`)

Ordem fixa:

```csharp
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Inventory.Api.Infra;
using Inventory.Api.Middleware;
using Inventory.Api.Repositories;
using Inventory.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// JSON: enums como string
builder.Services.AddControllers().AddJsonOptions(opts =>
{
    opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Swagger rico
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
    c.EnableAnnotations();
});

// Infrastructure — Singleton
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

// Repositories — Scoped (interface + impl)
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IStockMovementRepository, StockMovementRepository>();

// Services — Scoped (concrete, sem interface)
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<StockMovementService>();

// CORS pro frontend
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173")
     .AllowAnyMethod()
     .AllowAnyHeader()));

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
```

Também adicionar no `.csproj`:

```xml
<PropertyGroup>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```

| Lifetime | Quando |
|----------|--------|
| Singleton | `IDbConnectionFactory`, `IConfiguration` (já é) |
| Scoped | Repositories, Services |
| Transient | Validators (default do FluentValidation auto-validation) |

---

## Database Schema (`init.sql`)

Schema versionado em um único arquivo `init.sql` na raiz do repo, montado no container Postgres via `docker-compose.yml`. NÃO usar Migrations.

```sql
CREATE TABLE IF NOT EXISTS products (
    id              uuid PRIMARY KEY,
    code            varchar(50) NOT NULL UNIQUE,
    description     varchar(200) NOT NULL,
    type            int NOT NULL,
    supplier_value  numeric(18,2) NOT NULL DEFAULT 0 CHECK (supplier_value >= 0),
    stock_quantity  int NOT NULL DEFAULT 0 CHECK (stock_quantity >= 0),
    deleted_at      timestamptz NULL
);

CREATE INDEX IF NOT EXISTS idx_products_active_code
    ON products (code) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS stock_movements (
    id              uuid PRIMARY KEY,
    product_id      uuid NOT NULL REFERENCES products(id),
    type            int NOT NULL,
    quantity        int NOT NULL CHECK (quantity > 0),
    sale_value      numeric(18,2) NULL CHECK (sale_value IS NULL OR sale_value >= 0),
    supplier_value  numeric(18,2) NULL CHECK (supplier_value IS NULL OR supplier_value >= 0),
    idempotency_key uuid NOT NULL UNIQUE,
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_movements_product_created
    ON stock_movements (product_id, created_at DESC);
```

- Enums armazenados como `int` (ordinal do C# enum). Conversão automática do Dapper.
- `numeric(18,2)` para `decimal`.
- `timestamptz` (timestamp with time zone) sempre — armazenar UTC.
- `deleted_at NULL` = ativo; `NOT NULL` = soft-deleted.
- `idempotency_key NOT NULL UNIQUE` — required em todo INSERT de movimento.

---

## Agentic-Friendly API Design

A API é projetada para ser consumida por LLMs como tools. Cada decisão abaixo é deliberada — não inventar nada fora deste catálogo.

### 1. Enums como string no JSON

`JsonStringEnumConverter` global (configurado no `Program.cs` acima). Resposta vira `"type": "Electronic"` em vez de `"type": 0`. LLM e humano leem melhor.

### 2. OperationIds estáveis (viram tool names no LLM)

Via `[SwaggerOperation(OperationId = "verbNoun")]` em todo endpoint:

| operationId | Endpoint |
|-------------|----------|
| `createProduct` | POST /api/products |
| `listProducts` | GET /api/products |
| `getProduct` | GET /api/products/{id} |
| `deleteProduct` | DELETE /api/products/{id} |
| `registerStockMovement` | POST /api/stock-movements |
| `listStockMovements` | GET /api/stock-movements |
| `getStockMovement` | GET /api/stock-movements/{id} |

camelCase verb-noun. Estáveis — **não renomear depois**.

### 3. XML docs em tudo → OpenAPI rico

- `<summary>` em controllers (Tag description)
- `<summary>` em actions (Operation summary)
- `<remarks>` em actions (Operation description)
- `<summary>` em **cada campo de cada DTO** (Property description)

Habilitado via `<GenerateDocumentationFile>true</GenerateDocumentationFile>` no `.csproj` + `c.IncludeXmlComments(...)` no SwaggerGen.

### 4. ErrorResponse canônico (TODO erro segue este shape)

```json
{
  "errorCode": "INSUFFICIENT_BALANCE",
  "category": "BUSINESS_RULE",
  "message": "Saldo insuficiente: solicitado 10 unidades, disponível 3",
  "hint": "Reduza a quantidade para no máximo 3 ou registre uma entrada de estoque antes",
  "statusCode": 422,
  "retryable": false,
  "details": {
    "productId": "8f3a1b2c-...",
    "productCode": "P001",
    "requested": 10,
    "available": 3,
    "deficit": 7
  },
  "traceId": "01HVQ2K8X3M4N5P6Q7R8S9T0",
  "timestamp": "2026-05-16T14:32:11.0000000Z"
}
```

Campos obrigatórios em **toda** resposta de erro:
- `errorCode` (slug SCREAMING_SNAKE_CASE estável, vocabulário fechado)
- `category` (`VALIDATION` | `BUSINESS_RULE` | `NOT_FOUND` | `INTERNAL`)
- `message` (texto humano em PT)
- `statusCode` (HTTP status duplicado no body)
- `retryable` (bool — pode tentar de novo igual?)
- `traceId` (do `HttpContext.TraceIdentifier`)
- `timestamp` (ISO 8601 UTC)

Campos opcionais:
- `hint` (string PT acionável — preencher SEMPRE que possível)
- `details` (objeto estruturado com contexto, valores, IDs)

### 5. Hints dinâmicos por contexto

Service constrói a hint com dados reais ao throw — **NUNCA hardcoded genérico**:

```csharp
// ❌ ERRADO (genérico, inútil):
throw new BusinessRuleException("INSUFFICIENT_BALANCE", "Saldo insuficiente", hint: "Reduza a quantidade");

// ✅ CERTO (dinâmico, acionável):
throw new BusinessRuleException(
    errorCode: "INSUFFICIENT_BALANCE",
    message: $"Saldo insuficiente: solicitado {request.Quantity}, disponível {product.StockQuantity}",
    hint: $"Reduza a quantidade para no máximo {product.StockQuantity} ou registre uma entrada antes",
    details: new { requested = request.Quantity, available = product.StockQuantity });
```

### 6. `_links` em responses de recursos

Pra LLM descobrir próximas ações sem novo Swagger lookup:

```json
{
  "id": "8f3a1b2c-...",
  "code": "P001",
  "_links": {
    "self": "/api/products/8f3a1b2c-...",
    "movements": "/api/stock-movements?productId=8f3a1b2c-..."
  }
}
```

Implementado como `Dictionary<string, string>? _links` no DTO de response. Não obrigatório em todas as respostas — só onde adiciona valor.

### 7. Idempotency-Key required em mutations sensíveis

`POST /api/stock-movements` exige header `Idempotency-Key: <uuid>` (UUID v4):

- Ausente → `400 MISSING_IDEMPOTENCY_KEY` com hint "Gere um UUID v4 e envie no header"
- Presente + já existe movimento com esse key → retorna o existente com `Idempotency-Replay: true` header + status `200 OK` (não `201`)
- Presente + novo → cria normal, retorna `201 Created`

Coluna `stock_movements.idempotency_key uuid NOT NULL UNIQUE` no schema.

`POST /api/products` NÃO exige Idempotency-Key — `code` UNIQUE já dá idempotência natural por domínio.

### 8. Catálogo de errorCodes (vocabulário fechado)

Ver seção "Exceptions" acima. **Adicionar novo errorCode requer atualização do catálogo.**

---

## Pagination

**Toda listagem é paginada.** Sem exceção.

### Query params (canônicos)

```
GET /api/products?page=1&pageSize=30
GET /api/stock-movements?page=1&pageSize=30&productId=uuid&startDate=2026-01-01&endDate=2026-12-31
```

- `page`: 1-indexed, default `1`, min `1`
- `pageSize`: default `30`, min `1`, max `100`
- Validados via `PaginationValidator` (FluentValidation) — fora desses limites → `400 VALIDATION_ERROR`

### Response envelope

```json
{
  "items": [...],
  "pagination": {
    "page": 1,
    "pageSize": 30,
    "total": 245,
    "totalPages": 9,
    "hasNext": true,
    "hasPrev": false
  },
  "_links": {
    "self": "/api/products?page=1&pageSize=30",
    "next": "/api/products?page=2&pageSize=30",
    "first": "/api/products?page=1&pageSize=30",
    "last": "/api/products?page=9&pageSize=30"
  }
}
```

### Implementação Dapper

```csharp
public async Task<PagedResult<Product>> ListAsync(int page, int pageSize, bool includeDeleted)
{
    var offset = (page - 1) * pageSize;
    // 1 query para items, 1 query para count (NÃO N+1 — duas queries fixas)
    var items = await conn.QueryAsync<Product>("... LIMIT @pageSize OFFSET @offset", new { pageSize, offset });
    var total = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ...");
    return new PagedResult<Product>(items.ToList(), page, pageSize, total);
}
```

---

## Soft Delete (apenas em Products)

Movimentos **nunca** são deletados (histórico imutável). Produtos podem ser soft-deleted via `deleted_at`.

### Regras

- Coluna `products.deleted_at timestamptz NULL` no schema.
- `DELETE /api/products/{id}` → `UPDATE products SET deleted_at = now() WHERE id = @id AND deleted_at IS NULL`.
- `GET /api/products` filtra `WHERE deleted_at IS NULL` por padrão.
- `GET /api/products?includeDeleted=true` retorna **todos** (útil pra LLM/admin).
- `GET /api/products/{id}` retorna mesmo se deletado (com `deletedAt` no body).
- Service `StockMovementService.RegisterAsync` rejeita se produto está deletado: `BusinessRuleException("PRODUCT_DELETED", ...)`.
- Histórico de movimentos de produto deletado **continua acessível** (`GET /api/stock-movements?productId=X` funciona).
- Re-cadastrar produto com código já existente em produto soft-deleted → `DUPLICATE_CODE` (UNIQUE constraint sobre `code` sem filtro de `deleted_at` previne reuso de código).

---

## Zero N+1 Queries — Regra Estrita

**Proibido** loop que faz query por item:

```csharp
// ❌ NUNCA:
foreach (var m in movements)
{
    var p = await _products.GetByIdAsync(m.ProductId);
    result.Add(new MovementHistoryItem(m, p.Code, p.Description));
}
```

**Sempre** uma das três alternativas:

### Alternativa 1: JOIN (preferida)

```sql
SELECT m.id, m.product_id as ProductId, m.type, m.quantity, m.created_at as CreatedAt,
       p.code as ProductCode, p.description as ProductDescription
FROM stock_movements m
INNER JOIN products p ON p.id = m.product_id
WHERE m.created_at >= @start AND m.created_at < @end
ORDER BY m.created_at DESC
LIMIT @pageSize OFFSET @offset
```

### Alternativa 2: Batch fetch (quando JOIN não couber)

```csharp
var movements = await _movements.ListAsync(filter);
var productIds = movements.Select(m => m.ProductId).Distinct().ToList();
var products = (await _products.GetByIdsAsync(productIds))  // WHERE id IN @ids
    .ToDictionary(p => p.Id);
return movements.Select(m => new HistoryItem(m, products[m.ProductId])).ToList();
```

### Alternativa 3: Subquery / window function (casos avançados)

Para agregações ou rankings — usar SQL nativo, não loop.

**Observação:** BancoShu tem N+1 em `TransferService.GetHistoryAsync` (linha que busca conta dentro do foreach). Esse é exatamente o anti-pattern a evitar.

---

## Testing

xUnit + Moq. Estrutura espelha o projeto principal.

**Naming:** `Method_Should_Behavior_When_Condition`.

```csharp
namespace Inventory.Tests.Services;

public class StockMovementServiceTests
{
    private readonly Mock<IDbConnectionFactory> _factory = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IStockMovementRepository> _movements = new();
    private readonly StockMovementService _service;

    public StockMovementServiceTests()
    {
        var conn = new Mock<IDbConnection>();
        var tx = new Mock<IDbTransaction>();
        _factory.Setup(f => f.Create()).Returns(conn.Object);
        conn.Setup(c => c.BeginTransaction()).Returns(tx.Object);

        _service = new StockMovementService(_factory.Object, _products.Object, _movements.Object);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_INSUFFICIENT_BALANCE_When_Outbound_Exceeds_Stock()
    {
        var product = new Product { Id = Guid.NewGuid(), Code = "P001", StockQuantity = 3 };
        _products.Setup(p => p.GetByIdAsync(product.Id, It.IsAny<IDbTransaction>(), true))
                 .ReturnsAsync(product);

        var request = new CreateMovementRequest(product.Id, MovementType.Outbound, 10, 100m, null);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.RegisterAsync(request, Guid.NewGuid()));

        Assert.Equal("INSUFFICIENT_BALANCE", ex.ErrorCode);
        Assert.Contains("solicitado 10", ex.Message);
        Assert.Contains("disponível 3", ex.Message);
        Assert.NotNull(ex.Hint);
        Assert.Contains("máximo 3", ex.Hint);
    }

    [Fact]
    public async Task RegisterAsync_Should_Throw_PRODUCT_DELETED_When_Product_Is_Soft_Deleted()
    {
        var product = new Product { Id = Guid.NewGuid(), Code = "P001", DeletedAt = DateTime.UtcNow };
        _products.Setup(p => p.GetByIdAsync(product.Id, It.IsAny<IDbTransaction>(), true))
                 .ReturnsAsync(product);

        var request = new CreateMovementRequest(product.Id, MovementType.Outbound, 1, 100m, null);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _service.RegisterAsync(request, Guid.NewGuid()));

        Assert.Equal("PRODUCT_DELETED", ex.ErrorCode);
    }
}
```

Regras:
- Mock dependencies no construtor da classe de teste.
- Um `[Fact]` por cenário (não vários asserts pra cenários diferentes na mesma função).
- **Sempre** assertar `ex.ErrorCode` em testes de exception (não só o tipo).
- **Sempre** assertar que `ex.Hint` está preenchida em `BusinessRuleException`.
- Cobrir: happy path + cada errorCode possível + idempotency replay.

---

## API Behavior — Contract Summary

| Situação | HTTP | Body |
|----------|------|------|
| GET sucesso | 200 OK | resource ou `{ items, pagination, _links }` |
| POST sucesso (criou) | 201 Created + `Location` header | resource criado |
| POST replay idempotente | 200 OK + `Idempotency-Replay: true` header | resource existente |
| DELETE sucesso | 204 No Content | (vazio) |
| Validação falhou | 400 | ErrorResponse com `errorCode: VALIDATION_ERROR` + `details.fields[]` |
| Idempotency-Key ausente | 400 | ErrorResponse com `MISSING_IDEMPOTENCY_KEY` |
| Recurso não encontrado | 404 | ErrorResponse com `*_NOT_FOUND` |
| Regra de negócio violada | 422 | ErrorResponse com errorCode específico (`INSUFFICIENT_BALANCE`, `PRODUCT_DELETED`, `DUPLICATE_CODE`, ...) |
| Bug não tratado | 500 | ErrorResponse com `INTERNAL_ERROR` (sem stack trace no body) |

---

## Don'ts

- ❌ Entity Framework Core
- ❌ AutoMapper / Mapster / extension methods de mapping
- ❌ Interface pra cada Service
- ❌ MediatR / CQRS / Vertical Slices
- ❌ Repository Generic `IRepository<T>`
- ❌ Try/catch em Controllers
- ❌ Lógica de negócio em Controller ou Entity
- ❌ `async void` (sempre `Task` / `Task<T>`)
- ❌ `.Result` / `.Wait()` (sempre `await`)
- ❌ Logger em Controller
- ❌ Hardcoded connection strings
- ❌ Identifiers em português
- ❌ `DateTime.Now` (sempre `UtcNow`)
- ❌ `double`/`float` em dinheiro
- ❌ **N+1 queries** (JOIN ou batch fetch)
- ❌ **Hints genéricos** (sempre dinâmicos com dados reais)
- ❌ **errorCode fora do catálogo** (atualizar catálogo primeiro)
- ❌ **Enum como int no JSON** (`JsonStringEnumConverter` global)
- ❌ Hard delete de produtos (sempre soft via `deleted_at`)
- ❌ Modificar/deletar movimentos registrados (histórico imutável)
- ❌ Listagem sem paginação
- ❌ DTO/Controller sem XML doc
- ❌ Endpoint sem `[SwaggerOperation(OperationId=...)]`
- ❌ Endpoint sem `[ProducesResponseType]` exaustivo
- ❌ POST sem idempotency em mutation sensível

---

## Reference Project

Quando em dúvida sobre QUALQUER pattern não coberto acima, ler o equivalente em:
`/home/thallysrc/Projects/BancoShu/BancoShu/`

A regra é: **espelhar BancoShu**, exceto o N+1 em `TransferService.GetHistoryAsync`. Não inventar.
