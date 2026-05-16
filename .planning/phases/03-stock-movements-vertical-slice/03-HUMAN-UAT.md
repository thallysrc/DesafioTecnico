---
status: partial
phase: 03-stock-movements-vertical-slice
source: [03-VERIFICATION.md]
started: 2026-05-16T19:46:59Z
updated: 2026-05-16T19:46:59Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. CONF-01 modal visual rendering
expected: Open `/stock-movements?tab=saida`, register a Saída with qty <= stock. Modal opens with 5 definition rows (Produto, Quantidade atual, Quantidade a sair, Quantidade após, Motivo). Cancelar has autofocus. Confirmar is brand-primary (NOT destructive red).
result: [pending]

### 2. Esc dismiss behavior
expected: Press Esc while CONF-01 modal is open. Modal closes, form data preserved, no toast fired.
result: [pending]

### 3. INSUFFICIENT_BALANCE inside modal
expected: Trigger Outbound qty > current stock from inside the modal. Modal stays open, toast shows `apiError.hint`, Disponível helper refreshes from `apiError.details.available`.
result: [pending]

### 4. Default tab routing
expected: Navigate `/stock-movements` with no `?tab` param. Default tab is `historico`, `MovementHistory` mounts immediately.
result: [pending]

### 5. WAI-ARIA keyboard tab navigation
expected: Press ArrowRight on active tab. Focus wraps to next tab, URL `?tab=` updates via `router.replace`, `v-if` panel mounts. ArrowLeft/Home/End behave per WAI-ARIA spec.
result: [pending]

### 6. Network failure retry
expected: Trigger network failure (offline mode or backend stop). Error state shows in MovementHistory. Click "Tentar novamente". Retry refetches and recovers when network is back.
result: [pending]

## Summary

total: 6
passed: 0
issues: 0
pending: 6
skipped: 0
blocked: 0

## Gaps
