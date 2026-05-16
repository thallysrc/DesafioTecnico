using System.Text.Json.Serialization;
using Inventory.Api.Entities;

namespace Inventory.Api.Dtos;

/// <summary>Public representation of a product. Returned by create, get-by-id, and as the row shape of <see cref="PagedProductsResponse"/>.</summary>
/// <param name="Id">Server-generated UUID v4.</param>
/// <param name="Code">Unique product code (mirrors <c>products.code</c>).</param>
/// <param name="Description">Free-text description.</param>
/// <param name="Type">Product category (serialized as string).</param>
/// <param name="SupplierValue">Per-unit cost paid to the supplier, in BRL.</param>
/// <param name="StockQuantity">Current on-hand quantity.</param>
/// <param name="CreatedAt">UTC timestamp when the product was created.</param>
/// <param name="UpdatedAt">UTC timestamp of the latest mutation; equal to <c>CreatedAt</c> until a write occurs.</param>
/// <param name="DeletedAt">UTC timestamp when soft-deleted; <c>null</c> while active.</param>
/// <param name="Links">HATEOAS rel dictionary — <c>self</c> + <c>delete</c> (omitted when soft-deleted). Serialized as <c>_links</c> on the wire.</param>
public record ProductResponse(
    Guid Id,
    string Code,
    string Description,
    ProductType Type,
    decimal SupplierValue,
    int StockQuantity,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt,
    [property: JsonPropertyName("_links")]
    Dictionary<string, string> Links
);
