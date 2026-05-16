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

// Phase 3 will append movementTypeLabel here:
//   Inbound → 'Entrada'
//   Outbound → 'Saída'
