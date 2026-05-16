---
status: partial
phase: 02-products-vertical-slice
source: [02-VERIFICATION.md]
started: 2026-05-16
updated: 2026-05-16
---

## Current Test

[awaiting human testing — host has no Chromium for headless E2E]

## Tests

### 1. Drawer-create happy path → success toast surfacing
expected: User opens `http://localhost:5173/products`, clicks "Cadastrar produto", fills the form in the right-pane drawer (code, description, type dropdown, supplier value with R$ comma-decimal, initial stock quantity), submits. Drawer closes, list refreshes showing the new product, and a green toast appears bottom-right with text `Produto cadastrado com sucesso` for 3s. No confirmation modal appears between submit and list refresh (CONF-03).
result: [pending]

### 2. Soft-delete modal autofocus + a11y
expected: User clicks a product row → detail drawer opens → clicks "Excluir produto". A centered modal appears with title `Excluir produto?` and body explaining histórico preservation. The `Cancelar` button has initial focus (NOT the destructive `Excluir`). `Esc` key closes the modal. Tabbing wraps inside the modal. After confirming Excluir, the row disappears from the default list, a toast confirms `Produto excluído`, and toggling "Mostrar excluídos" shows the row again with the `Excluído` badge and `deletedAt` formatted.
result: [pending]

### 3. Network-error toast text surfacing (D-09)
expected: With the docker-compose stack running, user is on `/products` viewing the list. Run `docker compose stop backend` from terminal. In the SPA, click "Tentar novamente" on the error state (or refresh trigger). A red toast appears bottom-right with text starting with `Não foi possível conectar` (from `ApiError.hint` injected by the Axios interceptor for network errors / 5xx without body). The toast does NOT show a generic 'Error' or raw exception trace.
result: [pending]

## Summary

total: 3
passed: 0
issues: 0
pending: 3
skipped: 0
blocked: 0

## Gaps

(none recorded — pending manual testing)

## Notes

- All 5 ROADMAP success criteria for Phase 2 are met per automated checks (see `02-VERIFICATION.md`).
- Backend smoke matrix (12 scenarios, including `_links` literal, dynamic hint dynamism, pagination envelope, soft-delete behavior, includeDeleted toggle) — all PASS via curl.
- Swagger contract verified: 4 operationIds, ProducesResponseType for every status code, enums as strings.
- Validator message mirroring verified: 7 PT-BR strings byte-identical between Zod and FluentValidation.
- The 3 items above require a browser to observe — the host has no Chromium. User to run docker-compose locally and verify in a browser when ready.
- Items inherently Phase 3 (BACK-09 transactions, BACK-10 SELECT FOR UPDATE, UX-08 disponível indicator) noted in 02-VERIFICATION.md as deferred, not gaps.
