<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import BaseInput from '@/shared/components/BaseInput.vue'
import BaseSearchableSelect from '@/shared/components/BaseSearchableSelect.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import { useToast } from '@/shared/composables/useToast'
import { useStockMovements } from '../composables/useStockMovements'
import { createInboundSchema, type CreateInboundForm } from '../schemas'
import { productsApi } from '@/features/products/api'
import type { ProductResponse } from '@/features/products/types'
import type { ApiError } from '@/shared/api/client'

/**
 * InboundForm — register a stock entry (CONF-03: submit direct, no modal).
 *
 * Owns its own Vee-Validate form (mirrors ProductForm pattern from Plan 02).
 * Composes `useStockMovements.register({ type: 'Inbound', ... })` which
 * injects Idempotency-Key automatically per FRONT-12 (Plan 03-02).
 *
 * Submit success: success toast "Entrada registrada com sucesso" + reset form.
 * Submit error path forwards `apiError.hint ?? apiError.message` to a toast.
 */

const toast = useToast()
const { register } = useStockMovements()

const products = ref<ProductResponse[]>([])

const VALIDATE_ON_BLUR = {
  validateOnBlur: true,
  validateOnInput: false,
  validateOnChange: false,
} as const

const {
  handleSubmit,
  errors,
  isSubmitting,
  meta,
  setFieldValue,
  setFieldError,
  validateField,
  resetForm,
  values,
} = useForm<CreateInboundForm>({
  validationSchema: toTypedSchema(createInboundSchema),
  validateOnMount: false,
  ...VALIDATE_ON_BLUR,
})

// Raw BR-locale display string for the currency input. The form's
// `supplierValue` stays a JS number (Zod-validated); this ref holds the typed text.
const supplierValueRaw = ref<string>('')

onMounted(async () => {
  // D-04: includeDeleted=false — deleted products are not movable.
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

/** Parse BR-locale string ('1.234,56' or '1234,56') → number. */
function parseBrCurrency(s: string): number {
  if (!s) return NaN
  return parseFloat(s.replace(/\./g, '').replace(',', '.'))
}

function onSupplierValueInput(value: string | number | null): void {
  const raw = typeof value === 'string' ? value : value == null ? '' : String(value)
  const cleaned = raw.replace(/[^0-9.,]/g, '')
  const parts = cleaned.split(',')
  const masked =
    parts.length > 1 ? parts[0] + ',' + parts.slice(1).join('').replace(/,/g, '') : cleaned
  supplierValueRaw.value = masked
  const parsed = parseBrCurrency(masked)
  setFieldValue('supplierValue', Number.isNaN(parsed) ? (undefined as unknown as number) : parsed)
}

function onSupplierValueBlur(): void {
  const n = parseBrCurrency(supplierValueRaw.value)
  if (!Number.isNaN(n)) {
    supplierValueRaw.value = new Intl.NumberFormat('pt-BR', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(n)
    setFieldValue('supplierValue', n)
  }
  void validateField('supplierValue')
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

function onFieldBlur(field: keyof CreateInboundForm): void {
  void validateField(field)
}

const submitDisabled = computed(() => !meta.value.valid || isSubmitting.value)

const onSubmit = handleSubmit(async (formValues) => {
  try {
    await register({
      productId: formValues.productId,
      type: 'Inbound',
      quantity: formValues.quantity,
      supplierValue: formValues.supplierValue,
    })
    toast.success('Entrada registrada com sucesso')
    resetForm()
    supplierValueRaw.value = ''
  } catch (e) {
    const apiError = e as ApiError
    if (
      apiError.errorCode === 'VALIDATION_ERROR' &&
      apiError.details &&
      'fields' in apiError.details
    ) {
      const fields = (
        apiError.details as { fields: Array<{ field: string; message: string }> }
      ).fields
      for (const f of fields) {
        setFieldError(f.field as keyof CreateInboundForm, f.message)
      }
      toast.error('Verifique os campos destacados')
    } else if (apiError.errorCode === 'PRODUCT_DELETED') {
      toast.error(apiError.hint ?? apiError.message)
      setFieldValue('productId', undefined as unknown as string)
    } else {
      toast.error(apiError.hint ?? apiError.message)
    }
  }
})
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
    <BaseInput
      :model-value="quantityValue"
      label="Quantidade"
      :error="errors.quantity"
      type="number"
      :min="1"
      :step="1"
      inputmode="numeric"
      required
      @update:model-value="onQuantityInput"
      @blur="onFieldBlur('quantity')"
    />
    <BaseInput
      :model-value="supplierValueRaw"
      label="Valor do fornecedor"
      :error="errors.supplierValue"
      type="text"
      inputmode="decimal"
      mask="currency-brl"
      placeholder="0,00"
      required
      @update:model-value="onSupplierValueInput"
      @blur="onSupplierValueBlur"
    />
    <div class="flex items-center justify-end">
      <BaseButton
        variant="primary"
        type="submit"
        :disabled="submitDisabled"
        :loading="isSubmitting"
      >
        <span v-if="isSubmitting">Registrando entrada...</span>
        <span v-else>Registrar entrada</span>
      </BaseButton>
    </div>
  </form>
</template>
