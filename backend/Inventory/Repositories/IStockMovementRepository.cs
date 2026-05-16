using System.Data;
using Inventory.Api.Entities;
using Inventory.Api.Infra;

namespace Inventory.Api.Repositories;

/// <summary>
/// Persistence contract for the <c>stock_movements</c> table. All read paths emit
/// exactly two SQL statements per page (MOVE-09 zero N+1): one JOIN'd SELECT plus one COUNT.
///
/// Movements are append-only — there is NO update or delete (MOVE-11). The interface omits
/// those methods deliberately.
/// </summary>
public interface IStockMovementRepository
{
    /// <summary>
    /// Insert a new movement inside the caller's transaction. The caller is responsible for
    /// the surrounding <c>BeginTransaction</c> + <c>SELECT FOR UPDATE</c> on the product and
    /// the balance update on the product row. Returns the inserted entity with server-generated
    /// id + occurred_at + created_at populated.
    /// Throws <c>Npgsql.PostgresException</c> with SQLState <c>23505</c> when <c>idempotency_key</c>
    /// already exists; the caller maps that to <see cref="GetByIdempotencyKeyAsync"/> + replay header.
    /// </summary>
    Task<StockMovement> InsertAsync(StockMovement movement, IDbTransaction tx);

    /// <summary>Detail by id. Returns null when not found. NO soft-delete concept on movements.</summary>
    Task<StockMovementWithProduct?> GetByIdAsync(Guid id);

    /// <summary>
    /// Idempotency lookup. Returns the movement bound to the given key, or null when no row
    /// exists. Called by the service before INSERT (fast path) AND after catching a 23505 from
    /// INSERT (race-window recovery). Uses JOIN so the replay response carries productCode /
    /// productDescription with zero extra round-trips.
    /// </summary>
    Task<StockMovementWithProduct?> GetByIdempotencyKeyAsync(Guid idempotencyKey);

    /// <summary>
    /// Paginated history with optional filters. Date filters are INCLUSIVE on both ends.
    /// Movements whose products were soft-deleted remain visible (audit trail).
    /// Emits exactly TWO SQL statements: items SELECT (JOIN'd with products) + COUNT.
    /// </summary>
    Task<PagedResult<StockMovementWithProduct>> ListAsync(
        Guid? productId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize);
}
