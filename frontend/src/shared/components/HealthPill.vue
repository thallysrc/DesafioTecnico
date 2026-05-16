<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { Check, Loader2, XCircle } from 'lucide-vue-next'
import { fetchHealth, type HealthResponse } from '@/shared/api/health'

type PillState = 'checking' | 'connected' | 'offline'

const state = ref<PillState>('checking')
const controller = new AbortController()

onMounted(async () => {
  try {
    const health: HealthResponse = await fetchHealth(controller.signal)
    // UI-SPEC §"Health-Status Pill" (locked): Connected ← 2xx AND status==="ok" AND db==="up".
    // Anything else (2xx with degraded/db-down, or any error) → offline.
    state.value = health.status === 'ok' && health.db === 'up' ? 'connected' : 'offline'
  } catch {
    // Any rejection (network, timeout, 5xx) → offline.
    state.value = 'offline'
  }
})

onBeforeUnmount(() => {
  controller.abort()
})
</script>

<template>
  <div
    v-if="state === 'checking'"
    role="status"
    aria-live="polite"
    class="inline-flex items-center gap-2 px-3 py-1.5 rounded-md bg-surface"
  >
    <Loader2 class="w-4 h-4 text-muted animate-spin" aria-hidden="true" />
    <span class="text-xs text-muted">Verificando API…</span>
  </div>

  <div
    v-else-if="state === 'connected'"
    role="status"
    aria-live="polite"
    class="inline-flex items-center gap-2 px-3 py-1.5 rounded-md bg-success/10"
  >
    <Check class="w-4 h-4 text-success" aria-hidden="true" />
    <span class="text-xs font-medium text-success">API conectada</span>
  </div>

  <div
    v-else
    role="status"
    aria-live="polite"
    class="inline-flex items-center gap-2 px-3 py-1.5 rounded-md bg-danger/10"
  >
    <XCircle class="w-4 h-4 text-danger" aria-hidden="true" />
    <span class="text-xs font-medium text-danger">API offline</span>
  </div>
</template>
