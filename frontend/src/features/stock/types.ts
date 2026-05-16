import type { MovementTypeLiteral } from '@/shared/labels'

/**
 * Alias so feature code reads naturally (`movement.type === 'Inbound'`).
 * Plan 03-02 owns the canonical literal type in `@/shared/labels` to keep
 * `labels.ts` self-contained and aligned with the productTypeLabel pattern.
 */
export type MovementType = MovementTypeLiteral

/**
 * Wire shape returned by `GET /api/stock-movements/{id}` and as an item of
 * `GET /api/stock-movements`. Backend DTO: `Inventory.Api.Dtos.MovementResponse`
 * (Plan 03-01). The backend record annotates `Links` with
 * `[JsonPropertyName("_links")]` so the JSON field is `_links`.
 *
 * `productCode` + `productDescription` come from the JOIN inside
 * `StockMovementRepository.ListAsync` (MOVE-09 zero N+1) — no follow-up fetch needed.
 */
export interface MovementResponse {
  id: string
  productId: string
  productCode: string
  productDescription: string
  type: MovementType
  quantity: number
  supplierValue: number | null
  saleValue: number | null
  idempotencyKey: string
  occurredAt: string
  createdAt: string
  _links: Record<string, string>
}

/**
 * Payload for `POST /api/stock-movements` as an Inbound movement.
 * Backend DTO: `Inventory.Api.Dtos.CreateMovementRequest` with `Type=Inbound`.
 * Note: backend uses one DTO with optional `supplierValue?` / `saleValue?`;
 * frontend splits the type into Inbound / Outbound for compile-time safety,
 * then serializes to the union shape at the api.ts boundary.
 */
export interface CreateInboundRequest {
  productId: string
  type: 'Inbound'
  quantity: number
  supplierValue: number
}

/** Payload for `POST /api/stock-movements` as an Outbound movement. */
export interface CreateOutboundRequest {
  productId: string
  type: 'Outbound'
  quantity: number
  saleValue: number
}

/** Discriminated union accepted by `movementsApi.register`. */
export type CreateMovementRequest = CreateInboundRequest | CreateOutboundRequest

/**
 * Filter shape for the history listing (`GET /api/stock-movements?...`).
 * All fields optional — empty filters list all movements. Dates are ISO 8601
 * `YYYY-MM-DD` strings emitted by `<input type="date">`.
 */
export interface MovementHistoryFilters {
  productId?: string
  startDate?: string
  endDate?: string
  page?: number
  pageSize?: number
}
