import { createRouter, createWebHistory, type RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
  // FRONT-10: redirect root to /products
  { path: '/', redirect: '/products' },
  {
    path: '/products',
    name: 'products',
    component: () => import('@/features/products/pages/ProductsPage.vue'),
    meta: { title: 'Produtos' },
  },
  {
    path: '/stock-movements',
    name: 'stock-movements',
    component: () => import('@/features/stock/pages/StockMovementsPage.vue'),
    meta: { title: 'Movimentação de Estoque' },
  },
  // Fallback: anything unknown also goes to /products in Phase 1 (no 404 page yet)
  { path: '/:pathMatch(.*)*', redirect: '/products' },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

// Update document.title from route.meta.title (UI-SPEC §"Accessibility Floor")
router.afterEach((to) => {
  const title = (to.meta?.title as string | undefined) ?? 'StockEasy'
  document.title = title === 'StockEasy' ? 'StockEasy' : `${title} — StockEasy`
})

export default router
