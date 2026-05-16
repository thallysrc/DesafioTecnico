<script setup lang="ts">
interface Props {
  rows?: number
}
withDefaults(defineProps<Props>(), { rows: 5 })
</script>

<template>
  <div class="space-y-2">
    <div
      v-for="i in rows"
      :key="i"
      class="h-10 bg-line rounded animate-pulse motion-reduce:animate-none"
      aria-hidden="true"
    />
    <span class="sr-only">Carregando…</span>
  </div>
</template>
