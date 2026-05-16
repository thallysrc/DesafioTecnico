<script setup lang="ts">
import AppShell from '@/shared/components/AppShell.vue'
import BaseToastContainer from '@/shared/components/BaseToastContainer.vue'
</script>

<template>
  <AppShell>
    <RouterView />
  </AppShell>
  <BaseToastContainer />
</template>
