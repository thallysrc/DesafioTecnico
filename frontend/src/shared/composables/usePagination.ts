import { ref, computed } from 'vue'
import { useRoute, useRouter, type LocationQueryRaw } from 'vue-router'
import type { PaginationMeta } from '@/shared/types'

export interface UsePaginationOptions {
  defaultPage?: number
  defaultPageSize?: number
  /** When true, page/pageSize round-trip through `?page=` and `?pageSize=` query strings. */
  syncUrl?: boolean
}

export function usePagination(options: UsePaginationOptions = {}) {
  const { defaultPage = 1, defaultPageSize = 30, syncUrl = true } = options

  const route = useRoute()
  const router = useRouter()

  const page = ref(defaultPage)
  const pageSize = ref(defaultPageSize)
  const total = ref(0)
  const totalPages = ref(0)
  const hasNext = ref(false)
  const hasPrev = ref(false)

  // Hydrate from URL on mount.
  if (syncUrl) {
    const urlPage = Number(route.query.page)
    const urlPageSize = Number(route.query.pageSize)
    if (Number.isInteger(urlPage) && urlPage >= 1) page.value = urlPage
    if (Number.isInteger(urlPageSize) && urlPageSize >= 1 && urlPageSize <= 100) {
      pageSize.value = urlPageSize
    }
  }

  function applyMeta(meta: PaginationMeta): void {
    page.value = meta.page
    pageSize.value = meta.pageSize
    total.value = meta.total
    totalPages.value = meta.totalPages
    hasNext.value = meta.hasNext
    hasPrev.value = meta.hasPrev
  }

  async function goToPage(nextPage: number): Promise<void> {
    page.value = Math.max(1, nextPage)
    if (syncUrl) {
      const nextQuery: LocationQueryRaw = { ...route.query, page: String(page.value) }
      await router.replace({ query: nextQuery })
    }
  }

  const summary = computed(() => {
    if (total.value === 0) return '0 produtos'
    const start = (page.value - 1) * pageSize.value + 1
    const end = Math.min(page.value * pageSize.value, total.value)
    return `${start}–${end} de ${total.value} produtos · página ${page.value} de ${totalPages.value}`
  })

  return {
    page,
    pageSize,
    total,
    totalPages,
    hasNext,
    hasPrev,
    applyMeta,
    goToPage,
    summary,
  }
}
