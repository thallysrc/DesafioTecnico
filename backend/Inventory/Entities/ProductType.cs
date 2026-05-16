namespace Inventory.Api.Entities;

/// <summary>
/// Closed catalog of product types. Stored as <c>int</c> in <c>products.type</c> column
/// (CHECK 0..2). Serialized as string in JSON via global <c>JsonStringEnumConverter</c>
/// (registered in Program.cs Phase 1) — wire emits <c>"Electronic"</c>, not <c>0</c>.
/// </summary>
public enum ProductType
{
    /// <summary>Eletrônico (e.g. notebook, smartphone).</summary>
    Electronic = 0,

    /// <summary>Eletrodoméstico (e.g. geladeira, microondas).</summary>
    Appliance = 1,

    /// <summary>Móvel (e.g. mesa, cadeira).</summary>
    Furniture = 2
}
