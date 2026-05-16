const currencyFormatter = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
})

const dateFormatter = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'short',
  timeStyle: 'short',
})

const quantityFormatter = new Intl.NumberFormat('pt-BR')

/**
 * Format a numeric BRL amount as `R$ 1.234,56`. UI-SPEC line 587.
 * Example: formatCurrency(123.45) → 'R$ 123,45'
 */
export function formatCurrency(value: number): string {
  return currencyFormatter.format(value)
}

/**
 * Format an ISO-8601 string as `dd/mm/yyyy HH:mm` (no seconds). UI-SPEC line 588.
 * Example: formatDate('2026-05-16T14:32:08Z') → '16/05/2026 11:32' (local TZ)
 */
export function formatDate(iso: string): string {
  return dateFormatter.format(new Date(iso))
}

/**
 * Format an integer quantity with BR thousand separators. UI-SPEC line 589.
 * Example: formatQuantity(12345) → '12.345'
 */
export function formatQuantity(value: number): string {
  return quantityFormatter.format(value)
}
