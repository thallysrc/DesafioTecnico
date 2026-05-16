namespace Inventory.Api.Dtos;

/// <summary>
/// Helpers that build the <c>_links</c> dictionary attached to resource and listing responses.
/// Rel names per CONTEXT.md D-12: <c>self</c>, <c>delete</c> (resources);
/// <c>self</c>, <c>first</c>, <c>last</c>, <c>next</c>, <c>prev</c> (pagination).
/// </summary>
public static class LinksFactory
{
    /// <summary>Build links for a single product resource. Omits <c>delete</c> rel when the product is soft-deleted.</summary>
    /// <param name="id">Product identifier.</param>
    /// <param name="isDeleted">True when <c>DeletedAt</c> is set; suppresses the <c>delete</c> rel.</param>
    public static Dictionary<string, string> ForProduct(Guid id, bool isDeleted)
    {
        var links = new Dictionary<string, string>
        {
            ["self"] = $"/api/products/{id}"
        };
        if (!isDeleted)
        {
            links["delete"] = $"/api/products/{id}";
        }
        return links;
    }

    /// <summary>Build links for a paginated listing of products. Includes <c>next</c>/<c>prev</c> only when applicable.</summary>
    /// <param name="page">1-indexed current page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="totalPages">Total page count.</param>
    /// <param name="includeDeleted">Mirrors the listing's <c>includeDeleted</c> query parameter so generated URLs round-trip.</param>
    public static Dictionary<string, string> ForProductsListing(int page, int pageSize, int totalPages, bool includeDeleted)
    {
        var includeFlag = includeDeleted ? "&includeDeleted=true" : string.Empty;
        var basePath = $"/api/products?pageSize={pageSize}";
        var lastPage = Math.Max(totalPages, 1);

        var links = new Dictionary<string, string>
        {
            ["self"] = $"{basePath}&page={page}{includeFlag}",
            ["first"] = $"{basePath}&page=1{includeFlag}",
            ["last"] = $"{basePath}&page={lastPage}{includeFlag}"
        };
        if (page < totalPages)
        {
            links["next"] = $"{basePath}&page={page + 1}{includeFlag}";
        }
        if (page > 1)
        {
            links["prev"] = $"{basePath}&page={page - 1}{includeFlag}";
        }
        return links;
    }

    /// <summary>Build links for a single movement resource. <c>self</c> and <c>product</c>.</summary>
    /// <param name="movementId">Movement identifier.</param>
    /// <param name="productId">Identifier of the related product.</param>
    public static Dictionary<string, string> ForMovement(Guid movementId, Guid productId) =>
        new()
        {
            ["self"] = $"/api/stock-movements/{movementId}",
            ["product"] = $"/api/products/{productId}"
        };

    /// <summary>Build links for a paginated listing of movements. Filter params are echoed so generated URLs round-trip.</summary>
    /// <param name="page">1-indexed current page.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="totalPages">Total page count.</param>
    /// <param name="productId">Optional filter to echo.</param>
    /// <param name="startDate">Optional ISO 8601 date string to echo (inclusive).</param>
    /// <param name="endDate">Optional ISO 8601 date string to echo (inclusive).</param>
    public static Dictionary<string, string> ForMovementsListing(
        int page, int pageSize, int totalPages,
        Guid? productId, string? startDate, string? endDate)
    {
        var filterParts = new List<string>();
        if (productId is { } pid) filterParts.Add($"productId={pid}");
        if (!string.IsNullOrWhiteSpace(startDate)) filterParts.Add($"startDate={startDate}");
        if (!string.IsNullOrWhiteSpace(endDate)) filterParts.Add($"endDate={endDate}");
        var filterSuffix = filterParts.Count > 0 ? "&" + string.Join("&", filterParts) : string.Empty;
        var basePath = $"/api/stock-movements?pageSize={pageSize}";
        var lastPage = Math.Max(totalPages, 1);

        var links = new Dictionary<string, string>
        {
            ["self"] = $"{basePath}&page={page}{filterSuffix}",
            ["first"] = $"{basePath}&page=1{filterSuffix}",
            ["last"] = $"{basePath}&page={lastPage}{filterSuffix}"
        };
        if (page < totalPages) links["next"] = $"{basePath}&page={page + 1}{filterSuffix}";
        if (page > 1) links["prev"] = $"{basePath}&page={page - 1}{filterSuffix}";
        return links;
    }
}
