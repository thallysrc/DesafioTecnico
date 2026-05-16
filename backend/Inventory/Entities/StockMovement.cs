namespace Inventory.Api.Entities;

/// <summary>
/// Stock movement entity — POCO with mutable setters so Dapper can materialize from
/// <c>stock_movements</c> table rows. Lives only in the repository ↔ service plane;
/// never serialized directly (use <see cref="Inventory.Api.Dtos.MovementResponse"/>).
///
/// Immutability is enforced at the API surface: there is NO update or delete endpoint
/// (MOVE-11). Once inserted, rows are append-only.
/// </summary>
public class StockMovement
{
    /// <summary>Server-generated UUID v4 primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>FK to <c>products.id</c>.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Movement direction (Inbound / Outbound).</summary>
    public MovementType Type { get; set; }

    /// <summary>Quantity moved. Always positive (CHECK quantity &gt; 0).</summary>
    public int Quantity { get; set; }

    /// <summary>Per-unit sale price in BRL. Non-null on Outbound, NULL on Inbound.</summary>
    public decimal? SaleValue { get; set; }

    /// <summary>Per-unit supplier cost in BRL. Non-null on Inbound, NULL on Outbound.</summary>
    public decimal? SupplierValue { get; set; }

    /// <summary>Client-supplied UUID v4 — UNIQUE in DB, enables idempotency replay (MOVE-02 / MOVE-03).</summary>
    public Guid IdempotencyKey { get; set; }

    /// <summary>Business-time UTC timestamp when the movement occurred.</summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>Insertion UTC timestamp (server default <c>now()</c>).</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// In-memory projection used by <c>IStockMovementRepository.ListAsync</c>:
/// flattens the JOIN'd shape <c>stock_movements ⨝ products</c> so each row carries the product code
/// and description without a second round-trip. Lives in the entities layer because it is the raw
/// Dapper materialization target; the service maps it to <see cref="Inventory.Api.Dtos.MovementResponse"/>.
/// </summary>
public class StockMovementWithProduct
{
    /// <summary>Server-generated UUID v4 primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>FK to <c>products.id</c>.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Movement direction (Inbound / Outbound).</summary>
    public MovementType Type { get; set; }

    /// <summary>Quantity moved. Always positive.</summary>
    public int Quantity { get; set; }

    /// <summary>Per-unit sale price in BRL. Non-null on Outbound, NULL on Inbound.</summary>
    public decimal? SaleValue { get; set; }

    /// <summary>Per-unit supplier cost in BRL. Non-null on Inbound, NULL on Outbound.</summary>
    public decimal? SupplierValue { get; set; }

    /// <summary>Client-supplied UUID v4 enabling idempotency replay.</summary>
    public Guid IdempotencyKey { get; set; }

    /// <summary>Business-time UTC timestamp when the movement occurred.</summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>Insertion UTC timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Snapshot of <c>products.code</c> at read time (JOIN'd from the products table).</summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Snapshot of <c>products.description</c> at read time (JOIN'd from the products table).</summary>
    public string ProductDescription { get; set; } = string.Empty;
}
