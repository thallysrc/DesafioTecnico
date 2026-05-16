import { ref } from 'vue'
import { movementsApi } from '../api'
import type {
  CreateMovementRequest,
  MovementHistoryFilters,
  MovementResponse,
} from '../types'
import type { ApiError } from '@/shared/api/client'
import type { PaginationMeta } from '@/shared/types'

/**
 * Composable wiring `movementsApi` to a ref-based UI state surface.
 * Returns refs (not a `reactive({})`) so consumers can destructure without
 * losing reactivity (frontend/CLAUDE.md §Composables).
 *
 * Error semantics:
 *   - `fetchPage` catches and writes to `error.value` so MovementHistory can
 *     render the 4-state error UI.
 *   - `register` re-throws so the form / confirm-modal can dispatch the right
 *     toast (per-error code, hint fallback) and update local state on
 *     INSUFFICIENT_BALANCE (D-08).
 */
export function useStockMovements() {
  const items = ref<MovementResponse[]>([])
  const pagination = ref<PaginationMeta>({
    page: 1,
    pageSize: 30,
    total: 0,
    totalPages: 0,
    hasNext: false,
    hasPrev: false,
  })
  const loading = ref(false)
  const error = ref<ApiError | null>(null)
  const filters = ref<MovementHistoryFilters>({})

  /** Fetch a page with current filters; rebinds pagination + items; sets error on failure. */
  async function fetchPage(
    page: number = pagination.value.page,
    pageSize: number = pagination.value.pageSize,
  ): Promise<void> {
    loading.value = true
    error.value = null
    try {
      const result = await movementsApi.list({
        productId: filters.value.productId,
        startDate: filters.value.startDate,
        endDate: filters.value.endDate,
        page,
        pageSize,
      })
      items.value = result.items
      pagination.value = result.pagination
    } catch (e) {
      error.value = e as ApiError
    } finally {
      loading.value = false
    }
  }

  /**
   * Apply new filters (any subset of productId / startDate / endDate);
   * always resets to page 1. Pass `null`/empty string for a field to clear it.
   */
  async function setFilters(next: MovementHistoryFilters): Promise<void> {
    filters.value = {
      productId: next.productId || undefined,
      startDate: next.startDate || undefined,
      endDate: next.endDate || undefined,
    }
    await fetchPage(1)
  }

  /** Clear all filters and refetch from page 1. */
  async function clearFilters(): Promise<void> {
    filters.value = {}
    await fetchPage(1)
  }

  /**
   * Register a movement; refetch page 1 on success so the new row appears at the top
   * (occurred_at DESC ordering in the repository). Throws on failure so the form can
   * surface the right error UX (toast hint, local stockQuantity update on INSUFFICIENT_BALANCE).
   */
  async function register(req: CreateMovementRequest): Promise<MovementResponse> {
    const created = await movementsApi.register(req)
    await fetchPage(1)
    return created
  }

  /** Retry the last fetch (same filters + current page). */
  function retry(): Promise<void> {
    return fetchPage(pagination.value.page)
  }

  return {
    items,
    pagination,
    loading,
    error,
    filters,
    fetchPage,
    setFilters,
    clearFilters,
    register,
    retry,
  }
}
