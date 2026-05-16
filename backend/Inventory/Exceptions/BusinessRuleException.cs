namespace Inventory.Api.Exceptions;

/// <summary>
/// Base class for business-rule violations (uniqueness conflicts, balance constraints, lifecycle rules).
/// Middleware maps to HTTP 422. Most are NOT retryable (sending the same request again will fail
/// the same way); flip the <c>retryable</c> constructor argument to <c>true</c> only for transient
/// cases (concurrent updates etc.).
/// </summary>
public class BusinessRuleException : DomainException
{
    public BusinessRuleException(string errorCode, string message, string? hint = null, object? details = null, bool retryable = false)
        : base(message, errorCode, category: "BUSINESS_RULE", hint, retryable, details) { }
}
