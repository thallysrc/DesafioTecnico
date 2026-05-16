/**
 * Closed catalog of product types (mirrors backend ProductType enum).
 * Locked here so this file is self-contained in Wave 1; Plan 02-05's
 * `features/products/types.ts` re-exports the same union.
 */
export type ProductTypeLiteral = 'Electronic' | 'Appliance' | 'Furniture'

/**
 * Canonical API enum (English) → user-facing PT-BR label mapping for ProductType.
 * UI-SPEC line 581: Electronic → Eletrônico, Appliance → Eletrodoméstico, Furniture → Móvel.
 *
 * NEVER inline these mappings in components. Always import from here.
 */
export const productTypeLabel: Record<ProductTypeLiteral, string> = {
  Electronic: 'Eletrônico',
  Appliance: 'Eletrodoméstico',
  Furniture: 'Móvel',
}

/**
 * Closed catalog of movement directions (mirrors backend MovementType enum).
 * Same anchor-here pattern as ProductTypeLiteral — `features/stock/types.ts`
 * re-exports `MovementType` from here so the literal lives in exactly one place.
 */
export type MovementTypeLiteral = 'Inbound' | 'Outbound'

/**
 * Canonical API enum (English) → user-facing PT-BR label mapping for MovementType.
 * Locked by 03-UI-SPEC §"Enum translations": Inbound → Entrada, Outbound → Saída.
 *
 * NEVER inline `t === 'Inbound' ? 'Entrada' : 'Saída'` in components. Always import.
 */
export const movementTypeLabel: Record<MovementTypeLiteral, string> = {
  Inbound: 'Entrada',
  Outbound: 'Saída',
}
