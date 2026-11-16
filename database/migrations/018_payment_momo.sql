-- ============================================================
-- 018_payment_momo.sql — Giao dịch thanh toán online (MoMo) — Phase 8
-- Lưu transaction để đối soát IPN; KHÔNG tự động trừ công nợ TDONHANG
-- (kế toán xác nhận qua phiếu thu — tránh ghi trùng khi IPN retry).
-- Idempotent: CREATE TABLE IF NOT EXISTS.
-- ============================================================

CREATE TABLE IF NOT EXISTS W_PAYMENT_TXN (
    ID              CHAR(36) NOT NULL,
    PROVIDER        VARCHAR(30) NOT NULL,
    ORDER_ID        VARCHAR(100) NOT NULL,
    REQUEST_ID      VARCHAR(100) NOT NULL,
    DONHANGID       CHAR(36) NULL,
    AMOUNT          DECIMAL(18,2) NOT NULL,
    ORDER_INFO      VARCHAR(500) NULL,
    STATUS          VARCHAR(20) NOT NULL,          -- PENDING / PAID / FAILED
    RESULT_CODE     INT NULL,
    MESSAGE         VARCHAR(500) NULL,
    TRANS_ID        BIGINT NULL,
    PAY_URL         TEXT NULL,
    QR_CODE_URL     TEXT NULL,
    DEEPLINK        TEXT NULL,
    RAW_CREATE_JSON LONGTEXT NULL,
    RAW_IPN_JSON    LONGTEXT NULL,
    CREATED_AT      DATETIME(3) NOT NULL,
    UPDATED_AT      DATETIME(3) NOT NULL,
    PRIMARY KEY (ID),
    UNIQUE KEY UX_W_PAYMENT_TXN_ORDER (PROVIDER, ORDER_ID),
    KEY IDX_W_PAYMENT_TXN_DONHANG (DONHANGID),
    KEY IDX_W_PAYMENT_TXN_STATUS (STATUS)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
