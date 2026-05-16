<script setup lang="ts">
import { computed } from 'vue'
import { ChevronLeft, ChevronRight } from 'lucide-vue-next'

interface Props {
  page: number
  totalPages: number
  hasNext: boolean
  hasPrev: boolean
}
const props = defineProps<Props>()
defineEmits<{ (e: 'change', page: number): void }>()

const pages = computed<Array<number | 'ellipsis'>>(() => {
  const total = props.totalPages
  const current = props.page
  if (total <= 7) return Array.from({ length: total }, (_, i) => i + 1)
  const result: Array<number | 'ellipsis'> = []
  result.push(1)
  if (current > 4) result.push('ellipsis')
  for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
    result.push(i)
  }
  if (current < total - 3) result.push('ellipsis')
  result.push(total)
  return result
})

function btnClass(active: boolean, disabled = false): string {
  const base =
    'w-9 h-9 rounded-md flex items-center justify-center transition-colors focus:outline-none focus:ring-2 focus:ring-brand-500 text-sm'
  if (disabled) return `${base} text-muted opacity-40 cursor-not-allowed`
  if (active) return `${base} bg-brand-50 text-brand-700 font-medium`
  return `${base} text-muted hover:bg-surface hover:text-ink`
}
</script>

<template>
  <nav
    aria-label="Paginação"
    class="flex items-center gap-1"
  >
    <button
      type="button"
      :disabled="!hasPrev"
      :class="btnClass(false, !hasPrev)"
      aria-label="Página anterior"
      @click="$emit('change', page - 1)"
    >
      <ChevronLeft
        class="w-4 h-4"
        aria-hidden="true"
      />
    </button>
    <template
      v-for="(p, idx) in pages"
      :key="`p-${idx}-${p}`"
    >
      <span
        v-if="p === 'ellipsis'"
        class="px-1 text-muted"
        aria-hidden="true"
      >…</span>
      <button
        v-else
        type="button"
        :class="btnClass(p === page)"
        :aria-current="p === page ? 'page' : undefined"
        @click="$emit('change', p)"
      >
        {{ p }}
      </button>
    </template>
    <button
      type="button"
      :disabled="!hasNext"
      :class="btnClass(false, !hasNext)"
      aria-label="Próxima página"
      @click="$emit('change', page + 1)"
    >
      <ChevronRight
        class="w-4 h-4"
        aria-hidden="true"
      />
    </button>
  </nav>
</template>
