-- ============================================================
-- 019_fix_web_collation.sql — Sửa lệch collation bảng web-side
-- W_EMPLOYEE_EMAIL / W_PAYMENT_TXN được tạo với utf8mb4_unicode_ci trong khi
-- database + bảng mirror dùng utf8mb4_0900_ai_ci ⇒ JOIN theo ID báo lỗi
-- "Illegal mix of collations (utf8mb4_unicode_ci) and (utf8mb4_0900_ai_ci)"
-- (GET /api/employees trả 500, danh mục nhân viên không hiển thị).
-- Idempotent: CONVERT TO cùng collation chạy lại an toàn.
-- ============================================================

ALTER TABLE W_EMPLOYEE_EMAIL CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
ALTER TABLE W_PAYMENT_TXN CONVERT TO CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;
