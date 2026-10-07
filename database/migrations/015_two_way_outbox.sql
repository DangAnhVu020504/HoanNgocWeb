-- ============================================================
-- MIGRATION 015 — GĐ7: Đồng bộ 2 chiều có whitelist (KE-HOACH §7)
-- W_OUTBOX: hàng đợi MySQL → Firebird (chạy khi ENABLE_TWO_WAY_SYNC=true;
--   mặc định OFF — quy tắc vàng 6: Firebird = MASTER ở GĐ0–6).
-- W_CONFLICT: xung đột so TIMEMODIFIED 2 phía (§7.3) — người dùng chọn.
-- W_SYNC_WHITELIST: bảng web được phép ghi ngược (ADR-02 whitelist).
-- Idempotent.
-- ============================================================

CREATE TABLE IF NOT EXISTS W_OUTBOX (
  ID           CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL PRIMARY KEY,
  TABLE_NAME   VARCHAR(64)  NOT NULL,
  ROW_ID       CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  OP           VARCHAR(8)   NOT NULL,              -- INSERT | UPDATE
  PAYLOAD      JSON         NULL,                  -- cột ghi ngược (whitelist cột)
  STATUS       VARCHAR(16)  NOT NULL DEFAULT 'PENDING',  -- PENDING|RUNNING|DONE|FAILED|CONFLICT
  RETRY_COUNT  INT          NOT NULL DEFAULT 0,
  NEXT_RETRY_AT DATETIME(3) NULL,
  LAST_ERROR   TEXT NULL,
  CREATED_AT   DATETIME(3)  NOT NULL,
  SENT_AT      DATETIME(3)  NULL,
  KEY IX_OUTBOX (STATUS, NEXT_RETRY_AT)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS W_CONFLICT (
  ID          CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL PRIMARY KEY,
  TABLE_NAME  VARCHAR(64) NOT NULL,
  ROW_ID      CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  FB_MODIFIED DATETIME(3) NULL,           -- TIMEMODIFIED phía Firebird
  MY_MODIFIED DATETIME(3) NULL,           -- TIMEMODIFIED mirror
  DETAIL      TEXT NULL,                  -- payload 2 phía để người dùng chọn
  STATUS      VARCHAR(16) NOT NULL DEFAULT 'OPEN',  -- OPEN | FB_WINS | WEB_WINS
  DETECTED_AT DATETIME(3) NOT NULL,
  RESOLVED_AT DATETIME(3) NULL,
  KEY IX_WCONFLICT (STATUS, DETECTED_AT)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Whitelist bảng web được ghi ngược (KE-HOACH §7.1): chỉ DANH_MUC + ghi chú/ghi nợ.
-- GĐ7 bật cờ ENABLE_TWO_WAY_SYNC + INSERT whitelist thì worker mới chạy.
CREATE TABLE IF NOT EXISTS W_SYNC_OUTBOX_WHITELIST (
  TABLE_NAME  VARCHAR(64) NOT NULL PRIMARY KEY,
  WRITE_COLS  TEXT NULL,                  -- cột được ghi ngược (CSV) — NULL = tất cả
  IS_ENABLED  TINYINT(1) NOT NULL DEFAULT 1,
  NOTE        VARCHAR(255) NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
