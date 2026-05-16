using System.Data;
using Dapper;
using Inventory.Api.Entities;
using Inventory.Api.Infra;

namespace Inventory.Api.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IStockMovementRepository"/>. JOIN'd column list is reused across
/// detail / idempotency-lookup / list to keep one shape and guarantee MOVE-09 (zero N+1).
/// </summary>
public class StockMovementRepository : IStockMovementRepository
{
    private readonly IDbConnectionFactory _factory;

    public StockMovementRepository(IDbConnectionFactory factory) => _factory = factory;

    // Aliases bind snake_case columns to StockMovementWithProduct's PascalCase setters.
    // ProductCode + ProductDescription come from the JOIN — see FromJoin below.
    private const string SelectColumns = @"
        m.id              as Id,
        m.product_id      as ProductId,
        m.type            as Type,
        m.quantity        as Quantity,
        m.sale_value      as SaleValue,
        m.supplier_value  as SupplierValue,
        m.idempotency_key as IdempotencyKey,
        m.occurred_at     as OccurredAt,
        m.created_at      as CreatedAt,
        p.code            as ProductCode,
        p.description     as ProductDescription";

    private const string FromJoin = @"
        FROM stock_movements m
        INNER JOIN products p ON p.id = m.product_id";

    public async Task<StockMovement> InsertAsync(StockMovement movement, IDbTransaction tx)
    {
        const string sql = @"
            INSERT INTO stock_movements (id, product_id, type, quantity, sale_value, supplier_value, idempotency_key, occurred_at, created_at)
            VALUES (
                COALESCE(NULLIF(@Id, '00000000-0000-0000-0000-000000000000'::uuid), gen_random_uuid()),
                @ProductId, @Type, @Quantity, @SaleValue, @SupplierValue, @IdempotencyKey, now(), now()
            )
            RETURNING
                id              as Id,
                product_id      as ProductId,
                type            as Type,
                quantity        as Quantity,
                sale_value      as SaleValue,
                supplier_value  as SupplierValue,
                idempotency_key as IdempotencyKey,
                occurred_at     as OccurredAt,
                created_at      as CreatedAt";

        return await tx.Connection!.QuerySingleAsync<StockMovement>(sql, new
        {
            movement.Id,
            movement.ProductId,
            Type = (int)movement.Type,
            movement.Quantity,
            movement.SaleValue,
            movement.SupplierValue,
            movement.IdempotencyKey
        }, tx);
    }

    public async Task<StockMovementWithProduct?> GetByIdAsync(Guid id)
    {
        var sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE m.id = @id";

        using var conn = _factory.Create();
        return await conn.QuerySingleOrDefaultAsync<StockMovementWithProduct>(sql, new { id });
    }

    public async Task<StockMovementWithProduct?> GetByIdempotencyKeyAsync(Guid idempotencyKey)
    {
        var sql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            WHERE m.idempotency_key = @idempotencyKey";

        using var conn = _factory.Create();
        return await conn.QuerySingleOrDefaultAsync<StockMovementWithProduct>(sql, new { idempotencyKey });
    }

    public async Task<PagedResult<StockMovementWithProduct>> ListAsync(
        Guid? productId,
        DateTime? startDate,
        DateTime? endDate,
        int page,
        int pageSize)
    {
        var offset = (page - 1) * pageSize;

        // Filters use parameterized "X IS NULL OR column op @X" so a single SQL works for every combo.
        const string whereClause = @"
            WHERE (@productId::uuid IS NULL OR m.product_id = @productId)
              AND (@startDate::timestamptz IS NULL OR m.occurred_at >= @startDate)
              AND (@endDate::timestamptz IS NULL OR m.occurred_at <= @endDate)";

        var itemsSql = $@"
            SELECT {SelectColumns}
            {FromJoin}
            {whereClause}
            ORDER BY m.occurred_at DESC, m.id DESC
            LIMIT @pageSize OFFSET @offset";

        var countSql = $@"
            SELECT COUNT(*)
            {FromJoin}
            {whereClause}";

        using var conn = _factory.Create();
        var args = new { productId, startDate, endDate, pageSize, offset };
        var items = (await conn.QueryAsync<StockMovementWithProduct>(itemsSql, args)).ToList();
        var total = await conn.ExecuteScalarAsync<int>(countSql, args);

        return new PagedResult<StockMovementWithProduct>(items, page, pageSize, total);
    }
}
