namespace Inventory.Api.Exceptions;

/// <summary>
/// Thrown when a product create attempt collides with an existing <c>products.code</c>
/// (Postgres UNIQUE constraint violation — SQLState 23505). Mapped to HTTP 422 with
/// <c>errorCode: DUPLICATE_CODE</c>.
///
/// Hint per CONTEXT.md D-10 references the deleted-product recovery path: codes are NOT
/// reusable even after soft delete (init.sql enforces this via a full UNIQUE without a
/// <c>deleted_at IS NULL</c> filter).
/// </summary>
public class DuplicateCodeException : BusinessRuleException
{
    /// <summary>
    /// Build a DUPLICATE_CODE exception with the conflicting <paramref name="code"/>. When the
    /// existing product's id is known (read back from a follow-up SELECT), pass <paramref name="existingProductId"/>
    /// so the hint links directly to the existing record. When unknown, hint links to the listing.
    /// </summary>
    public DuplicateCodeException(string code, Guid? existingProductId = null)
        : base(
            errorCode: "DUPLICATE_CODE",
            message: $"Já existe um produto com código '{code}'",
            hint: BuildHint(code, existingProductId),
            details: new { code, existingProductId },
            retryable: false)
    { }

    private static string BuildHint(string code, Guid? existingProductId)
    {
        if (existingProductId is { } id)
        {
            return $"Já existe um produto com código '{code}'. Use outro código ou recupere o produto deletado em /api/products/{id}.";
        }
        return $"Já existe um produto com código '{code}'. Use outro código ou recupere o produto via /api/products?includeDeleted=true.";
    }
}
