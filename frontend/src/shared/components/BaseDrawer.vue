<script setup lang="ts">
import { computed, ref, watch, onBeforeUnmount, nextTick } from 'vue'
import { X } from 'lucide-vue-next'

interface Props {
  open: boolean
  title: string
  /** When true, backdrop click closes the drawer. Default true. */
  dismissOnBackdrop?: boolean
  /** Optional 'isDirty' guard — when true and dismissOnBackdrop attempts to close, emits 'requestClose' instead. */
  guardClose?: boolean
  returnFocusTo?: HTMLElement | null
}

const props = withDefaults(defineProps<Props>(), {
  dismissOnBackdrop: true,
  guardClose: false,
  returnFocusTo: null,
})

const emit = defineEmits<{
  (e: 'close'): void
  /** Emitted when user tries to close while guardClose is true. Consumer decides (e.g. native confirm). */
  (e: 'requestClose'): void
}>()

const panelRef = ref<HTMLDivElement | null>(null)
let previouslyFocused: HTMLElement | null = null

const titleId = computed(() => `drawer-${props.title.replace(/\W+/g, '-')}-title`)

function attemptClose(): void {
  if (props.guardClose) emit('requestClose')
  else emit('close')
}

function trapFocus(ev: KeyboardEvent): void {
  if (!panelRef.value) return
  const focusables = panelRef.value.querySelectorAll<HTMLElement>(
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
    attemptClose()
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
        panelRef.value?.querySelector<HTMLElement>('[data-autofocus]') ??
        panelRef.value?.querySelector<HTMLElement>(
          'input, select, textarea, button:not([disabled])',
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
      enter-active-class="transition-opacity duration-200"
      leave-active-class="transition-opacity duration-150"
      enter-from-class="opacity-0"
      leave-to-class="opacity-0"
    >
      <div
        v-if="open"
        class="fixed inset-0 bg-ink/50 z-50 motion-reduce:transition-none"
        @click.self="dismissOnBackdrop && attemptClose()"
      />
    </Transition>
    <Transition
      enter-active-class="transition-transform duration-200 ease-out motion-reduce:transition-none"
      leave-active-class="transition-transform duration-150 ease-in motion-reduce:transition-none"
      enter-from-class="translate-x-full"
      leave-to-class="translate-x-full"
    >
      <div
        v-if="open"
        ref="panelRef"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="titleId"
        class="fixed top-0 right-0 h-screen w-[480px] bg-white shadow-2xl z-[51] flex flex-col"
      >
        <header class="flex items-center justify-between px-6 py-4 border-b border-line">
          <h2
            :id="titleId"
            class="text-lg font-semibold text-ink"
          >
            {{ title }}
          </h2>
          <button
            type="button"
            class="inline-flex items-center justify-center w-10 h-10 rounded-md text-muted hover:bg-surface hover:text-ink transition-colors focus:outline-none focus:ring-2 focus:ring-brand-500"
            aria-label="Fechar"
            @click="attemptClose"
          >
            <X
              class="w-5 h-5"
              aria-hidden="true"
            />
          </button>
        </header>
        <div class="flex-1 overflow-y-auto px-6 py-6">
          <slot />
        </div>
        <footer
          v-if="$slots.footer"
          class="flex items-center justify-between gap-3 px-6 py-4 border-t border-line bg-white"
        >
          <slot name="footer" />
        </footer>
      </div>
    </Transition>
  </Teleport>
</template>
