-- ============================================================
-- 004_media_column.sql — W_MEDIA: thêm COLUMN_NAME (mục 32)
-- 1 hàng W_MEDIA = 1 cột BLOB của 1 row nguồn (VD: DMATHANG.ANH).
-- Idempotent: dùng INFORMATION_SCHEMA để tránh "Duplicate column" khi
-- W_MEDIA đã có sẵn cột này từ lần apply trước / tay ngoài.
-- ============================================================

SET @col_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_SCHEMA = DATABASE()
     AND TABLE_NAME   = 'W_MEDIA'
     AND COLUMN_NAME  = 'COLUMN_NAME'
);
SET @sql := IF(@col_exists = 0,
  'ALTER TABLE W_MEDIA ADD COLUMN COLUMN_NAME VARCHAR(64) NULL AFTER REFERENCE_ID',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @idx_exists := (
  SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
   WHERE TABLE_SCHEMA = DATABASE()
     AND TABLE_NAME   = 'W_MEDIA'
     AND INDEX_NAME   = 'UQ_WMEDIA'
);
SET @sql := IF(@idx_exists = 0,
  'ALTER TABLE W_MEDIA ADD UNIQUE KEY UQ_WMEDIA (REFERENCE_TYPE, COLUMN_NAME, REFERENCE_ID)',
  'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
