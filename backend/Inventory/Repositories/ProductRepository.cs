using Dapper;
using Inventory.Api.Entities;
using Inventory.Api.Infra;

namespace Inventory.Api.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IProductRepository"/>. Uses inline SQL with explicit
/// snake_case → PascalCase column aliases. Zero N+1 (ListAsync emits exactly two statements).
/// </summary>
public class ProductRepository : IProductRepository
{
    private readonly IDbConnectionFactory _factory;

    public ProductRepository(IDbConnectionFactory factory) => _factory = factory;

    // Reused SELECT column list — aliases map snake_case → PascalCase to bind Product's setters.
    private const string SelectColumns = @"
        id             as Id,
        code           as Code,
        description    as Description,
        type           as Type,
        supplier_value as SupplierValue,
        stock_quantity as StockQuantity,
        deleted_at     as DeletedAt,
        created_at     as CreatedAt,
        updated_at     as UpdatedAt";

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        var sql = $@"
            SELECT {SelectColumns}
            FROM products
            WHERE id = @id";

        using var conn = _factory.Create();
        return await conn.QuerySingleOrDefaultAsync<Product>(sql, new { id });
    }

    public async Task<PagedResult<Product>> ListAsync(int page, int pageSize, bool includeDeleted)
    {
        var filter = includeDeleted ? string.Empty : "WHERE deleted_at IS NULL";
        var offset = (page - 1) * pageSize;

        var itemsSql = $@"
            SELECT {SelectColumns}
            FROM products
            {filter}
            ORDER BY created_at DESC, id DESC
            LIMIT @pageSize OFFSET @offset";

        var countSql = $@"SELECT COUNT(*) FROM products {filter}";

        using var conn = _factory.Create();
        var items = (await conn.QueryAsync<Product>(itemsSql, new { pageSize, offset })).ToList();
        var total = await conn.ExecuteScalarAsync<int>(countSql);

        return new PagedResult<Product>(items, page, pageSize, total);
    }

    public async Task<Product> CreateAsync(Product product)
    {
        // Let Postgres assign id via DEFAULT gen_random_uuid() when caller passes Guid.Empty;
        // otherwise honor caller-provided id. RETURNING reads server defaults (id, created_at, updated_at).
        var sql = $@"
            INSERT INTO products (id, code, description, type, supplier_value, stock_quantity, deleted_at, created_at, updated_at)
            VALUES (
                COALESCE(NULLIF(@Id, '00000000-0000-0000-0000-000000000000'::uuid), gen_random_uuid()),
                @Code, @Description, @Type, @SupplierValue, @StockQuantity, NULL, now(), now()
            )
            RETURNING {SelectColumns}";

        using var conn = _factory.Create();
        return await conn.QuerySingleAsync<Product>(sql, new
        {
            Id = product.Id,
            product.Code,
            product.Description,
            Type = (int)product.Type,
            product.SupplierValue,
            product.StockQuantity
        });
    }

    public async Task<int> SoftDeleteAsync(Guid id)
    {
        var sql = @"
            UPDATE products
            SET deleted_at = now(), updated_at = now()
            WHERE id = @id AND deleted_at IS NULL";

        using var conn = _factory.Create();
        return await conn.ExecuteAsync(sql, new { id });
    }
}
