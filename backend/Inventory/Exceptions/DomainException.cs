namespace Inventory.Api.Exceptions;

/// <summary>
/// Base class for all business-domain exceptions. Subclasses populate the catalog of
/// closed-vocabulary <see cref="ErrorCode"/> values that the <c>ExceptionHandlingMiddleware</c>
/// maps to <c>ErrorResponse</c> per backend/CLAUDE.md §"Catálogo fechado de errorCodes".
///
/// Hints (<see cref="Hint"/>) MUST be constructed in the concrete subclass's constructor
/// using real context (per CONTEXT.md D-10 / D-08 / AGENT-08). Never throw a DomainException
/// with a hardcoded generic hint string.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>Closed-vocabulary slug in SCREAMING_SNAKE_CASE (e.g. <c>DUPLICATE_CODE</c>).</summary>
    public string ErrorCode { get; }

    /// <summary>One of <c>VALIDATION</c>, <c>BUSINESS_RULE</c>, <c>NOT_FOUND</c>, <c>INTERNAL</c>.</summary>
    public string Category { get; }

    /// <summary>Optional actionable PT-BR hint built from real context. Should rarely be null for typed subclasses.</summary>
    public string? Hint { get; }

    /// <summary>Whether the same request can succeed if retried as-is. Defaults to false.</summary>
    public bool Retryable { get; }

    /// <summary>Optional structured payload that surfaces in <c>ErrorResponse.Details</c> (IDs, conflicting values, etc.).</summary>
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
