-- ============================================================
-- MIGRATION 014 — Phase 7 Email: seed template mặc định cho 19 events
-- (W_EMAIL_EVENT đã seed ở database/seed/001 nhưng chưa có W_EMAIL_TEMPLATE).
-- Idempotent (INSERT IGNORE theo CODE unique).
-- Variables: {{CUSTOMER_NAME}} {{ORDER_NUMBER}} {{ORDER_DATE}} {{TOTAL_AMOUNT}}
--            {{DEBT_AMOUNT}} {{DUE_DATE}} {{TRACKING_CODE}} (EMAIL-ARCHITECTURE §4)
-- ============================================================

INSERT IGNORE INTO W_EMAIL_TEMPLATE (ID, CODE, NAME, SUBJECT, BODY_HTML, BODY_TEXT, IS_ACTIVE, CREATED_AT) VALUES
(UUID(),'ORDER_CREATED','Tạo đơn hàng',N'TechPos — Đơn hàng {{ORDER_NUMBER}} đã tạo',
 '<p>Kính gửi {{CUSTOMER_NAME}},</p><p>Đơn hàng <b>{{ORDER_NUMBER}}</b> ngày {{ORDER_DATE}} đã được tạo. Tổng tiền: <b>{{TOTAL_AMOUNT}}</b>.</p><p>Cảm ơn quý khách!</p>',
 N'Don hang {{ORDER_NUMBER}} da tao — tong cong {{TOTAL_AMOUNT}}', 1, NOW(3)),
(UUID(),'ORDER_PAID','Thanh toán đơn hàng',N'Đã nhận thanh toán đơn {{ORDER_NUMBER}}',
 '<p>Kính gửi {{CUSTOMER_NAME}},</p><p>Đơn hàng <b>{{ORDER_NUMBER}}</b> đã được thanh toán {{TOTAL_AMOUNT}}.</p><p>Cảm ơn quý khách!</p>', NULL, 1, NOW(3)),
(UUID(),'ORDER_CANCELLED','Huỷ đơn hàng',N'Đơn hàng {{ORDER_NUMBER}} đã huỷ',
 '<p>Đơn hàng <b>{{ORDER_NUMBER}}</b> ngày {{ORDER_DATE}} đã bị huỷ.</p>', NULL, 1, NOW(3)),
(UUID(),'ORDER_RETURNED','Khách trả hàng',N'Khách trả lại đơn {{ORDER_NUMBER}}',
 '<p>Đơn <b>{{ORDER_NUMBER}}</b> ngày {{ORDER_DATE}} đã được trả lại.</p>', NULL, 1, NOW(3)),
(UUID(),'PAYMENT_CREATED','Phiếu thu/chi',N'Phiếu {{ORDER_NUMBER}} — {{TOTAL_AMOUNT}}',
 '<p>Phiếu thu/chi <b>{{ORDER_NUMBER}}</b> ngày {{ORDER_DATE}} — số tiền {{TOTAL_AMOUNT}}.</p>', NULL, 1, NOW(3)),
(UUID(),'PAYMENT_RECEIVED','Nhận thanh toán',N'Đã nhận thanh toán đơn {{ORDER_NUMBER}}',
 '<p>Đã nhận thanh toán cho đơn <b>{{ORDER_NUMBER}}</b>: {{TOTAL_AMOUNT}}.</p>', NULL, 1, NOW(3)),
(UUID(),'DEBT_CREATED','Phát sinh công nợ',N'Công nợ mới {{DEBT_AMOUNT}} — {{CUSTOMER_NAME}}',
 '<p>Khách hàng {{CUSTOMER_NAME}} phát sinh công nợ {{DEBT_AMOUNT}} từ đơn {{ORDER_NUMBER}}.</p>', NULL, 1, NOW(3)),
(UUID(),'DEBT_DUE','Nhắc nợ trước hạn',N'Nhắc nợ: đơn {{ORDER_NUMBER}} đến hạn {{DUE_DATE}}',
 '<p>Đơn <b>{{ORDER_NUMBER}}</b> sẽ đến hạn {{DUE_DATE}} — công nợ {{DEBT_AMOUNT}}.</p>', NULL, 1, NOW(3)),
(UUID(),'DEBT_OVERDUE','Nhắc nợ quá hạn',N'[QUÁ HẠN] Công nợ {{DEBT_AMOUNT}} — {{CUSTOMER_NAME}}',
 '<p style="color:#cf1322">Đơn {{ORDER_NUMBER}} quá hạn thanh toán (đến hạn {{DUE_DATE}}) — nợ {{DEBT_AMOUNT}}.</p>', NULL, 1, NOW(3)),
(UUID(),'COD_CREATED','Tạo đơn COD',N'Đơn COD {{ORDER_NUMBER}} đã tạo',
 '<p>Đơn gửi COD {{ORDER_NUMBER}} — {{TOTAL_AMOUNT}}.</p>', NULL, 1, NOW(3)),
(UUID(),'COD_DELIVERED','COD giao thành công',N'COD {{TRACKING_CODE}} giao thành công',
 '<p>Đơn COD {{TRACKING_CODE}} đã giao thành công.</p>', NULL, 1, NOW(3)),
(UUID(),'COD_FAILED','COD giao thất bại',N'COD {{TRACKING_CODE}} giao thất bại',
 '<p>Đơn COD {{TRACKING_CODE}} giao thất bại — cần kiểm tra.</p>', NULL, 1, NOW(3)),
(UUID(),'LOW_STOCK','Cảnh báo tồn kho thấp',N'[Kho] {{ITEM_NAME}} sắp hết ({{QUANTITY}})',
 '<p>Mặt hàng <b>{{MAT_HANG}}</b> tại kho {{KHO}} còn <b>{{TON}}</b> (tối thiểu {{TON_TOI_THIEU}}).</p>', NULL, 1, NOW(3)),
(UUID(),'NEW_CUSTOMER','Khách hàng mới',N'Khách hàng mới: {{CUSTOMER_NAME}}',
 '<p>Khách hàng mới: {{CUSTOMER_NAME}} — {{DIEN_THOAI}}.</p>', NULL, 1, NOW(3)),
(UUID(),'CUSTOMER_CARED','Chăm sóc khách hàng',N'Chăm sóc khách {{CUSTOMER_NAME}}',
 '<p>Chăm sóc KH {{CUSTOMER_NAME}}.</p>', NULL, 1, NOW(3)),
(UUID(),'SALARY_COMPLETED','Hoàn tất tính lương',N'Bảng lương {{PERIOD}} đã hoàn tất',
 '<p>Bảng lương {{PERIOD}} đã hoàn tất. Tổng thực nhận {{TOTAL_AMOUNT}}.</p>', NULL, 1, NOW(3)),
(UUID(),'SYNC_FAILED','Đồng bộ thất bại',N'[Hệ thống] Đồng bộ Firebird lỗi',
 '<p>Đồng bộ Firebird → MySQL gặp lỗi: {{ERROR}}</p>', NULL, 1, NOW(3)),
(UUID(),'SYNC_RECOVERY','Đồng bộ phục hồi',N'[OK] Đồng bộ đã phục hồi',
 '<p>Đồng bộ Firebird → MySQL đã hoạt động bình thường trở lại.</p>', NULL, 1, NOW(3)),
(UUID(),'SYSTEM_ERROR','Lỗi hệ thống',N'[Lỗi hệ thống] {{ERROR_TEXT}}',
 '<p>Hệ thống gặp lỗi: {{ERROR}}.</p>', NULL, 1, NOW(3));

-- SMTP settings mặc định (rỗng — Admin nhập qua UI; password env/DPAPI, không commit secret)
INSERT IGNORE INTO W_EMAIL_SETTINGS (KEY_, VALUE_, UPDATED_AT) VALUES
('HOST', '', NOW(3)),
('PORT', '587', NOW(3)),
('USE_SSL', '1', NOW(3)),
('USERNAME', '', NOW(3)),
('PASSWORD', '', NOW(3)),
('FROM_EMAIL', 'no-reply@localhost', NOW(3)),
('FROM_NAME', N'TechPos Web', NOW(3));
