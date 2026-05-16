using Inventory.Api.Dtos;
using Inventory.Api.Entities;
using Inventory.Api.Exceptions;
using Inventory.Api.Repositories;
using Npgsql;

namespace Inventory.Api.Services;

/// <summary>
/// Orchestrates product CRUD operations. Concrete class (no interface) per backend/CLAUDE.md §"Services".
/// Entity ↔ DTO mapping is manual inline (no AutoMapper/Mapster). Exception translation lives here:
/// Postgres UNIQUE violations (SQLState 23505) → <see cref="DuplicateCodeException"/>; repository nulls/zeros
/// → <see cref="ProductNotFoundException"/>.
/// </summary>
public class ProductService
{
    private const int DefaultPageSize = 30;
    private const int MaxPageSize = 100;

    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Create a new product. Throws <see cref="DuplicateCodeException"/> on UNIQUE violation.</summary>
    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        var entity = new Product
        {
            Id = Guid.Empty, // let Postgres generate via DEFAULT gen_random_uuid()
            Code = request.Code,
            Description = request.Description,
            Type = request.Type,
            SupplierValue = request.SupplierValue,
            StockQuantity = request.InitialStockQuantity,
        };

        Product created;
        try
        {
            created = await _repository.CreateAsync(entity);
        }
        catch (PostgresException pgex) when (pgex.SqlState == "23505")
        {
            // UNIQUE violation on products.code. Even if a soft-deleted product holds this code,
            // re-use is forbidden by design (init.sql comment + backend/CLAUDE.md §"Soft Delete").
            throw new DuplicateCodeException(request.Code);
        }

        return MapToResponse(created);
    }

    /// <summary>Fetch product by id. Returns even when soft-deleted (caller sees <see cref="ProductResponse.DeletedAt"/>).</summary>
    public async Task<ProductResponse> GetByIdAsync(Guid id)
    {
        var product = await _repository.GetByIdAsync(id);
        if (product is null) throw new ProductNotFoundException(id);
        return MapToResponse(product);
    }

    /// <summary>List paginated products. Page/pageSize clamped to safe bounds.</summary>
    public async Task<PagedProductsResponse> ListAsync(int page, int pageSize, bool includeDeleted)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);

        var result = await _repository.ListAsync(safePage, safePageSize, includeDeleted);

        var items = result.Items.Select(MapToResponse).ToList();
        var meta = new PaginationMeta(
            Page: result.Page,
            PageSize: result.PageSize,
            Total: result.Total,
            TotalPages: result.TotalPages,
            HasNext: result.HasNext,
            HasPrev: result.HasPrev
        );

        var links = LinksFactory.ForProductsListing(meta.Page, meta.PageSize, meta.TotalPages, includeDeleted);

        return new PagedProductsResponse(items, meta, links);
    }

    /// <summary>
    /// Soft-delete by id. Throws <see cref="ProductNotFoundException"/> when the product
    /// does not exist OR is already soft-deleted (repository returns 0 rows affected in both cases).
    /// This conflation is intentional: from the caller's perspective both states map to
    /// "this id has no live product I can delete" — a fresh GET will disambiguate via <c>deletedAt</c>.
    /// </summary>
    public async Task SoftDeleteAsync(Guid id)
    {
        var affected = await _repository.SoftDeleteAsync(id);
        if (affected == 0)
        {
            // Disambiguate: check if the product exists at all.
            var existing = await _repository.GetByIdAsync(id);
            if (existing is null) throw new ProductNotFoundException(id);
            // Exists but was already soft-deleted: surface as PRODUCT_NOT_FOUND with the same hint —
            // the user-facing experience is identical (the product is no longer actionable).
            throw new ProductNotFoundException(id);
        }
    }

    // -- Inline mapping (per BACK-07, no AutoMapper) -------------------------------------------------
    private static ProductResponse MapToResponse(Product p) =>
        new ProductResponse(
            Id: p.Id,
            Code: p.Code,
            Description: p.Description,
            Type: p.Type,
            SupplierValue: p.SupplierValue,
            StockQuantity: p.StockQuantity,
            CreatedAt: p.CreatedAt,
            UpdatedAt: p.UpdatedAt,
            DeletedAt: p.DeletedAt,
            Links: LinksFactory.ForProduct(p.Id, isDeleted: p.DeletedAt is not null)
        );
}
