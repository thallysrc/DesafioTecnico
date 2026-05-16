import { z } from 'zod'

/**
 * Zod schema for the Inbound (entrada) form. Messages mirror backend
 * `CreateMovementRequestValidator` (Plan 03-01) byte-for-byte per
 * 03-UI-SPEC §"Form field validation messages".
 *
 * The frontend schema enforces format-level rules; the backend additionally
 * enforces business rules (PRODUCT_DELETED, INSUFFICIENT_BALANCE,
 * INVALID_MOVEMENT_VALUES) which surface as 422 with dynamic hints.
 */
export const createInboundSchema = z.object({
  productId: z
    .string({ required_error: 'Produto é obrigatório', invalid_type_error: 'Produto é obrigatório' })
    .uuid('Produto é obrigatório'),

  quantity: z
    .number({
      required_error: 'Quantidade é obrigatória',
      invalid_type_error: 'Quantidade é obrigatória',
    })
    .int('Quantidade deve ser um número inteiro')
    .min(1, 'Quantidade deve ser maior que zero'),

  supplierValue: z
    .number({
      required_error: 'Valor do fornecedor é obrigatório',
      invalid_type_error: 'Valor do fornecedor é obrigatório',
    })
    .nonnegative('Valor do fornecedor não pode ser negativo'),
})

export type CreateInboundForm = z.infer<typeof createInboundSchema>

/** Zod schema for the Outbound (saída) form. Same rules + sale value. */
export const createOutboundSchema = z.object({
  productId: z
    .string({ required_error: 'Produto é obrigatório', invalid_type_error: 'Produto é obrigatório' })
    .uuid('Produto é obrigatório'),

  quantity: z
    .number({
      required_error: 'Quantidade é obrigatória',
      invalid_type_error: 'Quantidade é obrigatória',
    })
    .int('Quantidade deve ser um número inteiro')
    .min(1, 'Quantidade deve ser maior que zero'),

  saleValue: z
    .number({
      required_error: 'Valor de venda é obrigatório',
      invalid_type_error: 'Valor de venda é obrigatório',
    })
    .nonnegative('Valor de venda não pode ser negativo'),
})

export type CreateOutboundForm = z.infer<typeof createOutboundSchema>
