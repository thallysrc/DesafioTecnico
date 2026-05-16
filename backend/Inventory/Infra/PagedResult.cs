namespace Inventory.Api.Infra;

/// <summary>
/// Canonical paginated result envelope used by every listing repository.
/// Phase 2 wires this into the API response shape <c>{ items, pagination, _links }</c>.
/// </summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}
