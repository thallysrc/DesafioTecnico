using System.Text.Json.Serialization;

namespace Inventory.Api.Dtos;

/// <summary>Pagination envelope used by every list endpoint.</summary>
/// <param name="Page">Current page number (1-indexed).</param>
/// <param name="PageSize">Items per page (default 30, max 100).</param>
/// <param name="Total">Total matching rows across all pages.</param>
/// <param name="TotalPages">Computed: <c>ceil(Total / PageSize)</c>.</param>
/// <param name="HasNext">True when a next page exists.</param>
/// <param name="HasPrev">True when a previous page exists.</param>
public record PaginationMeta(
    int Page,
    int PageSize,
    int Total,
    int TotalPages,
    bool HasNext,
    bool HasPrev
);

/// <summary>Paginated list of products with pagination metadata and HATEOAS links.</summary>
/// <param name="Items">Page of products.</param>
/// <param name="Pagination">Pagination metadata.</param>
/// <param name="Links">HATEOAS rel dictionary — <c>self</c>, <c>first</c>, <c>last</c>, <c>next</c>, <c>prev</c>. Serialized as <c>_links</c> on the wire.</param>
public record PagedProductsResponse(
    IReadOnlyList<ProductResponse> Items,
    PaginationMeta Pagination,
    [property: JsonPropertyName("_links")]
    Dictionary<string, string> Links
);
