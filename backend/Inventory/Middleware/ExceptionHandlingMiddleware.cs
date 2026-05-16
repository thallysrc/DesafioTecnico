using System.Text.Json;
using Inventory.Api.Dtos;

namespace Inventory.Api.Middleware;

/// <summary>
/// Phase 1 skeleton: catches any unhandled exception and emits a canonical <see cref="ErrorResponse"/>
/// with <c>errorCode: INTERNAL_ERROR</c>. Phase 2 extends this to map <c>FluentValidation.ValidationException</c>
/// to <c>VALIDATION_ERROR</c> and <c>DomainException</c> subclasses (NotFound / BusinessRule) to their
/// respective status codes — see backend/CLAUDE.md §"Middleware".
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
        _logger.LogError(ex, "Unhandled exception: {Type} - {Message}", ex.GetType().Name, ex.Message);

        var response = new ErrorResponse(
            ErrorCode: "INTERNAL_ERROR",
            Category: "INTERNAL",
            Message: "Ocorreu um erro interno no servidor",
            Hint: "Tente novamente em alguns instantes. Se persistir, contate o suporte com o traceId.",
            StatusCode: StatusCodes.Status500InternalServerError,
            Retryable: false,
            Details: null,
            TraceId: ctx.TraceIdentifier,
            Timestamp: DateTime.UtcNow.ToString("O")
        );

        ctx.Response.ContentType = "application/json";
        ctx.Response.StatusCode = response.StatusCode;
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
