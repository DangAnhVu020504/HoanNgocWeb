-- ============================================================
-- MIGRATION 011 — Phase 5.1 Quỹ: Phiếu thu / Phiếu chi (TTHUCHI)
--
-- TTHUCHI.LOAI đã xác minh trên DEMO (PHASE0-FINDINGS §4): 0=thu, 1=chi.
-- TTHUCHI.LOAIDOITUONG (0/1 = phân loại đối tượng) CHƯA xác minh semantics
-- ⇒ dùng lại W_DOC_TYPE_MAP: LOAI giữ giá trị TTHUCHI.LOAI, MODE_VAL giữ
-- LOAIDOITUONG, 4 tổ hợp THU/CHI × KH/NCC, VERIFIED=0 (quy tắc vàng 5).
-- API chặn ghi cho tới khi xác minh production rồi UPDATE ... SET VERIFIED=1.
-- Tiền tố số phiếu desktop: PT (phiếu thu), PC (phiếu chi).
-- Idempotent.
-- ============================================================

INSERT IGNORE INTO W_DOC_TYPE_MAP (LOAI, MODE_VAL, DOC_TYPE, PREFIX, VERIFIED) VALUES
  (0, 0, 'PHIEU_THU_KH',  'PT', 0),  -- thu tiền khách hàng
  (0, 1, 'PHIEU_THU_NCC', 'PT', 0),  -- thu từ nhà cung cấp
  (1, 0, 'PHIEU_CHI_KH',  'PC', 0),  -- chi cho khách hàng (hoàn tiền)
  (1, 1, 'PHIEU_CHI_NCC', 'PC', 0);  -- chi trả nhà cung cấp

-- Sequence số phiếu web (PTyy/xxxxx, PCyy/xxxxx)
INSERT INTO W_DOC_SEQUENCE (PREFIX_, YEAR_, LAST_SEQ)
SELECT p.PREFIX, YEAR(CURDATE()), 0
FROM (SELECT 'PT' AS PREFIX UNION ALL SELECT 'PC') p
WHERE NOT EXISTS (
  SELECT 1 FROM W_DOC_SEQUENCE s WHERE s.PREFIX_ = p.PREFIX AND s.YEAR_ = YEAR(CURDATE())
);

-- Permission xem phiếu chi (đối xứng CASHBOOK.RECEIPT.VIEW đã có từ seed 001)
INSERT IGNORE INTO W_PERMISSION (ID, CODE, NAME) VALUES
  ('22222222-0000-0000-0000-000000000057','CASHBOOK.PAYMENT.VIEW','Xem phiếu chi');

-- ADMIN & SUPER_ADMIN
INSERT IGNORE INTO W_ROLE_PERMISSION (ROLE_ID, PERMISSION_ID)
SELECT r.ID, p.ID FROM W_ROLE r CROSS JOIN W_PERMISSION p
WHERE r.CODE IN ('SUPER_ADMIN','ADMIN') AND p.CODE = 'CASHBOOK.PAYMENT.VIEW';

-- MANAGER (001 map theo LIKE 'CASHBOOK.%' chỉ áp dụng lúc seed — map tay)
INSERT IGNORE INTO W_ROLE_PERMISSION (ROLE_ID, PERMISSION_ID)
SELECT r.ID, p.ID FROM W_ROLE r CROSS JOIN W_PERMISSION p
WHERE r.CODE = 'MANAGER' AND p.CODE = 'CASHBOOK.PAYMENT.VIEW';

-- CASHIER + ACCOUNTANT xem phiếu chi
INSERT IGNORE INTO W_ROLE_PERMISSION (ROLE_ID, PERMISSION_ID)
SELECT r.ID, p.ID FROM W_ROLE r CROSS JOIN W_PERMISSION p
WHERE r.CODE IN ('CASHIER','ACCOUNTANT') AND p.CODE = 'CASHBOOK.PAYMENT.VIEW';