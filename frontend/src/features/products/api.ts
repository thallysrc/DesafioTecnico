import { apiClient } from '@/shared/api/client'
import type { PagedResult } from '@/shared/types'
import type { CreateProductRequest, ProductResponse } from './types'

/**
 * Product API surface. All methods unwrap `response.data` at the boundary so
 * callers receive the typed payload directly. Errors are normalized by the
 * Axios interceptor to `ApiError` (rejected) — callers handle in try/catch.
 *
 * baseURL `/api` lives in the Axios singleton; paths here are written as
 * `/products` and resolve to `/api/products` via the Vite proxy.
 */
export const productsApi = {
  list: (page = 1, pageSize = 30, includeDeleted = false): Promise<PagedResult<ProductResponse>> =>
    apiClient.get<PagedResult<ProductResponse>>('/products', { params: { page, pageSize, includeDeleted } }).then((r) => r.data),

  getById: (id: string): Promise<ProductResponse> =>
    apiClient.get<ProductResponse>(`/products/${id}`).then((r) => r.data),

  create: (req: CreateProductRequest): Promise<ProductResponse> =>
    apiClient.post<ProductResponse>('/products', req).then((r) => r.data),

  delete: (id: string): Promise<void> =>
    apiClient.delete<void>(`/products/${id}`).then(() => undefined),
}
