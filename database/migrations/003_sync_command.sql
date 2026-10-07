-- ============================================================
-- 003_sync_command.sql — Kênh điều khiển Sync từ API (mục 37)
-- API ghi lệnh (RECONCILE_ALL / RETRY_ROW_ERRORS) → Sync Worker nhận + chạy.
-- Kèm W_DOC_TYPE_MAP: ánh xạ TDONHANG đa năng — chỉ seed giá trị ĐÃ xác minh
-- trên DB thật (Phase 0); các loại khác bổ sung sau khi sync production.
-- ============================================================

CREATE TABLE IF NOT EXISTS W_SYNC_COMMAND (
  ID          CHAR(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL PRIMARY KEY,
  COMMAND     VARCHAR(32)  NOT NULL,               -- RECONCILE_ALL | RETRY_ROW_ERRORS
  PARAM       VARCHAR(128) NULL,
  STATUS      VARCHAR(16)  NOT NULL DEFAULT 'PENDING', -- PENDING|RUNNING|DONE|FAILED
  RESULT      TEXT NULL,
  CREATED_BY  VARCHAR(64)  NULL,
  CREATED_AT  DATETIME(3)  NOT NULL,
  STARTED_AT  DATETIME(3)  NULL,
  FINISHED_AT DATETIME(3)  NULL,
  KEY IX_WSYNCCMD (STATUS, CREATED_AT)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS W_DOC_TYPE_MAP (
  LOAI        INT NOT NULL,
  MODE_VAL    INT NOT NULL DEFAULT -1,
  DOC_TYPE    VARCHAR(32) NOT NULL,               -- NHAP_KHO | KIEM_KE | ... (đã xác minh)
  PREFIX      VARCHAR(8)  NULL,                   -- PN | KTL | ... (tiền tố số phiếu)
  VERIFIED    TINYINT(1)  NOT NULL DEFAULT 0,     -- 1 = xác minh trên DB thật
  PRIMARY KEY (LOAI, MODE_VAL)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Chỉ insert giá trị đã xác minh bằng SQL trên DEMO.FDB (PHASE0-FINDINGS §2)
INSERT IGNORE INTO W_DOC_TYPE_MAP (LOAI, MODE_VAL, DOC_TYPE, PREFIX, VERIFIED) VALUES
  (1, -1, 'NHAP_KHO', 'PN', 1),
  (5, -1, 'KIEM_KE',  'KTL', 1);
