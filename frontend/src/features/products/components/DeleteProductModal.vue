<script setup lang="ts">
import BaseModal from '@/shared/components/BaseModal.vue'
import BaseButton from '@/shared/components/BaseButton.vue'

interface Props {
  open: boolean
  isSubmitting?: boolean
}
withDefaults(defineProps<Props>(), { isSubmitting: false })
defineEmits<{ (e: 'confirm'): void; (e: 'cancel'): void }>()
</script>

<template>
  <!--
    CONF-02: Soft-delete confirmation modal. Copy is locked verbatim by
    02-UI-SPEC §"Delete confirmation modal" + CONTEXT.md decision block.

    Per Nielsen #5 (prevention) and UI-SPEC line 258, default focus lands on
    the SAFE action (Cancelar). The destructive Excluir button is reachable
    with one Tab keystroke. Backdrop is dismissable only while not submitting.
  -->
  <BaseModal
    :open="open"
    title="Excluir produto?"
    :dismiss-on-backdrop="!isSubmitting"
    @close="$emit('cancel')"
  >
    <p class="text-sm text-ink">
      Esta ação marca o produto como excluído. O histórico de movimentações permanece visível.
    </p>
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
        variant="destructive"
        :loading="isSubmitting"
        :disabled="isSubmitting"
        @click="$emit('confirm')"
      ><span v-if="isSubmitting">Excluindo...</span><span v-else>Excluir</span></BaseButton>
      <!-- eslint-enable vue/multiline-html-element-content-newline -->
    </template>
  </BaseModal>
</template>
