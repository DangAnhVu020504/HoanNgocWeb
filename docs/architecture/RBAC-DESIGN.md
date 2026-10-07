# RBAC + AUTHENTICATION DESIGN

## 1. Roles (mục 33)

```text
SUPER_ADMIN · ADMIN · MANAGER · SALES · CASHIER · WAREHOUSE · ACCOUNTANT · HR · VIEWER
```

- `SUPER_ADMIN`: toàn bộ + sync config + devtools + raw data.
- `ADMIN`: quản trị hệ thống, người dùng, email, audit (trừ sync raw).
- Còn lại theo module. `VIEWER` chỉ xem.

## 2. Permission model

```text
MODULE.RESOURCE.ACTION
```

| MODULE | Ví dụ RESOURCE | ACTION |
|---|---|---|
| SALES | INVOICE, QUOTATION, ORDER, CUSTOMER | VIEW, CREATE, UPDATE, DELETE, PAY, PRINT, EXPORT |
| INVENTORY | STOCK, TRANSFER, STOCKTAKE, RECEIPT | VIEW, CREATE, UPDATE, DELETE, APPROVE, PRINT |
| CASHBOOK | RECEIPT, PAYMENT | VIEW, CREATE, DELETE, PRINT |
| DEBT | CUSTOMER, SUPPLIER | VIEW, PAY, REMIND |
| HR | EMPLOYEE, PAYROLL, TIMESHEET | VIEW, CREATE, UPDATE, DELETE, APPROVE |
| REPORT | * | VIEW, EXPORT |
| ADMIN | USER, ROLE, SETTINGS, AUDIT | * |
| SYNC | * | VIEW, RUN, RETRY |

Ví dụ chuẩn: `SALES.INVOICE.VIEW / CREATE / UPDATE / DELETE / PAY / PRINT`.

### Scope

```text
ALL | STORE | WAREHOUSE
```
`W_USER_SCOPE(USER_ID, SCOPE_TYPE, SCOPE_ID)` — dữ liệu lọc theo cửa hàng/kho được gán.

## 3. Tables

```text
W_USER (ID, USERNAME UNIQUE, PASSWORD_HASH, FULL_NAME, EMAIL, IS_ACTIVE,
        SUSER_ID CHAR(36) NULL, -- map người dùng Firebird, không ghi ngược
        MUST_CHANGE_PASSWORD, LAST_LOGIN_AT, FAILED_LOGIN_COUNT, LOCKED_UNTIL, ...)
W_ROLE (ID, CODE UNIQUE, NAME)
W_PERMISSION (ID, CODE UNIQUE, NAME)
W_ROLE_PERMISSION (ROLE_ID, PERMISSION_ID)
W_USER_ROLE (USER_ID, ROLE_ID)
W_USER_SCOPE (USER_ID, SCOPE_ENUM, SCOPE_ID)
W_REFRESH_TOKEN (ID, USER_ID, TOKEN_HASH, EXPIRES_AT, REVOKED_AT, DEVICE_INFO, IP)
```

## 4. Auth flow

```text
Login (username+password) → verify PBKDF2/BCrypt hash → access JWT (15 phút, claims: sub, roles, permissions)
 → refresh token (7 ngày, rotate + revoke cũ) → W_USER_SESSION
Logout → revoke refresh + audit LOGOUT
Change password: verify cũ → hash mới → revoke tất cả refresh của user
Forgot/Reset: token 1 lần 30 phút gửi qua email queue
Lockout: 5 lần sai → khóa 15 phút (anti-bruteforce) + rate limit login API
```

- **Không ghi mật khẩu web về Firebird.** `SUSER` mirror chỉ để hiển thị/đối chiếu.
- JWT claims: `sub, name, roles[], perms[] (hoặc perms hash + lookup), scope[]`.
- Authorization handler: `[RequirePermission("SALES.INVOICE.CREATE")]` policy tự sinh.

## 5. Phân quyền UI

- Menu item hiển thị theo permission (menu 10 nhóm × 76 mục map 1-1 permission).
- Nút hành động (Lưu/Xóa/Thanh toán/In/Export) disable khi thiếu permission.
- Route guard + server-side double check (API luôn là nguồn chân lý).

## 6. Bảo mật liên quan (mục 53)

JWT + refresh rotate · password hash (BCrypt work factor 12) · RBAC server-side · audit mọi action ·
rate limit (login 5/phút, API 120/phút/user) · FluentValidation · parameterized SQL (Dapper) ·
XSS: React escape + CSP header · CSRF: JWT trong Authorization header (không cookie) ·
CORS whitelist · HTTPS + HSTS · Secure headers · Secrets qua env/DPAPI ·
Không log password/SMTP password · Không trả raw DB error.