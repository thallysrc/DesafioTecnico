<script setup lang="ts">
import { computed, nextTick, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import InboundForm from '../components/InboundForm.vue'
import OutboundForm from '../components/OutboundForm.vue'
import MovementHistory from '../components/MovementHistory.vue'

/**
 * StockMovementsPage — orchestrates the Entrada / Saída / Histórico tab strip.
 *
 * URL query `?tab=entrada|saida|historico` is the source of truth (D-01:
 * default tab is `historico` when the param is absent). Tab content uses
 * `v-if` so closed tabs don't run validators or fetch.
 *
 * Tab strip follows the WAI-ARIA tabs pattern with automatic activation
 * (focus = activation): roving tabindex, ArrowLeft/Right/Up/Down cycle with
 * wrap, Home/End jump to ends, Enter/Space activate. No new `BaseTabs`
 * primitive is authored — composition inlined per UI-SPEC §"Tab Strip".
 */

type Tab = 'entrada' | 'saida' | 'historico'
const TABS: ReadonlyArray<{ id: Tab; label: string }> = [
  { id: 'entrada', label: 'Entrada' },
  { id: 'saida', label: 'Saída' },
  { id: 'historico', label: 'Histórico' },
]

const route = useRoute()
const router = useRouter()

function parseTab(q: unknown): Tab {
  if (q === 'entrada' || q === 'saida' || q === 'historico') return q
  return 'historico'
}

const activeTab = computed<Tab>(() => parseTab(route.query.tab))

onMounted(async () => {
  // D-01: default URL to ?tab=historico when no tab param is set.
  if (!route.query.tab) {
    await router.replace({ query: { ...route.query, tab: 'historico' } })
  }
})

async function setTab(next: Tab): Promise<void> {
  if (next === activeTab.value) return
  await router.replace({ query: { ...route.query, tab: next } })
  // After the panel re-renders, move focus to the newly active tab so screen
  // readers announce the switch and keyboard users land in the right place.
  await nextTick()
  const el = document.getElementById(`tab-${next}`)
  el?.focus()
}

function onTabKeydown(event: KeyboardEvent, current: Tab): void {
  const idx = TABS.findIndex((t) => t.id === current)
  let nextIdx = idx
  switch (event.key) {
    case 'ArrowRight':
    case 'ArrowDown':
      nextIdx = (idx + 1) % TABS.length
      break
    case 'ArrowLeft':
    case 'ArrowUp':
      nextIdx = (idx - 1 + TABS.length) % TABS.length
      break
    case 'Home':
      nextIdx = 0
      break
    case 'End':
      nextIdx = TABS.length - 1
      break
    case 'Enter':
    case ' ':
      // Tabs auto-activate on focus (automatic activation pattern); Enter/Space
      // on the active tab is a no-op but harmless.
      void setTab(current)
      event.preventDefault()
      return
    default:
      return
  }
  event.preventDefault()
  void setTab(TABS[nextIdx].id)
}

function tabClasses(name: Tab): string[] {
  const base = [
    'inline-flex',
    'items-center',
    'justify-center',
    'px-4',
    'h-12',
    '-mb-px',
    'text-sm',
    'font-medium',
    'transition-colors',
    'focus:outline-none',
    'focus:ring-2',
    'focus:ring-brand-500',
    'focus:ring-offset-2',
    'rounded-t-md',
  ]
  if (activeTab.value === name) {
    return [...base, 'text-brand-700', 'border-b-2', 'border-brand-500', 'bg-white']
  }
  return [
    ...base,
    'text-muted',
    'hover:bg-surface',
    'hover:text-ink',
    'border-b-2',
    'border-transparent',
  ]
}

// Browser tab title per UI-SPEC Copywriting Contract.
watch(
  activeTab,
  () => {
    document.title = 'Movimentação de Estoque · StockEasy'
  },
  { immediate: true },
)
</script>

<template>
  <div class="max-w-7xl mx-auto">
    <header class="mb-6">
      <h1 class="text-2xl font-semibold text-ink">
        Movimentação de Estoque
      </h1>
    </header>

    <nav
      role="tablist"
      aria-label="Seções de movimentação"
      class="flex items-center gap-1 border-b border-line mb-6 h-12"
    >
      <button
        v-for="tab in TABS"
        :id="`tab-${tab.id}`"
        :key="tab.id"
        role="tab"
        type="button"
        :aria-selected="activeTab === tab.id"
        :aria-controls="`panel-${tab.id}`"
        :tabindex="activeTab === tab.id ? 0 : -1"
        :class="tabClasses(tab.id)"
        @click="setTab(tab.id)"
        @keydown="onTabKeydown($event, tab.id)"
      >
        {{ tab.label }}
      </button>
    </nav>

    <section
      :id="`panel-${activeTab}`"
      role="tabpanel"
      :aria-labelledby="`tab-${activeTab}`"
      tabindex="0"
    >
      <InboundForm v-if="activeTab === 'entrada'" />
      <OutboundForm v-else-if="activeTab === 'saida'" />
      <MovementHistory v-else />
    </section>
  </div>
</template>
