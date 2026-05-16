-- StockEasy database schema
-- Mounted into postgres:16-alpine via docker-compose at /docker-entrypoint-initdb.d/init.sql
-- Idempotent: safe to re-run (uses IF NOT EXISTS).

CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Products
-- products.code: full UNIQUE constraint (no deleted_at filter) per backend/CLAUDE.md §"Soft Delete":
--   "Re-cadastrar produto com código já existente em produto soft-deleted → DUPLICATE_CODE
--    (UNIQUE constraint sobre code sem filtro de deleted_at previne reuso de código)."
-- This makes PROD-06 (DUPLICATE_CODE) enforcement free at the DB layer.
CREATE TABLE IF NOT EXISTS products (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code            varchar(50) NOT NULL UNIQUE,
    description     varchar(200) NOT NULL,
    type            int NOT NULL,
    supplier_value  numeric(18, 2) NOT NULL DEFAULT 0,
    stock_quantity  int NOT NULL DEFAULT 0,
    deleted_at      timestamptz NULL,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT chk_products_type
        CHECK (type IN (0, 1, 2)),
    CONSTRAINT chk_products_supplier_value_nonneg
        CHECK (supplier_value >= 0),
    CONSTRAINT chk_products_stock_quantity_nonneg
        CHECK (stock_quantity >= 0)
);

-- Stock Movements
CREATE TABLE IF NOT EXISTS stock_movements (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id      uuid NOT NULL,
    type            int NOT NULL,
    quantity        int NOT NULL,
    sale_value      numeric(18, 2) NULL,
    supplier_value  numeric(18, 2) NULL,
    idempotency_key uuid NOT NULL,
    occurred_at     timestamptz NOT NULL DEFAULT now(),
    created_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_stock_movements_product
        FOREIGN KEY (product_id) REFERENCES products(id),
    CONSTRAINT chk_stock_movements_type
        CHECK (type IN (0, 1)),
    CONSTRAINT chk_stock_movements_quantity_positive
        CHECK (quantity > 0),
    CONSTRAINT chk_stock_movements_sale_value
        CHECK (sale_value IS NULL OR sale_value >= 0),
    CONSTRAINT chk_stock_movements_supplier_value
        CHECK (supplier_value IS NULL OR supplier_value >= 0),
    CONSTRAINT uq_stock_movements_idempotency_key
        UNIQUE (idempotency_key)
);

-- History queries: filter by product, ordered by time desc (MOVE-08, BACK-11 zero N+1)
CREATE INDEX IF NOT EXISTS idx_stock_movements_product_occurred
    ON stock_movements (product_id, occurred_at DESC);

-- Generic time-window filter (history without productId filter)
CREATE INDEX IF NOT EXISTS idx_stock_movements_occurred
    ON stock_movements (occurred_at DESC);
