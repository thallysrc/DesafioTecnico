<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ArrowDownToLine, ArrowUpFromLine } from 'lucide-vue-next'
import BaseBadge from '@/shared/components/BaseBadge.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import BaseSearchableSelect from '@/shared/components/BaseSearchableSelect.vue'
import BaseInput from '@/shared/components/BaseInput.vue'
import BasePagination from '@/shared/components/BasePagination.vue'
import BaseSkeleton from '@/shared/components/BaseSkeleton.vue'
import BaseEmptyState from '@/shared/components/BaseEmptyState.vue'
import BaseErrorState from '@/shared/components/BaseErrorState.vue'
import { formatCurrency, formatDate, formatQuantity } from '@/shared/format'
import { movementTypeLabel } from '@/shared/labels'
import { useStockMovements } from '../composables/useStockMovements'
import { productsApi } from '@/features/products/api'
import type { ProductResponse } from '@/features/products/types'
import type { MovementResponse } from '../types'

/**
 * MovementHistory — paginated stock-movement history with filter bar.
 *
 * 4-state pattern (loading / empty / filter-empty / error / data) per Phase 2
 * inheritance. Filter state persists to URL query (`productId`, `startDate`,
 * `endDate`, `page`); the product select is debounced 300ms (D-05) while
 * dates fire immediately.
 *
 * NOTE on raw <table> usage: BaseTable forces clickable rows
 * (`tabindex="0"`, click + Enter handlers, `cursor-pointer`). The UI-SPEC
 * mandates non-interactive rows here (MOVE-11 — no detail surface in v1),
 * so this component renders a plain <table> with the same visual styling
 * as BaseTable (header bg-brand-50, row border-line, hover bg-surface).
 * No new shared primitive is authored.
 */

const route = useRoute()
const router = useRouter()
const {
  items,
  pagination,
  loading,
  error,
  filters,
  fetchPage,
  setFilters,
  clearFilters,
  retry,
} = useStockMovements()

const products = ref<ProductResponse[]>([])
const productOptions = computed(() => [
  { value: '', label: 'Todos os produtos' },
  ...products.value.map((p) => ({ value: p.id, label: `${p.code} — ${p.description}` })),
])

// Local filter inputs (decoupled from composable filters so the productId can debounce).
const productIdInput = ref<string>('')
const startDateInput = ref<string>('')
const endDateInput = ref<string>('')

let productDebounce: ReturnType<typeof setTimeout> | null = null

type ViewState = 'loading' | 'empty' | 'filter-empty' | 'error' | 'data'
const viewState = computed<ViewState>(() => {
  if (loading.value && items.value.length === 0) return 'loading'
  if (error.value) return 'error'
  const hasFilters = !!(filters.value.productId || filters.value.startDate || filters.value.endDate)
  if (items.value.length === 0) return hasFilters ? 'filter-empty' : 'empty'
  return 'data'
})

const hasActiveFilters = computed(
  () => !!(filters.value.productId || filters.value.startDate || filters.value.endDate),
)

onMounted(async () => {
  // Hydrate filters from URL.
  productIdInput.value = (route.query.productId as string) ?? ''
  startDateInput.value = (route.query.startDate as string) ?? ''
  endDateInput.value = (route.query.endDate as string) ?? ''
  const page = Number(route.query.page) || 1

  await setFilters({
    productId: productIdInput.value || undefined,
    startDate: startDateInput.value || undefined,
    endDate: endDateInput.value || undefined,
  })
  if (page !== 1) await fetchPage(page)

  // Populate filter select with available products.
  const result = await productsApi.list(1, 100, false)
  products.value = result.items
})

function syncUrl(): void {
  const q: Record<string, string> = { tab: 'historico' }
  if (filters.value.productId) q.productId = filters.value.productId
  if (filters.value.startDate) q.startDate = filters.value.startDate
  if (filters.value.endDate) q.endDate = filters.value.endDate
  if (pagination.value.page > 1) q.page = String(pagination.value.page)
  void router.replace({ query: q })
}

watch([() => filters.value, () => pagination.value.page], syncUrl, { deep: true })

function onProductChange(v: string | null): void {
  productIdInput.value = v ?? ''
  if (productDebounce) clearTimeout(productDebounce)
  // D-05: debounce productId filter by 300ms; dates fire immediately.
  productDebounce = setTimeout(() => {
    void setFilters({
      productId: productIdInput.value || undefined,
      startDate: startDateInput.value || undefined,
      endDate: endDateInput.value || undefined,
    })
  }, 300)
}

function onStartDateChange(v: string | number | null): void {
  startDateInput.value = String(v ?? '')
  void setFilters({
    productId: productIdInput.value || undefined,
    startDate: startDateInput.value || undefined,
    endDate: endDateInput.value || undefined,
  })
}

function onEndDateChange(v: string | number | null): void {
  endDateInput.value = String(v ?? '')
  void setFilters({
    productId: productIdInput.value || undefined,
    startDate: startDateInput.value || undefined,
    endDate: endDateInput.value || undefined,
  })
}

// D-10: default the empty end of the date pair to today on first focus when
// the other end is set. Doesn't override user input on later focuses.
function onEndDateFocus(): void {
  if (!endDateInput.value && startDateInput.value) {
    endDateInput.value = new Date().toISOString().slice(0, 10)
  }
}
function onStartDateFocus(): void {
  if (!startDateInput.value && endDateInput.value) {
    startDateInput.value = new Date().toISOString().slice(0, 10)
  }
}

async function onClearFilters(): Promise<void> {
  productIdInput.value = ''
  startDateInput.value = ''
  endDateInput.value = ''
  await clearFilters()
}

async function onPageChange(next: number): Promise<void> {
  await fetchPage(next)
}

function goToEntrada(): void {
  void router.replace({ query: { tab: 'entrada' } })
}

// Map row → value column (D-09).
function valueFor(m: MovementResponse): string {
  return m.type === 'Inbound'
    ? formatCurrency(m.supplierValue ?? 0)
    : formatCurrency(m.saleValue ?? 0)
}
function valueTooltip(m: MovementResponse): string {
  return m.type === 'Inbound' ? 'Valor de fornecedor' : 'Valor de venda'
}
</script>

<template>
  <div>
    <!-- Filter bar -->
    <div class="flex flex-wrap items-end gap-4 mb-6">
      <div class="w-80">
        <BaseSearchableSelect
          :model-value="productIdInput || null"
          label="Produto"
          placeholder="Todos os produtos"
          :options="productOptions"
          @update:model-value="onProductChange"
        />
      </div>
      <div class="w-44">
        <BaseInput
          :model-value="startDateInput"
          label="Data inicial"
          type="date"
          @update:model-value="onStartDateChange"
          @focus="onStartDateFocus"
        />
      </div>
      <div class="w-44">
        <BaseInput
          :model-value="endDateInput"
          label="Data final"
          type="date"
          @update:model-value="onEndDateChange"
          @focus="onEndDateFocus"
        />
      </div>
      <button
        type="button"
        class="text-xs text-brand-600 hover:text-brand-700 hover:underline disabled:text-muted disabled:hover:no-underline disabled:cursor-not-allowed self-end pb-2"
        :disabled="!hasActiveFilters"
        @click="onClearFilters"
      >
        Limpar filtros
      </button>
    </div>

    <!-- Result count meta -->
    <p
      v-if="viewState === 'data'"
      class="text-xs text-muted text-right mb-4"
    >
      {{ pagination.total }} {{ pagination.total === 1 ? 'movimento' : 'movimentos' }} ·
      página {{ pagination.page }} de {{ pagination.totalPages }}
    </p>

    <!-- 4-state region -->
    <BaseSkeleton
      v-if="viewState === 'loading'"
      :rows="5"
    />

    <BaseEmptyState
      v-else-if="viewState === 'empty'"
      heading="Nenhuma movimentação registrada"
      body="As entradas e saídas de estoque aparecerão aqui assim que forem registradas."
    >
      <template #cta>
        <BaseButton
          variant="primary"
          @click="goToEntrada"
        >
          Registrar entrada
        </BaseButton>
      </template>
    </BaseEmptyState>

    <BaseEmptyState
      v-else-if="viewState === 'filter-empty'"
      heading="Nenhuma movimentação encontrada"
      body="Nenhuma movimentação corresponde aos filtros aplicados. Ajuste os filtros e tente novamente."
    >
      <template #cta>
        <BaseButton
          variant="secondary"
          @click="onClearFilters"
        >
          Limpar filtros
        </BaseButton>
      </template>
    </BaseEmptyState>

    <BaseErrorState
      v-else-if="viewState === 'error' && error"
      heading="Não foi possível carregar o histórico"
      :error="error"
      @retry="retry"
    />

    <template v-else-if="viewState === 'data'">
      <table
        class="w-full text-sm text-left border-collapse"
        aria-label="Histórico de movimentações"
      >
        <thead class="bg-brand-50">
          <tr>
            <th
              scope="col"
              class="text-brand-700 font-semibold px-4 py-3 text-xs uppercase tracking-wide"
            >
              Data/hora
            </th>
            <th
              scope="col"
              class="text-brand-700 font-semibold px-4 py-3 text-xs uppercase tracking-wide"
            >
              Tipo
            </th>
            <th
              scope="col"
              class="text-brand-700 font-semibold px-4 py-3 text-xs uppercase tracking-wide"
            >
              Produto
            </th>
            <th
              scope="col"
              class="text-brand-700 font-semibold px-4 py-3 text-xs uppercase tracking-wide text-right"
            >
              Quantidade
            </th>
            <th
              scope="col"
              class="text-brand-700 font-semibold px-4 py-3 text-xs uppercase tracking-wide text-right"
            >
              Valor
            </th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="m in items"
            :key="m.id"
            class="border-t border-line hover:bg-surface transition-colors"
          >
            <td class="px-4 py-3 align-middle text-muted">
              {{ formatDate(m.occurredAt) }}
            </td>
            <td class="px-4 py-3 align-middle">
              <BaseBadge :variant="m.type === 'Inbound' ? 'success' : 'danger'">
                <ArrowDownToLine
                  v-if="m.type === 'Inbound'"
                  class="w-3 h-3"
                  aria-hidden="true"
                />
                <ArrowUpFromLine
                  v-else
                  class="w-3 h-3"
                  aria-hidden="true"
                />
                {{ movementTypeLabel[m.type] }}
              </BaseBadge>
            </td>
            <td class="px-4 py-3 align-middle text-ink truncate max-w-xs">
              <span class="font-medium">{{ m.productCode }}</span>
              <span class="text-muted"> — </span>
              <span>{{ m.productDescription }}</span>
            </td>
            <td class="px-4 py-3 align-middle text-right tabular-nums text-ink">
              {{ formatQuantity(m.quantity) }} un.
            </td>
            <td
              class="px-4 py-3 align-middle text-right tabular-nums text-ink"
              :title="valueTooltip(m)"
            >
              {{ valueFor(m) }}
            </td>
          </tr>
        </tbody>
      </table>

      <div
        v-if="pagination.totalPages > 1"
        class="flex items-center justify-end mt-4"
      >
        <BasePagination
          :page="pagination.page"
          :total-pages="pagination.totalPages"
          :has-next="pagination.hasNext"
          :has-prev="pagination.hasPrev"
          @change="onPageChange"
        />
      </div>
    </template>
  </div>
</template>
