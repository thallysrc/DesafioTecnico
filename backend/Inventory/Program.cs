using System.Reflection;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Inventory.Api.Infra;
using Inventory.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------------------------------------------
// CORS — named policy "Frontend" (D-09 / INFRA-05). Origin overridable via Cors:AllowedOrigins config
// (env var Cors__AllowedOrigins__0 in docker-compose for future).
// -------------------------------------------------------------------------------------------------
const string CorsPolicyName = "Frontend";
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// -------------------------------------------------------------------------------------------------
// MVC + JSON — enums as strings (AGENT-01 lands in Phase 2 but the global converter is wired here so
// every Phase 2 controller automatically gets the right serialization).
// -------------------------------------------------------------------------------------------------
builder.Services
    .AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// -------------------------------------------------------------------------------------------------
// FluentValidation — auto-validation enabled, validator discovery from current assembly.
// Phase 1 ships ZERO validators; Phase 2 adds CreateProductValidator etc. and they're picked up
// automatically without further wiring.
// -------------------------------------------------------------------------------------------------
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// -------------------------------------------------------------------------------------------------
// Swagger — XML docs + Swashbuckle.Annotations (INFRA-07 + AGENT-02..05 prep).
// -------------------------------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "StockEasy API",
        Version = "v1",
        Description = "Inventory + stock-movements management API for the StockEasy challenge. " +
                      "Agentic-friendly: stable operationIds, structured errorCodes, dynamic hints, " +
                      "Idempotency-Key on mutations."
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    c.EnableAnnotations();
});

// -------------------------------------------------------------------------------------------------
// Infrastructure — singletons.
// -------------------------------------------------------------------------------------------------
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

// -------------------------------------------------------------------------------------------------
// Repositories — Scoped (interface + impl per BACK-08).
// -------------------------------------------------------------------------------------------------
builder.Services.AddScoped<Inventory.Api.Repositories.IProductRepository, Inventory.Api.Repositories.ProductRepository>();

// -------------------------------------------------------------------------------------------------
// Services — Scoped (concrete classes, no interface per BACK-08).
// -------------------------------------------------------------------------------------------------
builder.Services.AddScoped<Inventory.Api.Services.ProductService>();

var app = builder.Build();

// -------------------------------------------------------------------------------------------------
// Pipeline — ExceptionHandling FIRST (catches everything downstream), then CORS, then routing.
// No HTTPS redirect (we serve plain HTTP on :8080 in dev), no Authorization (no auth in v1).
// -------------------------------------------------------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors(CorsPolicyName);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "StockEasy API v1");
        c.DocumentTitle = "StockEasy API — Swagger";
    });
}

app.MapControllers();

app.Run();

/// <summary>
/// Marker for <see cref="FluentValidation.AspNetCore.FluentValidationMvcExtensions"/> assembly scanning
/// and integration tests using <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program { }
