<script setup lang="ts">
import { computed, ref, watch, onBeforeUnmount, nextTick } from 'vue'

interface Props {
  open: boolean
  title: string
  /** Element to return focus to when modal closes. Caller passes the trigger ref. */
  returnFocusTo?: HTMLElement | null
  /** When true, clicking the backdrop closes the modal. Defaults to true. */
  dismissOnBackdrop?: boolean
}

const props = withDefaults(defineProps<Props>(), {
  dismissOnBackdrop: true,
  returnFocusTo: null,
})

const emit = defineEmits<{ (e: 'close'): void }>()

const contentRef = ref<HTMLDivElement | null>(null)
let previouslyFocused: HTMLElement | null = null

const titleId = computed(() => `modal-${props.title.replace(/\W+/g, '-')}-title`)

function close(): void {
  emit('close')
}

function trapFocus(ev: KeyboardEvent): void {
  if (!contentRef.value) return
  const focusables = contentRef.value.querySelectorAll<HTMLElement>(
    'button:not([disabled]), [href], input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
  )
  if (focusables.length === 0) return
  const first = focusables[0]
  const last = focusables[focusables.length - 1]
  if (ev.shiftKey && document.activeElement === first) {
    ev.preventDefault()
    last.focus()
  } else if (!ev.shiftKey && document.activeElement === last) {
    ev.preventDefault()
    first.focus()
  }
}

function onKeydown(ev: KeyboardEvent): void {
  if (ev.key === 'Escape') {
    ev.preventDefault()
    close()
  } else if (ev.key === 'Tab') {
    trapFocus(ev)
  }
}

watch(
  () => props.open,
  async (isOpen) => {
    if (isOpen) {
      previouslyFocused = (document.activeElement as HTMLElement) ?? null
      document.body.style.overflow = 'hidden'
      document.addEventListener('keydown', onKeydown)
      await nextTick()
      const target =
        contentRef.value?.querySelector<HTMLElement>('[data-autofocus]') ??
        contentRef.value?.querySelector<HTMLElement>(
          'button:not([disabled]), input, select, textarea',
        )
      target?.focus()
    } else {
      document.body.style.overflow = ''
      document.removeEventListener('keydown', onKeydown)
      const focusTarget = props.returnFocusTo ?? previouslyFocused
      focusTarget?.focus()
    }
  },
  { immediate: false },
)

onBeforeUnmount(() => {
  document.body.style.overflow = ''
  document.removeEventListener('keydown', onKeydown)
})
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition-opacity duration-150"
      leave-active-class="transition-opacity duration-150"
      enter-from-class="opacity-0"
      leave-to-class="opacity-0"
    >
      <div
        v-if="open"
        class="fixed inset-0 bg-ink/50 flex items-center justify-center z-50 motion-reduce:transition-none"
        @click.self="dismissOnBackdrop && close()"
      >
        <div
          ref="contentRef"
          role="dialog"
          aria-modal="true"
          :aria-labelledby="titleId"
          class="bg-white rounded-lg shadow-xl p-6 max-w-md w-full mx-4"
        >
          <h2
            :id="titleId"
            class="text-lg font-semibold text-ink mb-2"
          >
            {{ title }}
          </h2>
          <div class="text-sm text-ink mb-6">
            <slot />
          </div>
          <div class="flex items-center justify-end gap-3">
            <slot name="footer" />
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>
