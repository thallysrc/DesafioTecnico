<script setup lang="ts">
interface Props {
  modelValue: boolean
  label: string
  ariaLabel?: string
  disabled?: boolean
}
const props = withDefaults(defineProps<Props>(), {
  ariaLabel: undefined,
  disabled: false,
})
const emit = defineEmits<{ (e: 'update:modelValue', value: boolean): void }>()

function toggle(): void {
  if (!props.disabled) emit('update:modelValue', !props.modelValue)
}
</script>

<template>
  <label class="inline-flex items-center gap-2 cursor-pointer select-none">
    <button
      type="button"
      role="switch"
      :aria-checked="modelValue"
      :aria-label="ariaLabel ?? label"
      :disabled="disabled"
      :class="[
        'relative w-10 h-6 rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2',
        modelValue ? 'bg-brand-500' : 'bg-line',
        disabled ? 'opacity-50 cursor-not-allowed' : '',
      ]"
      @click="toggle"
    >
      <span
        :class="[
          'absolute top-0.5 left-0.5 w-5 h-5 rounded-full bg-white shadow transition-transform',
          modelValue ? 'translate-x-4' : '',
        ]"
        aria-hidden="true"
      />
    </button>
    <span class="text-sm text-ink">{{ label }}</span>
  </label>
</template>
