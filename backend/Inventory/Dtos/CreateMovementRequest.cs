using Inventory.Api.Entities;

namespace Inventory.Api.Dtos;

/// <summary>Payload to register a stock movement (Inbound or Outbound).</summary>
/// <param name="ProductId"><summary>UUID of the product being moved. Must reference an active (non-deleted) product.</summary></param>
/// <param name="Type"><summary>Movement direction: <c>Inbound</c> or <c>Outbound</c>.</summary></param>
/// <param name="Quantity"><summary>Number of units moved. Must be ≥ 1.</summary></param>
/// <param name="SupplierValue"><summary>Per-unit supplier cost in BRL. Required for <c>Inbound</c>; MUST be omitted for <c>Outbound</c>. Non-negative.</summary></param>
/// <param name="SaleValue"><summary>Per-unit sale price in BRL. Required for <c>Outbound</c>; MUST be omitted for <c>Inbound</c>. Non-negative.</summary></param>
public record CreateMovementRequest(
    Guid ProductId,
    MovementType Type,
    int Quantity,
    decimal? SupplierValue,
    decimal? SaleValue
);
