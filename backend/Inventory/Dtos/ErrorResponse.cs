namespace Inventory.Api.Dtos;

/// <summary>
/// Canonical error envelope returned by every non-2xx response. Fully specified per
/// backend/CLAUDE.md §"Agentic-Friendly API Design — ErrorResponse canônico".
/// </summary>
/// <param name="ErrorCode">Closed-vocabulary slug in SCREAMING_SNAKE_CASE (e.g. <c>VALIDATION_ERROR</c>).</param>
/// <param name="Category">One of <c>VALIDATION</c>, <c>BUSINESS_RULE</c>, <c>NOT_FOUND</c>, <c>INTERNAL</c>.</param>
/// <param name="Message">Human-readable Portuguese message.</param>
/// <param name="Hint">Optional actionable hint constructed by the Service with real context.</param>
/// <param name="StatusCode">HTTP status code mirrored in the body for LLM convenience.</param>
/// <param name="Retryable">Whether the same request can succeed if retried.</param>
/// <param name="Details">Optional structured context (IDs, conflicting values, field errors).</param>
/// <param name="TraceId">Correlation id from <c>HttpContext.TraceIdentifier</c>.</param>
/// <param name="Timestamp">ISO 8601 UTC timestamp of the error.</param>
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
