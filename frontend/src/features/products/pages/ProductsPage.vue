<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { Plus } from 'lucide-vue-next'
import BaseButton from '@/shared/components/BaseButton.vue'
import BaseDrawer from '@/shared/components/BaseDrawer.vue'
import BaseSkeleton from '@/shared/components/BaseSkeleton.vue'
import BaseEmptyState from '@/shared/components/BaseEmptyState.vue'
import BaseErrorState from '@/shared/components/BaseErrorState.vue'
import BasePagination from '@/shared/components/BasePagination.vue'
import BaseToggle from '@/shared/components/BaseToggle.vue'
import { useToast } from '@/shared/composables/useToast'
import { useProducts } from '../composables/useProducts'
import ProductList from '../components/ProductList.vue'
import ProductForm from '../components/ProductForm.vue'
import ProductDetail from '../components/ProductDetail.vue'
import DeleteProductModal from '../components/DeleteProductModal.vue'
import type { ProductResponse } from '../types'
import type { CreateProductForm } from '../schemas/createProductSchema'
import type { ApiError } from '@/shared/api/client'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const {
  items,
  pagination,
  loading,
  error,
  showDeleted,
  fetchPage,
  setShowDeleted,
  create,
  softDelete,
  retry,
} = useProducts()

// -- Drawer / modal mount state --------------------------------------------
const createOpen = ref(false)
const detailProduct = ref<ProductResponse | null>(null)
const deleteModalOpen = ref(false)
const deleting = ref(false)
const formRef = ref<InstanceType<typeof ProductForm> | null>(null)
const formIsDirty = ref(false)

// Helpers for the template — kept outside templates so Vue lint doesn't
// misinterpret `?` chains as expressions it doesn't understand.
const submitDisabled = computed(() => {
  const f = formRef.value
  if (!f) return true
  // Vue auto-unwraps refs exposed via defineExpose at the component boundary,
  // so `isSubmitting` here is a plain boolean; `isValid` is a function.
  return f.isSubmitting === true || !f.isValid?.()
})
const submitInFlight = computed(() => formRef.value?.isSubmitting === true)

// -- Initial hydrate from URL ----------------------------------------------
onMounted(async () => {
  const urlPage = Number(route.query.page) || 1
  const urlPageSize = Math.min(Math.max(Number(route.query.pageSize) || 30, 1), 100)
  const urlIncludeDeleted = route.query.includeDeleted === 'true'

  showDeleted.value = urlIncludeDeleted
  await fetchPage(urlPage, urlPageSize)
})

// -- URL sync on state changes ---------------------------------------------
async function syncUrl(): Promise<void> {
  const nextQuery: Record<string, string> = {
    page: String(pagination.value.page),
    pageSize: String(pagination.value.pageSize),
  }
  if (showDeleted.value) nextQuery.includeDeleted = 'true'
  await router.replace({ query: nextQuery })
}

watch(
  () => [pagination.value.page, pagination.value.pageSize, showDeleted.value],
  () => {
    void syncUrl()
  },
)

// -- 4-state classification (UI-SPEC §"List 4-State Contract") -------------
type ViewState = 'loading' | 'empty' | 'filter-empty' | 'error' | 'data'
const viewState = computed<ViewState>(() => {
  if (loading.value && items.value.length === 0) return 'loading'
  if (error.value) return 'error'
  if (items.value.length === 0) return showDeleted.value ? 'filter-empty' : 'empty'
  return 'data'
})

// -- Create flow -----------------------------------------------------------
function openCreate(): void {
  createOpen.value = true
}

function closeCreate(): void {
  // If form is dirty, confirm before discarding. Native confirm is the
  // simplest UX per UI-SPEC line 401.
  if (formIsDirty.value) {
    const ok = window.confirm('Descartar alterações?')
    if (!ok) return
  }
  createOpen.value = false
  formIsDirty.value = false
}

async function onCreateSubmit(values: CreateProductForm): Promise<void> {
  try {
    await create(values)
    toast.success('Produto cadastrado com sucesso')
    createOpen.value = false
    formIsDirty.value = false
  } catch (e) {
    const apiError = e as ApiError
    // VALIDATION_ERROR fields are surfaced inline by ProductForm via setFieldError.
    // We still dispatch a summary toast (D-09 hint fallback for non-validation errors).
    if (apiError.errorCode === 'VALIDATION_ERROR') {
      toast.error('Verifique os campos destacados')
    } else {
      toast.error(apiError.hint ?? apiError.message)
    }
  }
}

function submitCreate(): void {
  if (!formRef.value) return
  void formRef.value.onSubmit()
}

// -- Detail flow -----------------------------------------------------------
function openDetail(product: ProductResponse): void {
  detailProduct.value = product
}

function closeDetail(): void {
  // Don't close the detail drawer while the delete modal is open or a
  // delete is in flight — the modal sits on top of the drawer.
  if (deleteModalOpen.value || deleting.value) return
  detailProduct.value = null
}

// -- Delete flow -----------------------------------------------------------
function openDeleteModal(): void {
  deleteModalOpen.value = true
}

async function confirmDelete(): Promise<void> {
  if (!detailProduct.value) return
  deleting.value = true
  try {
    await softDelete(detailProduct.value.id)
    toast.success('Produto excluído')
    deleteModalOpen.value = false
    detailProduct.value = null
  } catch (e) {
    const apiError = e as ApiError
    toast.error(apiError.hint ?? apiError.message)
    // Modal stays open per UI-SPEC §"Delete confirmation modal — Submit error path".
  } finally {
    deleting.value = false
  }
}

function cancelDelete(): void {
  if (deleting.value) return
  deleteModalOpen.value = false
}

// -- Pagination ------------------------------------------------------------
async function onPageChange(nextPage: number): Promise<void> {
  await fetchPage(nextPage)
}
</script>

<template>
  <div class="max-w-7xl mx-auto">
    <!-- Header row -->
    <div class="flex items-center justify-between mb-6">
      <h1 class="text-2xl font-semibold text-ink">
        Produtos
      </h1>
      <BaseButton
        variant="primary"
        @click="openCreate"
      >
        <span class="inline-flex items-center gap-2">
          <Plus
            class="w-4 h-4"
            aria-hidden="true"
          />
          Cadastrar produto
        </span>
      </BaseButton>
    </div>

    <!-- Controls row -->
    <div class="flex items-center justify-between mb-4">
      <BaseToggle
        :model-value="showDeleted"
        label="Mostrar excluídos"
        aria-label="Mostrar produtos excluídos"
        @update:model-value="setShowDeleted"
      />
      <p
        v-if="viewState === 'data'"
        class="text-xs text-muted"
      >
        {{ pagination.total }} produtos · página {{ pagination.page }} de {{ pagination.totalPages }}
      </p>
    </div>

    <!-- 4-state region -->
    <BaseSkeleton
      v-if="viewState === 'loading'"
      :rows="5"
    />

    <BaseEmptyState
      v-else-if="viewState === 'empty'"
      heading="Nenhum produto cadastrado"
      body="Comece cadastrando o primeiro produto do seu catálogo. Você poderá registrar entradas e saídas de estoque em seguida."
    >
      <template #cta>
        <BaseButton
          variant="primary"
          @click="openCreate"
        >
          Cadastrar primeiro produto
        </BaseButton>
      </template>
    </BaseEmptyState>

    <BaseEmptyState
      v-else-if="viewState === 'filter-empty'"
      heading="Nenhum produto excluído"
      body="Quando você excluir um produto, ele aparecerá aqui. Os produtos excluídos preservam o histórico de movimentações."
    />

    <BaseErrorState
      v-else-if="viewState === 'error' && error"
      heading="Não foi possível carregar os produtos"
      :error="error"
      @retry="retry"
    />

    <template v-else-if="viewState === 'data'">
      <ProductList
        :items="items"
        @row-click="openDetail"
      />
      <div
        v-if="pagination.totalPages > 1"
        class="flex items-center justify-end mt-4"
      >
        <BasePagination
          :page="pagination.page"
          :total-pages="pagination.totalPages"
          :has-next="pagination.hasNext"
          :has-prev="pagination.hasPrev"
          @change="onPageChange"
        />
      </div>
    </template>

    <!-- Create drawer -->
    <BaseDrawer
      :open="createOpen"
      title="Cadastrar produto"
      :guard-close="formIsDirty"
      @close="createOpen = false; formIsDirty = false"
      @request-close="closeCreate"
    >
      <ProductForm
        ref="formRef"
        @submit="onCreateSubmit"
        @dirty-change="(v: boolean) => (formIsDirty = v)"
      />
      <template #footer>
        <BaseButton
          variant="secondary"
          :disabled="submitInFlight"
          @click="closeCreate"
        >
          Cancelar
        </BaseButton>
        <BaseButton
          variant="primary"
          :disabled="submitDisabled"
          :loading="submitInFlight"
          @click="submitCreate"
        >
          <span v-if="submitInFlight">Cadastrando...</span>
          <span v-else>Cadastrar</span>
        </BaseButton>
      </template>
    </BaseDrawer>

    <!-- Detail drawer -->
    <BaseDrawer
      :open="detailProduct !== null"
      title="Detalhe do produto"
      @close="closeDetail"
    >
      <ProductDetail
        v-if="detailProduct"
        :product="detailProduct"
      />
      <template #footer>
        <BaseButton
          variant="secondary"
          @click="closeDetail"
        >
          Fechar
        </BaseButton>
        <BaseButton
          v-if="detailProduct && !detailProduct.deletedAt"
          variant="destructive"
          @click="openDeleteModal"
        >
          Excluir produto
        </BaseButton>
      </template>
    </BaseDrawer>

    <!-- Delete confirmation modal -->
    <DeleteProductModal
      :open="deleteModalOpen"
      :is-submitting="deleting"
      @confirm="confirmDelete"
      @cancel="cancelDelete"
    />
  </div>
</template>
