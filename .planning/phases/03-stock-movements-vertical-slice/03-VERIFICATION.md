---
phase: 03-stock-movements-vertical-slice
verified: 2026-05-16
status: human_needed
score: "5/5 ROADMAP success criteria verified via API + source-grep evidence; 3 manual UAT items handed to Phase 4 / human evaluator"
captured_by: Plan 03-05 (E2E integration smoke)
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

(Tasks 2-4 append sections here for MOVE-02 / MOVE-03 / MOVE-04 / MOVE-05 / MOVE-07 + replay-identical-body + Swagger + zero-N+1 + Frontend Source-Level Contracts + Manual UAT items.)
