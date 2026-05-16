<script setup lang="ts">
import { ArrowLeftRight, Package } from 'lucide-vue-next'
import { useRoute } from 'vue-router'
import HealthPill from './HealthPill.vue'

interface NavItem {
  basePath: string
  to: string
  label: string
  icon: typeof Package
}

const navItems: NavItem[] = [
  { basePath: '/products', to: '/products', label: 'Produtos', icon: Package },
  {
    basePath: '/stock-movements',
    to: '/stock-movements',
    label: 'Movimentação de Estoque',
    icon: ArrowLeftRight,
  },
]

const route = useRoute()
const isActive = (basePath: string) => route.path.startsWith(basePath)
</script>

<template>
  <div class="flex h-screen bg-white">
    <aside class="w-60 border-r border-line p-4 flex flex-col bg-white">
      <!-- FRONT-11: Wordmark with colorized "Easy". Single H1 on the page (a11y). -->
      <h1 class="text-lg font-semibold text-ink mb-6 px-2">
        Stock<span class="text-brand-500">Easy</span>
      </h1>

      <nav
        aria-label="Navegação principal"
        class="flex flex-col gap-1"
      >
        <RouterLink
          v-for="item in navItems"
          :key="item.basePath"
          :to="item.to"
          :class="[
            'flex items-center gap-3 px-4 py-2.5 rounded-md text-sm font-medium transition-colors',
            isActive(item.basePath)
              ? 'bg-brand-50 text-brand-700'
              : 'text-muted hover:bg-surface hover:text-ink',
          ]"
        >
          <component
            :is="item.icon"
            class="w-5 h-5"
            aria-hidden="true"
          />
          <span>{{ item.label }}</span>
        </RouterLink>
      </nav>

      <!-- Health pill at the bottom of the sidebar (UI-SPEC §"Health-Status Pill"). -->
      <div class="mt-auto pt-4">
        <HealthPill />
      </div>
    </aside>

    <main class="flex-1 p-6 overflow-auto">
      <slot />
    </main>
  </div>
</template>
