import { ref, readonly } from 'vue'

export type ToastVariant = 'success' | 'error' | 'info'

export interface ToastItem {
  id: string
  variant: ToastVariant
  message: string
  /** ms until auto-dismiss; 0 disables auto-dismiss */
  durationMs: number
  /** When user is hovering, we pause the auto-dismiss timer */
  paused: boolean
  /** Timestamp set when paused → resume schedules a new timer for the remaining duration */
  pausedAtMs: number
  /** When the timer was last scheduled (used to compute remaining duration on pause) */
  scheduledAtMs: number
  /** setTimeout handle so pause/dismiss can clear it */
  timerHandle: number | null
}

// Module-scoped (singleton): every useToast() call shares the same stack.
const stack = ref<ToastItem[]>([])
const MAX_STACK_SIZE = 3

const DURATIONS: Record<ToastVariant, number> = {
  success: 3_000,
  info: 3_000,
  error: 5_000,
}

function evictOldest(): void {
  // Stack visual order: newest is rendered on top via flex-col-reverse in the container.
  // The "oldest" entry is the one inserted first (lowest index).
  if (stack.value.length > MAX_STACK_SIZE) {
    const oldest = stack.value.shift()
    if (oldest?.timerHandle != null) window.clearTimeout(oldest.timerHandle)
  }
}

function schedule(toast: ToastItem, durationMs: number): void {
  toast.scheduledAtMs = performance.now()
  toast.timerHandle = window.setTimeout(() => dismiss(toast.id), durationMs)
}

function push(variant: ToastVariant, message: string): string {
  const id = crypto.randomUUID()
  const durationMs = DURATIONS[variant]
  const toast: ToastItem = {
    id,
    variant,
    message,
    durationMs,
    paused: false,
    pausedAtMs: 0,
    scheduledAtMs: 0,
    timerHandle: null,
  }
  stack.value.push(toast)
  evictOldest()
  if (durationMs > 0) schedule(toast, durationMs)
  return id
}

export function dismiss(id: string): void {
  const idx = stack.value.findIndex((t) => t.id === id)
  if (idx === -1) return
  const toast = stack.value[idx]
  if (toast.timerHandle != null) window.clearTimeout(toast.timerHandle)
  stack.value.splice(idx, 1)
}

function pause(id: string): void {
  const toast = stack.value.find((t) => t.id === id)
  if (!toast || toast.paused || toast.timerHandle == null) return
  window.clearTimeout(toast.timerHandle)
  toast.timerHandle = null
  toast.paused = true
  toast.pausedAtMs = performance.now()
}

function resume(id: string): void {
  const toast = stack.value.find((t) => t.id === id)
  if (!toast || !toast.paused) return
  toast.paused = false
  const elapsedBefore = toast.pausedAtMs - toast.scheduledAtMs
  const remaining = Math.max(toast.durationMs - elapsedBefore, 500)
  schedule(toast, remaining)
}

export function useToast() {
  return {
    /** Readonly view of the current stack — components iterate this in the container. */
    items: readonly(stack),
    /** Push a success toast (auto-dismiss 3s). */
    success: (message: string) => push('success', message),
    /** Push an error toast (auto-dismiss 5s). */
    error: (message: string) => push('error', message),
    /** Push an info toast (auto-dismiss 3s). */
    info: (message: string) => push('info', message),
    dismiss,
    pause,
    resume,
  }
}
