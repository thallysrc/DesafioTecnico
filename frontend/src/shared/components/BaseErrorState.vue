<script setup lang="ts">
import { AlertTriangle } from 'lucide-vue-next'
import type { ApiError } from '@/shared/api/client'

interface Props {
  heading: string
  error: ApiError
}
defineProps<Props>()
defineEmits<{ (e: 'retry'): void }>()
</script>

<template>
  <div class="flex flex-col items-center justify-center text-center min-h-[400px] py-8">
    <AlertTriangle
      class="w-12 h-12 text-danger"
      aria-hidden="true"
    />
    <h2 class="text-lg font-semibold text-ink mt-4">
      {{ heading }}
    </h2>
    <p class="text-sm text-muted mt-2 max-w-md">
      {{ error.hint ?? error.message }}
    </p>
    <p class="text-xs text-muted mt-1">
      Código: {{ error.errorCode }} · ID: {{ error.traceId }}
    </p>
    <button
      type="button"
      class="mt-6 bg-brand-500 hover:bg-brand-600 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors focus:outline-none focus:ring-2 focus:ring-brand-500 focus:ring-offset-2"
      @click="$emit('retry')"
    >
      Tentar novamente
    </button>
  </div>
</template>
