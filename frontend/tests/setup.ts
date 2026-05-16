/**
 * Vitest global setup. Intentionally minimal — composable + component tests
 * mock their own api modules via `vi.mock(...)`. Add global stubs here only
 * when they apply to >2 test files (e.g., `crypto.randomUUID` shim if a
 * future component reaches for it without going through the api seam).
 */
export {}
