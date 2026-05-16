namespace Inventory.Api.Entities;

/// <summary>
/// Direction of a stock movement. Persisted as <c>int</c> in <c>stock_movements.type</c>
/// (CHECK constraint <c>type IN (0, 1)</c>); serialized as <c>"Inbound"</c> / <c>"Outbound"</c>
/// over the wire via the global <c>JsonStringEnumConverter</c>.
/// </summary>
public enum MovementType
{
    /// <summary>Stock-in: increments <c>products.stock_quantity</c> and updates <c>supplier_value</c>.</summary>
    Inbound = 0,

    /// <summary>Stock-out: decrements <c>products.stock_quantity</c> and records <c>sale_value</c>.</summary>
    Outbound = 1
}
