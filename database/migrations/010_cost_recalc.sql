-- ============================================================
-- MIGRATION 010 — Phase 4.8: Tính lại giá vốn
-- Permission mới INVENTORY.COST_RECALC (desktop: menu KHO HÀNG ▸
-- Tính lại giá vốn dành cho Kế toán trưởng — KE-HOACH mục route map).
-- Kết quả tính lưu vào W_KHOLEDGER.GIA_VON (web table) + TDONHANGCHITIET.GIAVON
-- (chỉ dòng W_ORIGIN='WEB' — không đụng dữ liệu desktop, quy tắc vàng 1).
-- Idempotent.
-- ============================================================

INSERT IGNORE INTO W_PERMISSION (ID, CODE, NAME) VALUES
  ('22222222-0000-0000-0000-000000000056','INVENTORY.COST_RECALC','Tính lại giá vốn');

-- ADMIN & SUPER_ADMIN
INSERT IGNORE INTO W_ROLE_PERMISSION (ROLE_ID, PERMISSION_ID)
SELECT r.ID, p.ID FROM W_ROLE r CROSS JOIN W_PERMISSION p
WHERE r.CODE IN ('SUPER_ADMIN','ADMIN') AND p.CODE = 'INVENTORY.COST_RECALC';

-- MANAGER + ACCOUNTANT (kế toán)
INSERT IGNORE INTO W_ROLE_PERMISSION (ROLE_ID, PERMISSION_ID)
SELECT r.ID, p.ID FROM W_ROLE r CROSS JOIN W_PERMISSION p
WHERE r.CODE IN ('MANAGER','ACCOUNTANT') AND p.CODE = 'INVENTORY.COST_RECALC';