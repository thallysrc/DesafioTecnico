import { z } from 'zod'

/**
 * Zod schema for `POST /api/products` payload. Messages mirror backend
 * `CreateProductRequestValidator` (Plan 02-01) byte-for-byte per CONTEXT.md
 * D-11 / 02-UI-SPEC §"Form field validation messages".
 *
 * Messages-only-on-frontend (where the backend trusts JSON shape):
 *   - "Tipo é obrigatório", "Valor do fornecedor é obrigatório",
 *     "Quantidade inicial é obrigatória" (Zod sees `null`/`undefined` before
 *     user picks a value; FluentValidation never sees the null because the
 *     backend wouldn't be reached without a value coerced).
 *   - "Quantidade inicial deve ser um número inteiro" (Zod's integer check;
 *     the JSON `number` would round-trip fine to the backend's `int`
 *     deserialization but Zod surfaces the issue earlier with a more
 *     specific message).
 */
export const createProductSchema = z.object({
  code: z
    .string({ required_error: 'Código é obrigatório', invalid_type_error: 'Código é obrigatório' })
    .min(1, 'Código é obrigatório')
    .max(50, 'Código deve ter no máximo 50 caracteres'),

  description: z
    .string({ required_error: 'Descrição é obrigatória', invalid_type_error: 'Descrição é obrigatória' })
    .min(1, 'Descrição é obrigatória')
    .max(200, 'Descrição deve ter no máximo 200 caracteres'),

  type: z.enum(['Electronic', 'Appliance', 'Furniture'], {
    errorMap: (issue, ctx) => {
      if (issue.code === 'invalid_type') return { message: 'Tipo é obrigatório' }
      if (issue.code === 'invalid_enum_value') {
        return { message: 'Tipo inválido. Valores aceitos: Eletrônico, Eletrodoméstico, Móvel' }
      }
      return { message: ctx.defaultError }
    },
  }),

  supplierValue: z
    .number({
      required_error: 'Valor do fornecedor é obrigatório',
      invalid_type_error: 'Valor do fornecedor é obrigatório',
    })
    .nonnegative('Valor do fornecedor não pode ser negativo'),

  initialStockQuantity: z
    .number({
      required_error: 'Quantidade inicial é obrigatória',
      invalid_type_error: 'Quantidade inicial é obrigatória',
    })
    .int('Quantidade inicial deve ser um número inteiro')
    .nonnegative('Quantidade inicial não pode ser negativa'),
})

export type CreateProductForm = z.infer<typeof createProductSchema>
