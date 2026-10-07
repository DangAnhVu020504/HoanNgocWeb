-- ============================================================
-- MIGRATION 005 — POS Bán lẻ (Phase 4, mục 4.1)
-- W_DOC_TYPE_MAP đã tạo ở 003 (LOAI, MODE_VAL, DOC_TYPE, PREFIX, VERIFIED).
-- Migration này chỉ thêm dòng BAN_LE với VERIFIED=0 (chưa xác minh production)
-- + seed sequence BL.
-- Idempotent.
-- ============================================================

-- Bán lẻ: LOAI chưa xác minh trên DB thật (demo chỉ có 1=Nhập, 5=Kiểm kê).
-- Chèn row quản lý số phiếu BL với VERIFIED=0. Giá trị LOAI=2 chỉ là placeholder
-- đánh dấu "chưa xác minh" — khi có DB production phải cập nhật theo thực tế.
INSERT IGNORE INTO W_DOC_TYPE_MAP (LOAI, MODE_VAL, DOC_TYPE, PREFIX, VERIFIED)
VALUES (2, -1, 'BAN_LE', 'BL', 0);

-- Sequence bán lẻ (BL26/00001) — web-only, không xung đột desktop
INSERT INTO W_DOC_SEQUENCE (PREFIX_, YEAR_, LAST_SEQ)
SELECT 'BL', YEAR(CURDATE()), 0
WHERE NOT EXISTS (SELECT 1 FROM W_DOC_SEQUENCE WHERE PREFIX_='BL' AND YEAR_=YEAR(CURDATE()));

-- Unique index (DMATHANGID, DKHOHANGID, W_DELETED) cho OTONKHO — cần cho upsert tồn kho web.
-- Nếu DB mirror đã có dòng trùng (dữ liệu demo) thì index tạo thất bại — dọn trùng trước.
-- (MigrationRunner chạy từng statement; lỗi duplicate key sẽ hiện trong log, không chặn các migration khác.)
ALTER TABLE OTONKHO ADD UNIQUE INDEX UX_OTONKHO_ITEM_KHO (DMATHANGID, DKHOHANGID, W_DELETED);
