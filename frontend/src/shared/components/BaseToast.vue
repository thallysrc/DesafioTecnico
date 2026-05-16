<script setup lang="ts">
import { CheckCircle2, XCircle, Info } from 'lucide-vue-next'
import type { ToastItem } from '@/shared/composables/useToast'

interface Props {
  toast: ToastItem
}
defineProps<Props>()
defineEmits<{
  (e: 'dismiss'): void
  (e: 'pause'): void
  (e: 'resume'): void
}>()

const VARIANT_BAR: Record<string, string> = {
  success: 'border-l-4 border-success',
  error: 'border-l-4 border-danger',
  info: 'border-l-4 border-brand-500',
}
const VARIANT_ICON: Record<string, string> = {
  success: 'text-success',
  error: 'text-danger',
  info: 'text-brand-500',
}
</script>

<template>
  <div
    :class="[
      'flex items-start gap-3 min-w-[320px] max-w-[480px] bg-white rounded-md shadow-lg pl-4 pr-3 py-3 border border-line cursor-pointer',
      VARIANT_BAR[toast.variant],
    ]"
    :role="toast.variant === 'error' ? 'alert' : 'status'"
    :aria-live="toast.variant === 'error' ? 'assertive' : 'polite'"
    aria-label="Dispensar"
    @click="$emit('dismiss')"
    @mouseenter="$emit('pause')"
    @mouseleave="$emit('resume')"
  >
    <component
      :is="toast.variant === 'success' ? CheckCircle2 : toast.variant === 'error' ? XCircle : Info"
      :class="['w-4 h-4 mt-0.5 flex-shrink-0', VARIANT_ICON[toast.variant]]"
      aria-hidden="true"
    />
    <p class="text-sm text-ink flex-1">
      {{ toast.message }}
    </p>
  </div>
</template>
