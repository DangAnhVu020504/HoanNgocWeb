-- 006: Thêm ENTRY_SEQ cho W_KHOLEDGER — thứ tự ghi để thẻ kho sắp xếp đúng khi cùng ENTRY_DATE
-- (số phiếu sắp theo alphabet không bảo đảm thứ tự thời gian: KTL < PN dù kiểm kê xảy ra sau nhập)
-- Idempotent: MySQL 8 không hỗ trợ ADD COLUMN IF NOT EXISTS ⇒ kiểm tra qua information_schema + dynamic SQL.

SET @col := (SELECT COUNT(*) FROM information_schema.COLUMNS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'W_KHOLEDGER' AND COLUMN_NAME = 'ENTRY_SEQ');
SET @ddl := IF(@col = 0,
  'ALTER TABLE W_KHOLEDGER ADD COLUMN ENTRY_SEQ BIGINT NOT NULL DEFAULT 0',
  'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;

-- Backfill: dòng cũ chưa có ENTRY_SEQ (>0) đánh số theo (ENTRY_DATE, W_SYNC_AT, ID)
SET @seq := 0;
UPDATE W_KHOLEDGER
SET ENTRY_SEQ = (@seq := @seq + 1)
WHERE ENTRY_SEQ = 0
ORDER BY ENTRY_DATE, W_SYNC_AT, ID;

SET @idx := (SELECT COUNT(*) FROM information_schema.STATISTICS
  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'W_KHOLEDGER' AND INDEX_NAME = 'IX_WLEDGER_SEQ');
SET @ddl := IF(@idx = 0,
  'CREATE INDEX IX_WLEDGER_SEQ ON W_KHOLEDGER (DMathangId, DKhoHangId, ENTRY_DATE, ENTRY_SEQ)',
  'SELECT 1');
PREPARE s FROM @ddl; EXECUTE s; DEALLOCATE PREPARE s;
