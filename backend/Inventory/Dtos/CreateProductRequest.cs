using Inventory.Api.Entities;

namespace Inventory.Api.Dtos;

/// <summary>Payload to register a new product in the inventory catalog.</summary>
/// <param name="Code"><summary>Unique product code (max 50 chars). Acts as the business identifier; reuse blocked even after soft delete.</summary></param>
/// <param name="Description"><summary>Free-text product description (max 200 chars).</summary></param>
/// <param name="Type"><summary>Product category: <c>Electronic</c>, <c>Appliance</c>, or <c>Furniture</c>.</summary></param>
/// <param name="SupplierValue"><summary>Per-unit cost paid to the supplier in BRL. Must be ≥ 0.</summary></param>
/// <param name="InitialStockQuantity"><summary>Initial on-hand stock quantity. Must be ≥ 0; stored into <c>products.stock_quantity</c>.</summary></param>
public record CreateProductRequest(
    string Code,
    string Description,
    ProductType Type,
    decimal SupplierValue,
    int InitialStockQuantity
);
