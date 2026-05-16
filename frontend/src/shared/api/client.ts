import axios, { type AxiosError } from 'axios'

/**
 * Canonical error envelope returned by the backend. Phase 2 expands the interceptor below
 * to normalize every rejected request into this shape so UI components consume `.hint` directly.
 */
export interface ApiError {
  errorCode: string
  category: 'VALIDATION' | 'BUSINESS_RULE' | 'NOT_FOUND' | 'INTERNAL'
  message: string
  hint?: string
  statusCode: number
  retryable: boolean
  details?: Record<string, unknown>
  traceId: string
  timestamp: string
}

/**
 * Single Axios instance for the entire app. D-08: baseURL = '/api'. Calls written as
 * `apiClient.get('/health')` resolve to `/api/health`, which the Vite dev server proxies
 * to `http://backend:8080/api/health`.
 */
export const apiClient = axios.create({
  baseURL: '/api',
  timeout: 10_000,
  headers: { 'Content-Type': 'application/json' },
})

// Phase 1 interceptor: lightweight. Phase 2 expands this with full ApiError normalization,
// idempotency-key generation for POST /stock-movements, etc.
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiError>) => {
    if (error.response?.data?.errorCode) {
      return Promise.reject(error.response.data)
    }
    const fallback: ApiError = {
      errorCode: 'NETWORK_ERROR',
      category: 'INTERNAL',
      message: 'Erro ao se comunicar com o servidor',
      hint: 'Verifique sua conexão e tente novamente.',
      statusCode: error.response?.status ?? 0,
      retryable: true,
      traceId: 'client',
      timestamp: new Date().toISOString(),
    }
    return Promise.reject(fallback)
  },
)

/** UUID v4 generator for Idempotency-Key headers (Phase 3 wires this into movement POSTs). */
export function generateIdempotencyKey(): string {
  return crypto.randomUUID()
}
