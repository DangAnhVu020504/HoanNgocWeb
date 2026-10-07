-- ============================================================
-- MIGRATION 013 — Phase 6 Report Center: quyền in báo cáo/hoá đơn
--
-- Seed 001 đã có REPORT.VIEW (21) + REPORT.EXPORT (22) và SALES.INVOICE.PRINT (06).
-- Bổ sung REPORT.PRINT cho in phiếu thu/chi + báo cáo bản in (PDF);
-- map cho các vai trò nghiệp vụ. Idempotent.
-- ============================================================

INSERT IGNORE INTO W_PERMISSION (ID, CODE, NAME) VALUES
  ('22222222-0000-0000-0000-000000000069','REPORT.PRINT','In báo cáo / phiếu (PDF)');

-- ADMIN & SUPER_ADMIN toàn quyền
INSERT IGNORE INTO W_ROLE_PERMISSION (ROLE_ID, PERMISSION_ID)
SELECT r.ID, p.ID FROM W_ROLE r CROSS JOIN W_PERMISSION p
WHERE r.CODE IN ('SUPER_ADMIN','ADMIN') AND p.CODE = 'REPORT.PRINT';

-- MANAGER + ACCOUNTANT in báo cáo/phiếu
INSERT IGNORE INTO W_ROLE_PERMISSION (ROLE_ID, PERMISSION_ID)
SELECT r.ID, p.ID FROM W_ROLE r CROSS JOIN W_PERMISSION p
WHERE r.CODE IN ('MANAGER','ACCOUNTANT') AND p.CODE = 'REPORT.PRINT';

-- CASHIER in phiếu thu/chi tại quầy
INSERT IGNORE INTO W_ROLE_PERMISSION (ROLE_ID, PERMISSION_ID)
SELECT r.ID, p.ID FROM W_ROLE r CROSS JOIN W_PERMISSION p
WHERE r.CODE = 'CASHIER' AND p.CODE = 'REPORT.PRINT';
