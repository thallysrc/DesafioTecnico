<script setup lang="ts">
import { computed, useId } from 'vue'

interface Props {
  modelValue: string | number | null
  label: string
  id?: string
  type?: 'text' | 'number' | 'email' | 'date'
  placeholder?: string
  required?: boolean
  disabled?: boolean
  readonly?: boolean
  error?: string
  helper?: string
  autofocus?: boolean
  maxlength?: number
  min?: number
  step?: number
  inputmode?: 'text' | 'numeric' | 'decimal'
  /** When 'currency-brl', prepends R$ visual prefix and bumps left padding to pl-10. */
  mask?: 'none' | 'currency-brl'
}

const props = withDefaults(defineProps<Props>(), {
  id: undefined,
  type: 'text',
  placeholder: undefined,
  required: false,
  disabled: false,
  readonly: false,
  error: undefined,
  helper: undefined,
  autofocus: false,
  maxlength: undefined,
  min: undefined,
  step: undefined,
  inputmode: undefined,
  mask: 'none',
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: string | number | null): void
  (e: 'blur', ev: FocusEvent): void
}>()

const generatedId = useId()
const inputId = computed(() => props.id ?? `input-${generatedId}`)
const errorId = computed(() => `${inputId.value}-error`)

const inputClasses = computed(() => {
  const base =
    'w-full border rounded-md py-2 text-sm text-ink placeholder:text-muted outline-none transition-shadow bg-white'
  const padding = props.mask === 'currency-brl' ? 'pl-10 pr-3' : 'px-3'
  const state = props.error
    ? 'border-danger focus:ring-2 focus:ring-danger focus:border-danger'
    : 'border-line hover:border-muted focus:ring-2 focus:ring-brand-500 focus:border-brand-500'
  const disabled = props.disabled ? ' bg-surface text-muted cursor-not-allowed opacity-70' : ''
  return `${base} ${padding} ${state}${disabled}`
})

function onInput(event: Event): void {
  const target = event.target as HTMLInputElement
  let value: string | number | null = target.value
  if (props.type === 'number') {
    value = target.value === '' ? null : Number(target.value)
  }
  emit('update:modelValue', value)
}
</script>

<template>
  <div>
    <label
      :for="inputId"
      class="block text-sm font-medium text-ink mb-1"
    >
      {{ label }}
      <span
        v-if="required"
        class="text-danger"
        aria-hidden="true"
      >*</span>
    </label>
    <div class="relative">
      <span
        v-if="mask === 'currency-brl'"
        class="absolute left-3 top-1/2 -translate-y-1/2 text-sm text-muted pointer-events-none"
      >R$</span>
      <input
        :id="inputId"
        :type="type"
        :value="modelValue ?? ''"
        :placeholder="placeholder"
        :disabled="disabled"
        :readonly="readonly"
        :maxlength="maxlength"
        :min="min"
        :step="step"
        :inputmode="inputmode"
        :autofocus="autofocus"
        :class="inputClasses"
        :aria-invalid="error ? 'true' : undefined"
        :aria-required="required || undefined"
        :aria-describedby="error ? errorId : undefined"
        @input="onInput"
        @blur="$emit('blur', $event)"
      >
    </div>
    <p
      v-if="error"
      :id="errorId"
      class="text-xs text-danger mt-1"
    >
      {{ error }}
    </p>
    <p
      v-else-if="helper"
      class="text-xs text-muted mt-1"
    >
      {{ helper }}
    </p>
  </div>
</template>
