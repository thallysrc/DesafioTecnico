<script setup lang="ts">
import { useToast } from '@/shared/composables/useToast'
import BaseToast from './BaseToast.vue'

const { items, dismiss, pause, resume } = useToast()
</script>

<template>
  <Teleport to="body">
    <div
      class="fixed bottom-6 right-6 z-[60] flex flex-col-reverse gap-2 motion-reduce:transition-none"
      aria-live="polite"
    >
      <BaseToast
        v-for="toast in items"
        :key="toast.id"
        :toast="toast"
        @dismiss="dismiss(toast.id)"
        @pause="pause(toast.id)"
        @resume="resume(toast.id)"
      />
    </div>
  </Teleport>
</template>
