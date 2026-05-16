namespace Inventory.Api.Entities;

/// <summary>
/// Product entity — POCO with mutable setters so Dapper can materialize from
/// <c>products</c> table rows. Lives only in repository ↔ service plane; never serialized
/// directly to the wire (use <see cref="Inventory.Api.Dtos.ProductResponse"/>).
/// </summary>
public class Product
{
    /// <summary>Server-generated UUID v4 primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Unique product code (mirrors <c>products.code</c>; max 50 chars).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Free-text product description (max 200 chars).</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Product category enum (Electronic / Appliance / Furniture).</summary>
    public ProductType Type { get; set; }

    /// <summary>Per-unit cost paid to the supplier in BRL. Non-negative.</summary>
    public decimal SupplierValue { get; set; }

    /// <summary>Current on-hand stock quantity. Non-negative.</summary>
    public int StockQuantity { get; set; }

    /// <summary>UTC timestamp when soft-deleted; <c>null</c> while active.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>UTC timestamp when the product was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp of the latest mutation; equal to <c>CreatedAt</c> until a write occurs.</summary>
    public DateTime UpdatedAt { get; set; }
}
