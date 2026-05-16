import { defineConfig } from 'vitest/config'
import vue from '@vitejs/plugin-vue'
import path from 'node:path'

/**
 * Vitest configuration. Mirrors `vite.config.ts`'s `@` alias so test files can
 * import from `@/shared/...` exactly as production code does (D-07).
 *
 * - `environment: 'happy-dom'` per D-06 (faster than jsdom; we don't reach for
 *   any jsdom-exclusive APIs in composable + component tests).
 * - `globals: true` per CONTEXT.md "Claude's Discretion" — terser tests with
 *   `describe`/`it`/`expect`/`vi` available without explicit imports (we still
 *   import them in test files for IDE friendliness; the flag just keeps the
 *   door open).
 * - `setupFiles` points to a minimal placeholder for future global hooks.
 * - `include` picks up co-located `*.test.ts` next to the production code.
 */
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  test: {
    environment: 'happy-dom',
    globals: true,
    setupFiles: ['./tests/setup.ts'],
    include: ['src/**/*.test.ts', 'tests/**/*.test.ts'],
  },
})
