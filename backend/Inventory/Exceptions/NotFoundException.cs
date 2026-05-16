namespace Inventory.Api.Exceptions;

/// <summary>
/// Base class for "resource X by id Y was not found" errors. Middleware maps to HTTP 404.
/// Concrete subclasses (e.g. <c>ProductNotFoundException</c>) set their own
/// <see cref="DomainException.ErrorCode"/> and build dynamic hints in their constructors.
/// </summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string errorCode, string message, string? hint = null, object? details = null)
        : base(message, errorCode, category: "NOT_FOUND", hint, retryable: false, details) { }
}
