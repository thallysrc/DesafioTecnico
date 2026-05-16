<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import BaseInput from '@/shared/components/BaseInput.vue'
import BaseSelect from '@/shared/components/BaseSelect.vue'
import { createProductSchema, type CreateProductForm } from '../schemas/createProductSchema'
import { productTypeLabel } from '@/shared/labels'
import type { ProductType } from '../types'
import type { ApiError } from '@/shared/api/client'

const emit = defineEmits<{
  /**
   * Submit handler. Parent awaits this; if it throws an ApiError with
   * VALIDATION_ERROR + details.fields, the form re-maps fields inline via
   * setFieldError. Re-thrown so the parent can dispatch a summary toast.
   */
  (e: 'submit', values: CreateProductForm): Promise<void>
  /** Emitted when form dirty state changes, so the drawer can guard backdrop-close. */
  (e: 'dirtyChange', isDirty: boolean): void
}>()

// D-06: validateOnBlur: true — errors surface inline when each field blurs,
// never eagerly as the user types. We disable per-value validation here and
// call `validateField` explicitly inside each input's blur handler.
const VALIDATE_ON_BLUR = { validateOnBlur: true, validateOnInput: false, validateOnChange: false } as const

const {
  handleSubmit,
  errors,
  isSubmitting,
  meta,
  setFieldValue,
  setFieldError,
  validateField,
  values,
} = useForm<CreateProductForm>({
  validationSchema: toTypedSchema(createProductSchema),
  validateOnMount: false,
  // Above policy is per-field via VALIDATE_ON_BLUR passed to each input's blur handler.
  ...VALIDATE_ON_BLUR,
})

// Raw BR-locale display string for the currency input. Form's `supplierValue`
// stays a JS number (Zod-validated); this ref holds the user's typed text.
const supplierValueRaw = ref<string>('')

const typeOptions: Array<{ value: ProductType; label: string }> = (
  ['Electronic', 'Appliance', 'Furniture'] as const
).map((v) => ({ value: v, label: productTypeLabel[v] }))

// Computed bindings — kept outside the template so Vue lint doesn't
// misinterpret `|` (union) as a Vue filter pipe.
const codeValue = computed<string>(() => (values.code ?? '') as string)
const descriptionValue = computed<string>(() => (values.description ?? '') as string)
const typeValue = computed<string | null>(() => (values.type ?? null) as string | null)
const quantityValue = computed<number | null>(() =>
  values.initialStockQuantity == null ? null : Number(values.initialStockQuantity),
)

/** Parse BR-locale string ('1.234,56' or '1234,56') → number. */
function parseBrCurrency(s: string): number {
  if (!s) return NaN
  return parseFloat(s.replace(/\./g, '').replace(',', '.'))
}

/** Mask handler: allow only digits, single comma, optional thousand dots. */
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

/** Blur: render canonical BR-formatted decimal (without R$ — primitive owns prefix). */
function onSupplierValueBlur(): void {
  const n = parseBrCurrency(supplierValueRaw.value)
  if (!Number.isNaN(n)) {
    supplierValueRaw.value = new Intl.NumberFormat('pt-BR', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(n)
    setFieldValue('supplierValue', n)
  }
}

/** Numeric input handler for initialStockQuantity. */
function onQuantityInput(value: string | number | null): void {
  setFieldValue(
    'initialStockQuantity',
    value == null || value === '' ? (undefined as unknown as number) : Number(value),
  )
}

/** Generic string-input updater for code/description. */
function makeStringSetter(field: 'code' | 'description') {
  return (value: string | number | null): void => {
    setFieldValue(field, (value ?? '') as string)
  }
}
const onCodeInput = makeStringSetter('code')
const onDescriptionInput = makeStringSetter('description')

/** Type select setter. */
function onTypeInput(value: string | null): void {
  setFieldValue('type', (value ?? undefined) as ProductType)
}

/** Field-level blur validator — D-06 validateOnBlur trigger. */
function onFieldBlur(field: keyof CreateProductForm): void {
  void validateField(field)
}

const onSubmit = handleSubmit(async (formValues) => {
  try {
    await emit('submit', formValues)
  } catch (e) {
    const apiError = e as ApiError
    if (
      apiError.errorCode === 'VALIDATION_ERROR' &&
      apiError.details &&
      'fields' in apiError.details
    ) {
      const fields = (apiError.details as { fields: Array<{ field: string; message: string }> }).fields
      for (const f of fields) {
        setFieldError(f.field as keyof CreateProductForm, f.message)
      }
    }
    // Re-throw so the page can dispatch the summary toast.
    throw apiError
  }
})

// Expose imperative API for the page-level submit button.
defineExpose({
  onSubmit,
  isSubmitting,
  isValid: () => meta.value.valid,
})

// Surface dirty state to the drawer for the backdrop-close guard.
watch(
  () => meta.value.dirty,
  (isDirty) => emit('dirtyChange', isDirty),
)
</script>

<template>
  <form
    class="flex flex-col gap-4"
    novalidate
    @submit.prevent="onSubmit"
  >
    <BaseInput
      :model-value="codeValue"
      label="Código"
      :error="errors.code"
      :maxlength="50"
      required
      autofocus
      type="text"
      @update:model-value="onCodeInput"
      @blur="onFieldBlur('code')"
    />
    <BaseInput
      :model-value="descriptionValue"
      label="Descrição"
      :error="errors.description"
      :maxlength="200"
      required
      type="text"
      @update:model-value="onDescriptionInput"
      @blur="onFieldBlur('description')"
    />
    <BaseSelect
      :model-value="typeValue"
      label="Tipo"
      placeholder="Selecione o tipo"
      :options="typeOptions"
      :error="errors.type"
      required
      @update:model-value="onTypeInput"
      @blur="onFieldBlur('type')"
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
      @blur="onSupplierValueBlur(); onFieldBlur('supplierValue')"
    />
    <BaseInput
      :model-value="quantityValue"
      label="Quantidade inicial em estoque"
      :error="errors.initialStockQuantity"
      type="number"
      :min="0"
      :step="1"
      inputmode="numeric"
      required
      @update:model-value="onQuantityInput"
      @blur="onFieldBlur('initialStockQuantity')"
    />
  </form>
</template>
