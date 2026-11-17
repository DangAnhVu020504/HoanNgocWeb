-- ============================================================
-- 020_employee_position.sql — Chức vụ nhân viên (web-side)
-- DNHANVIEN (Firebird) không có cột chức vụ ⇒ lưu ở bảng web-side
-- W_EMPLOYEE_POSITION, KHÔNG sửa Firebird/desktop (quy tắc vàng 1).
-- Idempotent: CREATE TABLE IF NOT EXISTS.
-- Collation utf8mb4_0900_ai_ci để JOIN được với DNHANVIEN.ID.
-- ============================================================

CREATE TABLE IF NOT EXISTS W_EMPLOYEE_POSITION (
    DNHANVIENID CHAR(36) NOT NULL,
    CHUCVU      VARCHAR(100) NOT NULL,
    UPDATED_AT  DATETIME(3) NOT NULL,
    UPDATED_BY  CHAR(36) NULL,
    PRIMARY KEY (DNHANVIENID),
    KEY IDX_W_EMPLOYEE_POSITION_CHUCVU (CHUCVU)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Backfill chức vụ mặc định cho nhân viên hiện có (chỉ khi chưa có — chạy lại an toàn).
INSERT IGNORE INTO W_EMPLOYEE_POSITION (DNHANVIENID, CHUCVU, UPDATED_AT, UPDATED_BY)
SELECT e.ID, 'Nhân viên', NOW(3), NULL
FROM DNHANVIEN e
WHERE e.W_DELETED = 0;
