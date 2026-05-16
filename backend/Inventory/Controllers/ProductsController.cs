using Inventory.Api.Dtos;
using Inventory.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory.Api.Controllers;

/// <summary>
/// CRUD endpoints for products. Soft-delete only (no hard delete). Codes are unique across the
/// whole table (including soft-deleted rows) so PROD-06 <c>DUPLICATE_CODE</c> is enforced at the
/// DB layer.
/// </summary>
[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _service;

    public ProductsController(ProductService service)
    {
        _service = service;
    }

    /// <summary>Register a new product in the inventory catalog.</summary>
    /// <remarks>
    /// Validates the body via FluentValidation (per-field PT-BR messages mirror the frontend Zod schema).
    /// On UNIQUE conflict with an existing <c>products.code</c> (including soft-deleted rows), returns
    /// 422 with <c>errorCode: DUPLICATE_CODE</c> and a dynamic hint linking to the conflicting product.
    /// </remarks>
    /// <param name="request">Product to create.</param>
    /// <returns>201 with the created product (including <c>_links.self</c> and <c>_links.delete</c>).</returns>
    [HttpPost]
    [SwaggerOperation(OperationId = "createProduct", Tags = new[] { "Products" })]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var response = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>List products paginated.</summary>
    /// <remarks>
    /// Defaults: <c>page=1</c>, <c>pageSize=30</c> (max 100), <c>includeDeleted=false</c>. Ordered by
    /// <c>created_at DESC, id DESC</c> server-side. Pagination envelope conforms to AGENT-10:
    /// <c>{ items, pagination, _links }</c>.
    /// </remarks>
    /// <param name="page">1-indexed page number. Defaults to 1; values below 1 are clamped to 1.</param>
    /// <param name="pageSize">Items per page. Defaults to 30; values are clamped to [1,100].</param>
    /// <param name="includeDeleted">When true, soft-deleted products are included (with <c>deletedAt</c> populated).</param>
    [HttpGet]
    [SwaggerOperation(OperationId = "listProducts", Tags = new[] { "Products" })]
    [ProducesResponseType(typeof(PagedProductsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30,
        [FromQuery] bool includeDeleted = false)
    {
        var response = await _service.ListAsync(page, pageSize, includeDeleted);
        return Ok(response);
    }

    /// <summary>Fetch a single product by id. Soft-deleted products are returned with <c>deletedAt</c> populated.</summary>
    /// <remarks>Returns 404 with <c>errorCode: PRODUCT_NOT_FOUND</c> when the id does not exist.</remarks>
    /// <param name="id">UUID of the product.</param>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(OperationId = "getProduct", Tags = new[] { "Products" })]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var response = await _service.GetByIdAsync(id);
        return Ok(response);
    }

    /// <summary>Soft-delete a product by id.</summary>
    /// <remarks>
    /// Sets <c>products.deleted_at = now()</c>. The product is filtered out of default listings
    /// (<c>GET /api/products</c>) but remains retrievable via <c>GET /api/products/{id}</c> and
    /// <c>GET /api/products?includeDeleted=true</c>. Codes are NOT reusable even after delete
    /// (UNIQUE constraint without filter). The product's movement history (Phase 3) remains visible.
    /// </remarks>
    /// <param name="id">UUID of the product to soft-delete.</param>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(OperationId = "deleteProduct", Tags = new[] { "Products" })]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        await _service.SoftDeleteAsync(id);
        return NoContent();
    }
}
