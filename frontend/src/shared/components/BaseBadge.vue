<script setup lang="ts">
import { computed } from 'vue'

interface Props {
  variant?: 'warning' | 'success' | 'danger' | 'neutral'
}
const props = withDefaults(defineProps<Props>(), { variant: 'neutral' })

const classes = computed(() => {
  const base = 'inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium'
  const variants: Record<Required<Props>['variant'], string> = {
    warning: 'bg-warning/10 text-warning border border-warning/20',
    success: 'bg-success/10 text-success',
    danger: 'bg-danger/10 text-danger',
    neutral: 'bg-surface text-muted',
  }
  return `${base} ${variants[props.variant]}`
})
</script>

<template>
  <span :class="classes"><slot /></span>
</template>
