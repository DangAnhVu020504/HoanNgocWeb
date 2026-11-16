-- ============================================================
-- 017_employee_email.sql — Email riêng từng nhân viên (Phase 8)
-- DNHANVIEN (Firebird) không có cột EMAIL → bảng web-side W_EMPLOYEE_EMAIL.
-- Idempotent: CREATE TABLE IF NOT EXISTS / INSERT IGNORE.
-- ============================================================

CREATE TABLE IF NOT EXISTS W_EMPLOYEE_EMAIL (
    DNHANVIENID CHAR(36) NOT NULL,
    EMAIL       VARCHAR(200) NOT NULL,
    UPDATED_AT  DATETIME(3) NOT NULL,
    UPDATED_BY  CHAR(36) NULL,
    PRIMARY KEY (DNHANVIENID),
    KEY IDX_W_EMPLOYEE_EMAIL_EMAIL (EMAIL)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Kế thừa email đã có sẵn trong SUSER (desktop) nếu nhân viên được liên kết.
INSERT IGNORE INTO W_EMPLOYEE_EMAIL (DNHANVIENID, EMAIL, UPDATED_AT, UPDATED_BY)
SELECT s.DNHANVIENID, MAX(TRIM(s.EMAIL)), NOW(3), NULL
FROM SUSER s
WHERE s.DNHANVIENID IS NOT NULL
  AND TRIM(COALESCE(s.EMAIL, '')) <> ''
GROUP BY s.DNHANVIENID;
