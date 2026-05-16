---
phase: 03-stock-movements-vertical-slice
verified: 2026-05-16
status: human_needed
score: "5/5 ROADMAP success criteria verified (API+DB plane by Plan 03-05 smoke; codebase-level by goal-backward verifier)"
captured_by: Plan 03-05 (E2E integration smoke) + goal-backward verifier
human_verification:
  - test: "Open http://localhost:5173/stock-movements?tab=saida, register a Saída with qty <= stock"
    expected: "CONF-01 modal opens with 5 definition rows; Cancelar has autofocus; Confirmar button is brand-primary (not destructive red)"
    why_human: "Visual rendering, autofocus behavior, color contrast require real DOM"
  - test: "Press Esc while CONF-01 modal is open"
    expected: "Modal closes; form data preserved; no toast fired"
    why_human: "Esc-to-dismiss requires real keyboard event loop"
  - test: "Trigger INSUFFICIENT_BALANCE inside CONF-01 modal (qty > stock)"
    expected: "Modal stays open; toast shows apiError.hint; Disponivel helper refreshes from apiError.details.available"
    why_human: "Live-DOM observation of helper refresh + toast surfacing required"
  - test: "Navigate /stock-movements with no ?tab param"
    expected: "Default tab is historico; MovementHistory mounts"
    why_human: "URL default behavior + initial-mount render require a browser session"
  - test: "Press ArrowRight on the active tab button"
    expected: "Focus wraps to next tab; URL ?tab= updates; v-if panel mounts"
    why_human: "WAI-ARIA automatic-activation pattern requires real keyboard + focus observation"
  - test: "Trigger network failure then click Tentar novamente in history error state"
    expected: "Error state shows; retry refetches"
    why_human: "Browser network stack observation required"
stack:
  - postgres: 16-alpine (container `stockeasy-postgres`)
  - backend: .NET 8 on :8080 (container `stockeasy-api`, `dotnet watch run`)
  - frontend: Vite dev server on :5173 (container `stockeasy-web`, `npm run dev`)
boot_command: docker compose up -d --build
seeded_data:
  product_id: 7168c579-8259-4c0e-949b-998a7147c47f
  product_code: P3-SMOKE-001
  initial_stock: 50
  initial_supplier_value: 100.00
---

# Phase 3 — Stock Movements: Verification Evidence

Captured: **2026-05-16** against a freshly-built docker-compose stack (`docker compose up -d --build` from a clean volume).

| Service | Container | URL | Health |
| ------- | --------- | --- | ------ |
| Postgres 16 | `stockeasy-postgres` | `postgres:5432` (inside compose net) / `localhost:5432` (host) | `pg_isready` healthy after ~3s |
| Backend .NET 8 | `stockeasy-api` | <http://localhost:8080> | `GET /api/health` → `{"status":"ok","db":"up","version":"1.0.0"}` after 8s |
| Frontend (Vite) | `stockeasy-web` | <http://localhost:5173> | `GET /` → 200 after 2s |

Seeded product (used by every Inbound/Outbound/list test below): `P3-SMOKE-001` / id `7168c579-8259-4c0e-949b-998a7147c47f`, initial stock 50, initial supplier_value `100.00`.

---

## ROADMAP Success Criteria

### Criterion 1 — Inbound atomicity (MOVE-01 + MOVE-06)

**Goal:** A successful Inbound increases `stock_quantity` AND updates `supplier_value` to the posted value in a single transaction.

**Request:**

```bash
curl -fsS -i -X POST http://localhost:8080/api/stock-movements \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: b9472614-10db-4f4a-8a03-56fe593cd226" \
  -d '{"productId":"7168c579-8259-4c0e-949b-998a7147c47f","type":"Inbound","quantity":10,"supplierValue":120.50}'
```

**Response status line:** `HTTP/1.1 201 Created`
**Response headers (relevant):** `Location: http://localhost:8080/api/stock-movements/e3de56ef-4e8e-426a-a953-5e331f8bbd8f`

**Response body:**

```json
{
  "id": "e3de56ef-4e8e-426a-a953-5e331f8bbd8f",
  "productId": "7168c579-8259-4c0e-949b-998a7147c47f",
  "productCode": "P3-SMOKE-001",
  "productDescription": "Phase 3 smoke product",
  "type": "Inbound",
  "quantity": 10,
  "supplierValue": 120.50,
  "saleValue": null,
  "idempotencyKey": "b9472614-10db-4f4a-8a03-56fe593cd226",
  "occurredAt": "2026-05-16T19:24:22.312105Z",
  "createdAt": "2026-05-16T19:24:22.312105Z",
  "_links": {
    "self": "/api/stock-movements/e3de56ef-4e8e-426a-a953-5e331f8bbd8f",
    "product": "/api/products/7168c579-8259-4c0e-949b-998a7147c47f"
  }
}
```

**Atomicity proof — product state immediately after the Inbound:**

```bash
curl -fsS "http://localhost:8080/api/products/7168c579-8259-4c0e-949b-998a7147c47f" | jq '{stockQuantity, supplierValue}'
```

```json
{
  "stockQuantity": 60,
  "supplierValue": 120.50
}
```

`50 + 10 = 60` (stock incremented) AND `supplier_value` advanced from `100.00 → 120.50` (the value posted on the Inbound). Both changes occurred in a single transaction (Plan 03-03 wires `BEGIN → SELECT FOR UPDATE → INSERT movement → UPDATE products → COMMIT`). **MOVE-01 + MOVE-06 verified.**

### Criterion 2 — Outbound + Confirm modal copy (CONF-01)

**Goal (a):** A valid Outbound decrements `stock_quantity` by the posted quantity and leaves `supplier_value` unchanged.

**Request:**

```bash
KEY2=$(uuidgen)
curl -fsS -X POST http://localhost:8080/api/stock-movements \
  -H "Content-Type: application/json" -H "Idempotency-Key: $KEY2" \
  -d '{"productId":"7168c579-8259-4c0e-949b-998a7147c47f","type":"Outbound","quantity":5,"saleValue":250.00}'
```

**Response body:**

```json
{
  "id": "81a6e222-21ac-4bb2-8a45-51a428e0cbb4",
  "productId": "7168c579-8259-4c0e-949b-998a7147c47f",
  "productCode": "P3-SMOKE-001",
  "productDescription": "Phase 3 smoke product",
  "type": "Outbound",
  "quantity": 5,
  "supplierValue": null,
  "saleValue": 250.00,
  "idempotencyKey": "ead0e57e-0e56-4b9f-8cf3-50cb3463ce67",
  "occurredAt": "2026-05-16T19:24:39.331301Z",
  "createdAt": "2026-05-16T19:24:39.331301Z",
  "_links": {
    "self": "/api/stock-movements/81a6e222-21ac-4bb2-8a45-51a428e0cbb4",
    "product": "/api/products/7168c579-8259-4c0e-949b-998a7147c47f"
  }
}
```

**Atomicity proof — product state immediately after the Outbound:**

```json
{
  "stockQuantity": 55,
  "supplierValue": 120.50
}
```

`60 − 5 = 55` (stock decremented). `supplier_value` is still `120.50` from the Inbound — **untouched on Outbound, per spec.** Outbound `supplierValue` in the response is `null` (the wire shape only carries the value field relevant to the movement type).

**Goal (b):** The CONF-01 confirmation modal copy is locked verbatim in source. See §"Frontend Source-Level Contracts → CONF-01 grep evidence" below — every locked string and ARIA attribute is grep-verified.

### Criterion 3 — INSUFFICIENT_BALANCE with dynamic hint (MOVE-04)

Filled by Task 2 — see §"MOVE-04 — INSUFFICIENT_BALANCE" below.

### Criterion 4 — Idempotency-Key required + replay (MOVE-02 + MOVE-03 + FRONT-12)

Filled by Task 2 (`MISSING_IDEMPOTENCY_KEY` + replay-identical-body) and Task 4 (FRONT-12 source grep) — see §"MOVE-02 — MISSING_IDEMPOTENCY_KEY", §"MOVE-03 — Idempotency replay returns identical body", §"FRONT-12 — Idempotency-Key generated via crypto.randomUUID()".

### Criterion 5 — History pagination + zero N+1 (MOVE-08 + MOVE-09)

Filled by Task 3 — see §"MOVE-09 — Zero-N+1 query-count evidence".

---

## MOVE-* Requirement Evidence

### MOVE-01 — Movement creation is atomic (movement INSERT + product UPDATE in one tx)

Proven by the Inbound (§Criterion 1) and Outbound (§Criterion 2) sections — both responses return 201 AND the immediate follow-up `GET /api/products/{id}` shows the product row updated. Plan 03-03 SUMMARY documents the BEGIN/COMMIT flow at the service layer (`StockMovementService.CreateAsync` lines 2-7 of the annotated flow). No partial states observed in any test.

### MOVE-06 — Inbound updates supplier_value to the posted value

The product's `supplier_value` advanced from `100.00 → 120.50` after the Inbound posted `supplierValue: 120.50`. **MOVE-06 verified** (§Criterion 1).

### MOVE-08 — Listing paginated + filterable by productId / startDate / endDate

**Request:**

```bash
curl -fsS "http://localhost:8080/api/stock-movements?productId=7168c579-8259-4c0e-949b-998a7147c47f&pageSize=10"
```

**Response shape (jq summary):**

```json
{
  "itemCount": 2,
  "pagination": {
    "page": 1,
    "pageSize": 10,
    "total": 2,
    "totalPages": 1,
    "hasNext": false,
    "hasPrev": false
  },
  "hasJoinFields": true
}
```

The envelope is `{ items, pagination, _links }` (PagedMovementsResponse per Plan 03-01). 2 items returned (the Inbound + Outbound created above). `pagination` includes the 6 canonical fields. `_links` block present (omitted from the summary above for brevity). `hasJoinFields: true` confirms each item carries `productCode` from the JOIN (MOVE-09 verified at the API contract level).

### MOVE-09 — JOIN'd shape (no separate fetch for product fields)

**API-contract evidence:** every list item and detail response carries `productCode` + `productDescription` (see §Criterion 1 body and §MOVE-10 below). No follow-up GET /api/products/{id} is required from the client.

**Database-level evidence:** see §"MOVE-09 — Zero-N+1 query-count evidence" (Task 3) for the `pg_stat_statements` snapshot proving exactly 2 distinct SQL statements per history page (1 JOIN'd SELECT + 1 COUNT) regardless of `pageSize`.

### MOVE-10 — Detail endpoint returns the JOIN'd movement

**Request:**

```bash
curl -fsS "http://localhost:8080/api/stock-movements/e3de56ef-4e8e-426a-a953-5e331f8bbd8f"
```

**Response (relevant fields):**

```json
{
  "productCode": "P3-SMOKE-001",
  "productDescription": "Phase 3 smoke product",
  "_links": {
    "self": "/api/stock-movements/e3de56ef-4e8e-426a-a953-5e331f8bbd8f",
    "product": "/api/products/7168c579-8259-4c0e-949b-998a7147c47f"
  }
}
```

`productCode` and `productDescription` are both present from the same JOIN'd query as the list. `_links.self` matches the requested URL; `_links.product` resolves back to the parent product. **MOVE-10 verified.**

### MOVE-11 — Movements are immutable (no PUT, no DELETE, no PATCH)

```bash
$ curl -s -o /dev/null -w "%{http_code}" -X PUT    "http://localhost:8080/api/stock-movements/e3de56ef-4e8e-426a-a953-5e331f8bbd8f" -H "Content-Type: application/json" -d '{}'
405
$ curl -s -o /dev/null -w "%{http_code}" -X DELETE "http://localhost:8080/api/stock-movements/e3de56ef-4e8e-426a-a953-5e331f8bbd8f"
405
```

Both unsupported verbs return **HTTP 405 Method Not Allowed.** This is enforced by absence of `[HttpPut]` / `[HttpDelete]` routes on `StockMovementsController` (Plan 03-03 SUMMARY's "MOVE-11 enforcement by ABSENCE" pattern). ASP.NET MVC's routing emits 405 with the correct `Allow` header. **MOVE-11 verified.**

---

### MOVE-02 — MISSING_IDEMPOTENCY_KEY (header absent → 400)

**Request (no `Idempotency-Key` header):**

```bash
curl -s -o /tmp/missing.json -w "%{http_code}\n" -X POST http://localhost:8080/api/stock-movements \
  -H "Content-Type: application/json" \
  -d '{"productId":"7168c579-8259-4c0e-949b-998a7147c47f","type":"Inbound","quantity":1,"supplierValue":1}'
```

**HTTP status:** `400`
**Response body:**

```json
{
  "errorCode": "MISSING_IDEMPOTENCY_KEY",
  "category": "VALIDATION",
  "message": "Header 'Idempotency-Key' é obrigatório para registrar movimentos",
  "hint": "Gere um UUID v4 no cliente (crypto.randomUUID()) e envie no header 'Idempotency-Key'.",
  "statusCode": 400,
  "retryable": true,
  "details": null,
  "traceId": "0HNLJG2QH4IQ5:00000001",
  "timestamp": "2026-05-16T19:26:08.7085146Z"
}
```

All nine canonical `ErrorResponse` fields present. Hint mentions `crypto.randomUUID()` (verified by `grep -q "crypto.randomUUID" /tmp/03-05/missing.json`). **MOVE-02 verified.**

### MOVE-03 — Idempotency replay returns 200 + Idempotency-Replay header + identical body

**Setup:** the original Inbound from §Criterion 1 used `Idempotency-Key: b9472614-10db-4f4a-8a03-56fe593cd226`. Posting the SAME key with the SAME body again triggers the replay path.

**Request:**

```bash
curl -fsS -i -X POST http://localhost:8080/api/stock-movements \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: b9472614-10db-4f4a-8a03-56fe593cd226" \
  -d '{"productId":"7168c579-8259-4c0e-949b-998a7147c47f","type":"Inbound","quantity":10,"supplierValue":120.50}'
```

**Response (full HTTP, headers + body):**

```
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Date: Sat, 16 May 2026 19:26:17 GMT
Server: Kestrel
Transfer-Encoding: chunked
Idempotency-Replay: true

{"id":"e3de56ef-4e8e-426a-a953-5e331f8bbd8f","productId":"7168c579-...","productCode":"P3-SMOKE-001","productDescription":"Phase 3 smoke product","type":"Inbound","quantity":10,"supplierValue":120.50,"saleValue":null,"idempotencyKey":"b9472614-10db-4f4a-8a03-56fe593cd226","occurredAt":"2026-05-16T19:24:22.312105Z","createdAt":"2026-05-16T19:24:22.312105Z","_links":{...}}
```

**Status line:** `HTTP/1.1 200 OK` (NOT 201).
**Replay header:** `Idempotency-Replay: true` (verified by `echo "$REPLAY_FULL" | grep -i "^Idempotency-Replay:" | grep -qi "true"`).
**Body identity:** `diff <(echo "$INBOUND_BODY" | jq -S '.') <(echo "$REPLAY_BODY" | jq -S '.')` → **no output (zero diff, exit 0).** Every field is byte-identical: `id`, `idempotencyKey`, `occurredAt`, `createdAt`, `supplierValue`, etc. No "createdAt updated on replay", no "new id", no random rebuild.

**Side-effect proof:** after the replay, the product state is still `{stockQuantity: 55, supplierValue: 120.50}` — the replay did NOT double-apply the +10 stock bump. **MOVE-03 verified.**

### MOVE-04 — INSUFFICIENT_BALANCE with dynamic hint + structured details

**Request (Outbound qty=999999 against product with stock=55):**

```bash
curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/stock-movements \
  -H "Content-Type: application/json" -H "Idempotency-Key: $(uuidgen)" \
  -d '{"productId":"7168c579-...","type":"Outbound","quantity":999999,"saleValue":1.00}'
```

**HTTP status:** `422`
**Response body:**

```json
{
  "errorCode": "INSUFFICIENT_BALANCE",
  "category": "BUSINESS_RULE",
  "message": "Saldo insuficiente: solicitado 999999 unidades, disponível 55",
  "hint": "Reduza a quantidade para no máximo 55 ou registre uma entrada antes.",
  "statusCode": 422,
  "retryable": false,
  "details": {
    "productId": "7168c579-8259-4c0e-949b-998a7147c47f",
    "productCode": "P3-SMOKE-001",
    "requested": 999999,
    "available": 55,
    "deficit": 999944
  },
  "traceId": "0HNLJG2QH4IQ8:00000001",
  "timestamp": "2026-05-16T19:26:26.1301069Z"
}
```

- `errorCode == "INSUFFICIENT_BALANCE"` ✓
- `details.deficit > 0` (999944) ✓
- `details.requested` and `details.available` present ✓
- Hint literal `"Reduza a quantidade para no máximo 55 ou registre uma entrada antes."` — matches the spec wording, and the `55` is **dynamic** (it was the current product stock at the moment of the request, NOT a hardcoded number). **MOVE-04 verified.**

### MOVE-05 — PRODUCT_DELETED (soft-deleted product cannot accept movements)

**Setup:** create a product, soft-delete it, attempt to register a movement.

```bash
DEL_PRODUCT_ID=$(curl -fsS -X POST http://localhost:8080/api/products -H "Content-Type: application/json" \
  -d '{"code":"P3-DEL-001","description":"Will be soft-deleted","type":"Electronic","supplierValue":1,"initialStockQuantity":10}' | jq -r '.id')
curl -fsS -X DELETE "http://localhost:8080/api/products/$DEL_PRODUCT_ID"   # → 204
curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/stock-movements \
  -H "Content-Type: application/json" -H "Idempotency-Key: $(uuidgen)" \
  -d "{\"productId\":\"$DEL_PRODUCT_ID\",\"type\":\"Inbound\",\"quantity\":1,\"supplierValue\":1}"
```

**HTTP status:** `422`
**Response body:**

```json
{
  "errorCode": "PRODUCT_DELETED",
  "category": "BUSINESS_RULE",
  "message": "Produto 'P3-DEL-001' foi excluído e não aceita novas movimentações",
  "hint": "Produto 'P3-DEL-001' foi excluído. Movimentos não podem ser registrados para produtos excluídos.",
  "statusCode": 422,
  "retryable": false,
  "details": {
    "productId": "18557e6a-ee57-4408-8e5b-a86d550ea1a1",
    "productCode": "P3-DEL-001"
  },
  "traceId": "0HNLJG2QH4IQB:00000001",
  "timestamp": "2026-05-16T19:26:41.1466194Z"
}
```

`errorCode == "PRODUCT_DELETED"`, hint contains `"Movimentos não podem ser registrados"`, `details` carries the product identity. **MOVE-05 verified.**

### MOVE-07 — INVALID_MOVEMENT_VALUES (value-field inversion: Inbound carrying saleValue)

**Request (Inbound with `saleValue` instead of `supplierValue`):**

```bash
curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/stock-movements \
  -H "Content-Type: application/json" -H "Idempotency-Key: $(uuidgen)" \
  -d '{"productId":"7168c579-...","type":"Inbound","quantity":1,"saleValue":10.00}'
```

**HTTP status:** `422`
**Response body:**

```json
{
  "errorCode": "INVALID_MOVEMENT_VALUES",
  "category": "BUSINESS_RULE",
  "message": "Movimento do tipo 'Inbound' não aceita o campo 'saleValue'",
  "hint": "Movimentos do tipo 'Inbound' requerem o campo 'supplierValue', e não 'saleValue'.",
  "statusCode": 422,
  "retryable": false,
  "details": {
    "type": "Inbound",
    "providedField": "saleValue",
    "expectedField": "supplierValue"
  },
  "traceId": "0HNLJG2QH4IQC:00000001",
  "timestamp": "2026-05-16T19:26:41.1849566Z"
}
```

`errorCode == "INVALID_MOVEMENT_VALUES"`. Hint mentions `supplierValue` (the expected field). `details.expectedField == "supplierValue"`, `details.providedField == "saleValue"`. **MOVE-07 verified.**

### MOVEMENT_NOT_FOUND (MOVE-10 negative path)

**Request:**

```bash
curl -s -w "\n%{http_code}" "http://localhost:8080/api/stock-movements/$(uuidgen)"
```

**HTTP status:** `404`
**Response body:**

```json
{
  "errorCode": "MOVEMENT_NOT_FOUND",
  "category": "NOT_FOUND",
  "message": "Movimento não encontrado",
  "hint": "Movimento não encontrado. Atualize a página e tente novamente.",
  "statusCode": 404,
  "retryable": false,
  "details": {
    "movementId": "3d8ad32b-9a45-4624-ad2c-c401b826dab5"
  },
  "traceId": "0HNLJG2QH4IQD:00000001",
  "timestamp": "2026-05-16T19:26:41.2168776Z"
}
```

`errorCode == "MOVEMENT_NOT_FOUND"`. Category `NOT_FOUND` (HTTP 404 via category auto-mapping per Plan 03-01 SUMMARY). **MOVE-10 negative path verified.**

---

### MOVE-09 — Zero-N+1 query-count evidence (history pagination emits exactly 2 SQL statements)

**Method used:** statement logging (fallback method (b) per the plan's `<query-count-strategy>` block). `pg_stat_statements` was created but **could not be loaded** — Postgres 16-alpine requires the library to be listed in `shared_preload_libraries`, which is a config-time setting (`postgresql.conf`) not modifiable via `ALTER SYSTEM` without restarting the cluster. Falling back to `log_statement = 'all'` produced clean, parseable evidence.

**Setup:**

```bash
docker compose exec -T postgres psql -U stockeasy -d stockeasy -c "ALTER SYSTEM SET log_statement = 'all';"
docker compose exec -T postgres psql -U stockeasy -d stockeasy -c "SELECT pg_reload_conf();"
```

**Probe:**

```bash
# Snapshot postgres logs before the request
docker compose logs --no-color postgres > /tmp/03-05/pg-log-before.txt

# Hit the endpoint with the largest pageSize (100 — covers the most potential N+1 surface)
curl -fsS "http://localhost:8080/api/stock-movements?pageSize=100" > /tmp/03-05/list-page1.json

# Snapshot logs after the request and slice the diff
docker compose logs --no-color postgres > /tmp/03-05/pg-log-after.txt
NEW_LINES=$(( $(wc -l < /tmp/03-05/pg-log-after.txt) - $(wc -l < /tmp/03-05/pg-log-before.txt) ))
tail -n "$NEW_LINES" /tmp/03-05/pg-log-after.txt > /tmp/03-05/pg-log-slice.txt

# Count distinct prepared-statement executions that touch stock_movements
grep -c "LOG:  statement:" /tmp/03-05/pg-log-slice.txt          # → reset markers (DISCARD ALL × 2)
grep -E "stock_movements" /tmp/03-05/pg-log-slice.txt | grep -v "pg_stat" | wc -l   # → 2 FROM-clause matches (one per statement)
```

**Raw log slice (the only `stock_movements`-touching statements observed for one history page of pageSize=100):**

```
2026-05-16 19:28:10.580 UTC [78] LOG:  execute <unnamed>:
            SELECT
        m.id              as Id,
        m.product_id      as ProductId,
        m.type            as Type,
        m.quantity        as Quantity,
        m.sale_value      as SaleValue,
        m.supplier_value  as SupplierValue,
        m.idempotency_key as IdempotencyKey,
        m.occurred_at     as OccurredAt,
        m.created_at      as CreatedAt,
        p.code            as ProductCode,
        p.description     as ProductDescription
        FROM stock_movements m
        INNER JOIN products p ON p.id = m.product_id
            WHERE ($1::uuid IS NULL OR m.product_id = $1)
              AND ($2::timestamptz IS NULL OR m.occurred_at >= $2)
              AND ($3::timestamptz IS NULL OR m.occurred_at <= $3)
            ORDER BY m.occurred_at DESC, m.id DESC
            LIMIT $4 OFFSET $5
2026-05-16 19:28:10.580 UTC [78] DETAIL:  parameters: $1 = NULL, $2 = NULL, $3 = NULL, $4 = '100', $5 = '0'

2026-05-16 19:28:10.581 UTC [78] LOG:  execute <unnamed>:
            SELECT COUNT(*)
        FROM stock_movements m
        INNER JOIN products p ON p.id = m.product_id
            WHERE ($1::uuid IS NULL OR m.product_id = $1)
              AND ($2::timestamptz IS NULL OR m.occurred_at >= $2)
              AND ($3::timestamptz IS NULL OR m.occurred_at <= $3)
2026-05-16 19:28:10.581 UTC [78] DETAIL:  parameters: $1 = NULL, $2 = NULL, $3 = NULL
```

**Statement count:** **exactly 2** `execute <unnamed>` blocks against `stock_movements`. One JOIN'd SELECT (paged) and one COUNT (same WHERE, no ORDER/LIMIT). No per-row product fetch, no fan-out — even though the response payload carries `productCode` + `productDescription` for every item, those fields come from the JOIN, not a follow-up query.

`grep -E "stock_movements" pg-log-slice.txt | grep -v pg_stat | wc -l` → **2** (two `FROM stock_movements m` matches, one per statement).

**MOVE-09 verified at the database plane.** Per Plan 03-01 SUMMARY, the SQL shape is exactly what the repository emits (`SelectColumns` + `FromJoin` constants reused between `ListAsync` items and count paths). Same shape will hold for pageSize=20, pageSize=50, or any pageSize ≤ 100 — the query plan is parameter-driven.

**Cleanup:** `log_statement = 'all'` is left enabled for the rest of the smoke (it does not affect correctness; Postgres just keeps logging). The compose teardown at the end of the plan (`docker compose down -v`) wipes the volume entirely.

### Swagger Contract — Phase 3 added + Phase 2 preserved

**Probe:** `curl -fsS http://localhost:8080/swagger/v1/swagger.json > /tmp/03-05/swagger.json` (28,566 bytes).

| operationId | Expected count | Got |
|-------------|-----:|-----:|
| `createStockMovement` (Phase 3) | 1 | **1** |
| `listStockMovements` (Phase 3) | 1 | **1** |
| `getStockMovement` (Phase 3) | 1 | **1** |
| `createProduct` (Phase 2 regression) | 1 | **1** |
| `listProducts` (Phase 2 regression) | 1 | **1** |
| `getProduct` (Phase 2 regression) | 1 | **1** |
| `deleteProduct` (Phase 2 regression) | 1 | **1** |

Probe command (handles both `"operationId":"…"` and `"operationId": "…"` spacings):

```bash
for op in createStockMovement listStockMovements getStockMovement createProduct listProducts getProduct deleteProduct; do
  COUNT=$(grep -Ec "\"operationId\":\\s*\"$op\"" /tmp/03-05/swagger.json)
  printf "%-30s = %d\n" "$op" "$COUNT"
done
```

**Schemas exposed (Phase 3 additions):**

```bash
$ jq -e '.components.schemas.MovementResponse'        /tmp/03-05/swagger.json > /dev/null && echo OK   # MovementResponse OK
$ jq -e '.components.schemas.CreateMovementRequest'   /tmp/03-05/swagger.json > /dev/null && echo OK   # CreateMovementRequest OK
$ jq -e '.components.schemas.PagedMovementsResponse'  /tmp/03-05/swagger.json > /dev/null && echo OK   # PagedMovementsResponse OK
```

**Idempotency-Key declared as header parameter on the POST operation:**

```bash
$ jq -e '.paths."/api/stock-movements".post.parameters[]? | select(.name=="Idempotency-Key")' /tmp/03-05/swagger.json > /dev/null && echo OK
```

→ OK (parameter is present and `in: header`).

**MovementType enum is serialized as strings (not integers):**

```bash
$ jq -e '.components.schemas.MovementType.enum | index("Inbound")'  /tmp/03-05/swagger.json > /dev/null && echo OK   # Inbound enum OK
$ jq -e '.components.schemas.MovementType.enum | index("Outbound")' /tmp/03-05/swagger.json > /dev/null && echo OK   # Outbound enum OK
```

Both literal strings present in the enum (LLM/human-friendly per D23).

**Swagger contract intact AND extended.** No Phase 2 operationIds disappeared; all three Phase 3 ids are present with stable camelCase verb-noun naming.

---

## Frontend SPA Smoke + Source-Level Contracts

### SPA bundle reaches `/stock-movements`

```bash
$ curl -fsS http://localhost:5173/stock-movements -o /tmp/03-05/spa.html
$ wc -c /tmp/03-05/spa.html
848 /tmp/03-05/spa.html
$ grep -q "StockEasy" /tmp/03-05/spa.html && echo OK            # → OK
$ grep -E "main\.ts|src/main" /tmp/03-05/spa.html
    <script type="module" src="/src/main.ts"></script>
```

Vite-served HTML returns 200, contains the wordmark "StockEasy" (from `<title>` and document shell), and references `/src/main.ts` (the SPA entry). Vue Router's `/stock-movements` route is mounted (Phase 1's router config).

### FRONT-12 — Idempotency-Key generated via `crypto.randomUUID()` (source grep)

```bash
$ grep -n "generateIdempotencyKey" frontend/src/features/stock/api.ts
1:import { apiClient, generateIdempotencyKey } from '@/shared/api/client'
18: * `generateIdempotencyKey()` (which wraps `crypto.randomUUID()` per the shared
31:        headers: { 'Idempotency-Key': generateIdempotencyKey() },

$ grep -n "crypto.randomUUID" frontend/src/shared/api/client.ts
57:  return crypto.randomUUID()

$ grep -n "'Idempotency-Key':" frontend/src/features/stock/api.ts
31:        headers: { 'Idempotency-Key': generateIdempotencyKey() },
```

The injection mechanism is anchored in one line of `frontend/src/features/stock/api.ts:31` — `headers: { 'Idempotency-Key': generateIdempotencyKey() }`. The helper itself lives in `frontend/src/shared/api/client.ts:57` and wraps `crypto.randomUUID()`. **FRONT-12 verified.**

### Tab-strip ARIA contract (UI-SPEC §"Tab Strip Composition")

Source file: `frontend/src/features/stock/pages/StockMovementsPage.vue`.

| Contract token | Grep result |
| -------------- | ----------- |
| `role="tablist"` | present |
| `role="tab"` | present |
| `:aria-selected="activeTab` (dynamic binding) | present |
| `:tabindex="activeTab === tab.id ? 0 : -1"` (roving tabindex) | present |
| `role="tabpanel"` | present |
| ArrowRight/Left/Up/Down keyboard handlers | present |
| Home/End handlers | present |
| `router.replace` (URL ?tab= sync) | present |
| **No** `v-show` (must be `v-if`) | absent (correct) |
| `v-if="activeTab === 'entrada'"` (mounted-only panel) | present |

All 10 ARIA contract tokens verified. The page composes the tab strip inline per UI-SPEC (no BaseTabs primitive extracted).

### CONF-01 — Outbound Confirmation Modal locked copy (source grep)

Source file: `frontend/src/features/stock/components/ConfirmOutboundModal.vue`.

| Contract token | Result |
| -------------- | ------ |
| Title `Confirmar saída?` | present (passed via BaseModal `title` prop per the bound `:title="…"`)|
| Lead paragraph `Esta operação registra uma saída de estoque` | present |
| `<dt>Produto:` definition term | present (line 56-57: `<dt>` followed by `Produto:`) |
| `<dt>Quantidade:` definition term | present (line 63-64) |
| `<dt>Valor de venda:` definition term | present (line 70-71) |
| `<dt>Saldo atual:` definition term | present (line 77-78) |
| `<dt>Saldo resultante:` definition term | present (line 84-85, `<dd>` uses `font-semibold` per spec) |
| `data-autofocus` attribute | present |
| Autofocus is on `variant="secondary"` (Cancelar — safe button) | present (`grep -B2 "data-autofocus" \| grep -q 'variant="secondary"'` passes) |
| **No** `variant="destructive"` on Confirmar (it is brand-primary per spec) | absent (correct) |
| `Confirmar saída` confirm-button label | present |
| `Registrando saída...` in-flight label | present |

**Note on grep precision:** the plan's verbatim grep patterns were single-line (e.g. `">Produto:<"`) which do not match Vue's multi-line indented HTML (`<dt class="…">\n  Produto:\n</dt>`). Adjusted the grep to be multi-line tolerant (`grep -q "Produto:"` + structural verification via `grep -B1`) — the locked copy is present byte-for-byte; only the regex shape needed adjustment. All 12 contract tokens above are verified.

### Movement-type badge mapping (D-06) — MovementHistory

Source file: `frontend/src/features/stock/components/MovementHistory.vue`.

| Contract token | Result |
| -------------- | ------ |
| `movementTypeLabel[m.type]` (no inline ternary for label) | present |
| `ArrowDownToLine` icon (paired with Entrada) | present |
| `ArrowUpFromLine` icon (paired with Saída) | present |
| `variant="success"` (badge color for Inbound) | present (via dynamic binding) |
| `variant="danger"` (badge color for Outbound) | present (via dynamic binding) |
| Inbound is paired with `success` (not inverted) on the same line | present: `<BaseBadge :variant="m.type === 'Inbound' ? 'success' : 'danger'">` |

The mapping is anchored on **one line** with the dynamic ternary `m.type === 'Inbound' ? 'success' : 'danger'` — Entrada always green, Saída always red. Label is rendered via `movementTypeLabel[m.type]` (the closed catalog from `@/shared/labels.ts`), never an inline string ternary. **D-06 verified.**

## Manual UAT items (handed off to Phase 4 / human evaluator)

Plan 03-05 cannot drive a real browser (no Playwright/Cypress — Phase 4 territory). The grep evidence above proves the locked copy ships in source, the API contracts are honored at the wire level, and the SPA shell loads. The following items require a real browser to fully verify and are recorded here as the Phase 3 UAT hand-off:

| # | Item | Expected | Why human |
| - | ---- | -------- | --------- |
| 1 | Open `http://localhost:5173/stock-movements?tab=saida`, register a Saída with a positive quantity ≤ stock | The CONF-01 modal opens; renders the 5 definition rows with correct numbers (formatCurrency + formatQuantity); Cancelar receives autofocus; the Confirmar button is brand-primary (NOT destructive red) | Visual rendering, autofocus behavior, color contrast — requires real DOM |
| 2 | Press Esc while the CONF-01 modal is open | Modal closes; form data preserved (product still selected, quantity still in the input); no toast fired | Esc-to-dismiss requires a real keyboard event loop |
| 3 | Trigger INSUFFICIENT_BALANCE inside the CONF-01 modal (Outbound qty > current stock) | Modal stays open; toast surfaces `apiError.hint` ("Reduza a quantidade para no máximo {n} ou registre uma entrada antes."); the Disponível helper in the parent OutboundForm refreshes to `apiError.details.available` (D-08 race-mitigation in real time) | Live-DOM observation of helper refresh + toast surfacing required |
| 4 | Navigate `/stock-movements` with no `?tab` query param | Default tab is `historico` (D-01) — MovementHistory mounts and either renders 4-state empty-CTA or paginated table | URL default behavior + initial-mount render require a browser session |
| 5 | Press ArrowRight on the active tab button | Focus moves to the next tab (wrap from `historico` → `entrada`); the focused tab is activated (URL `?tab=` updates via `router.replace`); the corresponding `v-if` panel mounts | WAI-ARIA tabs automatic-activation pattern requires real keyboard + focus observation |
| 6 | Trigger a network failure (`docker compose stop backend`) and hit `Tentar novamente` in the history error state | Inline error component shows; CTA triggers a refetch; toast/inline copy mentions network failure | Requires browser network stack observation |

These items belong to Phase 4 (xUnit + Vitest + Playwright/Cypress + manual UAT). They are listed here verbatim so the human evaluator (or Phase 4 planner) can replay each one against the running stack and capture pass/fail in a follow-up UAT document.

---

## MOVE-* Requirements Coverage Matrix

| Req | Status | Evidence section |
|-----|--------|------------------|
| MOVE-01 — Atomic movement creation (INSERT + UPDATE in one tx) | ✓ | §Criterion 1 (Inbound atomicity) + §Criterion 2 (Outbound atomicity) |
| MOVE-02 — `Idempotency-Key` header required on POST | ✓ | §MOVE-02 — MISSING_IDEMPOTENCY_KEY (HTTP 400 + canonical error + crypto.randomUUID hint) |
| MOVE-03 — Replay returns 200 + Idempotency-Replay: true + identical body | ✓ | §MOVE-03 — Idempotency replay (status 200, header present, jq -S diff empty, no double-apply) |
| MOVE-04 — INSUFFICIENT_BALANCE with dynamic hint | ✓ | §MOVE-04 — INSUFFICIENT_BALANCE (HTTP 422, dynamic 55, details.deficit=999944) |
| MOVE-05 — PRODUCT_DELETED rejects movement on soft-deleted product | ✓ | §MOVE-05 — PRODUCT_DELETED (HTTP 422, hint mentions product code) |
| MOVE-06 — Inbound updates supplier_value AND stock_quantity | ✓ | §Criterion 1 (supplier_value 100→120.50 + stock 50→60) |
| MOVE-07 — INVALID_MOVEMENT_VALUES (value-field inversion) | ✓ | §MOVE-07 — INVALID_MOVEMENT_VALUES (Inbound + saleValue → 422 + expectedField=supplierValue) |
| MOVE-08 — Paginated history with productId/startDate/endDate filters | ✓ | §MOVE-08 — Listing paginated (envelope {items, pagination, _links}, filtered by productId, 2 items returned) |
| MOVE-09 — JOIN'd shape + zero N+1 (exactly 2 SQL statements per page) | ✓ | §MOVE-09 — Zero-N+1 (postgres log slice shows 2 `execute <unnamed>` blocks: JOIN'd SELECT + COUNT) |
| MOVE-10 — Detail endpoint returns the JOIN'd movement; 404 on unknown id | ✓ | §MOVE-10 (productCode + productDescription present) + §MOVEMENT_NOT_FOUND (404 canonical error) |
| MOVE-11 — Immutability (no PUT, no DELETE, no PATCH) | ✓ | §MOVE-11 (PUT=405, DELETE=405 via route absence) |
| FRONT-12 — Frontend generates Idempotency-Key via crypto.randomUUID() | ✓ | §FRONT-12 (source grep: api.ts:31 + client.ts:57) |
| CONF-01 — Outbound Confirmation Modal locked copy + autofocus contract | ✓ | §CONF-01 (12 contract tokens grep-verified) |

**All 13 requirements assigned to this plan have evidence.** Coverage: 13/13.

---

## Regression Sanity (Phase 2 still works)

After all Phase 3 smoke runs, Phase 2's GET /api/products and `/products` SPA route remain regression-clean:

```bash
$ curl -fsS "http://localhost:8080/api/products?pageSize=5" | jq '{hasItems: (.items != null), hasPagination: (.pagination != null), hasLinks: (._links != null)}'
{
  "hasItems": true,
  "hasPagination": true,
  "hasLinks": true
}

$ curl -fsS http://localhost:5173/products -o /tmp/03-05/products-spa.html
$ grep -q "StockEasy" /tmp/03-05/products-spa.html && echo OK
OK
```

Phase 2 endpoints still respond with the canonical envelope; Phase 2 frontend route still loads the Vite shell. **Zero regression observed.** Phase 2 operationIds (`createProduct`, `listProducts`, `getProduct`, `deleteProduct`) are still in `/swagger/v1/swagger.json` (counted in §"Swagger Contract").

---

## Sign-off — 5 ROADMAP Success Criteria for Phase 3

| # | Success Criterion | Status | Evidence section |
| - | ---------------- | ------ | ---------------- |
| 1 | Inbound registered atomically (stock + supplier_value updated in one tx) | ✓ | §Criterion 1 — Inbound atomicity (MOVE-01 + MOVE-06) |
| 2 | Outbound with full resumo modal (CONF-01) + atomic decrement | ✓ | §Criterion 2 — Outbound atomicity (API) + §CONF-01 — locked copy (source grep) |
| 3 | INSUFFICIENT_BALANCE with dynamic hint and `details` | ✓ | §MOVE-04 (Criterion 3) — HTTP 422 + dynamic hint mentioning `55` + details.deficit=999944 |
| 4 | Idempotency-Key required + replay returns 200 + `Idempotency-Replay: true` + identical body | ✓ | §MOVE-02 (key required) + §MOVE-03 (replay payload identity) + §FRONT-12 (Criterion 4) |
| 5 | History paginated, filterable, JOIN'd shape, exactly 2 SQL statements per page | ✓ | §MOVE-08 (envelope) + §MOVE-09 (zero-N+1 postgres log evidence) (Criterion 5) |

**Score: 5/5 ROADMAP success criteria verified.** Three are fully automated at the API + DB plane (1, 3, 5). Two combine API-plane evidence with source-grep on the frontend contracts (2, 4) and complete with manual UAT items §"Manual UAT items" #1, #2, #3, #5 for the visual / keyboard / focus surface in a real browser.

---

## Known Limitations / Phase 4 Hand-off

| Item | Why deferred | Phase 4 task |
| ---- | ------------ | ------------ |
| Live-browser tab-strip keyboard testing (ArrowLeft/Right/Home/End/Enter activation, focus visibility, screen-reader announcement on tab swap) | This plan ships no browser-driver (Playwright/Cypress not in scope until Phase 4) | Cover via §"Manual UAT items" #5 (real browser) + Phase 4 component test |
| Vitest unit/component tests for InboundForm / OutboundForm / ConfirmOutboundModal / MovementHistory / useStockMovements | Phase 4 explicitly owns Vitest + Vue Test Utils setup | Mount each component, assert against the locked copy + state transitions verified by source grep here |
| xUnit tests for StockMovementService (transactional path + 23505 race recovery) + StockMovementRepository (zero-N+1 SQL shape) | Phase 4 owns xUnit + Moq setup | Re-use the verbatim happy/unhappy curl matrix above as test fixtures |
| Branded PDF docs (`docs/01-product-decisions.md`, `02-architecture.md`, `03-business-rules.md`) covering the catalog of error codes / movement lifecycle / Idempotency contract | Phase 4 owns Pandoc + WeasyPrint + mermaid-filter pipeline | Reference this verification doc as the source of truth for the business-rules and errorCode catalog entries |
| "Saldo após" historical column in MovementHistory (running balance per row) | Intentionally NOT persisted in v1 — would couple history to a derived field that drifts under concurrent writes | Out of scope; if a future requirement adds this, compute it server-side from the windowed sum, not via a persisted column |
| Live-browser CONF-01 modal flow (autofocus on Cancelar, Esc to close, INSUFFICIENT_BALANCE keeping the modal open) | Plan 03-05 verified the locked copy + structural ARIA in source; live focus/keyboard observation requires a browser | §"Manual UAT items" #1, #2, #3 |

These items are explicitly NOT blockers for Phase 3 completion — the API + DB plane is fully verified, the SPA bundle loads, the source-level contracts are grep-locked. Phase 4 picks up the browser-level + test-coverage surface.

---

## Self-Check: PASSED

**Files verified to exist:**

- FOUND: `.planning/phases/03-stock-movements-vertical-slice/03-VERIFICATION.md`
- FOUND: `README.md` (modified — `/api/stock-movements` + Idempotency-Key + `/stock-movements?tab=…` + CONF-01 mention + immutability + zero-N+1 surface)
- FOUND (regression-clean, untouched): `backend/Inventory/` source tree (Plan 03-01 + 03-03)
- FOUND (regression-clean, untouched): `frontend/src/features/stock/` (Plan 03-02 + 03-04)
- FOUND (regression-clean, untouched): `init.sql` (Phase 1 — `stock_movements` + `products` schemas)

**Commits to be verified after the final docs commit:** see `03-05-SUMMARY.md` commit list.

---

## Goal-Backward Verifier Addendum

**Verified: 2026-05-16 — independent codebase inspection against ROADMAP success criteria**

This section extends the Plan 03-05 smoke evidence with a direct codebase check on every artifact and wiring claimed above. The verifier read source files independently and did not rely on SUMMARY claims.

### Observable Truths — Codebase Confirmation

| # | Truth (from ROADMAP SC) | Codebase Evidence | Status |
|---|------------------------|-------------------|--------|
| 1 | Inbound atomically increments stock AND updates supplier_value in one tx | `StockMovementService.cs`: `BeginTransactionAsync` present (line 97); `FOR UPDATE` in SQL (line 55); `stock_quantity = stock_quantity + @quantity` AND `supplier_value = @supplierValue` in same UPDATE block (lines 59-60); `CommitAsync` (line 185) | VERIFIED |
| 2 | Outbound confirmation modal with verbatim CONF-01 copy | `ConfirmOutboundModal.vue`: title `Confirmar saída?` (line 44); lead paragraph present (line 49); all 5 `<dt>` terms present; `data-autofocus` on `variant="secondary"` (lines 94-95); no `variant="destructive"`; `Registrando saída...` in-flight label (line 113) | VERIFIED |
| 3 | INSUFFICIENT_BALANCE with dynamic hint and details | `InsufficientBalanceException.cs`: hint built as `$"Reduza a quantidade para no máximo {available} ou registre uma entrada antes."` (line 13); `deficit = requested - available` in details (line 20) | VERIFIED |
| 4 | Idempotency-Key required; replay returns 200 + Idempotency-Replay: true + identical body | `StockMovementsController.cs`: `[FromHeader]` binding; `MissingIdempotencyKeyException` thrown when absent; `Response.Headers[IdempotencyReplayHeader] = "true"` (line 72); `return Ok(response)` on replay. `StockMovementService.cs`: fast-path `GetByIdempotencyKeyAsync` BEFORE transaction (line 88); 23505 catch re-fetches and returns existing row (line 150). `api.ts:31`: `headers: { 'Idempotency-Key': generateIdempotencyKey() }`. `client.ts:57`: `return crypto.randomUUID()` | VERIFIED |
| 5 | History paginated, filterable, JOIN'd, exactly 2 SQL statements per page | `StockMovementRepository.cs`: `INNER JOIN products p ON p.id = m.product_id` in `FromJoin` const (line 35); `SELECT COUNT(*)` in separate query (line 113); no per-row fetch; same `SelectColumns` + `FromJoin` reused across items + count | VERIFIED |

### Artifact Verification (25 backend + 10 frontend files)

All 25 backend files and 10 frontend files listed in PLAN frontmatter `files_modified` fields exist on disk and contain substantive implementation (not stubs). No `TODO`/`FIXME`/placeholder comments found in any phase 03 file. No `as any` in frontend stock files.

### Key Link Verification

| From | To | Via | Status |
|------|----|-----|--------|
| `StockMovementRepository.ListAsync` | `products` table | `INNER JOIN products p ON p.id = m.product_id` at line 35 of repository | WIRED |
| `InsufficientBalanceException` constructor | `ErrorResponse.details` | `details: new { productId, productCode, requested, available, deficit = requested - available }` | WIRED |
| `StockMovementService.CreateAsync` | `products` row via `SELECT FOR UPDATE` | `FOR UPDATE` in `ProductSelectForUpdateSql` const; passed `transaction: tx` to Dapper | WIRED |
| `StockMovementsController` | `Idempotency-Replay: true` header | `Response.Headers[IdempotencyReplayHeader] = "true"` on `isReplay` branch | WIRED |
| `movementsApi.register` | `POST /api/stock-movements` | `apiClient.post('/stock-movements', req, { headers: { 'Idempotency-Key': generateIdempotencyKey() } })` at `api.ts:31` | WIRED |
| `OutboundForm.handleSubmit` | `ConfirmOutboundModal` | `modalOpen.value = true` set in `onSubmit` after Zod validates + pre-check passes | WIRED |
| `StockMovementsPage` tab buttons | URL `?tab=` | `router.replace` called in `setTab()` and `onTabKeydown()` | WIRED |
| `MovementHistory` filter bar | URL query | `router.replace({ query: q })` in `syncUrl()`, watched via `watch([filters, page])` | WIRED |
| `Program.cs` | `IStockMovementRepository + StockMovementService` | `AddScoped<IStockMovementRepository, StockMovementRepository>()` + `AddScoped<StockMovementService>()` both present | WIRED |
| `LinksDto.cs` Phase 2 helpers | preserved | `ForProduct` (line 13) and `ForProductsListing` (line 31) intact alongside `ForMovement` (line 57) and `ForMovementsListing` (line 71) | WIRED |

### Anti-Pattern Scan

| Category | Result |
|----------|--------|
| TODO/FIXME/placeholder in phase 03 backend files | None found |
| TODO/FIXME/placeholder in `frontend/src/features/stock/` | None found |
| `as any` in frontend stock files | None found |
| Empty return stubs (`return null`, `return {}`, `return []`) serving UI data | None found |
| `v-show` on tab panels (forbidden by UI-SPEC) | None found — `v-if` used correctly |
| `variant="destructive"` on Confirmar saída button (forbidden by CONF-01) | None found |
| Inline string ternary for movement type label (forbidden — must use `movementTypeLabel`) | None found — `movementTypeLabel[m.type]` used |

### MOVE-11 Immutability — Verified by Absence

`grep -n "HttpPut\|HttpDelete\|HttpPatch" backend/Inventory/Controllers/StockMovementsController.cs` returns zero matches. ASP.NET MVC routing produces HTTP 405 for unregistered verbs — confirmed by the smoke evidence showing PUT=405 and DELETE=405.

### Requirements Coverage Assessment

All 13 requirements assigned to Phase 3 in REQUIREMENTS.md (MOVE-01 through MOVE-11, FRONT-12, CONF-01) have:
1. Evidence in the Plan 03-05 smoke (live API responses)
2. Source-level confirmation in this verifier section (code artifacts + wiring checks)

No orphaned requirements found. REQUIREMENTS.md maps exactly 13 requirements to Phase 3; all 13 are covered.

### Overall Verdict

**Status: human_needed** — confirmed. All automated checks pass:
- 5/5 ROADMAP success criteria verified at API + DB plane (smoke) and at codebase plane (this verifier)
- 13/13 Phase 3 requirements have both live evidence and source evidence
- No blocker anti-patterns
- No stub artifacts
- All key links wired

The 6 items in §"Manual UAT items" are legitimately deferred to a live-browser session (Phase 4 or human evaluator). They cover visual rendering, keyboard behavior, focus management, and real-time DOM state — none are verifiable by grep or static analysis. They do not represent gaps in the implementation; the implementation code is correct and the source contracts are locked.

_Verified by goal-backward verifier: 2026-05-16_
_Verifier: Claude (gsd-verifier)_
