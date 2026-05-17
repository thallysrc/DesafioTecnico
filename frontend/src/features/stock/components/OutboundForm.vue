<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { AlertTriangle } from 'lucide-vue-next'
import BaseInput from '@/shared/components/BaseInput.vue'
import BaseSearchableSelect from '@/shared/components/BaseSearchableSelect.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import ConfirmOutboundModal from './ConfirmOutboundModal.vue'
import { useToast } from '@/shared/composables/useToast'
import { useStockMovements } from '../composables/useStockMovements'
import { createOutboundSchema, type CreateOutboundForm } from '../schemas'
import { productsApi } from '@/features/products/api'
import { formatQuantity } from '@/shared/format'
import type { ProductResponse } from '@/features/products/types'
import type { ApiError } from '@/shared/api/client'

/**
 * OutboundForm — register a stock exit.
 *
 * UX contracts (locked by 03-UI-SPEC):
 *  - "Disponível: N unidades" helper text above the Quantidade input (UX-08).
 *  - Local balance pre-check (D-08): when quantity > stockQuantity, submit is
 *    disabled and an inline error renders below the input.
 *  - Submit opens `ConfirmOutboundModal` (CONF-01) — the form NEVER POSTs
 *    directly. The modal's Confirm button triggers `useStockMovements.register`.
 *  - On INSUFFICIENT_BALANCE: modal stays open; local stockQuantity is updated
 *    from `apiError.details.available` so the next attempt pre-checks against
 *    the real saldo (race-mitigation per D-08).
 *  - On success: refresh disponível via `productsApi.getById(productId)` (D-03).
 */

const toast = useToast()
const { register } = useStockMovements()

const products = ref<ProductResponse[]>([])
const selectedProduct = ref<ProductResponse | null>(null)
const modalOpen = ref(false)
const isSubmitting = ref(false)

const VALIDATE_ON_BLUR = {
  validateOnBlur: true,
  validateOnInput: false,
  validateOnChange: false,
} as const

const {
  handleSubmit,
  errors,
  meta,
  setFieldValue,
  setFieldError,
  validateField,
  resetForm,
  values,
} = useForm<CreateOutboundForm>({
  validationSchema: toTypedSchema(createOutboundSchema),
  validateOnMount: false,
  ...VALIDATE_ON_BLUR,
})

const saleValueRaw = ref<string>('')

onMounted(async () => {
  const result = await productsApi.list(1, 100, false)
  products.value = result.items
})

const productOptions = computed(() =>
  products.value.map((p) => ({ value: p.id, label: `${p.code} — ${p.description}` })),
)

const productIdValue = computed<string | null>(() => (values.productId ?? null) as string | null)
const quantityValue = computed<number | null>(() =>
  values.quantity == null ? null : Number(values.quantity),
)

watch(
  () => values.productId,
  (id) => {
    selectedProduct.value = id ? (products.value.find((p) => p.id === id) ?? null) : null
  },
)

/** D-08 local balance pre-check. */
const preCheckError = computed<string | null>(() => {
  if (!selectedProduct.value || values.quantity == null) return null
  if (values.quantity > selectedProduct.value.stockQuantity) {
    return `Quantidade indisponível. Saldo: ${formatQuantity(selectedProduct.value.stockQuantity)}`
  }
  return null
})

const quantityFieldError = computed<string | undefined>(
  () => errors.value.quantity ?? preCheckError.value ?? undefined,
)

const submitDisabled = computed(
  () => !meta.value.valid || isSubmitting.value || preCheckError.value !== null,
)

/** Parse BR-locale string → number. */
function parseBrCurrency(s: string): number {
  if (!s) return NaN
  return parseFloat(s.replace(/\./g, '').replace(',', '.'))
}

function onSaleValueInput(value: string | number | null): void {
  const raw = typeof value === 'string' ? value : value == null ? '' : String(value)
  const cleaned = raw.replace(/[^0-9.,]/g, '')
  const parts = cleaned.split(',')
  const masked =
    parts.length > 1 ? parts[0] + ',' + parts.slice(1).join('').replace(/,/g, '') : cleaned
  saleValueRaw.value = masked
  const parsed = parseBrCurrency(masked)
  setFieldValue('saleValue', Number.isNaN(parsed) ? (undefined as unknown as number) : parsed)
}

function onSaleValueBlur(): void {
  const n = parseBrCurrency(saleValueRaw.value)
  if (!Number.isNaN(n)) {
    saleValueRaw.value = new Intl.NumberFormat('pt-BR', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(n)
    setFieldValue('saleValue', n)
  }
  void validateField('saleValue')
}

function onProductInput(v: string | null): void {
  setFieldValue('productId', (v ?? undefined) as string)
}

function onQuantityInput(v: string | number | null): void {
  setFieldValue(
    'quantity',
    v == null || v === '' ? (undefined as unknown as number) : Number(v),
  )
}

function onFieldBlur(field: keyof CreateOutboundForm): void {
  void validateField(field)
}

// Submit handler: validate via Zod (handled by handleSubmit) AND pre-check
// (gated by `submitDisabled`); on pass, open CONF-01 (no POST yet).
const onSubmit = handleSubmit(() => {
  if (preCheckError.value !== null) return
  if (!selectedProduct.value) return
  modalOpen.value = true
})

// Test seam: expose onSubmit so component tests can invoke the same handler
// the template wires to `@submit.prevent` without relying on vue-test-utils'
// `trigger('submit')`, which does not always dispatch through Vue's listener.
defineExpose({ onSubmit })

async function onConfirm(): Promise<void> {
  if (!selectedProduct.value || values.productId == null || values.quantity == null || values.saleValue == null) return
  isSubmitting.value = true
  try {
    await register({
      productId: values.productId,
      type: 'Outbound',
      quantity: values.quantity,
      saleValue: values.saleValue,
    })
    modalOpen.value = false
    toast.success('Saída registrada com sucesso')
    // Capture id before resetForm clears values; then refresh disponível (D-03).
    const refreshedId = selectedProduct.value.id
    resetForm()
    saleValueRaw.value = ''
    try {
      const refreshed = await productsApi.getById(refreshedId)
      // Update local cache so a follow-up entry on the same product reflects new saldo.
      const idx = products.value.findIndex((p) => p.id === refreshedId)
      if (idx >= 0) products.value.splice(idx, 1, refreshed)
    } catch {
      // Non-blocking: stale disponível only affects pre-check on next attempt.
    }
    selectedProduct.value = null
  } catch (e) {
    const apiError = e as ApiError
    if (apiError.errorCode === 'INSUFFICIENT_BALANCE') {
      toast.error(apiError.hint ?? apiError.message)
      const details = apiError.details as { available?: number } | undefined
      if (details && typeof details.available === 'number' && selectedProduct.value) {
        // D-08 race-mitigation: trust server-reported saldo over local cache.
        selectedProduct.value = { ...selectedProduct.value, stockQuantity: details.available }
        const idx = products.value.findIndex((p) => p.id === selectedProduct.value!.id)
        if (idx >= 0) products.value.splice(idx, 1, selectedProduct.value)
      }
      // Modal stays open so user can adjust + retry without re-entering everything.
    } else if (apiError.errorCode === 'PRODUCT_DELETED') {
      toast.error(apiError.hint ?? apiError.message)
      modalOpen.value = false
      selectedProduct.value = null
      setFieldValue('productId', undefined as unknown as string)
    } else if (
      apiError.errorCode === 'VALIDATION_ERROR' &&
      apiError.details &&
      'fields' in apiError.details
    ) {
      const fields = (
        apiError.details as { fields: Array<{ field: string; message: string }> }
      ).fields
      for (const f of fields) {
        setFieldError(f.field as keyof CreateOutboundForm, f.message)
      }
      modalOpen.value = false
      toast.error('Verifique os campos destacados')
    } else {
      toast.error(apiError.hint ?? apiError.message)
    }
  } finally {
    isSubmitting.value = false
  }
}

function onCancel(): void {
  if (isSubmitting.value) return // never close mid-flight
  modalOpen.value = false
}
</script>

<template>
  <form
    class="flex flex-col gap-4 max-w-2xl"
    novalidate
    @submit.prevent="onSubmit"
  >
    <BaseSearchableSelect
      :model-value="productIdValue"
      label="Produto"
      placeholder="Selecione um produto"
      :options="productOptions"
      :error="errors.productId"
      required
      @update:model-value="onProductInput"
      @blur="onFieldBlur('productId')"
    />

    <!-- Quantidade row: label + Disponível helper (UX-08) above the input -->
    <div>
      <div class="flex items-center justify-between mb-1">
        <label
          for="outbound-qty"
          class="block text-sm font-medium text-ink"
        >
          Quantidade <span
            class="text-danger"
            aria-hidden="true"
          >*</span>
        </label>
        <span
          v-if="selectedProduct"
          class="text-xs"
          aria-live="polite"
          aria-atomic="true"
        >
          <template v-if="selectedProduct.stockQuantity > 0">
            <span class="text-muted">Disponível:</span>
            <span class="font-medium text-ink ml-1">{{ formatQuantity(selectedProduct.stockQuantity) }}</span>
            <span class="text-muted"> unidades</span>
          </template>
          <template v-else>
            <AlertTriangle
              class="w-3 h-3 inline -mt-0.5"
              aria-hidden="true"
            />
            <span class="text-danger font-medium ml-1">Disponível: 0 unidades</span>
          </template>
        </span>
      </div>
      <BaseInput
        id="outbound-qty"
        :model-value="quantityValue"
        label=""
        :error="quantityFieldError"
        type="number"
        :min="1"
        :step="1"
        inputmode="numeric"
        @update:model-value="onQuantityInput"
        @blur="onFieldBlur('quantity')"
      />
    </div>

    <BaseInput
      :model-value="saleValueRaw"
      label="Valor de venda"
      :error="errors.saleValue"
      type="text"
      inputmode="decimal"
      mask="currency-brl"
      placeholder="0,00"
      required
      @update:model-value="onSaleValueInput"
      @blur="onSaleValueBlur"
    />

    <div class="flex items-center justify-end">
      <BaseButton
        variant="primary"
        type="submit"
        :disabled="submitDisabled"
        :loading="isSubmitting"
      >
        Registrar saída
      </BaseButton>
    </div>

    <ConfirmOutboundModal
      :open="modalOpen"
      :product="selectedProduct"
      :quantity="values.quantity ?? 0"
      :sale-value="values.saleValue ?? 0"
      :is-submitting="isSubmitting"
      @confirm="onConfirm"
      @cancel="onCancel"
    />
  </form>
</template>
