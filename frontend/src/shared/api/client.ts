import axios, { type AxiosError } from 'axios'

/**
 * Canonical error envelope. Mirrors backend ErrorResponse exactly (Plan 02-02).
 * The interceptor below normalizes every rejection to this shape so UI code reads
 * `error.hint`, `error.errorCode`, `error.traceId` without coercion.
 */
export interface ApiError {
  errorCode: string
  category: 'VALIDATION' | 'BUSINESS_RULE' | 'NOT_FOUND' | 'INTERNAL'
  message: string
  hint?: string
  statusCode: number
  retryable: boolean
  details?: Record<string, unknown> | { fields: Array<{ field: string; message: string; rejectedValue?: unknown }> }
  traceId: string
  timestamp: string
}

/**
 * Single Axios instance. baseURL '/api' resolves via Vite proxy to http://backend:8080/api (D-08).
 */
export const apiClient = axios.create({
  baseURL: '/api',
  timeout: 10_000,
  headers: { 'Content-Type': 'application/json' },
})

apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ApiError>) => {
    // Backend returned a canonical ErrorResponse → pass through.
    if (
      error.response?.data &&
      typeof error.response.data === 'object' &&
      'errorCode' in error.response.data
    ) {
      return Promise.reject(error.response.data)
    }
    // Network/timeout/CORS/etc → synthesize a NETWORK_ERROR ApiError.
    const fallback: ApiError = {
      errorCode: 'NETWORK_ERROR',
      category: 'INTERNAL',
      message: 'Erro ao se comunicar com o servidor',
      hint: 'Não foi possível conectar. Verifique sua conexão e tente novamente.',
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
