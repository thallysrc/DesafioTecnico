namespace Inventory.Api.Exceptions;

/// <summary>
/// Thrown when an operation targets a product id that doesn't exist in the database.
/// Mapped to HTTP 404 with <c>errorCode: PRODUCT_NOT_FOUND</c>. Soft-deleted products
/// are NOT considered "not found" by detail endpoints — they are returned with
/// <c>deletedAt</c> populated. This exception is only raised when the id is absent entirely.
/// </summary>
public class ProductNotFoundException : NotFoundException
{
    public ProductNotFoundException(Guid productId)
        : base(
            errorCode: "PRODUCT_NOT_FOUND",
            message: $"Produto não encontrado",
            hint: "Produto não encontrado. Atualize a lista e tente novamente.",
            details: new { productId })
    { }
}
