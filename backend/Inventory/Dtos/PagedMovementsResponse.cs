using System.Text.Json.Serialization;

namespace Inventory.Api.Dtos;

/// <summary>Paginated list of stock movements with pagination metadata and HATEOAS links.</summary>
/// <param name="Items">Page of movements (enriched with productCode/productDescription via JOIN).</param>
/// <param name="Pagination">Pagination metadata.</param>
/// <param name="Links">HATEOAS rel dictionary — <c>self</c>, <c>first</c>, <c>last</c>, <c>next</c>, <c>prev</c>. Serialized as <c>_links</c> on the wire.</param>
public record PagedMovementsResponse(
    IReadOnlyList<MovementResponse> Items,
    PaginationMeta Pagination,
    [property: JsonPropertyName("_links")]
    Dictionary<string, string> Links
);
