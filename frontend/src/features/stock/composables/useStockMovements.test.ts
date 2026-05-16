import { describe, it, expect, beforeEach, vi } from 'vitest'
import { useStockMovements } from './useStockMovements'
import { movementsApi } from '../api'
import type { ApiError } from '@/shared/api/client'
import type { MovementResponse, CreateMovementRequest } from '../types'
import type { PagedResult } from '@/shared/types'

vi.mock('../api')

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

function makeMovement(overrides: Partial<MovementResponse> = {}): MovementResponse {
  return {
    id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    productId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    productCode: 'P001',
    productDescription: 'Notebook',
    type: 'Inbound',
    quantity: 5,
    supplierValue: 100,
    saleValue: null,
    idempotencyKey: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
    occurredAt: '2026-05-16T00:00:00Z',
    createdAt: '2026-05-16T00:00:00Z',
    _links: { self: '/api/stock-movements/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa' },
    ...overrides,
  }
}

function makePage(items: MovementResponse[]): PagedResult<MovementResponse> {
  return {
    items,
    pagination: {
      page: 1,
      pageSize: 30,
      total: items.length,
      totalPages: items.length === 0 ? 0 : 1,
      hasNext: false,
      hasPrev: false,
    },
    _links: {},
  }
}

describe('useStockMovements', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('starts with empty filters and zero items', () => {
    const { items, pagination, loading, error, filters } = useStockMovements()
    expect(items.value).toHaveLength(0)
    expect(pagination.value.page).toBe(1)
    expect(pagination.value.pageSize).toBe(30)
    expect(pagination.value.total).toBe(0)
    expect(loading.value).toBe(false)
    expect(error.value).toBeNull()
    expect(filters.value).toEqual({})
  })

  it('fetchPage forwards current filters to movementsApi.list', async () => {
    vi.mocked(movementsApi.list).mockResolvedValue(makePage([]))
    const { setFilters, fetchPage } = useStockMovements()
    await setFilters({ productId: 'abc', startDate: '2026-01-01' })
    // setFilters already called fetchPage(1). Clear mocks then issue an explicit fetchPage
    // to assert the merge cleanly.
    vi.mocked(movementsApi.list).mockClear()
    await fetchPage()
    const lastCall = vi.mocked(movementsApi.list).mock.calls.at(-1)!
    expect(lastCall[0]).toEqual({
      productId: 'abc',
      startDate: '2026-01-01',
      endDate: undefined,
      page: 1,
      pageSize: 30,
    })
  })

  it('fetchPage writes error.value on rejection and does not throw', async () => {
    vi.mocked(movementsApi.list).mockRejectedValueOnce(
      makeApiError({ errorCode: 'INTERNAL_ERROR', message: 'Erro interno' }),
    )
    const { error, loading, fetchPage } = useStockMovements()
    await expect(fetchPage()).resolves.toBeUndefined()
    expect(error.value?.errorCode).toBe('INTERNAL_ERROR')
    expect(loading.value).toBe(false)
  })

  it('setFilters resets to page 1 and maps empty strings to undefined', async () => {
    vi.mocked(movementsApi.list).mockResolvedValue(makePage([]))
    const { filters, setFilters } = useStockMovements()
    await setFilters({ productId: '', startDate: '2026-01-01', endDate: '' })
    expect(filters.value).toEqual({
      productId: undefined,
      startDate: '2026-01-01',
      endDate: undefined,
    })
    const lastCall = vi.mocked(movementsApi.list).mock.calls.at(-1)!
    expect(lastCall[0]).toEqual({
      productId: undefined,
      startDate: '2026-01-01',
      endDate: undefined,
      page: 1,
      pageSize: 30,
    })
  })

  it('clearFilters resets filters to empty and refetches page 1', async () => {
    vi.mocked(movementsApi.list).mockResolvedValue(makePage([]))
    const { filters, setFilters, clearFilters } = useStockMovements()
    await setFilters({ productId: 'abc', startDate: '2026-01-01', endDate: '2026-02-01' })
    vi.mocked(movementsApi.list).mockClear()
    await clearFilters()
    expect(filters.value).toEqual({})
    const lastCall = vi.mocked(movementsApi.list).mock.calls.at(-1)!
    expect(lastCall[0]).toEqual({
      productId: undefined,
      startDate: undefined,
      endDate: undefined,
      page: 1,
      pageSize: 30,
    })
  })

  it('register refetches page 1 and returns the created movement', async () => {
    const created = makeMovement({ id: 'new-mov-id', type: 'Inbound', quantity: 7 })
    vi.mocked(movementsApi.register).mockResolvedValueOnce(created)
    vi.mocked(movementsApi.list).mockResolvedValueOnce(makePage([created]))
    const { register } = useStockMovements()
    const req: CreateMovementRequest = {
      productId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
      type: 'Inbound',
      quantity: 7,
      supplierValue: 50,
    }
    const result = await register(req)
    expect(result).toEqual(created)
    const lastCall = vi.mocked(movementsApi.list).mock.calls.at(-1)!
    expect(lastCall[0]?.page).toBe(1)
  })

  it('register re-throws INSUFFICIENT_BALANCE so caller can update local saldo (D-08)', async () => {
    const apiError = makeApiError({
      errorCode: 'INSUFFICIENT_BALANCE',
      category: 'BUSINESS_RULE',
      message: 'Saldo insuficiente: solicitado 10 unidades, disponível 3',
      hint: 'Reduza a quantidade para no máximo 3 ou registre uma entrada antes.',
      statusCode: 422,
      details: {
        productId: 'p',
        productCode: 'P001',
        requested: 10,
        available: 3,
        deficit: 7,
      },
    })
    vi.mocked(movementsApi.register).mockRejectedValueOnce(apiError)

    const { register } = useStockMovements()
    const req: CreateMovementRequest = {
      productId: 'p',
      type: 'Outbound',
      quantity: 10,
      saleValue: 1,
    }
    await expect(register(req)).rejects.toMatchObject({
      errorCode: 'INSUFFICIENT_BALANCE',
      details: expect.objectContaining({ available: 3, deficit: 7 }),
    })
  })

  it('register does NOT refetch on failure', async () => {
    vi.mocked(movementsApi.register).mockRejectedValueOnce(
      makeApiError({ errorCode: 'INSUFFICIENT_BALANCE', category: 'BUSINESS_RULE' }),
    )
    const { register } = useStockMovements()
    const req: CreateMovementRequest = {
      productId: 'p',
      type: 'Outbound',
      quantity: 10,
      saleValue: 1,
    }
    await expect(register(req)).rejects.toMatchObject({ errorCode: 'INSUFFICIENT_BALANCE' })
    expect(movementsApi.list).not.toHaveBeenCalled()
  })

  it('retry re-runs fetchPage with current page + filters preserved', async () => {
    // Seed: setFilters with productId='x' (refetches page 1), then fetchPage(2)
    // returning a response that pins pagination.page to 2.
    const pageOnePayload = makePage([])
    const pageTwoPayload: PagedResult<MovementResponse> = {
      items: [makeMovement()],
      pagination: { page: 2, pageSize: 30, total: 31, totalPages: 2, hasNext: false, hasPrev: true },
      _links: {},
    }
    vi.mocked(movementsApi.list)
      .mockResolvedValueOnce(pageOnePayload) // setFilters → fetchPage(1)
      .mockResolvedValueOnce(pageTwoPayload) // fetchPage(2) — pins pagination to page 2

    const { setFilters, fetchPage, retry, pagination } = useStockMovements()
    await setFilters({ productId: 'x' })
    await fetchPage(2)
    expect(pagination.value.page).toBe(2)

    vi.mocked(movementsApi.list).mockClear()
    vi.mocked(movementsApi.list).mockResolvedValueOnce(pageTwoPayload)
    await retry()

    const lastCall = vi.mocked(movementsApi.list).mock.calls.at(-1)!
    expect(lastCall[0]).toEqual({
      productId: 'x',
      startDate: undefined,
      endDate: undefined,
      page: 2,
      pageSize: 30,
    })
  })
})
