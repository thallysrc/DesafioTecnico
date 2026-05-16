# Frontend Conventions — StockEasy Web

Regras que devem ser seguidas **religiosamente** ao escrever código no `frontend/`.
Frontend é uma SPA Vue 3 que consome a API `.NET` em `../backend/`.

---

## Brand & Visual Identity

**Produto:** StockEasy (família visual do RoboteAsy)
**Wordmark:** `Stock<span class="text-brand-500">Easy</span>` — em todo header/sidebar/favicon

### Paleta (extraída de roboteasy.tech)

| Token | Hex | Uso |
|-------|-----|-----|
| `brand-500` | `#1863DC` | Primary — botões CTA, links ativos, ícones brand |
| `brand-600` | `#0056A7` | Primary dark — hover, links, headers fortes |
| `brand-700` | `#003F7D` | Sidebar item ativo (text), títulos PDF |
| `brand-50` | `#EFF4FC` | Bg item ativo de sidebar, header de tabela |
| `brand-100` | `#D6E2F7` | Tags, badges suaves |
| `ink` | `#212121` | Texto body |
| `muted` | `#7B7B7B` | Texto secundário, captions |
| `line` | `#EBEBEB` | Divisórias, bordas neutras |
| `surface` | `#F4F4F4` | Cards, bg sutil |
| `success` | `#009C34` | Verde — sucesso, tag Entrada |
| `warning` | `#FCB900` | Amarelo — alertas |
| `danger` | `#CF2E2E` | Vermelho — erro, destrutivo, tag Saída |

Escala completa `brand-50..900` no `tailwind.config.js`.

### Tipografia

**Inter** via Google Fonts, fallback `system-ui, sans-serif`.

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
```

Hierarquia: 2 tamanhos de heading + 1 corpo + 1 caption. Nada mais.

---

## Stack

| Camada | Tecnologia |
|--------|------------|
| Framework | **Vue 3** com Composition API + `<script setup>` |
| Linguagem | **TypeScript** estrito — `strict: true` no `tsconfig.json` |
| Build | **Vite 5+** |
| Estilo | **Tailwind CSS 3+** puro — sem biblioteca de UI |
| Router | **Vue Router 4** |
| Estado | **Composables apenas** — sem Pinia, sem Vuex |
| HTTP | **Axios** com interceptors |
| Forms | **Vee-Validate 4** + **Zod** (schema-based) |
| Tests | **Vitest** + **@vue/test-utils** |
| Linting | **ESLint** (vue3-recommended + @typescript-eslint) + **Prettier** |
| Package manager | npm |

**Pacotes adicionais aprovados**: nenhum sem decisão explícita. Em particular: nada de Pinia, Vuex, Element Plus, PrimeVue, Vuetify, dayjs, lodash, jQuery.

---

## Project Structure (Feature-Based)

```
frontend/
├── index.html
├── package.json
├── tsconfig.json
├── vite.config.ts
├── tailwind.config.js
├── postcss.config.js
├── .eslintrc.cjs
├── .prettierrc
├── public/
│   └── favicon.svg
├── src/
│   ├── main.ts                    # bootstrap (createApp, router, axios setup)
│   ├── App.vue                    # root layout (sidebar + <router-view/>)
│   ├── router/
│   │   └── index.ts
│   ├── shared/                    # cross-feature
│   │   ├── api/
│   │   │   └── client.ts          # axios instance + interceptors + ApiError
│   │   ├── components/            # UI primitivos: BaseButton, BaseInput, BaseTable, BaseModal, BaseToast
│   │   ├── composables/           # genéricos: useToast, useConfirm, usePagination
│   │   ├── types/                 # tipos compartilhados (ApiError, PaginationMeta)
│   │   ├── format.ts              # formatCurrency, formatDate, formatQuantity (Intl)
│   │   └── labels.ts              # productTypeLabel, movementTypeLabel, errorCodeLabel
│   ├── features/
│   │   ├── products/
│   │   │   ├── components/        # ProductForm.vue, ProductList.vue, DeleteProductModal.vue
│   │   │   ├── composables/       # useProducts.ts
│   │   │   ├── pages/             # ProductsPage.vue
│   │   │   ├── schemas.ts         # zod (espelha FluentValidation)
│   │   │   ├── types.ts           # Product, CreateProductRequest, ProductResponse
│   │   │   └── api.ts             # productsApi.list/get/create/delete
│   │   └── stock/
│   │       ├── components/        # InboundForm.vue, OutboundForm.vue, MovementHistory.vue, ConfirmOutboundModal.vue
│   │       ├── composables/       # useStockMovements.ts
│   │       ├── pages/             # StockMovementsPage.vue (tabs Entrada/Saída/Histórico)
│   │       ├── schemas.ts
│   │       ├── types.ts
│   │       └── api.ts
│   └── assets/                    # logo SVG, ícones inline
└── tests/
    ├── unit/                       # espelha src/
    └── setup.ts
```

Regras:
- **Não criar `src/views/`** — pages vivem em `features/<dominio>/pages/`.
- **Não criar `src/store/`** — sem Pinia.
- **Não criar `src/services/`** — HTTP vive em `features/<dominio>/api.ts`.
- Coisas usadas por **2+ features** sobem pra `src/shared/`. Antes disso, mantenha local.

---

## Tailwind Config

```js
// frontend/tailwind.config.js
/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./index.html', './src/**/*.{vue,ts}'],
  theme: {
    extend: {
      colors: {
        brand: {
          50:  '#EFF4FC',
          100: '#D6E2F7',
          200: '#A6C0EE',
          300: '#759DE5',
          400: '#447BDC',
          500: '#1863DC',
          600: '#0056A7',
          700: '#003F7D',
          800: '#002A54',
          900: '#001530',
        },
        ink:     '#212121',
        muted:   '#7B7B7B',
        line:    '#EBEBEB',
        surface: '#F4F4F4',
        success: '#009C34',
        warning: '#FCB900',
        danger:  '#CF2E2E',
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
}
```

---

## Routing

`src/router/index.ts`:

```ts
import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/products' },
    {
      path: '/products',
      component: () => import('@/features/products/pages/ProductsPage.vue'),
      meta: { title: 'Produtos' },
    },
    {
      path: '/stock-movements',
      component: () => import('@/features/stock/pages/StockMovementsPage.vue'),
      meta: { title: 'Movimentação de Estoque' },
    },
  ],
})

export default router
```

- Lazy imports (`() => import(...)`) em todas as pages.
- `App.vue` tem **sidebar light fixa à esquerda** + `<router-view/>` no main.
- Sidebar usa `<RouterLink>` com `:class` condicional pra item ativo:
  ```html
  <RouterLink to="/products"
    :class="[
      'flex items-center gap-3 px-4 py-2.5 rounded-md text-sm font-medium transition-colors',
      $route.path.startsWith('/products')
        ? 'bg-brand-50 text-brand-700'
        : 'text-muted hover:bg-surface hover:text-ink'
    ]">
    Produtos
  </RouterLink>
  ```

---

## TypeScript

- `"strict": true` no `tsconfig.json`.
- `"noUnusedLocals": true`, `"noUnusedParameters": true`.
- `paths` alias: `"@/*": ["src/*"]`.
- Sempre tipar explícito retornos de funções `export`-adas.
- `as any` proibido. `as unknown as T` só com comentário justificando.

### Tipos espelhando a API

`src/features/products/types.ts`:

```ts
export type ProductType = 'Electronic' | 'Appliance' | 'Furniture'

export interface ProductResponse {
  id: string
  code: string
  description: string
  type: ProductType
  supplierValue: number
  stockQuantity: number
  deletedAt: string | null
  _links?: Record<string, string>
}

export interface CreateProductRequest {
  code: string
  description: string
  type: ProductType
  supplierValue: number
  initialStockQuantity: number
}
```

- Enums do .NET viram **string literal unions** no TS (backend serializa enum como string via `JsonStringEnumConverter`).
- `Guid` (.NET) ↔ `string` (TS).
- `decimal` (.NET) ↔ `number` (TS) — toleramos imprecisão pra esse desafio.
- `DateTime` (.NET) ↔ `string` ISO 8601 (TS) — formatar com `Intl.DateTimeFormat` na exibição.

---

## HTTP Client (`src/shared/api/client.ts`)

```ts
import axios, { AxiosError } from 'axios'

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

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080',
  timeout: 10_000,
  headers: { 'Content-Type': 'application/json' },
})

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
      hint: 'Verifique sua conexão e tente novamente',
      statusCode: error.response?.status ?? 0,
      retryable: true,
      traceId: 'client',
      timestamp: new Date().toISOString(),
    }
    return Promise.reject(fallback)
  },
)

export function generateIdempotencyKey(): string {
  return crypto.randomUUID()
}
```

`src/features/products/api.ts`:

```ts
import { apiClient } from '@/shared/api/client'
import type { CreateProductRequest, ProductResponse } from './types'
import type { PagedResult } from '@/shared/types'

export const productsApi = {
  list: (page = 1, pageSize = 30, includeDeleted = false) =>
    apiClient
      .get<PagedResult<ProductResponse>>('/api/products', { params: { page, pageSize, includeDeleted } })
      .then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<ProductResponse>(`/api/products/${id}`).then((r) => r.data),

  create: (req: CreateProductRequest) =>
    apiClient.post<ProductResponse>('/api/products', req).then((r) => r.data),

  delete: (id: string) =>
    apiClient.delete<void>(`/api/products/${id}`).then(() => undefined),
}
```

`src/features/stock/api.ts` (com Idempotency-Key):

```ts
import { apiClient, generateIdempotencyKey } from '@/shared/api/client'
import type { CreateMovementRequest, MovementResponse } from './types'

export const movementsApi = {
  register: (req: CreateMovementRequest) =>
    apiClient
      .post<MovementResponse>('/api/stock-movements', req, {
        headers: { 'Idempotency-Key': generateIdempotencyKey() },
      })
      .then((r) => r.data),

  // ... list / getById
}
```

Regras:
- **Uma instância axios** (`apiClient`). Não criar outras.
- API por feature em `<feature>/api.ts`.
- Sempre `.then(r => r.data)` no boundary.
- Idempotency-Key gerada via `crypto.randomUUID()` (UUID v4 nativo do browser) no api.ts.
- Erros normalizados pro shape `ApiError` rico (mesmo formato do backend) — UI consome `.hint` direto.

---

## Composables (`features/<dominio>/composables/`)

```ts
import { ref } from 'vue'
import { productsApi } from '../api'
import type { ProductResponse, CreateProductRequest } from '../types'
import type { ApiError } from '@/shared/api/client'

export function useProducts() {
  const items = ref<ProductResponse[]>([])
  const pagination = ref({ page: 1, pageSize: 30, total: 0, totalPages: 0 })
  const loading = ref(false)
  const error = ref<ApiError | null>(null)

  async function fetchPage(page = 1) {
    loading.value = true
    error.value = null
    try {
      const result = await productsApi.list(page, pagination.value.pageSize)
      items.value = result.items
      pagination.value = result.pagination
    } catch (e) {
      error.value = e as ApiError
    } finally {
      loading.value = false
    }
  }

  async function create(req: CreateProductRequest) {
    const created = await productsApi.create(req)
    await fetchPage(1)  // refetch para refletir total atualizado
    return created
  }

  async function softDelete(id: string) {
    await productsApi.delete(id)
    await fetchPage(pagination.value.page)
  }

  return { items, pagination, loading, error, fetchPage, create, softDelete }
}
```

Regras:
- Nome: `use<Recurso>` (`useProducts`, `useStockMovements`).
- Retorna **refs nomeadas** + **funções**. NÃO `reactive({})` (quebra destructuring).
- `loading`, `error`, `pagination` por composable.
- Tratar erro do HTTP no composable — componente só lê `error.value`.
- Ações que **lançam** (como `create`) deixam throw passar pra `try/catch` no submit do form.

---

## Form Validation (Vee-Validate + Zod)

`features/products/schemas.ts` — schemas Zod **espelham** FluentValidation do backend:

```ts
import { z } from 'zod'

export const createProductSchema = z.object({
  code: z.string().min(1, 'Código é obrigatório').max(50, 'Máximo 50 caracteres'),
  description: z.string().min(1, 'Descrição é obrigatória').max(200),
  type: z.enum(['Electronic', 'Appliance', 'Furniture'], {
    errorMap: () => ({ message: 'Tipo inválido. Valores aceitos: Electronic, Appliance, Furniture' }),
  }),
  supplierValue: z.number().nonnegative('Valor do fornecedor não pode ser negativo'),
  initialStockQuantity: z.number().int().nonnegative('Quantidade inicial não pode ser negativa'),
})

export type CreateProductForm = z.infer<typeof createProductSchema>
```

Componente:

```vue
<script setup lang="ts">
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { createProductSchema } from '../schemas'
import { useProducts } from '../composables/useProducts'
import { useToast } from '@/shared/composables/useToast'
import type { ApiError } from '@/shared/api/client'

const { create } = useProducts()
const toast = useToast()
const { handleSubmit, errors, defineField, isSubmitting } = useForm({
  validationSchema: toTypedSchema(createProductSchema),
  validateOnBlur: true,
})

const [code] = defineField('code')
const [description] = defineField('description')
// ...

const onSubmit = handleSubmit(async (values) => {
  try {
    await create(values)
    toast.success('Produto criado com sucesso')
  } catch (e) {
    const apiError = e as ApiError
    toast.error(apiError.hint ?? apiError.message)
  }
})
</script>
```

Regras:
- `validateOnBlur: true` — feedback **inline ao perder foco**, não só no submit.
- Schema Zod com mesmas regras e mesmas mensagens do FluentValidation.
- Botão submit `:disabled="!meta.valid || isSubmitting"`.
- Em erro do submit: mostrar `apiError.hint` (preferred) ou `apiError.message`.

---

## UX — 10 Heurísticas de Nielsen aplicadas

### #1 Visibilidade do status do sistema

**Toda lista/form em um dos 4 estados** — nunca estado morto:

| Estado | Quando | Render |
|--------|--------|--------|
| `loading` | Fetch inicial em andamento | Skeleton (3-5 rows com `animate-pulse bg-line`) |
| `empty` | Fetch terminou, `items.length === 0` | EmptyState component com ícone + CTA |
| `error` | Fetch falhou | ErrorState com `apiError.hint` + botão "Tentar novamente" |
| `success-with-data` | Fetch terminou, items existem | Lista renderizada |

Form submetendo: botão muda pra "Salvando..." + `disabled` + spinner inline. Form `aria-busy="true"`.

### #2 Mundo real — Formatação BR

`src/shared/format.ts`:

```ts
export const formatCurrency = (v: number) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(v)

export const formatDate = (iso: string) =>
  new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(iso))

export const formatQuantity = (v: number) =>
  new Intl.NumberFormat('pt-BR').format(v)
```

`src/shared/labels.ts` — **único módulo** de tradução API → PT:

```ts
import type { ProductType } from '@/features/products/types'
import type { MovementType } from '@/features/stock/types'

export const productTypeLabel: Record<ProductType, string> = {
  Electronic: 'Eletrônico',
  Appliance: 'Eletrodoméstico',
  Furniture: 'Móvel',
}

export const movementTypeLabel: Record<MovementType, string> = {
  Inbound: 'Entrada',
  Outbound: 'Saída',
}
```

NUNCA `t === 'Inbound' ? 'Entrada' : 'Saída'` espalhado nos componentes. Sempre via `labels.ts`.

### #3 Controle e liberdade

- Modal: botão "Cancelar" sempre à esquerda, sempre visível. `Esc` fecha. Clique fora **não** fecha se houver dados não-salvos.
- Form com dados modificados: `beforeRouteLeave` exibe confirm.

### #4 Consistência

- **Botões primários sempre à direita** (cancela à esquerda).
- Cor primária = `bg-brand-500 hover:bg-brand-600`. Destrutiva = `bg-danger hover:bg-red-700`. Sucesso = `bg-success`.
- Labels acima de inputs, sempre. Nunca placeholder-as-label.
- Erros de campo: `text-danger text-sm` **abaixo** do input.
- Tabelas: header `bg-brand-50 text-brand-700`, row hover `bg-surface`, divisória `border-line`.
- Spacing: card `p-6`, gap entre seções `gap-6`.

### #5 Prevenção de erros

- Validação inline `onBlur` (Vee-Validate `validateOnBlur: true`).
- Submit desabilitado enquanto form tem erros.
- Campo numérico: `<input type="number" :step="..." :min="...">`.
- Saída de estoque: mostrar **saldo disponível acima do campo quantidade** (`Disponível: 8 unidades`).
- Modal de confirmação em **saída de estoque** e **soft delete** (resumo da operação).

### #6 Reconhecimento em vez de memória

- Seleção de produto na movimentação: **searchable dropdown** com `code — description` ("P001 — Notebook Dell").
- Dropdowns pra ProductType e MovementType (nunca free text).
- Tooltips em campos não-óbvios.

### #7 Flexibilidade

- `Enter` submete forms.
- `Tab` order linear e lógico.
- (Sem atalhos custom no MVP.)

### #8 Estético e minimalista

- **Uma ação primária por tela.** Secundárias com peso menor (`text-muted`, sem fill).
- Sem ícones decorativos. Ícones só com significado.
- Whitespace generoso: `p-6`, `gap-4`, `max-w-2xl` em forms.
- Tipografia: H1 24pt, H2 18pt, corpo 14pt, caption 12pt. Não inventar tamanhos.

### #9 Recuperação de erros

- Toast de erro **usa a hint da API**: `apiError.hint || apiError.message`.
- Erro de saldo insuficiente: toast mostra saldo atual + sugere "Registrar entrada" (link pré-preenchido).
- 500/network: toast com "Tentar novamente" como ação.
- `src/shared/labels.ts` opcional: dicionário `errorCodeLabel` pra customizar mensagem por errorCode em cenários específicos.

### #10 A11y básico

- Semantic HTML: `<button>` (não `<div onclick>`), `<label for="...">`, `<form>`, `<table>`, `<th scope="col">`.
- Cada input tem `<label>` associado por `for` + `id`.
- Erros com `aria-describedby` pro input.
- Foco visível: NUNCA `outline: none` sem substituir. Usar `focus:ring-2 focus:ring-brand-500`.
- Modal: foco move pro primeiro elemento ao abrir; `Esc` fecha; foco volta ao trigger ao fechar.
- Contraste: textos sempre `text-ink` ou `text-muted` em fundo claro. Nunca abaixo de WCAG AA.
- Cor **nunca como único sinal** — erro tem texto + cor + ícone opcional.

---

## Components

### Naming

- Arquivos: `PascalCase.vue` — `ProductForm.vue`, `BaseButton.vue`.
- Componentes "base" (UI primitivos genéricos): `Base*` em `shared/components/`.
- Em templates, use a tag em **PascalCase**: `<ProductForm />`.

### Single File Component shape

```vue
<script setup lang="ts">
// 1. imports
// 2. props / emits
// 3. composables / state
// 4. computed
// 5. funções
// 6. lifecycle (onMounted etc.)
</script>

<template>
  <!-- markup -->
</template>

<!-- SEM <style scoped> — usar Tailwind. Exceções (animação custom) podem usar <style scoped>. -->
```

Regras:
- `<script setup lang="ts">` sempre.
- `defineProps<{}>` com generics, NÃO runtime declaration.
- `defineEmits<{ (e: 'submit', payload: X): void }>()` tipado.
- Sem `data()`, sem `mixins`, sem Options API.

### Tailwind usage

- Classes utilitárias direto no template.
- Composição longa: quebrar logicamente:
  ```html
  <button class="
    px-4 py-2 rounded-md font-medium
    bg-brand-500 text-white hover:bg-brand-600
    disabled:bg-brand-200 disabled:cursor-not-allowed
    transition-colors
  ">
  ```
- Classes condicionais via `:class="[...]"` ou objeto.

### Catálogo de classes recorrentes

| Elemento | Classes |
|----------|---------|
| Btn primário | `bg-brand-500 hover:bg-brand-600 disabled:bg-brand-200 text-white px-4 py-2 rounded-md font-medium transition-colors` |
| Btn secundário | `border border-line text-ink hover:bg-surface px-4 py-2 rounded-md font-medium transition-colors` |
| Btn destrutivo | `bg-danger hover:bg-red-700 disabled:bg-red-200 text-white px-4 py-2 rounded-md font-medium transition-colors` |
| Input | `w-full border border-line rounded-md px-3 py-2 text-ink focus:ring-2 focus:ring-brand-500 focus:border-brand-500 outline-none transition-shadow` |
| Input com erro | `... border-danger focus:ring-danger focus:border-danger` |
| Label | `block text-sm font-medium text-ink mb-1` |
| Helper text | `text-sm text-muted mt-1` |
| Error text | `text-sm text-danger mt-1` |
| Card | `bg-white border border-line rounded-lg shadow-sm p-6` |
| Modal overlay | `fixed inset-0 bg-ink/50 flex items-center justify-center z-50` |
| Modal content | `bg-white rounded-lg shadow-xl p-6 max-w-md w-full mx-4` |
| Tag Entrada | `inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium bg-success/10 text-success` |
| Tag Saída | `inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium bg-danger/10 text-danger` |
| Skeleton row | `h-10 bg-line rounded animate-pulse` |
| Sidebar item ativo | `bg-brand-50 text-brand-700 flex items-center gap-3 px-4 py-2.5 rounded-md text-sm font-medium` |
| Sidebar item inativo | `text-muted hover:bg-surface hover:text-ink flex items-center gap-3 px-4 py-2.5 rounded-md text-sm font-medium` |

---

## API Contract Sync

Frontend espera estes formatos do backend. Se backend mudar, sincronizar `types.ts` e `schemas.ts`.

**Produto:**
```json
POST /api/products
{ "code": "P001", "description": "...", "type": "Electronic", "supplierValue": 100.50, "initialStockQuantity": 10 }
→ 201 Created { "id": "uuid", "code": "P001", ..., "_links": { ... } }
```

**Movimento:**
```http
POST /api/stock-movements
Idempotency-Key: <uuid v4>
```
```json
{ "productId": "uuid", "type": "Inbound" | "Outbound", "quantity": 5, "supplierValue": 100, "saleValue": 150 }
→ 201 Created { "id": "uuid", ... }
```

**Erro (canônico):**
```json
{
  "errorCode": "INSUFFICIENT_BALANCE",
  "category": "BUSINESS_RULE",
  "message": "Saldo insuficiente: solicitado 10, disponível 3",
  "hint": "Reduza a quantidade para no máximo 3 ou registre uma entrada antes",
  "statusCode": 422,
  "retryable": false,
  "details": { ... },
  "traceId": "...",
  "timestamp": "..."
}
```

**Lista paginada (canônica):**
```json
{
  "items": [...],
  "pagination": { "page": 1, "pageSize": 30, "total": 245, "totalPages": 9, "hasNext": true, "hasPrev": false },
  "_links": { "self": "...", "next": "...", "first": "...", "last": "..." }
}
```

---

## Testing (Vitest)

```ts
// tests/unit/features/products/useProducts.spec.ts
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { useProducts } from '@/features/products/composables/useProducts'
import { productsApi } from '@/features/products/api'

vi.mock('@/features/products/api')

describe('useProducts', () => {
  beforeEach(() => vi.clearAllMocks())

  it('should populate items after fetchPage', async () => {
    vi.mocked(productsApi.list).mockResolvedValue({
      items: [{ id: '1', code: 'P', description: 'd', type: 'Electronic', supplierValue: 1, stockQuantity: 1, deletedAt: null }],
      pagination: { page: 1, pageSize: 30, total: 1, totalPages: 1, hasNext: false, hasPrev: false },
      _links: {},
    })

    const { items, fetchPage, loading } = useProducts()
    await fetchPage()

    expect(items.value).toHaveLength(1)
    expect(loading.value).toBe(false)
  })

  it('should set error when API fails', async () => {
    vi.mocked(productsApi.list).mockRejectedValue({
      errorCode: 'INTERNAL_ERROR',
      message: 'Erro interno',
      hint: 'Tente novamente',
    })

    const { error, fetchPage } = useProducts()
    await fetchPage()

    expect(error.value?.errorCode).toBe('INTERNAL_ERROR')
  })
})
```

Componentes: testar forms críticos (`ProductForm`, `OutboundForm`) — montar com `@vue/test-utils`, preencher campos, assertar `emit` ou chamada ao composable mockado.

Regras:
- **Foco em composables** (lógica). Componentes só os críticos.
- Mock HTTP layer (`vi.mock('@/features/<feat>/api')`).
- Sem snapshot tests pra componentes.
- Testar cenário de erro de cada errorCode crítico (`INSUFFICIENT_BALANCE`, `DUPLICATE_CODE`).

---

## Linting & Formatting

- ESLint com `plugin:vue/vue3-recommended` + `@typescript-eslint/recommended`.
- Prettier com config padrão + `singleQuote: true`, `semi: false`, `printWidth: 100`.
- `npm run lint` antes de commitar.

---

## Don'ts

- ❌ Pinia / Vuex / store global
- ❌ Options API (sempre Composition + `<script setup>`)
- ❌ Mixins
- ❌ Bibliotecas de UI (Element Plus, PrimeVue, Vuetify, etc.)
- ❌ jQuery, lodash, dayjs, moment
- ❌ `<style scoped>` com CSS custom — usar Tailwind (exceções com justificativa)
- ❌ `any` no TypeScript
- ❌ `as any`
- ❌ HTTP fora de `features/<feat>/api.ts` ou `shared/api/`
- ❌ `console.log` em produção
- ❌ `style="..."` inline
- ❌ Identifiers em português (PT só em mensagens ao usuário)
- ❌ `var` (use `const`/`let`)
- ❌ Component sem `lang="ts"` no `<script setup>`
- ❌ String literal de tipo PT espalhada — sempre via `labels.ts`
- ❌ Formatação manual de moeda/data — sempre via `format.ts`
- ❌ Estado morto (sempre um dos 4: loading/empty/error/success)
- ❌ `outline: none` sem `focus:ring-*` substituto
- ❌ Mensagem de erro sem hint (sempre `apiError.hint ?? apiError.message`)
- ❌ Idempotency-Key faltando em movimento (api.ts gera automaticamente — não esquecer)
- ❌ Listagem sem paginação no UI (sempre via `usePagination` ou similar)
- ❌ Modal sem botão Cancel
- ❌ Submit sem `:disabled` quando há erros
- ❌ Saída de estoque sem confirmação modal
- ❌ Soft delete sem confirmação modal
- ❌ Cor como único sinal (sempre + texto/ícone)

---

## Reference

- Vue 3 docs: <https://vuejs.org/>
- Vee-Validate + Zod: <https://vee-validate.logaretm.com/v4/integrations/zod-schema-validation/>
- Tailwind: <https://tailwindcss.com/docs>
- Nielsen 10 Usability Heuristics: <https://www.nngroup.com/articles/ten-usability-heuristics/>

Quando em dúvida sobre padrão Vue: seguir o que os docs oficiais demonstram com Composition API + `<script setup>` + TS. Não inventar.
