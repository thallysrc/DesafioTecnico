import type { ProductTypeLiteral } from '@/shared/labels'

/**
 * Alias so feature code reads naturally (`type === 'Electronic'`).
 * Plan 02-03 owns the canonical literal type in `@/shared/labels` to keep
 * `labels.ts` self-contained in Wave 1.
 */
export type ProductType = ProductTypeLiteral

/**
 * Wire shape returned by `GET /api/products/{id}` and as an item of
 * `GET /api/products` response. Backend DTO: `Inventory.Api.Dtos.ProductResponse`
 * (Plan 02-01). The backend record uses `[JsonPropertyName("_links")]` so the
 * JSON field is `_links` (underscore prefix).
 */
export interface ProductResponse {
  id: string
  code: string
  description: string
  type: ProductType
  supplierValue: number
  stockQuantity: number
  createdAt: string
  updatedAt: string
  deletedAt: string | null
  _links: Record<string, string>
}

/**
 * Payload for `POST /api/products`. Backend DTO:
 * `Inventory.Api.Dtos.CreateProductRequest`. `type` is sent as the English
 * enum literal; `supplierValue` and `initialStockQuantity` are JSON numbers.
 */
export interface CreateProductRequest {
  code: string
  description: string
  type: ProductType
  supplierValue: number
  initialStockQuantity: number
}
