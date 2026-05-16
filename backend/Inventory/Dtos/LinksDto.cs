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
}
