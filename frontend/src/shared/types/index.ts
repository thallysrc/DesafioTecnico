/**
 * Pagination metadata mirroring backend's PaginationMeta record (Plan 02-01).
 * Wire shape: { page, pageSize, total, totalPages, hasNext, hasPrev }.
 */
export interface PaginationMeta {
  page: number
  pageSize: number
  total: number
  totalPages: number
  hasNext: boolean
  hasPrev: boolean
}

/**
 * Canonical pagination envelope. Backend emits `_links` (with underscore prefix);
 * the interceptor passes through verbatim. Generic over the row type.
 */
export interface PagedResult<TItem> {
  items: TItem[]
  pagination: PaginationMeta
  _links: Record<string, string>
}
