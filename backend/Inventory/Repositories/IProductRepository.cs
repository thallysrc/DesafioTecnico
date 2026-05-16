using Inventory.Api.Entities;
using Inventory.Api.Infra;

namespace Inventory.Api.Repositories;

/// <summary>
/// Persistence contract for the <c>products</c> table. All operations use Dapper with inline SQL;
/// snake_case columns are aliased to PascalCase to match <see cref="Product"/>'s setters
/// (no <c>DefaultTypeMap.MatchNamesWithUnderscores</c> — aliases are explicit per backend/CLAUDE.md).
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Fetch a single product by id. Returns null when not found. Returns even when soft-deleted
    /// so the caller can decide whether to surface it (e.g. detail view).
    /// </summary>
    Task<Product?> GetByIdAsync(Guid id);

    /// <summary>
    /// Paginated list, ordered by <c>created_at DESC, id DESC</c> (stable cursor per CONTEXT.md D-04).
    /// When <paramref name="includeDeleted"/> is false, filters <c>WHERE deleted_at IS NULL</c>.
    /// Emits exactly two SQL statements per call (SELECT items + SELECT COUNT) — N+1 free.
    /// </summary>
    Task<PagedResult<Product>> ListAsync(int page, int pageSize, bool includeDeleted);

    /// <summary>
    /// Insert a new product. Server-generated UUID is read back on the returned <see cref="Product"/>.
    /// Throws <c>Npgsql.PostgresException</c> with SQLState <c>23505</c> when <c>code</c> already exists
    /// (caller maps to <c>DuplicateCodeException</c> in Plan 02-04).
    /// </summary>
    Task<Product> CreateAsync(Product product);

    /// <summary>
    /// Soft-delete: <c>UPDATE products SET deleted_at = now(), updated_at = now() WHERE id = @id AND deleted_at IS NULL</c>.
    /// Returns affected row count (0 when product is missing or already deleted — idempotent).
    /// </summary>
    Task<int> SoftDeleteAsync(Guid id);
}
