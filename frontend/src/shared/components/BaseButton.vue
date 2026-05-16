<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  variant?: 'primary' | 'secondary' | 'destructive' | 'ghost'
  type?: 'button' | 'submit' | 'reset'
  disabled?: boolean
  loading?: boolean
  ariaLabel?: string
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'primary',
  type: 'button',
  disabled: false,
  loading: false,
  ariaLabel: undefined,
})

defineEmits<{ (e: 'click', ev: MouseEvent): void }>()

const VARIANT_CLASSES: Record<Required<Props>['variant'], string> = {
  primary:
    'bg-brand-500 hover:bg-brand-600 disabled:bg-brand-200 disabled:cursor-not-allowed text-white focus:ring-brand-500',
  secondary:
    'border border-line text-ink hover:bg-surface disabled:opacity-50 disabled:cursor-not-allowed focus:ring-brand-500',
  destructive:
    'bg-danger hover:bg-red-700 disabled:bg-red-200 disabled:cursor-not-allowed text-white focus:ring-danger',
  ghost:
    'inline-flex items-center justify-center w-10 h-10 text-muted hover:bg-surface hover:text-ink focus:ring-brand-500',
}

const buttonClasses = computed(() => {
  if (props.variant === 'ghost') {
    return `${VARIANT_CLASSES.ghost} rounded-md transition-colors focus:outline-none focus:ring-2`
  }
  return `${VARIANT_CLASSES[props.variant]} px-4 py-2 rounded-md text-sm font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2`
})
</script>

<template>
  <button
    :type="type"
    :class="buttonClasses"
    :disabled="disabled || loading"
    :aria-busy="loading || undefined"
    :aria-label="ariaLabel"
    @click="$emit('click', $event)"
  >
    <slot />
  </button>
</template>
