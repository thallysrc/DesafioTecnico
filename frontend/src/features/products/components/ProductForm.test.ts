/**
 * ProductForm component tests — TEST-06 (Plan 04-03 / Task 1).
 *
 * Strategy (CONTEXT.md D-09): shallow mount with `BaseInput` / `BaseSelect`
 * stubbed as `true`. The Base primitives were proven by Phase 2 implementation
 * and are NOT under test here — we exercise the form wiring (mounts cleanly,
 * exposes the imperative API the parent uses, surfaces `dirtyChange`, and
 * emits the normalized `submit` payload).
 *
 * Mocks: none — ProductForm itself does not perform HTTP; the parent page
 * handles `productsApi.create` based on the emitted `submit` event.
 */
import { describe, it, expect } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import ProductForm from './ProductForm.vue'

function makeWrapper() {
  return mount(ProductForm, {
    global: {
      stubs: { BaseInput: true, BaseSelect: true },
    },
  })
}

describe('ProductForm', () => {
  it('mounts without throwing and renders 4 BaseInputs + 1 BaseSelect (5 fields)', () => {
    const wrapper = makeWrapper()
    expect(wrapper.exists()).toBe(true)

    // Template order: code, description, supplierValue, initialStockQuantity
    // (4 BaseInput) + type (1 BaseSelect).
    const inputs = wrapper.findAllComponents({ name: 'BaseInput' })
    expect(inputs).toHaveLength(4)
    expect(wrapper.findComponent({ name: 'BaseSelect' }).exists()).toBe(true)
  })

  it('exposes onSubmit, isSubmitting, isValid via defineExpose', () => {
    const wrapper = makeWrapper()
    const vm = wrapper.vm as unknown as {
      onSubmit: () => Promise<void>
      isSubmitting: boolean
      isValid: () => boolean
    }
    expect(typeof vm.onSubmit).toBe('function')
    expect('isSubmitting' in vm).toBe(true)
    expect(typeof vm.isValid).toBe('function')
  })

  it('emits dirtyChange(true) when a field is changed', async () => {
    const wrapper = makeWrapper()
    const inputs = wrapper.findAllComponents({ name: 'BaseInput' })

    // Touch the code field — Vee-Validate's `meta.dirty` flips and the watch
    // in ProductForm.vue surfaces it as a dirtyChange emit.
    inputs[0].vm.$emit('update:modelValue', 'P001')
    await flushPromises()

    const events = wrapper.emitted('dirtyChange')
    expect(events).toBeTruthy()
    expect(events!.some((args) => args[0] === true)).toBe(true)
  })

  it('emits submit with the normalized payload when all fields are valid', async () => {
    const wrapper = makeWrapper()
    const inputs = wrapper.findAllComponents({ name: 'BaseInput' })

    // Order in template: [0]=code, [1]=description, [2]=supplierValue, [3]=initialStockQuantity.
    // BaseSelect is for `type`.
    inputs[0].vm.$emit('update:modelValue', 'P001')
    inputs[1].vm.$emit('update:modelValue', 'Notebook')
    wrapper.findComponent({ name: 'BaseSelect' }).vm.$emit('update:modelValue', 'Electronic')

    // supplierValue: BR-locale input. update:modelValue receives the raw string;
    // the form parses it to a JS number. The blur handler normalizes the display.
    inputs[2].vm.$emit('update:modelValue', '100,00')
    inputs[2].vm.$emit('blur')

    inputs[3].vm.$emit('update:modelValue', 5)
    await flushPromises()

    // onSubmit is exposed via defineExpose. handleSubmit returns a function that
    // resolves the Vee-Validate validation pipeline and (on pass) emits 'submit'.
    await (wrapper.vm as unknown as { onSubmit: () => Promise<void> }).onSubmit()
    await flushPromises()

    const emitted = wrapper.emitted('submit')
    expect(emitted).toBeTruthy()
    expect(emitted![0][0]).toMatchObject({
      code: 'P001',
      description: 'Notebook',
      type: 'Electronic',
      supplierValue: 100,
      initialStockQuantity: 5,
    })
  })
})
