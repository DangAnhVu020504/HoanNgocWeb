# EMAIL + NOTIFICATION ARCHITECTURE

## 1. Email flow (mục 20–24)

```text
Business Event (nghiệp vụ hoặc sync/system)
      ↓  (in-process event bus → KHÔNG gửi trực tiếp trong transaction)
W_EMAIL_EVENT (config bật/tắt theo event)
      ↓
Email Composer (render template theo event + reference data)
      ↓
W_EMAIL_QUEUE (PENDING) ──► Email Worker (BackgroundService)
      ↓  retry backoff: 1m → 5m → 15m → 30m (tối đa 4 lần)
SMTP Provider ──► W_EMAIL_LOG (mỗi lần thử 1 bản ghi)
```

- **Không bao giờ** fail transaction nghiệp vụ vì email: enqueue là fire-and-forget trong cùng
  transaction (ghi bảng queue), gửi xảy ra sau commit.
- Worker: .NET `BackgroundService` trong TechPos.Api, poll `PENDING AND NEXT_RETRY_AT <= now()`,
  claim row bằng `UPDATE ... WHERE STATUS='PENDING'` (atomic, an toàn khi multi-instance).
- Sau max retry → `FAILED` + notification "Email gửi thất bại".
- Admin: retry tay, cancel, xem payload/error.

## 2. Tables

```sql
W_EMAIL_TEMPLATE  (ID CHAR(36) PK, CODE UNIQUE, NAME, SUBJECT, BODY_HTML LONGTEXT,
                   BODY_TEXT LONGTEXT, IS_ACTIVE, CREATED_AT, UPDATED_AT)
W_EMAIL_EVENT     (ID, CODE UNIQUE, NAME, TEMPLATE_CODE, ENABLED, RECIPIENT_RULE JSON,
                   UPDATED_AT)  -- event-driven, admin bật/tắt
W_EMAIL_QUEUE     (ID, EVENT_TYPE, REFERENCE_TYPE, REFERENCE_ID, SUBJECT, BODY_HTML,
                   STATUS ENUM('PENDING','PROCESSING','SENT','FAILED','CANCELLED'),
                   RETRY_COUNT, NEXT_RETRY_AT, LAST_ERROR, CREATED_AT, SENT_AT)
W_EMAIL_RECIPIENT (ID, QUEUE_ID FK, TYPE ENUM('TO','CC','BCC'), EMAIL, NAME, STATUS, ERROR)
W_EMAIL_LOG       (ID, QUEUE_ID, EMAIL, SUBJECT, STATUS, SMTP_RESPONSE, ERROR,
                   RETRY_COUNT, SENT_AT)
W_EMAIL_SETTINGS  (KEY, VALUE)  -- host/port/ssl/user/pass lưu từ Admin UI (password mã hoá DPAPI)
```

SMTP config KHÔNG hard-code, KHÔNG commit secret: ưu tiên environment variables /
`appsettings.Production.json` ngoài Git / Windows Credential; Admin UI có `[Test Email]`.

## 3. Email Events mặc định (mục 21) — configurable

```text
ORDER_CREATED · ORDER_PAID · ORDER_CANCELLED · ORDER_RETURNED
PAYMENT_CREATED · PAYMENT_RECEIVED
DEBT_CREATED · DEBT_DUE · DEBT_OVERDUE
COD_CREATED · COD_DELIVERED · COD_FAILED
LOW_STOCK · NEW_CUSTOMER · CUSTOMER_CARED · SALARY_COMPLETED
SYNC_FAILED · SYNC_RECOVERY · SYSTEM_ERROR
```

`RECIPIENT_RULE` JSON ví dụ (LOW_STOCK): chọn theo vai trò + kho:
```json
{ "roles": ["MANAGER","WAREHOUSE"], "scope": { "WAREHOUSEID": "{KHO_ID}" }, "fixed": [] }
```
Nhận được phép: Khách hàng · Nhân viên · Quản lý · Kế toán · Thủ kho · Admin · Email cố định —
theo cửa hàng / kho / vai trò / nhân viên / khách hàng.

## 4. Templates + variables (mục 48)

Variables: `{{CUSTOMER_NAME}} {{ORDER_NUMBER}} {{ORDER_DATE}} {{TOTAL_AMOUNT}}
{{DEBT_AMOUNT}} {{DUE_DATE}} {{TRACKING_CODE}} …`
- Template KHÔNG truy cập DB trực tiếp — chỉ nhận dictionary biến do Composer cung cấp.
- Admin UI: list + editor (Subject, HTML body, Text body, variables, Preview, Send test).
- Sanitize HTML (chống XSS khi preview); escape biến giá trị.

## 5. Notification Center (mục 25/47)

```text
Business/System event → Notification Engine → W_NOTIFICATION (+ W_NOTIFICATION_READ)
                        └→ SignalR hub `NotificationHub` → realtime 🔔 badge
```

- Nhóm filter: All · Sales · Inventory · Debt · COD · System · Email.
- Item: icon, title, description, time, read/unread, action (link deep sang màn hình liên quan).
- API: list (paged), unread-count (poll 30s + SignalR), mark-read, mark-all-read, delete.
- Lưu vết: notification không sửa được nội dung, chỉ trạng thái read.

## 6. Không làm vỡ nghiệp vụ (mục 23)

- Email/notification worker chạy độc lập; lỗi SMTP chỉ đánh dấu FAILED + retry.
- Ví dụ đảm bảo: `Tồn kho dưới mức tối thiểu → LOW_STOCK → tìm Manager + Thủ kho theo WAREHOUSEID
  → Email Queue → SMTP` — nếu SMTP chết, tồn kho/cửa hàng vẫn bán bình thường.