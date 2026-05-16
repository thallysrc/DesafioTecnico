using System.Text.Json;
using FluentValidation;
using Inventory.Api.Dtos;
using Inventory.Api.Exceptions;

namespace Inventory.Api.Middleware;

/// <summary>
/// Maps every unhandled exception to a canonical <see cref="ErrorResponse"/> per
/// backend/CLAUDE.md §"Middleware". Pipeline ordering keeps this middleware first
/// so it catches controller exceptions + CORS errors + routing errors uniformly.
///
/// Mapping:
/// <list type="bullet">
///   <item><description><see cref="ValidationException"/> → 400 <c>VALIDATION_ERROR</c> with <c>details.fields[]</c></description></item>
///   <item><description><see cref="DomainException"/> → status from <see cref="DomainException.Category"/> (404 NOT_FOUND / 422 BUSINESS_RULE / 400 VALIDATION / 500 fallback)</description></item>
///   <item><description>Any other <see cref="Exception"/> → 500 <c>INTERNAL_ERROR</c></description></item>
///   <item><description>Phase 3 additions (<see cref="Inventory.Api.Exceptions.MissingIdempotencyKeyException"/>, <see cref="Inventory.Api.Exceptions.InsufficientBalanceException"/>, <see cref="Inventory.Api.Exceptions.ProductDeletedException"/>, <see cref="Inventory.Api.Exceptions.InvalidMovementValuesException"/>, <see cref="Inventory.Api.Exceptions.MovementNotFoundException"/>) all flow through the <see cref="DomainException"/> arm and inherit their HTTP status from <see cref="DomainException.Category"/> — no per-type case required.</description></item>
/// </list>
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext ctx, Exception ex)
    {
        _logger.LogError(ex, "Handled exception: {Type} - {Message}", ex.GetType().Name, ex.Message);

        var traceId = ctx.TraceIdentifier;
        var timestamp = DateTime.UtcNow.ToString("O");

        var response = ex switch
        {
            ValidationException v => new ErrorResponse(
                ErrorCode: "VALIDATION_ERROR",
                Category: "VALIDATION",
                Message: "Um ou mais campos estão inválidos",
                Hint: "Corrija os campos listados em 'details.fields' e tente novamente",
                StatusCode: StatusCodes.Status400BadRequest,
                Retryable: true,
                Details: new
                {
                    fields = v.Errors.Select(e => new
                    {
                        field = ToCamelCase(e.PropertyName),
                        message = e.ErrorMessage,
                        rejectedValue = e.AttemptedValue
                    }).ToArray()
                },
                TraceId: traceId,
                Timestamp: timestamp),

            DomainException d => new ErrorResponse(
                ErrorCode: d.ErrorCode,
                Category: d.Category,
                Message: d.Message,
                Hint: d.Hint,
                StatusCode: d.Category switch
                {
                    "NOT_FOUND" => StatusCodes.Status404NotFound,
                    "BUSINESS_RULE" => StatusCodes.Status422UnprocessableEntity,
                    "VALIDATION" => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status500InternalServerError
                },
                Retryable: d.Retryable,
                Details: d.Details,
                TraceId: traceId,
                Timestamp: timestamp),

            _ => new ErrorResponse(
                ErrorCode: "INTERNAL_ERROR",
                Category: "INTERNAL",
                Message: "Ocorreu um erro interno no servidor",
                Hint: "Tente novamente em alguns instantes. Se persistir, contate o suporte com o traceId.",
                StatusCode: StatusCodes.Status500InternalServerError,
                Retryable: false,
                Details: null,
                TraceId: traceId,
                Timestamp: timestamp)
        };

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = response.StatusCode;
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    /// <summary>
    /// Lower-camelCases a FluentValidation property path (e.g. <c>Code</c> → <c>code</c>,
    /// <c>SupplierValue</c> → <c>supplierValue</c>). Phase 2 only validates single-segment
    /// properties so a first-letter lowercase is sufficient. Phase 3 may need fuller handling
    /// (nested paths) — revisit then.
    /// </summary>
    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return propertyName;
        if (char.IsLower(propertyName[0])) return propertyName;
        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}
