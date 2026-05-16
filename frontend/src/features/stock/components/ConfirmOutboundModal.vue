<script setup lang="ts">
import { computed } from 'vue'
import { Loader2 } from 'lucide-vue-next'
import BaseModal from '@/shared/components/BaseModal.vue'
import BaseButton from '@/shared/components/BaseButton.vue'
import { formatQuantity, formatCurrency } from '@/shared/format'
import type { ProductResponse } from '@/features/products/types'

/**
 * CONF-01: Outbound confirmation modal. Copy is LOCKED VERBATIM by 03-UI-SPEC
 * §"CONF-01 — Outbound Confirmation Modal (LOCKED COPY)". Auditor will grep.
 *
 * Per UI-SPEC and CONTEXT.md D-02:
 *  - Title: "Confirmar saída?"  (NOT "Confirmar saída de estoque?")
 *  - Confirm button uses brand-500 primary (NOT destructive — Outbound is a
 *    normal business op).
 *  - data-autofocus is on Cancelar (safer default; Enter cannot accidentally
 *    confirm).
 *  - Backdrop click and Esc dismiss without API call (form data preserved).
 *
 * NOTE on BaseModal integration: BaseModal owns the `<h2>` rendered from the
 * `title` prop (and the matching `aria-labelledby`). We pass the locked title
 * verbatim. The lead paragraph + definition list + footer slot all render
 * inside the modal body / footer slots.
 */

interface Props {
  open: boolean
  product: ProductResponse | null
  quantity: number
  saleValue: number
  isSubmitting?: boolean
}

const props = withDefaults(defineProps<Props>(), { isSubmitting: false })
defineEmits<{ (e: 'confirm'): void; (e: 'cancel'): void }>()

const remainingStock = computed(() => (props.product?.stockQuantity ?? 0) - props.quantity)
</script>

<template>
  <BaseModal
    :open="open"
    title="Confirmar saída?"
    :dismiss-on-backdrop="!isSubmitting"
    @close="$emit('cancel')"
  >
    <p class="text-sm text-ink mb-4">
      Esta operação registra uma saída de estoque. Confira o resumo abaixo antes de confirmar.
    </p>

    <dl
      v-if="product"
      class="grid grid-cols-[max-content_1fr] gap-x-4 gap-y-2 mb-2 bg-surface rounded-md p-4"
    >
      <dt class="text-xs font-medium text-muted">
        Produto:
      </dt>
      <dd class="text-sm text-ink">
        {{ product.code }} — {{ product.description }}
      </dd>

      <dt class="text-xs font-medium text-muted">
        Quantidade:
      </dt>
      <dd class="text-sm text-ink tabular-nums">
        {{ formatQuantity(quantity) }} unidades
      </dd>

      <dt class="text-xs font-medium text-muted">
        Valor de venda:
      </dt>
      <dd class="text-sm text-ink tabular-nums">
        {{ formatCurrency(saleValue) }}
      </dd>

      <dt class="text-xs font-medium text-muted">
        Saldo atual:
      </dt>
      <dd class="text-sm text-ink tabular-nums">
        {{ formatQuantity(product.stockQuantity) }} unidades
      </dd>

      <dt class="text-xs font-medium text-muted">
        Saldo resultante:
      </dt>
      <dd class="text-sm font-semibold text-ink tabular-nums">
        {{ formatQuantity(remainingStock) }} unidades
      </dd>
    </dl>

    <template #footer>
      <BaseButton
        variant="secondary"
        data-autofocus
        :disabled="isSubmitting"
        @click="$emit('cancel')"
      >
        Cancelar
      </BaseButton>
      <!-- eslint-disable vue/multiline-html-element-content-newline -->
      <BaseButton
        variant="primary"
        :loading="isSubmitting"
        :disabled="isSubmitting"
        @click="$emit('confirm')"
      ><span
        v-if="isSubmitting"
        class="inline-flex items-center gap-2"
      ><Loader2
        class="w-4 h-4 animate-spin"
        aria-hidden="true"
      />Registrando saída...</span><span v-else>Confirmar saída</span></BaseButton>
      <!-- eslint-enable vue/multiline-html-element-content-newline -->
    </template>
  </BaseModal>
</template>
