<script setup lang="ts">
import { computed } from 'vue'
import { productTypeLabel } from '@/shared/labels'
import { formatCurrency, formatDate, formatQuantity } from '@/shared/format'
import type { ProductResponse } from '../types'

interface Props {
  product: ProductResponse
}
const props = defineProps<Props>()

const showUpdatedAt = computed(() => props.product.updatedAt !== props.product.createdAt)
</script>

<template>
  <dl class="flex flex-col gap-4">
    <div>
      <dt class="block text-sm font-medium text-ink mb-1">
        Código
      </dt>
      <dd class="border-0 bg-surface text-ink px-3 py-2 rounded-md select-text text-sm">
        {{ product.code }}
      </dd>
    </div>
    <div>
      <dt class="block text-sm font-medium text-ink mb-1">
        Descrição
      </dt>
      <dd class="border-0 bg-surface text-ink px-3 py-2 rounded-md select-text text-sm">
        {{ product.description }}
      </dd>
    </div>
    <div>
      <dt class="block text-sm font-medium text-ink mb-1">
        Tipo
      </dt>
      <dd class="border-0 bg-surface text-ink px-3 py-2 rounded-md select-text text-sm">
        {{ productTypeLabel[product.type] }}
      </dd>
    </div>
    <div>
      <dt class="block text-sm font-medium text-ink mb-1">
        Valor do fornecedor
      </dt>
      <dd class="border-0 bg-surface text-ink px-3 py-2 rounded-md select-text text-sm tabular-nums">
        {{ formatCurrency(product.supplierValue) }}
      </dd>
    </div>
    <div>
      <dt class="block text-sm font-medium text-ink mb-1">
        Estoque atual
      </dt>
      <dd class="border-0 bg-surface text-ink px-3 py-2 rounded-md select-text text-sm tabular-nums">
        {{ formatQuantity(product.stockQuantity) }} un.
      </dd>
    </div>
    <div>
      <dt class="block text-sm font-medium text-ink mb-1">
        Criado em
      </dt>
      <dd class="border-0 bg-surface text-ink px-3 py-2 rounded-md select-text text-sm">
        {{ formatDate(product.createdAt) }}
      </dd>
    </div>
    <div v-if="showUpdatedAt">
      <dt class="block text-sm font-medium text-ink mb-1">
        Atualizado em
      </dt>
      <dd class="border-0 bg-surface text-ink px-3 py-2 rounded-md select-text text-sm">
        {{ formatDate(product.updatedAt) }}
      </dd>
    </div>
    <div v-if="product.deletedAt">
      <dt class="block text-sm font-medium text-ink mb-1">
        Excluído em
      </dt>
      <dd class="border-0 bg-danger/10 text-danger px-3 py-2 rounded-md select-text text-sm font-medium">
        {{ formatDate(product.deletedAt) }}
      </dd>
    </div>
  </dl>
</template>
