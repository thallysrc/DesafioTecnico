<script setup lang="ts" generic="TItem extends { id: string }">
interface Props {
  items: TItem[]
  ariaLabel?: string
}

defineProps<Props>()
defineEmits<{ (e: 'rowClick', item: TItem): void }>()
</script>

<template>
  <table
    class="w-full text-sm text-left border-collapse"
    :aria-label="ariaLabel"
  >
    <thead class="bg-brand-50">
      <tr>
        <slot name="header" />
      </tr>
    </thead>
    <tbody>
      <tr
        v-for="item in items"
        :key="item.id"
        tabindex="0"
        class="border-t border-line hover:bg-surface transition-colors cursor-pointer focus:outline-none focus:bg-surface focus:ring-2 focus:ring-inset focus:ring-brand-500"
        @click="$emit('rowClick', item)"
        @keydown.enter="$emit('rowClick', item)"
        @keydown.space.prevent="$emit('rowClick', item)"
      >
        <slot
          name="row"
          :item="item"
        />
      </tr>
    </tbody>
  </table>
</template>
