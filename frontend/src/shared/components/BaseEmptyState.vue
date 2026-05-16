<script setup lang="ts">
interface Props {
  heading: string
  body: string
}
defineProps<Props>()
</script>

<template>
  <div class="flex flex-col items-center justify-center text-center min-h-[400px] py-8">
    <svg
      class="w-[120px] h-[120px] text-brand-500 mb-6"
      viewBox="0 0 64 64"
      fill="none"
      stroke="currentColor"
      stroke-width="2"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
    >
      <path d="M8 16 L32 6 L56 16 L32 26 Z" />
      <path d="M8 16 L8 48 L32 58 L56 48 L56 16" />
      <path d="M32 26 L32 58" />
      <path d="M44 12 L44 36" />
    </svg>
    <h2 class="text-lg font-semibold text-ink">
      {{ heading }}
    </h2>
    <p class="text-sm text-muted mt-2 max-w-md">
      {{ body }}
    </p>
    <div class="mt-6">
      <slot name="cta" />
    </div>
  </div>
</template>
