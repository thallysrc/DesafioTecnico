import { describe, it, expect, beforeEach, vi } from 'vitest'
import { useProducts } from './useProducts'
import { productsApi } from '../api'
import type { ApiError } from '@/shared/api/client'
import type { ProductResponse, CreateProductRequest } from '../types'
import type { PagedResult } from '@/shared/types'

vi.mock('../api')

/**
 * Build a canonical ApiError fixture. The interceptor in `@/shared/api/client.ts`
 * normalizes every backend rejection to this shape, so composable tests assert
 * against it directly.
 */
function makeApiError(overrides: Partial<ApiError> = {}): ApiError {
  return {
    errorCode: 'INTERNAL_ERROR',
    category: 'INTERNAL',
    message: 'Erro interno',
    hint: 'Tente novamente',
    statusCode: 500,
    retryable: false,
    traceId: 'test-trace',
    timestamp: '2026-05-16T00:00:00Z',
    ...overrides,
  }
}

function makeProduct(overrides: Partial<ProductResponse> = {}): ProductResponse {
  return {
    id: '11111111-1111-1111-1111-111111111111',
    code: 'P001',
    description: 'Notebook',
    type: 'Electronic',
    supplierValue: 100,
    stockQuantity: 5,
    createdAt: '2026-05-16T00:00:00Z',
    updatedAt: '2026-05-16T00:00:00Z',
    deletedAt: null,
    _links: { self: '/api/products/11111111-1111-1111-1111-111111111111' },
    ...overrides,
  }
}

function makePage(
  items: ProductResponse[],
  pageOverrides: Partial<PagedResult<ProductResponse>['pagination']> = {},
): PagedResult<ProductResponse> {
  return {
    items,
    pagination: {
      page: 1,
      pageSize: 30,
      total: items.length,
      totalPages: items.length === 0 ? 0 : 1,
      hasNext: false,
      hasPrev: false,
      ...pageOverrides,
    },
    _links: {},
  }
}

describe('useProducts', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('starts with empty state and loading=false', () => {
    const { items, pagination, loading, error, showDeleted } = useProducts()
    expect(items.value).toHaveLength(0)
    expect(pagination.value.page).toBe(1)
    expect(pagination.value.pageSize).toBe(30)
    expect(loading.value).toBe(false)
    expect(error.value).toBeNull()
    expect(showDeleted.value).toBe(false)
  })

  it('fetchPage populates items + pagination and sets loading false on success', async () => {
    vi.mocked(productsApi.list).mockResolvedValueOnce(makePage([makeProduct()]))
    const { items, pagination, loading, error, fetchPage } = useProducts()
    await fetchPage()
    expect(items.value).toHaveLength(1)
    expect(pagination.value.total).toBe(1)
    expect(loading.value).toBe(false)
    expect(error.value).toBeNull()
  })

  it('fetchPage writes error.value on rejection and does not throw', async () => {
    vi.mocked(productsApi.list).mockRejectedValueOnce(
      makeApiError({
        errorCode: 'INTERNAL_ERROR',
        message: 'Erro interno',
        hint: 'Tente novamente',
      }),
    )
    const { items, loading, error, fetchPage } = useProducts()
    await expect(fetchPage()).resolves.toBeUndefined()
    expect(error.value?.errorCode).toBe('INTERNAL_ERROR')
    expect(error.value?.hint).toBe('Tente novamente')
    expect(loading.value).toBe(false)
    expect(items.value).toHaveLength(0)
  })

  it('setShowDeleted flips the flag and refetches page 1 including deleted', async () => {
    vi.mocked(productsApi.list).mockResolvedValue(makePage([]))
    const { showDeleted, setShowDeleted } = useProducts()
    await setShowDeleted(true)
    expect(showDeleted.value).toBe(true)
    const lastCall = vi.mocked(productsApi.list).mock.calls.at(-1)!
    expect(lastCall[0]).toBe(1) // page
    expect(lastCall[1]).toBe(30) // pageSize
    expect(lastCall[2]).toBe(true) // includeDeleted
  })

  it('create re-throws on failure and does NOT refetch', async () => {
    vi.mocked(productsApi.create).mockRejectedValueOnce(
      makeApiError({
        errorCode: 'DUPLICATE_CODE',
        category: 'BUSINESS_RULE',
        message: "Já existe um produto com código 'P001'",
        statusCode: 422,
      }),
    )
    const { create } = useProducts()
    const req: CreateProductRequest = {
      code: 'P001',
      description: 'X',
      type: 'Electronic',
      supplierValue: 1,
      initialStockQuantity: 0,
    }
    await expect(create(req)).rejects.toMatchObject({ errorCode: 'DUPLICATE_CODE' })
    expect(productsApi.list).not.toHaveBeenCalled()
  })

  it('create succeeds + refetches page 1 + returns the created product', async () => {
    const created = makeProduct({ code: 'P002', description: 'Geladeira' })
    vi.mocked(productsApi.create).mockResolvedValueOnce(created)
    vi.mocked(productsApi.list).mockResolvedValueOnce(makePage([created]))
    const { create } = useProducts()
    const req: CreateProductRequest = {
      code: 'P002',
      description: 'Geladeira',
      type: 'Appliance',
      supplierValue: 1500,
      initialStockQuantity: 2,
    }
    const result = await create(req)
    expect(result).toEqual(created)
    const lastCall = vi.mocked(productsApi.list).mock.calls.at(-1)!
    expect(lastCall[0]).toBe(1) // page
    expect(lastCall[1]).toBe(30) // pageSize
    expect(lastCall[2]).toBe(false) // includeDeleted (showDeleted defaults to false)
  })

  it('softDelete with remaining items refetches the current page', async () => {
    // Seed: page 2 with 3 items
    vi.mocked(productsApi.list).mockResolvedValueOnce(
      makePage(
        [
          makeProduct({ id: 'a', code: 'P-A' }),
          makeProduct({ id: 'b', code: 'P-B' }),
          makeProduct({ id: 'c', code: 'P-C' }),
        ],
        { page: 2, total: 3, hasPrev: true },
      ),
    )
    const { items, fetchPage, softDelete } = useProducts()
    await fetchPage(2)
    expect(items.value).toHaveLength(3)

    vi.mocked(productsApi.delete).mockResolvedValueOnce(undefined)
    vi.mocked(productsApi.list).mockResolvedValueOnce(
      makePage(
        [makeProduct({ id: 'b', code: 'P-B' }), makeProduct({ id: 'c', code: 'P-C' })],
        { page: 2, total: 2, hasPrev: true },
      ),
    )
    await softDelete('a')

    const lastCall = vi.mocked(productsApi.list).mock.calls.at(-1)!
    expect(lastCall[0]).toBe(2) // current page preserved
  })

  it('softDelete on the last item of page > 1 falls back to page - 1', async () => {
    // Seed: page 2 with exactly 1 item
    vi.mocked(productsApi.list).mockResolvedValueOnce(
      makePage([makeProduct()], { page: 2, total: 1, hasPrev: true }),
    )
    const { items, pagination, fetchPage, softDelete } = useProducts()
    await fetchPage(2)
    expect(items.value).toHaveLength(1)
    expect(pagination.value.page).toBe(2)

    vi.mocked(productsApi.delete).mockResolvedValueOnce(undefined)
    vi.mocked(productsApi.list).mockResolvedValueOnce(makePage([], { page: 1, total: 0 }))
    await softDelete(items.value[0].id)

    const lastCall = vi.mocked(productsApi.list).mock.calls.at(-1)!
    expect(lastCall[0]).toBe(1) // page-1 fallback
  })

  it('retry re-uses pagination.value.page', async () => {
    // Seed: land on page 3
    vi.mocked(productsApi.list).mockResolvedValueOnce(
      makePage([makeProduct()], { page: 3, total: 60, totalPages: 2, hasPrev: true }),
    )
    const { pagination, fetchPage, retry } = useProducts()
    await fetchPage(3)
    expect(pagination.value.page).toBe(3)

    vi.mocked(productsApi.list).mockResolvedValueOnce(
      makePage([makeProduct()], { page: 3, total: 60, totalPages: 2, hasPrev: true }),
    )
    await retry()

    const lastCall = vi.mocked(productsApi.list).mock.calls.at(-1)!
    expect(lastCall[0]).toBe(3) // current page preserved on retry
  })
})
