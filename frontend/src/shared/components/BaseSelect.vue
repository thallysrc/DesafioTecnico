<script setup lang="ts">
import { computed, useId } from 'vue'

interface Option {
  value: string
  label: string
}

interface Props {
  modelValue: string | null
  label: string
  options: Option[]
  id?: string
  placeholder?: string
  required?: boolean
  disabled?: boolean
  error?: string
}

const props = withDefaults(defineProps<Props>(), {
  id: undefined,
  placeholder: undefined,
  required: false,
  disabled: false,
  error: undefined,
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: string | null): void
  (e: 'blur', ev: FocusEvent): void
}>()

const generatedId = useId()
const selectId = computed(() => props.id ?? `select-${generatedId}`)
const errorId = computed(() => `${selectId.value}-error`)

const selectClasses = computed(() => {
  const base =
    'w-full border rounded-md px-3 py-2 text-sm text-ink outline-none transition-shadow bg-white pr-8 appearance-none'
  const state = props.error
    ? 'border-danger focus:ring-2 focus:ring-danger focus:border-danger'
    : 'border-line hover:border-muted focus:ring-2 focus:ring-brand-500 focus:border-brand-500'
  const disabled = props.disabled ? ' bg-surface text-muted cursor-not-allowed opacity-70' : ''
  return `${base} ${state}${disabled}`
})

function onChange(event: Event): void {
  const target = event.target as HTMLSelectElement
  emit('update:modelValue', target.value === '' ? null : target.value)
}
</script>

<template>
  <div>
    <label
      :for="selectId"
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
      <select
        :id="selectId"
        :value="modelValue ?? ''"
        :disabled="disabled"
        :class="selectClasses"
        :aria-invalid="error ? 'true' : undefined"
        :aria-required="required || undefined"
        :aria-describedby="error ? errorId : undefined"
        @change="onChange"
        @blur="$emit('blur', $event)"
      >
        <option
          v-if="placeholder"
          value=""
          disabled
        >
          {{ placeholder }}
        </option>
        <option
          v-for="opt in options"
          :key="opt.value"
          :value="opt.value"
        >
          {{ opt.label }}
        </option>
      </select>
    </div>
    <p
      v-if="error"
      :id="errorId"
      class="text-xs text-danger mt-1"
    >
      {{ error }}
    </p>
  </div>
</template>
