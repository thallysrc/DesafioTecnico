namespace Inventory.Api.Exceptions;

/// <summary>
/// Thrown when a stock movement is attempted on a product whose <c>deleted_at</c> is set.
/// Mapped to HTTP 422 with <c>errorCode: PRODUCT_DELETED</c> (MOVE-05).
/// </summary>
public class ProductDeletedException : BusinessRuleException
{
    public ProductDeletedException(Guid productId, string productCode)
        : base(
            errorCode: "PRODUCT_DELETED",
            message: $"Produto '{productCode}' foi excluído e não aceita novas movimentações",
            hint: $"Produto '{productCode}' foi excluído. Movimentos não podem ser registrados para produtos excluídos.",
            details: new { productId, productCode },
            retryable: false)
    { }
}
