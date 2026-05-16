<script setup lang="ts">
import { computed, ref, useId, watch, nextTick } from 'vue'

interface Option {
  value: string
  label: string
}

interface Props {
  modelValue: string | null
  label: string
  options: Option[]
  id?: string
  placeholder?: string
  required?: boolean
  disabled?: boolean
  error?: string
}

const props = withDefaults(defineProps<Props>(), {
  id: undefined,
  placeholder: undefined,
  required: false,
  disabled: false,
  error: undefined,
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: string | null): void
  (e: 'blur', ev: FocusEvent): void
}>()

const generatedId = useId()
const inputId = computed(() => props.id ?? `combo-${generatedId}`)
const errorId = computed(() => `${inputId.value}-error`)
const listboxId = computed(() => `${inputId.value}-list`)

const search = ref('')
const open = ref(false)
const activeIndex = ref(-1)
const inputRef = ref<HTMLInputElement | null>(null)

const filtered = computed(() =>
  search.value === ''
    ? props.options
    : props.options.filter((o) => o.label.toLowerCase().includes(search.value.toLowerCase())),
)

const selectedLabel = computed(() => {
  const sel = props.options.find((o) => o.value === props.modelValue)
  return sel?.label ?? ''
})

watch(
  () => props.modelValue,
  () => {
    if (!open.value) search.value = selectedLabel.value
  },
  { immediate: true },
)

function openList(): void {
  if (props.disabled) return
  open.value = true
  search.value = ''
  activeIndex.value = props.options.findIndex((o) => o.value === props.modelValue)
}

function closeList(): void {
  open.value = false
  search.value = selectedLabel.value
}

function select(opt: Option): void {
  emit('update:modelValue', opt.value)
  closeList()
}

function onInput(ev: Event): void {
  search.value = (ev.target as HTMLInputElement).value
  open.value = true
  activeIndex.value = 0
}

function onBlur(ev: FocusEvent): void {
  // Defer to allow @mousedown on option to fire first.
  setTimeout(() => closeList(), 100)
  emit('blur', ev)
}

function onKeydown(ev: KeyboardEvent): void {
  if (!open.value && ['ArrowDown', 'Enter', ' '].includes(ev.key)) {
    ev.preventDefault()
    openList()
    return
  }
  if (!open.value) return
  if (ev.key === 'ArrowDown') {
    ev.preventDefault()
    activeIndex.value = (activeIndex.value + 1) % Math.max(filtered.value.length, 1)
  } else if (ev.key === 'ArrowUp') {
    ev.preventDefault()
    activeIndex.value =
      activeIndex.value <= 0 ? filtered.value.length - 1 : activeIndex.value - 1
  } else if (ev.key === 'Enter') {
    ev.preventDefault()
    if (activeIndex.value >= 0 && filtered.value[activeIndex.value]) {
      select(filtered.value[activeIndex.value])
    }
  } else if (ev.key === 'Escape') {
    ev.preventDefault()
    closeList()
    void nextTick(() => inputRef.value?.focus())
  }
}
</script>

<template>
  <div>
    <label
      :for="inputId"
      class="block text-sm font-medium text-ink mb-1"
    >
      {{ label }}
      <span
        v-if="required"
        class="text-danger"
        aria-hidden="true"
      >*</span>
    </label>
    <div class="relative">
      <input
        :id="inputId"
        ref="inputRef"
        :value="open ? search : selectedLabel"
        :placeholder="placeholder"
        :disabled="disabled"
        role="combobox"
        :aria-expanded="open"
        :aria-controls="listboxId"
        :aria-activedescendant="activeIndex >= 0 ? `${listboxId}-${activeIndex}` : undefined"
        :aria-invalid="error ? 'true' : undefined"
        :aria-required="required || undefined"
        :aria-describedby="error ? errorId : undefined"
        :class="[
          'w-full border rounded-md px-3 py-2 text-sm text-ink placeholder:text-muted outline-none transition-shadow bg-white',
          error
            ? 'border-danger focus:ring-2 focus:ring-danger'
            : 'border-line focus:ring-2 focus:ring-brand-500 focus:border-brand-500',
        ]"
        autocomplete="off"
        @input="onInput"
        @focus="openList"
        @keydown="onKeydown"
        @blur="onBlur"
      >
      <ul
        v-if="open && filtered.length > 0"
        :id="listboxId"
        role="listbox"
        class="absolute z-10 mt-1 w-full max-h-64 overflow-y-auto bg-white border border-line rounded-md shadow-lg"
      >
        <li
          v-for="(opt, idx) in filtered"
          :id="`${listboxId}-${idx}`"
          :key="opt.value"
          role="option"
          :aria-selected="idx === activeIndex"
          :class="[
            'px-3 py-2 text-sm cursor-pointer',
            idx === activeIndex ? 'bg-brand-50 text-brand-700' : 'text-ink hover:bg-surface',
          ]"
          @mousedown.prevent="select(opt)"
        >
          {{ opt.label }}
        </li>
      </ul>
    </div>
    <p
      v-if="error"
      :id="errorId"
      class="text-xs text-danger mt-1"
    >
      {{ error }}
    </p>
  </div>
</template>
