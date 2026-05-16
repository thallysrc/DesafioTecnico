import { ref } from 'vue'
import { productsApi } from '../api'
import type { ProductResponse, CreateProductRequest } from '../types'
import type { ApiError } from '@/shared/api/client'
import type { PaginationMeta } from '@/shared/types'

/**
 * Composable wiring `productsApi` to a ref-based UI state surface.
 * Returns refs (not a `reactive({})`) so consumers can destructure without
 * losing reactivity (frontend/CLAUDE.md §Composables).
 *
 * Error semantics:
 *   - `fetchPage` catches and writes to `error.value` so the page can render
 *     the 4-state error UI.
 *   - `create` and `softDelete` re-throw so the page can dispatch the right
 *     toast (per-error code, hint fallback) and surface field errors inline.
 */
export function useProducts() {
  const items = ref<ProductResponse[]>([])
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
  const showDeleted = ref(false)

  /** Fetch a page; rebinds pagination + items; sets error on failure. */
  async function fetchPage(
    page: number = pagination.value.page,
    pageSize: number = pagination.value.pageSize,
  ): Promise<void> {
    loading.value = true
    error.value = null
    try {
      const result = await productsApi.list(page, pageSize, showDeleted.value)
      items.value = result.items
      pagination.value = result.pagination
    } catch (e) {
      error.value = e as ApiError
    } finally {
      loading.value = false
    }
  }

  /** Toggle the "Mostrar excluídos" filter; refetches page 1. */
  async function setShowDeleted(next: boolean): Promise<void> {
    showDeleted.value = next
    await fetchPage(1)
  }

  /**
   * Create a product; refetch page 1 on success (created_at DESC sort lands
   * new entry on top). Throws on failure so the form can surface field errors.
   */
  async function create(request: CreateProductRequest): Promise<ProductResponse> {
    const created = await productsApi.create(request)
    await fetchPage(1)
    return created
  }

  /**
   * Soft-delete and refetch current page (or fall back to a lower page if
   * the current page became empty as a result of the delete).
   */
  async function softDelete(id: string): Promise<void> {
    await productsApi.delete(id)
    const remainingOnPage = items.value.length - 1
    const nextPage =
      remainingOnPage === 0 && pagination.value.page > 1
        ? pagination.value.page - 1
        : pagination.value.page
    await fetchPage(nextPage)
  }

  /** Retry after a failed fetch — re-runs `fetchPage` with last requested page. */
  function retry(): Promise<void> {
    return fetchPage(pagination.value.page)
  }

  return {
    items,
    pagination,
    loading,
    error,
    showDeleted,
    fetchPage,
    setShowDeleted,
    create,
    softDelete,
    retry,
  }
}
