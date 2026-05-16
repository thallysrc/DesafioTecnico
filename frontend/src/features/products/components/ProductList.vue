<script setup lang="ts">
import BaseTable from '@/shared/components/BaseTable.vue'
import BaseBadge from '@/shared/components/BaseBadge.vue'
import { productTypeLabel } from '@/shared/labels'
import { formatCurrency, formatQuantity } from '@/shared/format'
import type { ProductResponse } from '../types'

interface Props {
  items: ProductResponse[]
}
defineProps<Props>()
defineEmits<{ (e: 'rowClick', product: ProductResponse): void }>()
</script>

<template>
  <BaseTable
    :items="items"
    aria-label="Lista de produtos"
    @row-click="(item: ProductResponse) => $emit('rowClick', item)"
  >
    <template #header>
      <th
        scope="col"
        class="text-brand-700 font-semibold px-4 py-3 text-xs uppercase tracking-wide"
      >
        Código
      </th>
      <th
        scope="col"
        class="text-brand-700 font-semibold px-4 py-3 text-xs uppercase tracking-wide"
      >
        Descrição
      </th>
      <th
        scope="col"
        class="text-brand-700 font-semibold px-4 py-3 text-xs uppercase tracking-wide"
      >
        Tipo
      </th>
      <th
        scope="col"
        class="text-brand-700 font-semibold px-4 py-3 text-xs uppercase tracking-wide text-right"
      >
        Estoque
      </th>
      <th
        scope="col"
        class="text-brand-700 font-semibold px-4 py-3 text-xs uppercase tracking-wide text-right"
      >
        Valor fornecedor
      </th>
      <th
        scope="col"
        class="text-brand-700 font-semibold px-4 py-3 text-xs uppercase tracking-wide"
      >
        Status
      </th>
    </template>
    <template #row="{ item }">
      <td
        :class="[
          'px-4 py-3 align-middle text-ink font-medium',
          item.deletedAt ? 'opacity-60' : '',
        ]"
      >
        {{ item.code }}
      </td>
      <td
        :class="[
          'px-4 py-3 align-middle text-ink max-w-xs truncate',
          item.deletedAt ? 'opacity-60' : '',
        ]"
      >
        {{ item.description }}
      </td>
      <td
        :class="[
          'px-4 py-3 align-middle text-ink',
          item.deletedAt ? 'opacity-60' : '',
        ]"
      >
        {{ productTypeLabel[item.type] }}
      </td>
      <td
        :class="[
          'px-4 py-3 align-middle text-ink text-right tabular-nums',
          item.deletedAt ? 'opacity-60' : '',
        ]"
      >
        {{ formatQuantity(item.stockQuantity) }} un.
      </td>
      <td
        :class="[
          'px-4 py-3 align-middle text-ink text-right tabular-nums',
          item.deletedAt ? 'opacity-60' : '',
        ]"
      >
        {{ formatCurrency(item.supplierValue) }}
      </td>
      <td class="px-4 py-3 align-middle">
        <!-- eslint-disable vue/multiline-html-element-content-newline -->
        <BaseBadge
          v-if="item.deletedAt"
          variant="warning"
        >Excluído</BaseBadge>
        <!-- eslint-enable vue/multiline-html-element-content-newline -->
      </td>
    </template>
  </BaseTable>
</template>
