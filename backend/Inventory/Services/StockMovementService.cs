using System.Data;
using Dapper;
using Inventory.Api.Dtos;
using Inventory.Api.Entities;
using Inventory.Api.Exceptions;
using Inventory.Api.Infra;
using Inventory.Api.Repositories;
using Npgsql;

namespace Inventory.Api.Services;

/// <summary>
/// Orchestrates stock movement registration and reads. Concrete class (no interface) per
/// backend/CLAUDE.md §"Services".
///
/// Write path (<see cref="CreateAsync"/>) is fully transactional:
/// <list type="number">
///   <item><description>Idempotency fast-path lookup BEFORE opening a transaction (avoids holding a connection in idle state).</description></item>
///   <item><description><c>BeginTransaction()</c> + <c>SELECT FOR UPDATE</c> on the product row (BACK-09 / BACK-10).</description></item>
///   <item><description>Validate: <c>deleted_at IS NULL</c>, value-field invariants (MOVE-07), balance (MOVE-04 — Outbound only).</description></item>
///   <item><description>INSERT movement; catch Postgres 23505 (unique_violation on idempotency_key) → race-window recovery: re-fetch by key, return as replay.</description></item>
///   <item><description>UPDATE product (Inbound: stock+supplier_value; Outbound: stock−).</description></item>
///   <item><description>COMMIT; rollback on any exception (using-scoped tx).</description></item>
/// </list>
///
/// Read paths (<see cref="GetByIdAsync"/>, <see cref="ListAsync"/>) delegate to the repository's
/// JOIN'd queries (MOVE-09 zero N+1 — exactly two SQL statements per history page).
///
/// Returns a <c>(MovementResponse Response, bool IsReplay)</c> tuple from <see cref="CreateAsync"/>
/// so the controller can set HTTP 200 + <c>Idempotency-Replay: true</c> on replays (per CONTEXT.md
/// D-07 and AGENT contract). New rows return 201 without the replay header.
/// </summary>
public class StockMovementService
{
    private const int DefaultPageSize = 30;
    private const int MaxPageSize = 100;
    private const string PostgresUniqueViolationSqlState = "23505";

    // Locked-read SQL — runs inside the caller's transaction so the row is FOR UPDATE-locked
    // until commit. Returns soft-deleted products so the service can throw PRODUCT_DELETED rather
    // than the generic NOT_FOUND (different errorCode + hint).
    private const string ProductSelectForUpdateSql = @"
        SELECT
            id             as Id,
            code           as Code,
            description    as Description,
            type           as Type,
            supplier_value as SupplierValue,
            stock_quantity as StockQuantity,
            deleted_at     as DeletedAt,
            created_at     as CreatedAt,
            updated_at     as UpdatedAt
        FROM products
        WHERE id = @id
        FOR UPDATE";

    private const string ProductInboundUpdateSql = @"
        UPDATE products
        SET stock_quantity = stock_quantity + @quantity,
            supplier_value = @supplierValue,
            updated_at = now()
        WHERE id = @productId";

    private const string ProductOutboundUpdateSql = @"
        UPDATE products
        SET stock_quantity = stock_quantity - @quantity,
            updated_at = now()
        WHERE id = @productId";

    private readonly IDbConnectionFactory _factory;
    private readonly IStockMovementRepository _movements;

    public StockMovementService(IDbConnectionFactory factory, IStockMovementRepository movements)
    {
        _factory = factory;
        _movements = movements;
    }

    /// <summary>
    /// Register a movement. Returns the movement plus a flag indicating whether this was an
    /// idempotency replay (200 + replay header) or a fresh insert (201, no header).
    /// </summary>
    public async Task<(MovementResponse Response, bool IsReplay)> CreateAsync(
        CreateMovementRequest request,
        Guid idempotencyKey)
    {
        // ----- 1. Idempotency fast-path (no transaction needed) -----
        var existing = await _movements.GetByIdempotencyKeyAsync(idempotencyKey);
        if (existing is not null)
        {
            return (MapToResponse(existing), IsReplay: true);
        }

        // ----- 2. Open connection + transaction -----
        using var conn = (NpgsqlConnection)_factory.Create();
        await conn.OpenAsync();
        using var tx = await conn.BeginTransactionAsync();

        try
        {
            // ----- 3. SELECT FOR UPDATE on the product (BACK-09 / BACK-10) -----
            var product = await conn.QuerySingleOrDefaultAsync<Product>(
                ProductSelectForUpdateSql,
                new { id = request.ProductId },
                transaction: tx);

            if (product is null)
            {
                throw new ProductNotFoundException(request.ProductId);
            }

            // ----- 4. Validate: deleted_at + value-field invariants + balance -----
            if (product.DeletedAt is not null)
            {
                throw new ProductDeletedException(product.Id, product.Code);
            }

            EnforceMovementValueInvariants(request);

            if (request.Type == MovementType.Outbound && product.StockQuantity < request.Quantity)
            {
                throw new InsufficientBalanceException(
                    productId: product.Id,
                    productCode: product.Code,
                    requested: request.Quantity,
                    available: product.StockQuantity);
            }

            // ----- 5. INSERT movement (catch 23505 race window) -----
            StockMovement inserted;
            try
            {
                inserted = await _movements.InsertAsync(new StockMovement
                {
                    Id = Guid.Empty, // let server generate via DEFAULT gen_random_uuid()
                    ProductId = request.ProductId,
                    Type = request.Type,
                    Quantity = request.Quantity,
                    SupplierValue = request.Type == MovementType.Inbound ? request.SupplierValue : null,
                    SaleValue = request.Type == MovementType.Outbound ? request.SaleValue : null,
                    IdempotencyKey = idempotencyKey
                    // OccurredAt + CreatedAt populated by server defaults / RETURNING
                }, tx);
            }
            catch (PostgresException pgex) when (pgex.SqlState == PostgresUniqueViolationSqlState)
            {
                // Race window: another request with the same Idempotency-Key won the INSERT.
                // Roll back our transaction explicitly, then return the winning row as a replay.
                await tx.RollbackAsync();
                var winning = await _movements.GetByIdempotencyKeyAsync(idempotencyKey);
                if (winning is null)
                {
                    // Should be impossible — the 23505 means a row exists. Surface as 500.
                    throw;
                }
                return (MapToResponse(winning), IsReplay: true);
            }

            // ----- 6. UPDATE product balance (and supplier_value on Inbound) -----
            if (request.Type == MovementType.Inbound)
            {
                await conn.ExecuteAsync(
                    ProductInboundUpdateSql,
                    new
                    {
                        quantity = request.Quantity,
                        supplierValue = request.SupplierValue!.Value,
                        productId = request.ProductId
                    },
                    transaction: tx);
            }
            else
            {
                await conn.ExecuteAsync(
                    ProductOutboundUpdateSql,
                    new
                    {
                        quantity = request.Quantity,
                        productId = request.ProductId
                    },
                    transaction: tx);
            }

            // ----- 7. COMMIT -----
            await tx.CommitAsync();

            // Compose the response — we hold the inserted row plus the locked product's code/description.
            var response = new MovementResponse(
                Id: inserted.Id,
                ProductId: inserted.ProductId,
                ProductCode: product.Code,
                ProductDescription: product.Description,
                Type: inserted.Type,
                Quantity: inserted.Quantity,
                SupplierValue: inserted.SupplierValue,
                SaleValue: inserted.SaleValue,
                IdempotencyKey: inserted.IdempotencyKey,
                OccurredAt: inserted.OccurredAt,
                CreatedAt: inserted.CreatedAt,
                Links: LinksFactory.ForMovement(inserted.Id, inserted.ProductId)
            );
            return (response, IsReplay: false);
        }
        catch
        {
            // `using` on tx ensures rollback when control leaves the scope without commit.
            throw;
        }
    }

    /// <summary>Fetch movement by id. Throws <see cref="MovementNotFoundException"/> when absent.</summary>
    public async Task<MovementResponse> GetByIdAsync(Guid id)
    {
        var row = await _movements.GetByIdAsync(id);
        if (row is null) throw new MovementNotFoundException(id);
        return MapToResponse(row);
    }

    /// <summary>List movements with optional filters. Page/pageSize clamped to safe bounds.</summary>
    public async Task<PagedMovementsResponse> ListAsync(
        Guid? productId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);

        var result = await _movements.ListAsync(productId, startDate, endDate, safePage, safePageSize);

        var items = result.Items.Select(MapToResponse).ToList();
        var meta = new PaginationMeta(
            Page: result.Page,
            PageSize: result.PageSize,
            Total: result.Total,
            TotalPages: result.TotalPages,
            HasNext: result.HasNext,
            HasPrev: result.HasPrev);

        var links = LinksFactory.ForMovementsListing(
            meta.Page, meta.PageSize, meta.TotalPages,
            productId, startDate?.ToString("O"), endDate?.ToString("O"));

        return new PagedMovementsResponse(items, meta, links);
    }

    // -- Helpers ------------------------------------------------------------------------------

    private static void EnforceMovementValueInvariants(CreateMovementRequest request)
    {
        // MOVE-07: Inbound needs SupplierValue + must NOT have SaleValue; Outbound is the mirror.
        if (request.Type == MovementType.Inbound)
        {
            if (request.SaleValue.HasValue)
                throw new InvalidMovementValuesException(MovementType.Inbound, "saleValue", "supplierValue");
            if (!request.SupplierValue.HasValue)
                throw new InvalidMovementValuesException(MovementType.Inbound, "(faltando)", "supplierValue");
        }
        else // Outbound
        {
            if (request.SupplierValue.HasValue)
                throw new InvalidMovementValuesException(MovementType.Outbound, "supplierValue", "saleValue");
            if (!request.SaleValue.HasValue)
                throw new InvalidMovementValuesException(MovementType.Outbound, "(faltando)", "saleValue");
        }
    }

    private static MovementResponse MapToResponse(StockMovementWithProduct m) =>
        new MovementResponse(
            Id: m.Id,
            ProductId: m.ProductId,
            ProductCode: m.ProductCode,
            ProductDescription: m.ProductDescription,
            Type: m.Type,
            Quantity: m.Quantity,
            SupplierValue: m.SupplierValue,
            SaleValue: m.SaleValue,
            IdempotencyKey: m.IdempotencyKey,
            OccurredAt: m.OccurredAt,
            CreatedAt: m.CreatedAt,
            Links: LinksFactory.ForMovement(m.Id, m.ProductId));
}
