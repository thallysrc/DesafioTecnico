import { apiClient } from './client'

export interface HealthResponse {
  status: 'ok' | 'degraded'
  db: 'up' | 'down'
  version: string
  timestamp: string
}

/**
 * GET /api/health — pings backend + DB. Called by `HealthPill.vue` on mount.
 * The 5-second timeout matches UI-SPEC §"Health-Status Pill" behavior rule.
 */
export function fetchHealth(signal?: AbortSignal): Promise<HealthResponse> {
  return apiClient
    .get<HealthResponse>('/health', { signal, timeout: 5_000 })
    .then((r) => r.data)
}
