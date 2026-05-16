using Inventory.Api.Dtos;
using Inventory.Api.Exceptions;
using Inventory.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Inventory.Api.Controllers;

/// <summary>
/// Endpoints for stock movements (Inbound / Outbound). Movements are append-only — this
/// controller deliberately omits PUT and DELETE actions, which is the enforcement mechanism
/// for MOVE-11 (immutable history). Attempts to PUT/DELETE return 405 Method Not Allowed
/// from the MVC routing layer.
/// </summary>
[ApiController]
[Route("api/stock-movements")]
[Produces("application/json")]
public class StockMovementsController : ControllerBase
{
    /// <summary>Header name expected on POST per MOVE-02 (UUID v4).</summary>
    private const string IdempotencyKeyHeader = "Idempotency-Key";

    /// <summary>Response header set on replay per MOVE-03 (silent replay UX per CONTEXT.md D-07).</summary>
    private const string IdempotencyReplayHeader = "Idempotency-Replay";

    private readonly StockMovementService _service;

    public StockMovementsController(StockMovementService service)
    {
        _service = service;
    }

    /// <summary>Register a new stock movement (Inbound or Outbound).</summary>
    /// <remarks>
    /// Requires header <c>Idempotency-Key</c> (UUID v4). Absent → 400 <c>MISSING_IDEMPOTENCY_KEY</c>.
    /// Replay (same key) → 200 with header <c>Idempotency-Replay: true</c> and the previously persisted body.
    /// New row → 201 with <c>Location</c> header.
    ///
    /// Atomicity: the service opens a transaction, locks the product row with <c>SELECT FOR UPDATE</c>,
    /// validates (not deleted, balance, value-field invariants), inserts the movement, updates the
    /// product (Inbound bumps stock + sets supplier_value; Outbound decrements stock), and commits.
    /// </remarks>
    /// <param name="idempotencyKey">Client-generated UUID v4 (frontend uses <c>crypto.randomUUID()</c>). Required.</param>
    /// <param name="request">Movement payload.</param>
    [HttpPost]
    [SwaggerOperation(OperationId = "createStockMovement", Tags = new[] { "StockMovements" })]
    [ProducesResponseType(typeof(MovementResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(MovementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromHeader(Name = IdempotencyKeyHeader)] string? idempotencyKey,
        [FromBody] CreateMovementRequest request)
    {
        // MOVE-02: absent or blank header → 400 MISSING_IDEMPOTENCY_KEY (Category="VALIDATION" → middleware maps to 400)
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new MissingIdempotencyKeyException();
        }
        if (!Guid.TryParse(idempotencyKey, out var key))
        {
            // Surface the same errorCode + hint — bad-format key is semantically "missing a valid key".
            throw new MissingIdempotencyKeyException();
        }

        var (response, isReplay) = await _service.CreateAsync(request, key);

        if (isReplay)
        {
            // MOVE-03 / CONTEXT.md D-07: silent replay — 200 + Idempotency-Replay header + SAME body.
            Response.Headers[IdempotencyReplayHeader] = "true";
            return Ok(response);
        }

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    /// <summary>List stock movements paginated with optional filters.</summary>
    /// <remarks>
    /// MOVE-08/09: filtering by <c>productId</c>, <c>startDate</c>, <c>endDate</c> (both inclusive),
    /// JOIN'd with <c>products</c> so each item carries <c>productCode</c> + <c>productDescription</c>
    /// (zero N+1 — exactly two SQL statements per page).
    /// Movements of soft-deleted products remain visible (audit trail).
    /// </remarks>
    /// <param name="productId">Optional UUID filter on <c>product_id</c>.</param>
    /// <param name="startDate">Optional ISO 8601 lower bound on <c>occurred_at</c> (inclusive).</param>
    /// <param name="endDate">Optional ISO 8601 upper bound on <c>occurred_at</c> (inclusive).</param>
    /// <param name="page">1-indexed page (default 1, clamped to ≥ 1).</param>
    /// <param name="pageSize">Items per page (default 30, clamped to [1,100]).</param>
    [HttpGet]
    [SwaggerOperation(OperationId = "listStockMovements", Tags = new[] { "StockMovements" })]
    [ProducesResponseType(typeof(PagedMovementsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? productId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 30)
    {
        var response = await _service.ListAsync(productId, startDate, endDate, page, pageSize);
        return Ok(response);
    }

    /// <summary>Fetch a single movement by id (MOVE-10).</summary>
    /// <remarks>Returns 404 <c>MOVEMENT_NOT_FOUND</c> when the id doesn't exist.</remarks>
    /// <param name="id">UUID of the movement.</param>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(OperationId = "getStockMovement", Tags = new[] { "StockMovements" })]
    [ProducesResponseType(typeof(MovementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var response = await _service.GetByIdAsync(id);
        return Ok(response);
    }
}
