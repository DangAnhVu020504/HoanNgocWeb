# API STRUCTURE — TechPos.Api

## 1. Cấu trúc route (mục 35)

```text
/api/auth/          login, refresh, logout, me, change-password, forgot-password, reset-password, sessions
/api/dashboard/     summary, charts, alerts
/api/products/      CRUD + search + barcode + price-lists + groups
/api/customers/     CRUD + groups + loyalty + history + debt-summary
/api/suppliers/     CRUD + groups
/api/orders/        quotation, order, retail (bán lẻ), wholesale (bán sỉ), returns, delivery
/api/inventory/     warehouses, stock, ledger (thẻ kho), transfer, stocktake, revaluation, production
/api/purchases/     purchase-orders (đặt mua NCC), goods-receipt (nhập kho), returns-to-supplier
/api/payments/      cashbook (phieu thu/chi), reasons, cash-balance (tồn quỹ)
/api/debts/         customers, suppliers, tracking, remind-config
/api/employees/     CRUD, advances, rewards-penalties, timesheet, payroll
/api/crm/           care (chăm sóc KH), messages, feedback, loyalty
/api/reports/       catalog, run, export(pdf/excel/csv), print
/api/cod/           shipments, results
/api/notifications/ list, unread-count, mark-read, mark-all-read, delete
/api/emails/        templates, queue, history, settings, test-send
/api/admin/         users, roles, permissions, audit, settings, feature-flags, media, devtools
/api/sync/          status, cursors, errors, retry, tables, run-now (SUPER_ADMIN/ADMIN)
```

Quy tắc:
- Frontend **không** truy cập MySQL trực tiếp; chỉ gọi API.
- Không business logic trong controller → gọi Application service.
- Admin-only (`SUPER_ADMIN`/`ADMIN`/`IT`): raw SQL, sync config, system config, xóa dữ liệu.

## 2. Response contract (mục 36)

Mọi endpoint trả envelope thống nhất:

```json
{
  "success": true,
  "data": { },
  "message": null,
  "errors": [],
  "traceId": "b3c1..."
}
```

Lỗi validation:

```json
{
  "success": false,
  "data": null,
  "message": "Dữ liệu không hợp lệ",
  "errors": [ { "field": "quantity", "code": "GT_0", "message": "Số lượng phải > 0" } ],
  "traceId": "..."
}
```

Pagination:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "total": 100
}
```

- Pagination **server-side** mọi danh sách; search server-side có debounce client.
- `pageSize` max 200; sort whitelist theo cột cho phép (chống SQL injection, dùng parameter + whitelist).

## 3. Error handling

- Middleware `ExceptionHandlingMiddleware`: map exception → HTTP code + envelope; log Serilog kèm `traceId`;
  **không trả raw DB error** cho frontend.
- `ConflictException` → 409, `NotFoundException` → 404, `ValidationException` → 400, `ForbiddenException` → 403.
- Correlation ID: middleware gắn `traceId` vào response header `X-Trace-Id` + audit log.

## 4. Validation

- FluentValidation ở Application layer (DTO validators); Controller chỉ check `[RequirePermission]`.
- Không duplicate validation client/server cùng quy tắc: client validate UX, server là nguồn chân lý.

## 5. Convention

- REST tên số nhiều, verbs: GET/POST/PUT/DELETE; action đặc biệt dùng sub-resource:
  `POST /api/orders/retail/{id}/pay`, `POST /api/emails/queue/{id}/retry`.
- Version: `/api/v1/...` (prefix cấu hình); Swagger/OpenAPI bật ở tất cả môi trường, secure production.
- Idempotency: các lệnh tạo chứng từ nhận `Idempotency-Key` header (lưu `W_CHANGELOG`).