/**
 * OutboundForm component tests — TEST-06 (Plan 04-03 / Task 2).
 *
 * Strategy (CONTEXT.md D-09): shallow mount with `BaseSearchableSelect`,
 * `BaseInput`, `BaseButton`, and `ConfirmOutboundModal` stubbed as `true`.
 * The Base primitives + modal contract were proven by Phase 2/3 implementation
 * and are NOT under test here — these tests cover OutboundForm's logic:
 *
 *   1. Mounts, calls productsApi.list(1, 100, false), populates dropdown options.
 *   2. Renders the UX-08 "Disponível: N unidades" helper when a product is
 *      selected with stockQuantity > 0.
 *   3. Surfaces the D-08 local pre-check error (quantity > stockQuantity).
 *   4. Submit only opens the CONF-01 confirmation modal (does NOT call register).
 *   5. Modal confirm calls movementsApi.register with the normalized Outbound
 *      payload `{ productId, type: 'Outbound', quantity, saleValue }`.
 *
 * Mocks: `productsApi` (list + getById) and `movementsApi` (register + list)
 * are auto-mocked via vi.mock at the api module seam. No HTTP touches the wire.
 */
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import OutboundForm from './OutboundForm.vue'
import { productsApi } from '@/features/products/api'
import { movementsApi } from '@/features/stock/api'
import type { ProductResponse } from '@/features/products/types'
import type { MovementResponse } from '@/features/stock/types'
import type { PagedResult } from '@/shared/types'

vi.mock('@/features/products/api')
vi.mock('@/features/stock/api')

function makeProduct(overrides: Partial<ProductResponse> = {}): ProductResponse {
  return {
    id: 'p1',
    code: 'P001',
    description: 'Notebook',
    type: 'Electronic',
    supplierValue: 100,
    stockQuantity: 10,
    createdAt: '2026-05-16T00:00:00Z',
    updatedAt: '2026-05-16T00:00:00Z',
    deletedAt: null,
    _links: {},
    ...overrides,
  }
}

function makeMovement(overrides: Partial<MovementResponse> = {}): MovementResponse {
  return {
    id: 'm1',
    productId: 'p1',
    productCode: 'P001',
    productDescription: 'Notebook',
    type: 'Outbound',
    quantity: 5,
    supplierValue: null,
    saleValue: 100,
    idempotencyKey: 'k1',
    occurredAt: '2026-05-16T00:00:00Z',
    createdAt: '2026-05-16T00:00:00Z',
    _links: {},
    ...overrides,
  }
}

function makePagedProducts(items: ProductResponse[]): PagedResult<ProductResponse> {
  return {
    items,
    pagination: {
      page: 1,
      pageSize: 100,
      total: items.length,
      totalPages: 1,
      hasNext: false,
      hasPrev: false,
    },
    _links: {},
  }
}

function makePagedMovements(items: MovementResponse[]): PagedResult<MovementResponse> {
  return {
    items,
    pagination: {
      page: 1,
      pageSize: 30,
      total: items.length,
      totalPages: 1,
      hasNext: false,
      hasPrev: false,
    },
    _links: {},
  }
}

function makeWrapper() {
  return mount(OutboundForm, {
    global: {
      stubs: {
        BaseSearchableSelect: true,
        BaseInput: true,
        BaseButton: true,
        ConfirmOutboundModal: true,
      },
    },
  })
}

describe('OutboundForm', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('mounts and loads active products into the dropdown on mount', async () => {
    vi.mocked(productsApi.list).mockResolvedValueOnce(
      makePagedProducts([
        makeProduct({ id: 'p1', code: 'P001', description: 'Notebook' }),
        makeProduct({ id: 'p2', code: 'P002', description: 'Mouse' }),
      ]),
    )

    const wrapper = makeWrapper()
    await flushPromises()

    expect(productsApi.list).toHaveBeenCalledWith(1, 100, false)

    const select = wrapper.findComponent({ name: 'BaseSearchableSelect' })
    const options = select.props('options') as Array<{ value: string; label: string }>
    expect(options).toHaveLength(2)
    expect(options[0]).toEqual({ value: 'p1', label: 'P001 — Notebook' })
    expect(options[1]).toEqual({ value: 'p2', label: 'P002 — Mouse' })
  })

  it('renders Disponível helper with formatted quantity when a product is selected with stock > 0', async () => {
    vi.mocked(productsApi.list).mockResolvedValueOnce(
      makePagedProducts([makeProduct({ id: 'p1', stockQuantity: 8 })]),
    )

    const wrapper = makeWrapper()
    await flushPromises()

    wrapper.findComponent({ name: 'BaseSearchableSelect' }).vm.$emit('update:modelValue', 'p1')
    await flushPromises()

    const html = wrapper.html()
    expect(html).toContain('Disponível:')
    // formatQuantity(8) → '8' in pt-BR locale (no thousand separator for small numbers).
    expect(html).toContain('8')
    expect(html).toContain('unidades')
  })

  it('surfaces the D-08 pre-check error when quantity > selectedProduct.stockQuantity', async () => {
    vi.mocked(productsApi.list).mockResolvedValueOnce(
      makePagedProducts([makeProduct({ id: 'p1', stockQuantity: 3 })]),
    )

    const wrapper = makeWrapper()
    await flushPromises()

    wrapper.findComponent({ name: 'BaseSearchableSelect' }).vm.$emit('update:modelValue', 'p1')
    await flushPromises()

    // Order in template: [0]=quantity, [1]=saleValue.
    const inputs = wrapper.findAllComponents({ name: 'BaseInput' })
    inputs[0].vm.$emit('update:modelValue', 10) // 10 > stockQuantity(3) → pre-check fires
    await flushPromises()

    const quantityError = inputs[0].props('error') as string | undefined
    expect(quantityError).toBe('Quantidade indisponível. Saldo: 3')
  })

  it('opens the CONF-01 modal on submit when pre-check passes — does NOT call register yet', async () => {
    vi.mocked(productsApi.list).mockResolvedValueOnce(
      makePagedProducts([makeProduct({ id: 'p1', stockQuantity: 10 })]),
    )

    const wrapper = makeWrapper()
    await flushPromises()

    // Set product + quantity + saleValue to valid values
    wrapper.findComponent({ name: 'BaseSearchableSelect' }).vm.$emit('update:modelValue', 'p1')
    await flushPromises()
    const inputs = wrapper.findAllComponents({ name: 'BaseInput' })
    inputs[0].vm.$emit('update:modelValue', 5) // quantity
    inputs[1].vm.$emit('update:modelValue', '100,00') // saleValueRaw → parses to 100
    inputs[1].vm.$emit('blur') // commits parsed saleValue to form
    await flushPromises()

    // Trigger submit via form element (onSubmit is template-bound, not exposed).
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()

    // Modal should be open and register MUST NOT have been called yet.
    const modal = wrapper.findComponent({ name: 'ConfirmOutboundModal' })
    expect(modal.props('open')).toBe(true)
    expect(movementsApi.register).not.toHaveBeenCalled()
  })

  it('confirming the modal calls movementsApi.register with the normalized Outbound payload', async () => {
    vi.mocked(productsApi.list).mockResolvedValueOnce(
      makePagedProducts([makeProduct({ id: 'p1', stockQuantity: 10 })]),
    )
    vi.mocked(movementsApi.register).mockResolvedValueOnce(makeMovement())
    // useStockMovements.register refetches the history page after success.
    vi.mocked(movementsApi.list).mockResolvedValueOnce(makePagedMovements([makeMovement()]))
    // OutboundForm.onConfirm calls productsApi.getById after register success (D-03).
    vi.mocked(productsApi.getById).mockResolvedValueOnce(
      makeProduct({ id: 'p1', stockQuantity: 5 }),
    )

    const wrapper = makeWrapper()
    await flushPromises()

    wrapper.findComponent({ name: 'BaseSearchableSelect' }).vm.$emit('update:modelValue', 'p1')
    await flushPromises()
    const inputs = wrapper.findAllComponents({ name: 'BaseInput' })
    inputs[0].vm.$emit('update:modelValue', 5)
    inputs[1].vm.$emit('update:modelValue', '100,00')
    inputs[1].vm.$emit('blur')
    await flushPromises()

    // Step 1: submit → opens modal (no register yet).
    await wrapper.find('form').trigger('submit.prevent')
    await flushPromises()
    expect(movementsApi.register).not.toHaveBeenCalled()

    // Step 2: confirm the modal → register fires with the normalized payload.
    wrapper.findComponent({ name: 'ConfirmOutboundModal' }).vm.$emit('confirm')
    await flushPromises()

    expect(movementsApi.register).toHaveBeenCalledTimes(1)
    expect(movementsApi.register).toHaveBeenCalledWith({
      productId: 'p1',
      type: 'Outbound',
      quantity: 5,
      saleValue: 100,
    })
  })
})
