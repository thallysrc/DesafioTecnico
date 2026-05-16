# Phase 2 Verification — Endpoint Smoke Matrix

**Generated:** 2026-05-16T14:53:29-03:00
**Plan:** 02-06 (E2E smoke + Swagger contract + README touchup)
**Stack:** `docker compose up -d --build` — postgres + backend + frontend all healthy.

Backend reached health in ~6s after `docker compose up` completed.

## Boot Sequence

```bash
NAME                 IMAGE                COMMAND                  SERVICE    CREATED              STATUS                        PORTS
stockeasy-api        stockeasy-api        "dotnet watch run --…"   backend    About a minute ago   Up About a minute             0.0.0.0:8080->8080/tcp
stockeasy-postgres   postgres:16-alpine   "docker-entrypoint.s…"   postgres   About a minute ago   Up About a minute (healthy)   0.0.0.0:5432->5432/tcp
stockeasy-web        stockeasy-web        "docker-entrypoint.s…"   frontend   About a minute ago   Up About a minute             0.0.0.0:5173->5173/tcp
```

## Endpoint Smoke Matrix

### 1. POST /api/products — happy path (3 products with distinct codes)

#### Request 1 — code=P001 (Electronic)
```http
POST /api/products HTTP/1.1
Content-Type: application/json

{"code":"P001","description":"Notebook Dell Latitude","type":"Electronic","supplierValue":4500.00,"initialStockQuantity":12}
```

**Status:** 201

```json
{
  "id": "e3c9c0d5-204c-4829-91da-dacef3e5ff6c",
  "code": "P001",
  "description": "Notebook Dell Latitude",
  "type": "Electronic",
  "supplierValue": 4500.00,
  "stockQuantity": 12,
  "createdAt": "2026-05-16T17:53:29.451222Z",
  "updatedAt": "2026-05-16T17:53:29.451222Z",
  "deletedAt": null,
  "_links": {
    "self": "/api/products/e3c9c0d5-204c-4829-91da-dacef3e5ff6c",
    "delete": "/api/products/e3c9c0d5-204c-4829-91da-dacef3e5ff6c"
  }
}
```

#### Request 2 — code=P002 (Appliance)
```http
POST /api/products HTTP/1.1
Content-Type: application/json

{"code":"P002","description":"Geladeira Brastemp","type":"Appliance","supplierValue":2200.50,"initialStockQuantity":5}
```

**Status:** 201

```json
{
  "id": "84ef7f36-0c39-41ac-ba29-b113cbb8e764",
  "code": "P002",
  "description": "Geladeira Brastemp",
  "type": "Appliance",
  "supplierValue": 2200.50,
  "stockQuantity": 5,
  "createdAt": "2026-05-16T17:53:29.507072Z",
  "updatedAt": "2026-05-16T17:53:29.507072Z",
  "deletedAt": null,
  "_links": {
    "self": "/api/products/84ef7f36-0c39-41ac-ba29-b113cbb8e764",
    "delete": "/api/products/84ef7f36-0c39-41ac-ba29-b113cbb8e764"
  }
}
```

#### Request 3 — code=P003 (Furniture)
```http
POST /api/products HTTP/1.1
Content-Type: application/json

{"code":"P003","description":"Mesa de escritório","type":"Furniture","supplierValue":850.00,"initialStockQuantity":3}
```

**Status:** 201

```json
{
  "id": "0174e032-79fd-4ff2-97d7-2678cec9a3cf",
  "code": "P003",
  "description": "Mesa de escritório",
  "type": "Furniture",
  "supplierValue": 850.00,
  "stockQuantity": 3,
  "createdAt": "2026-05-16T17:53:29.535213Z",
  "updatedAt": "2026-05-16T17:53:29.535213Z",
  "deletedAt": null,
  "_links": {
    "self": "/api/products/0174e032-79fd-4ff2-97d7-2678cec9a3cf",
    "delete": "/api/products/0174e032-79fd-4ff2-97d7-2678cec9a3cf"
  }
}
```

### 2. POST /api/products — empty code (400 VALIDATION_ERROR)

```http
POST /api/products HTTP/1.1
Content-Type: application/json

{"code":"","description":"Foo","type":"Electronic","supplierValue":1,"initialStockQuantity":1}
```

**Status:** 400

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Code": [
      "Código é obrigatório"
    ]
  },
  "traceId": "00-631fbc1800078a82d7974f0874b47293-fa6c7a31ea659501-00"
}
```

### 3. POST /api/products — duplicate code (422 DUPLICATE_CODE)

```http
POST /api/products HTTP/1.1
Content-Type: application/json

{"code":"P001","description":"Outro Notebook","type":"Electronic","supplierValue":100,"initialStockQuantity":1}
```

**Status:** 422

```json
{
  "errorCode": "DUPLICATE_CODE",
  "category": "BUSINESS_RULE",
  "message": "Já existe um produto com código 'P001'",
  "hint": "Já existe um produto com código 'P001'. Use outro código ou recupere o produto via /api/products?includeDeleted=true.",
  "statusCode": 422,
  "retryable": false,
  "details": {
    "code": "P001",
    "existingProductId": null
  },
  "traceId": "0HNLJEFH093O5:00000001",
  "timestamp": "2026-05-16T17:53:29.6165624Z"
}
```

### 4. POST /api/products — negative supplierValue (400 VALIDATION_ERROR)

```http
POST /api/products HTTP/1.1
Content-Type: application/json

{"code":"X999","description":"Test","type":"Electronic","supplierValue":-10,"initialStockQuantity":1}
```

**Status:** 400

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "SupplierValue": [
      "Valor do fornecedor não pode ser negativo"
    ]
  },
  "traceId": "00-4d577ec91c2ca13ca768b725aa4fb657-f11ef8261cdc1636-00"
}
```

### 4b. POST /api/products — invalid enum type "Vehicle"

**Note:** JsonStringEnumConverter rejects unknown enum values during deserialization → FluentValidation never runs on this case. Documenting observed behavior.

```http
POST /api/products HTTP/1.1
Content-Type: application/json

{"code":"X998","description":"Test","type":"Vehicle","supplierValue":10,"initialStockQuantity":1}
```

**Status:** 400

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "request": [
      "The request field is required."
    ],
    "$.type": [
      "The JSON value could not be converted to Inventory.Api.Dtos.CreateProductRequest. Path: $.type | LineNumber: 0 | BytePositionInLine: 52."
    ]
  },
  "traceId": "00-5413b955cf7c956d3f6fef8681d56563-a9470e6789472ec6-00"
}
```

### 5. GET /api/products — default pagination (page=1, pageSize=30)

```http
GET /api/products HTTP/1.1
```

**Status:** 200

```json
{
  "items": [
    {
      "id": "0174e032-79fd-4ff2-97d7-2678cec9a3cf",
      "code": "P003",
      "description": "Mesa de escritório",
      "type": "Furniture",
      "supplierValue": 850.00,
      "stockQuantity": 3,
      "createdAt": "2026-05-16T17:53:29.535213Z",
      "updatedAt": "2026-05-16T17:53:29.535213Z",
      "deletedAt": null,
      "_links": {
        "self": "/api/products/0174e032-79fd-4ff2-97d7-2678cec9a3cf",
        "delete": "/api/products/0174e032-79fd-4ff2-97d7-2678cec9a3cf"
      }
    },
    {
      "id": "84ef7f36-0c39-41ac-ba29-b113cbb8e764",
      "code": "P002",
      "description": "Geladeira Brastemp",
      "type": "Appliance",
      "supplierValue": 2200.50,
      "stockQuantity": 5,
      "createdAt": "2026-05-16T17:53:29.507072Z",
      "updatedAt": "2026-05-16T17:53:29.507072Z",
      "deletedAt": null,
      "_links": {
        "self": "/api/products/84ef7f36-0c39-41ac-ba29-b113cbb8e764",
        "delete": "/api/products/84ef7f36-0c39-41ac-ba29-b113cbb8e764"
      }
    },
    {
      "id": "e3c9c0d5-204c-4829-91da-dacef3e5ff6c",
      "code": "P001",
      "description": "Notebook Dell Latitude",
      "type": "Electronic",
      "supplierValue": 4500.00,
      "stockQuantity": 12,
      "createdAt": "2026-05-16T17:53:29.451222Z",
      "updatedAt": "2026-05-16T17:53:29.451222Z",
      "deletedAt": null,
      "_links": {
        "self": "/api/products/e3c9c0d5-204c-4829-91da-dacef3e5ff6c",
        "delete": "/api/products/e3c9c0d5-204c-4829-91da-dacef3e5ff6c"
      }
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 30,
    "total": 3,
    "totalPages": 1,
    "hasNext": false,
    "hasPrev": false
  },
  "_links": {
    "self": "/api/products?pageSize=30&page=1",
    "first": "/api/products?pageSize=30&page=1",
    "last": "/api/products?pageSize=30&page=1"
  }
}
```

### 6. GET /api/products?pageSize=2&page=1 (pagination — hasNext=true expected)

```http
GET /api/products?pageSize=2&page=1 HTTP/1.1
```

```json
{
  "count": 2,
  "pagination": {
    "page": 1,
    "pageSize": 2,
    "total": 3,
    "totalPages": 2,
    "hasNext": true,
    "hasPrev": false
  },
  "_links": {
    "self": "/api/products?pageSize=2&page=1",
    "first": "/api/products?pageSize=2&page=1",
    "last": "/api/products?pageSize=2&page=2",
    "next": "/api/products?pageSize=2&page=2"
  }
}
```

### 6b. GET /api/products?pageSize=2&page=2 (hasPrev=true expected)

```http
GET /api/products?pageSize=2&page=2 HTTP/1.1
```

```json
{
  "count": 1,
  "pagination": {
    "page": 2,
    "pageSize": 2,
    "total": 3,
    "totalPages": 2,
    "hasNext": false,
    "hasPrev": true
  },
  "_links": {
    "self": "/api/products?pageSize=2&page=2",
    "first": "/api/products?pageSize=2&page=1",
    "last": "/api/products?pageSize=2&page=2",
    "prev": "/api/products?pageSize=2&page=1"
  }
}
```

### 7. GET /api/products/{id} — existing product (200)

**Product ID:** e3c9c0d5-204c-4829-91da-dacef3e5ff6c

```http
GET /api/products/e3c9c0d5-204c-4829-91da-dacef3e5ff6c HTTP/1.1
```

**Status:** 200

```json
{
  "id": "e3c9c0d5-204c-4829-91da-dacef3e5ff6c",
  "code": "P001",
  "description": "Notebook Dell Latitude",
  "type": "Electronic",
  "supplierValue": 4500.00,
  "stockQuantity": 12,
  "createdAt": "2026-05-16T17:53:29.451222Z",
  "updatedAt": "2026-05-16T17:53:29.451222Z",
  "deletedAt": null,
  "_links": {
    "self": "/api/products/e3c9c0d5-204c-4829-91da-dacef3e5ff6c",
    "delete": "/api/products/e3c9c0d5-204c-4829-91da-dacef3e5ff6c"
  }
}
```

### 8. GET /api/products/{non-existent-guid} — (404 PRODUCT_NOT_FOUND)

```http
GET /api/products/00000000-0000-0000-0000-000000000000 HTTP/1.1
```

**Status:** 404

```json
{
  "errorCode": "PRODUCT_NOT_FOUND",
  "category": "NOT_FOUND",
  "message": "Produto não encontrado",
  "hint": "Produto não encontrado. Atualize a lista e tente novamente.",
  "statusCode": 404,
  "retryable": false,
  "details": {
    "productId": "00000000-0000-0000-0000-000000000000"
  },
  "traceId": "0HNLJEFH093OC:00000001",
  "timestamp": "2026-05-16T17:53:29.7887221Z"
}
```

### 9. DELETE /api/products/{id} — soft-delete (204)

**Product ID:** 0174e032-79fd-4ff2-97d7-2678cec9a3cf

```http
DELETE /api/products/0174e032-79fd-4ff2-97d7-2678cec9a3cf HTTP/1.1
```

**Status:** 204 (no body)

### 9b. DELETE /api/products/{same-id} — second call (404 PRODUCT_NOT_FOUND)


**Status:** 404

```json
{
  "errorCode": "PRODUCT_NOT_FOUND",
  "category": "NOT_FOUND",
  "message": "Produto não encontrado",
  "hint": "Produto não encontrado. Atualize a lista e tente novamente.",
  "statusCode": 404,
  "retryable": false,
  "details": {
    "productId": "0174e032-79fd-4ff2-97d7-2678cec9a3cf"
  },
  "traceId": "0HNLJEFH093OE:00000001",
  "timestamp": "2026-05-16T17:53:29.8384040Z"
}
```

### 10. GET /api/products/{soft-deleted-id} — deletedAt populated, _links.delete absent

```http
GET /api/products/0174e032-79fd-4ff2-97d7-2678cec9a3cf HTTP/1.1
```

**Status:** 200

```json
{
  "id": "0174e032-79fd-4ff2-97d7-2678cec9a3cf",
  "code": "P003",
  "description": "Mesa de escritório",
  "deletedAt": "2026-05-16T17:53:29.820103Z",
  "_links": {
    "self": "/api/products/0174e032-79fd-4ff2-97d7-2678cec9a3cf"
  }
}
```

### 11. GET /api/products — default (deleted EXCLUDED — should be 2 items)


```json
{
  "count": 2,
  "codes": [
    "P002",
    "P001"
  ]
}
```

### 12. GET /api/products?includeDeleted=true — includes soft-deleted (3 items)


```json
{
  "count": 3,
  "codes": [
    "P003",
    "P002",
    "P001"
  ],
  "deleted": [
    {
      "code": "P003",
      "deletedAt": "2026-05-16T17:53:29.820103Z"
    },
    {
      "code": "P002",
      "deletedAt": null
    },
    {
      "code": "P001",
      "deletedAt": null
    }
  ]
}
```

### 13. POST /api/products — re-use soft-deleted code 'P003' (422 DUPLICATE_CODE)

**Per backend/CLAUDE.md anti-reuse policy:** full UNIQUE constraint (no filter on deleted_at) prevents code reuse — re-POST is rejected even after soft-delete.

```http
POST /api/products HTTP/1.1
Content-Type: application/json

{"code":"P003","description":"Tentativa de reuso","type":"Furniture","supplierValue":100,"initialStockQuantity":1}
```

**Status:** 422

```json
{
  "errorCode": "DUPLICATE_CODE",
  "category": "BUSINESS_RULE",
  "message": "Já existe um produto com código 'P003'",
  "hint": "Já existe um produto com código 'P003'. Use outro código ou recupere o produto via /api/products?includeDeleted=true.",
  "statusCode": 422,
  "retryable": false,
  "details": {
    "code": "P003",
    "existingProductId": null
  },
  "traceId": "0HNLJEFH093OI:00000001",
  "timestamp": "2026-05-16T17:53:29.9204022Z"
}
```

## Swagger Contract Verification

### Swagger document fetched at /swagger/v1/swagger.json

**Paths:**

```json
[
  "/api/health",
  "/api/products",
  "/api/products/{id}"
]
```

### OperationIds (AGENT-02 — stable camelCase verb-noun)

```json
{
  "createProduct": "createProduct",
  "listProducts": "listProducts",
  "getProduct": "getProduct",
  "deleteProduct": "deleteProduct"
}
```

### ProductType enum schema (AGENT-01 — JsonStringEnumConverter took effect)

```json
{
  "enum": [
    "Electronic",
    "Appliance",
    "Furniture"
  ],
  "type": "string",
  "description": "Closed catalog of product types. Stored as `int` in `products.type` column\r\n(CHECK 0..2). Serialized as string in JSON via global `JsonStringEnumConverter`\r\n(registered in Program.cs Phase 1) — wire emits `\"Electronic\"`, not `0`."
}
```

### Response code coverage per endpoint (AGENT-05 — ProducesResponseType)

**POST /api/products:**

```json
[
  "201",
  "400",
  "422"
]
```

**GET /api/products:**

```json
[
  "200"
]
```

**GET /api/products/{id}:**

```json
[
  "200",
  "404"
]
```

**DELETE /api/products/{id}:**

```json
[
  "204",
  "404"
]
```

### XML doc descriptions (AGENT-03 — XML doc → OpenAPI description)

**POST 201 description:**

```
Created
```

**POST summary:**

```
Register a new product in the inventory catalog.
```
