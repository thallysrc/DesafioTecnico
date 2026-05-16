import { ref, readonly } from 'vue'

export interface ConfirmOptions {
  title: string
  body: string
  confirmLabel?: string
  cancelLabel?: string
  /** Visual variant of the confirm button. Defaults to 'primary'; pass 'destructive' for delete flows. */
  variant?: 'primary' | 'destructive'
}

interface ConfirmState {
  open: boolean
  options: ConfirmOptions | null
  resolve: ((value: boolean) => void) | null
}

const state = ref<ConfirmState>({ open: false, options: null, resolve: null })

export function useConfirm() {
  /**
   * Open the confirm modal and resolve true when user confirms, false when cancelled or escaped.
   * Awaits a single global modal mount (BaseModal at App.vue level). Plan 02-05 mounts the modal.
   */
  function confirm(options: ConfirmOptions): Promise<boolean> {
    return new Promise<boolean>((resolve) => {
      state.value = { open: true, options, resolve }
    })
  }

  function _resolve(value: boolean): void {
    if (state.value.resolve) state.value.resolve(value)
    state.value = { open: false, options: null, resolve: null }
  }

  return {
    confirm,
    /** Read-only view for the modal mount component to consume. */
    state: readonly(state),
    /** Called by the modal mount when user confirms. */
    _accept: () => _resolve(true),
    /** Called by the modal mount when user cancels / escapes / clicks backdrop. */
    _reject: () => _resolve(false),
  }
}
