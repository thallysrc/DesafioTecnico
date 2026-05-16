using System.Text.Json.Serialization;
using Inventory.Api.Entities;

namespace Inventory.Api.Dtos;

/// <summary>Public representation of a stock movement, enriched with product identity from a JOIN (MOVE-09 zero N+1).</summary>
/// <param name="Id">Server-generated UUID v4.</param>
/// <param name="ProductId">UUID of the moved product.</param>
/// <param name="ProductCode">Snapshot of <c>products.code</c> at read time.</param>
/// <param name="ProductDescription">Snapshot of <c>products.description</c> at read time.</param>
/// <param name="Type">Movement direction (serialized as <c>"Inbound"</c> / <c>"Outbound"</c>).</param>
/// <param name="Quantity">Units moved.</param>
/// <param name="SupplierValue">Per-unit supplier cost. Populated on Inbound; null on Outbound.</param>
/// <param name="SaleValue">Per-unit sale price. Populated on Outbound; null on Inbound.</param>
/// <param name="IdempotencyKey">UUID that uniquely identifies this movement for replay protection.</param>
/// <param name="OccurredAt">Business-time UTC timestamp.</param>
/// <param name="CreatedAt">Insertion UTC timestamp.</param>
/// <param name="Links">HATEOAS rel dictionary — <c>self</c> + <c>product</c>. Serialized as <c>_links</c> on the wire.</param>
public record MovementResponse(
    Guid Id,
    Guid ProductId,
    string ProductCode,
    string ProductDescription,
    MovementType Type,
    int Quantity,
    decimal? SupplierValue,
    decimal? SaleValue,
    Guid IdempotencyKey,
    DateTime OccurredAt,
    DateTime CreatedAt,
    [property: JsonPropertyName("_links")]
    Dictionary<string, string> Links
);
