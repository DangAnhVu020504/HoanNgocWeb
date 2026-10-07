# BÁO CÁO TỔNG HỢP DỰ ÁN — TechPosWeb (HoanNgocWeb)

> **Ngày lập:** 07/10/2026
> **Phạm vi:** Tổng hợp toàn bộ thông tin dự án — chức năng, luồng hoạt động, phụ thuộc, logic, database, bảo mật, triển khai.
> **Nguồn:** Khảo sát trực tiếp mã nguồn `backend/`, `frontend/`, `database/`, `deploy/`, `docs/` và các tài liệu gốc trong repo.

---

## MỤC LỤC

1. [Tổng quan dự án](#1-tổng-quan-dự-án)
2. [Mục tiêu & phạm vi nghiệp vụ](#2-mục-tiêu--phạm-vi-nghiệp-vụ)
3. [Kiến trúc tổng thể](#3-kiến-trúc-tổng-thể)
4. [Backend .NET 10 (9 project + 4 tools + tests)](#4-backend-net-10)
5. [Frontend React (cấu trúc + 28 route)](#5-frontend-react)
6. [Cơ sở dữ liệu (Firebird + MySQL mirror + migrations)](#6-cơ-sở-dữ-liệu)
7. [Luồng hoạt động end-to-end](#7-luồng-hoạt-động-end-to-end)
8. [Danh mục chức năng đầy đủ (API ↔ UI)](#8-danh-mục-chức-năng)
9. [Logic nghiệp vụ quan trọng](#9-logic-nghiệp-vụ-quan-trọng)
10. [Bảo mật & phân quyền RBAC](#10-bảo-mật--phân-quyền)
11. [Email & Notification](#11-email--notification)
12. [Triển khai & vận hành](#12-triển-khai--vận-hành)
13. [Phụ thuộc (NuGet + npm)](#13-phụ-thuộc)
14. [Kiểm thử & chất lượng](#14-kiểm-thử--chất-lượng)
15. [Trạng thái dự án & việc còn lại](#15-trạng-thái-dự-án--việc-còn-lại)
16. [Quy tắc vàng & rủi ro](#16-quy-tắc-vàng--rủi-ro)
17. [Cấu trúc thư mục repo](#17-cấu-trúc-thư-mục-repo)

---

## 1. TỔNG QUAN DỰ ÁN

| Hạng mục | Nội dung |
|---|---|
| **Tên dự án** | TechPosWeb — Web Application cho TechPos SM4 v4.5 |
| **Khách thể** | Công ty TNHH Ngọc Châu Âu — cửa hàng vàng bạc & quà tặng (455 Sư Vạn Hạnh, P.12, Q.10, TP.HCM) |
| **Mục đích** | Web ERP quản lý bán hàng / kho / quỹ / công nợ / nhân sự / báo cáo — **thay thế & song song** với desktop TechPos SM4 v4.5 (Firebird 2.5 Embedded) **không sửa mã nguồn desktop** |
| **Quy mô chức năng gốc** | 10 nhóm menu · 76 mục menu · ~41 màn hình nghiệp vụ (từ 51 ảnh chụp desktop 16/09/2026) |
| **Trạng thái** | Phase 0–7 + 9 hoàn tất (04/10/2026); còn GĐ7 bật 2 chiều + GĐ8 cắt chuyển |
| **Giải pháp** | Đọc Firebird READ-ONLY → đồng bộ sang MySQL 8 → Web API + SPA đa thiết bị |

### Chuỗi công nghệ

```text
Firebird 2.5 Embedded (DEMO.FDB — DB phần mềm cũ, READ-ONLY)
        ↓  Watermark + Reconcile sync (Windows Service, x86)
MySQL 8 (86 bảng mirror + 36 bảng W_* web-only)
        ↓
ASP.NET Core Web API (.NET 10, JWT, RBAC, Audit, Email, Notification, SignalR)
        ↓
React SPA (Vite + TypeScript 5.9 + Ant Design 6 + Zustand + Axios)
```

**Nguyên tắc cốt lõi:** Web **không đụng vào** Firebird/Desktop ở giai đoạn đầu. Firebird = MASTER, MySQL = MIRROR. Dữ liệu ghi từ web đi vào mirror MySQL với nhãn `W_ORIGIN='WEB'`; chỉ khi bật 2 chiều (GĐ7) mới ghi ngược Firebird có kiểm soát qua outbox whitelist.

---

## 2. MỤC TIÊU & PHẠM VI NGHIỆP VỤ

### 2.1 Mục tiêu đo được (kế hoạch v1.0)

| # | Mục tiêu | Chỉ số |
|---|---|---|
| M1 | Web hoá đủ chức năng | 76/76 mục menu |
| M2 | Số liệu khớp tuyệt đối | 0 đồng / 0 dòng lệch FB ↔ MySQL |
| M3 | Độ trễ giao dịch | ≤ 30 giây |
| M4 | RPO | ≤ 15 phút |
| M5 | Bổ sung chức năng thiếu | 12 mục desktop thiếu bảng |
| M6 | Bảo mật | JWT + RBAC + Audit log |
| M7 | Đa thiết bị | Desktop / tablet / mobile responsive |

### 2.2 Miền nghiệp vụ

- Bán hàng – kho – quỹ – công nợ – nhân sự – báo cáo cho cửa hàng vàng bạc & quà tặng.
- **Đa giá & đa tiền tệ:** giá bán lẻ/sỉ VND, giá lẻ/sỉ USD, giá nhập, giá vốn, tồn tối thiểu, ĐVT + ĐVT chẵn; in mã vạch.
- **Đa kho – đa cửa hàng:** chứng từ kho có Kho xuất/nhập, Cửa hàng, Thanh toán, Tài khoản ngân hàng, Đã xuất hóa đơn.
- **COD:** tích hợp VIETTEL/VNPT + giao hàng theo xe.
- **Hạng khách:** Khách lẻ / TV Bạc / TV Đồng / TV Vàng; số ngày cho nợ.
- **Xóa mềm + lưu vết:** Thùng rác mọi danh mục → web: `W_DELETED` + `W_AUDIT_LOG`.

### 2.3 Đối chiếu chức năng desktop ↔ trạng thái web (10 nhóm / 76 mục)

| Nhóm | Số mục | Trạng thái web |
|---|---|---|
| 1. HỆ THỐNG | 6 | DB backup/restore thuộc desktop; web có Auth + Sync Admin |
| 2. BÁN HÀNG | 16 | ✅ bán lẻ/sỉ, trả lại, báo giá, đặt hàng, hợp đồng, COD, giao xe, thống kê |
| 3. CHĂM SÓC KH | 4 | ⚠️ Chưa làm |
| 4. KHO HÀNG | 12 | ✅ nhập/xuất/chuyển/kiểm kê/trả NCC/tồn kho/thẻ kho/tồn tất cả kho/giá vốn/đặt mua |
| 5. QUỸ | 8 | ✅ phiếu thu/chi, lý do thu chi, tồn quỹ |
| 6. CÔNG NỢ | 3 | ✅ công nợ KH/NCC + nhắc nợ tự động |
| 7. NHÂN SỰ | 6 | ✅ NV, giấy giới thiệu, tạm ứng, thưởng phạt, chấm công, tính lương |
| 8. QUẢN TRỊ | 9 | ⚠️ một phần: audit log, user, sync admin; khóa sổ/xóa dữ liệu chưa làm |
| 9. BÁO CÁO | 9 nhóm | ✅ Report Center 10 báo cáo + export |
| 10. TRỢ GIÚP | 3 | ❌ ngoài DB |

> Đối chiếu CSDL: **64/76 mục (~84%) có bảng hỗ trợ**; thiếu 18 hạng mục (phiếu sản xuất, sổ kho, bảng giá, log gửi tin, TRỢ GIÚP…) — chi tiết `DOI-CHIEU-CSDL-VA-CHUC-NANG.md`.

---

## 3. KIẾN TRÚC TỔNG THỂ

### 3.1 Sơ đồ triển khai

```text
┌───────────────────────────── Máy chủ shop (Windows) ─────────────────────────────┐
│                                                                                   │
│  Desktop TechPos SM4 v4.5 ──► Firebird 2.5 Embedded (DEMO.FDB)  [KHÔNG ĐỘNG VÀO] │
│                                        │ (bản copy .tmp-fb, READ-ONLY)            │
│                                        ▼                                          │
│  TechPosSync (Windows Service, x86)  ← fbembed.dll 32-bit + ICU 3.0               │
│   • WatermarkSyncEngine (W1) 30s/120s/600s  • ReconcileEngine (W2) 02:00          │
│   • MediaExtractor 02:30  • Retry row-error  • TwoWaySyncEngine (GĐ7, OFF)        │
│                                        ▼                                          │
│  MySQL 8 (techposweb, 3306 bind 127.0.0.1)                                        │
│   • 86 bảng mirror + 36 bảng W_* (auth, audit, sync, email, notify, outbox…)      │
│                                        ▼                                          │
│  TechPosApi (Windows Service, x64, cổng 5005)                                     │
│   • JWT · RBAC · Audit · EmailWorker · ScheduledJobsWorker                        │
│   • SignalR /hubs/notifications · serve SPA                                       │
│                                        ▼                                          │
│  React SPA (frontend/techpos-web/dist — cùng cổng 5005)                           │
│   + SMTP outbound + mysqldump backup 23:30                                        │
└───────────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Layer backend (quy tắc bắt buộc)

```text
Controller (HTTP) → Service (business logic) → Repository (Dapper, MySQL) → Mirror tables
      │                    │
  DTO (Request/Response)   Validator (FluentValidation)
```

- Controller **không gọi Repository trực tiếp**; không trả Entity ra ngoài (luôn DTO).
- Phép tính tiền/giá vốn tách **pure-function** để unit test (`PosCalculator`, `CostCalculator`).
- Repository là `sealed class` dùng **Dapper** (không ORM, không interface).
- Envelope chuẩn mọi response: `{ success, data, message, errors[], traceId }`.

---

## 4. BACKEND .NET 10

**Solution:** `backend/TechPosWeb.slnx` — 9 project src + 1 test + 4 tools, toàn bộ `net10.0`. Data access thuần **Dapper** (không EF Core).

### 4.1 Ma trận phụ thuộc project

```text
TechPos.Shared (ApiResult, Options, 10 file DTO)            [cốt lõi dùng chung]
 ├── TechPos.Domain (rỗng — nợ kỹ thuật)
 ├── TechPos.Data.Firebird (FbClient READ-ONLY, x86)
 ├── TechPos.Reporting (QuestPDF · ClosedXML · ZXing)
 ├── TechPos.Data.MySql (Dapper + MySqlConnector) ── TechPos.Application (services)
 └── TechPos.Sync.Core (engine sync) ── TechPos.Sync.Service (Worker, x86)
TechPos.Api → Application + Data.MySql + Reporting + Shared + Sync.Core
TechPos.UnitTests → Application, Reporting, Shared, Sync.Core
```

### 4.2 Chi tiết từng project

#### TechPos.Shared
- `ApiResult<T>`: `success/data/message/errors/traceId` + `PageResult<T>`; exception domain `NotFoundException/ConflictException/ForbiddenException/ValidationException`.
- `Options.cs`: `DatabaseOptions`, `JwtOptions`, `FeatureFlagOptions`, `SyncTables` (TRANSACTION 30s · DICTIONARY 120s · SMALL 600s · LEDGER 300s).
- 10 file DTO: Cashbook, Catalog, Email, History, Hr, Inventory, Pos, Product, Report, SalesDoc.

#### TechPos.Domain
- **Rỗng** (chỉ csproj) — entity/validator nằm trong Shared + Application. Nợ kỹ thuật.

#### TechPos.Data.Firebird (READ-ONLY)
- `FirebirdConnectionFactory`: `ServerType=Embedded, Dialect=3, UTF8`; `ClientLibrary` trỏ `fbembed.dll` + ICU 3.0 của app cũ ⇒ **bắt buộc build x86**.
- `FirebirdSchemaDiscovery`: discovery từ `RDB$RELATIONS / RDB$RELATION_FIELDS / RDB$INDICES / RDB$TRIGGERS / RDB$GENERATORS` — nguồn chân lý cho DDL mirror (cấm sinh DDL từ tài liệu).

#### TechPos.Data.MySql (33 file, ~5.400 dòng)
- `MySqlConnectionFactory` — connection string từ config/env, không hard-code secret.
- `MigrationRunner` — chạy idempotent các `.sql` theo tên, ghi `W_MIGRATION`; API + Sync Service tự chạy lúc startup.
- `AuditLogger` — ghi `W_AUDIT_LOG` (old/new JSON, IP, UA, correlationId).
- **28 repository Dapper**: User, Product, Customer, Supplier, Warehouse, PosSale, Wholesale, CustomerReturn, Inventory (OTONKHO + W_KHOLEDGER), Receipt, StockIssue, StockTransfer, Stocktake, SupplierReturn, CostRecalc, Cashbook, Debt, Hr (571 dòng — lớn nhất), CustomerDoc, PurchaseOrder, Contract, Cod, VehicleDelivery, Report, Email, Notification, History, Lookup, DocRepositoryHelpers, DocTypeResolver.
- **Quy ước ghi web:** mirror table + `W_ORIGIN='WEB'` + `W_SYNC_AT` + soft-delete `W_DELETED` + audit.

#### TechPos.Application (~20 service + 23 validator FluentValidation)

| Nhóm | Service | Logic chính |
|---|---|---|
| Auth | `AuthService`, `JwtTokenService` | BCrypt; khóa tài khoản sau 5 lần sai (15'); JWT HS256 claim `perms`; refresh token SHA256 hash lưu DB |
| Danh mục | `CatalogService`, `ProductService`, `CatalogWriteGuard` | CRUD mirror; chặn ghi khi `ENABLE_CATALOG_WRITE=false` |
| POS | `PosSaleService`, `WholesaleService`, `CustomerReturnService`, `PosCalculator` | Decimal, round away-from-zero, tiền thừa/công nợ |
| Kho | `InventoryService`, `CostCalculator`, `CostRecalcService` | Nhập/xuất/chuyển/kiểm kê/trả NCC; giá vốn FIFO / BQ liên hoàn / BQ cuối kỳ |
| Quỹ/Công nợ | `CashbookService`, `DebtService` | Phiếu thu PT/chi PC + phân bổ trả nợ; công nợ KH (SUM CONNO) / NCC (SUM CONLAI) |
| Nhân sự | `HrService` | NV, chấm công (chặn trùng), thưởng phạt, tạm ứng, bảng lương, giấy giới thiệu |
| Chứng từ BH | `CustomerDocService`, `PurchaseOrderService`, `ContractService`, `DeliveryService` | Báo giá/đặt hàng/đặt mua/hợp đồng/COD/giao xe — bảng riêng |
| Báo cáo | `ReportService`, `CatalogImportService` | Orchestrator + import Excel danh mục |
| Email/Notify | `EmailService` (IEmailSender + SmtpEmailSender), `NotificationService` | Queue fire-and-forget, template render escape HTML |

#### TechPos.Sync.Core (6 file — engine sync)

| Engine | Chức năng |
|---|---|
| `WatermarkSyncEngine` (W1) | Pull FB theo watermark `(TIMEMODIFIED, ID)`; 3 chế độ initial/paginate-ID/incremental; 1 row lỗi không chết batch (ghi `W_SYNC_ROW_ERROR`); cursor chỉ advance khi batch 0 lỗi |
| `ReconcileEngine` (W2) | Đối chiếu COUNT/ID/`W_ROW_HASH` FB↔MySQL; thiếu→upsert, hash khác→resync, dư→soft-delete + `W_SYNC_TOMBSTONE`; không đụng cursor |
| `MirrorWriter` | Dò cột 2 phía, `ComputeHash` MD5 ổn định thứ tự key, upsert so-hash-trước-khi-ghi |
| `SyncMetaRepository` | W_SYNC_CURSOR/LOG/ROW_ERROR/HEARTBEAT/TABLE_POLICY/COMMAND/TOMBSTONE; command queue API→Service qua MySQL |
| `MediaExtractor` | Trích BLOB 12 cột (DMATHANG.ANH, SIMAGE.IMAGE, SREPORT.SQL…) → `media-store/` + `W_MEDIA`; chạy 02:30 |
| `OutboxRepository` | 2 chiều W_OUTBOX (claim atomic PENDING→RUNNING), whitelist, conflict W_CONFLICT |

- Retry row lỗi: backoff **1/5/15/30 phút**, quá 4 lần → **DEAD_LETTER**.

#### TechPos.Sync.Service (Windows Worker Service, x86)

- `Worker` vòng lặp 5s: W1 theo policy → nhận lệnh `W_SYNC_COMMAND` → retry row lỗi (50/lượt) → reconcile 02:00 giờ VN → media 02:30 → outbox 2 chiều → heartbeat. Lỗi không bao giờ chết worker.
- `TwoWaySyncEngine` (GĐ7): ghi ngược FB **chỉ cột whitelist**; xung đột so `TIMEMODIFIED` (FB mới hơn ⇒ giữ FB + conflict OPEN); **no-op khi `ENABLE_TWO_WAY_SYNC=false`**.
- `SyncPolicySeeder`: seed policy INSERT IGNORE (TRANSACTION 30s / DICTIONARY 120s, batch 2000).

#### TechPos.Reporting (Windows-only)

| Builder | Chức năng |
|---|---|
| `InvoicePdfBuilder` | Hoá đơn bán lẻ/sỉ A5 (QuestPDF, font Lato) |
| `CashVoucherPdfBuilder` | Phiếu thu/chi A5 |
| `StockVoucherPdfBuilder` | Phiếu nhập/xuất/chuyển/kiểm kê/trả NCC A5 |
| `PayrollPdfBuilder` | Bảng lương A4 |
| `ExcelExporter` | ClosedXML — header bold, freeze, tiền `#,##0.####`, ngày dd/MM/yyyy |
| `BarcodeGenerator` | ZXing CODE_128 → PNG |

#### TechPos.Api (host chính)

**Program.cs (214 dòng):**
- Serilog console + file `logs/api-.log` rolling ngày.
- DI: ~30 repository (Singleton) + ~20 service (Scoped); 2 HostedService (**EmailWorker** 20s, **ScheduledJobsWorker** 30 phút); `AddSignalR()`; MigrationRunner on startup (fail không dừng app).
- Auth: JWT Bearer; **SignalR đọc token từ query** `?access_token=` cho `/hubs`.
- Pipeline: ExceptionHandlingMiddleware → SerilogRequestLogging → UseAuthentication → UseAuthorization → MapControllers + serve SPA `frontend/techpos-web/dist` (1 cổng duy nhất) + `MapHub<NotificationHub>("/hubs/notifications")`.
- `RequirePermissionAttribute` check claim `perms`; SUPER_ADMIN pass hết.
- `ExceptionHandlingMiddleware` → envelope chuẩn + `X-Trace-Id`, không lộ raw DB error.

**appsettings.json (section chính, secret qua env):**

| Section | Nội dung |
|---|---|
| `Database` | MySqlConnectionString, FirebirdDatabasePath (DEMO.FDB), FirebirdClientLibrary |
| `Jwt` | Issuer/Audience, SecretKey qua env `TECHPOS_JWT_SECRET`, AccessToken 15 phút, Refresh 7 ngày |
| `FeatureFlags` | EnableWebTransaction / EnableTwoWaySync / EnableCatalogWrite = **false**; EnableEmail / EnableSignalR = true |

### 4.3 Controllers — 26 class, 34 route prefix, **136 HTTP endpoint**

| Nhóm | Route | Số EP | Nội dung |
|---|---|---|---|
| Auth | `api/auth` | 3 | login / refresh (501) / me |
| Sản phẩm | `api/products` | 9 | list/detail/barcode/groups/stock + CRUD + import Excel |
| Danh mục | `api/customers`, `api/suppliers`, `api/warehouses` | 18 | CRUD + groups + import |
| POS | `api/pos` | 4 | quote / sales / wholesale / returns |
| Kho | `api/inventory`, `api/receipts`, stocktakes, issues, transfers, supplier-returns, cost-recalc | 9 | tồn kho, thẻ kho, tồn tất cả kho, giá vốn, 5 loại phiếu kho |
| Đặt hàng/Giao | quotations, orders, purchase-orders, contracts, cod, vehicle-deliveries | 10 | bảng riêng TBAOGIA/TDATHANG/… |
| Quỹ/Công nợ | `api/cashbook`, `api/debts` | 7 | thu/chi 4 tổ hợp, phân bổ trả nợ, tồn quỹ, công nợ |
| Nhân sự | employees, attendance, bonus-penalties, payroll (+PDF), intro-letters | 20 | CRUD + chấm công + tính lương |
| Báo cáo | `api/reports` + barcode | 27 | **10 báo cáo JSON + 13 export Excel + 3 PDF + barcode** |
| Sync | `api/sync` (SUPER_ADMIN/ADMIN/IT) | 12 | status/reconcile/retry/row-errors/outbox/conflicts+resolve |
| Email/Notify | `api/notifications`, `api/emails` | 14 | notification + email admin |
| Khác | dashboard / history / health | 3 | summary, lịch sử chứng từ web, health |

### 4.4 Tools (4 console tool)

| Tool | Mục đích |
|---|---|
| `TechPos.FirebirdExtractor` (x86) | Phase 0 discovery: đọc RDB$ → 86 JSON (`docs/database/discovery/tables/`) + SCHEMA_DISCOVERY.md; mode `--sql` chạy query READ-ONLY |
| `TechPos.SchemaGenerator` | JSON discovery → MySQL mirror DDL (tên UPPERCASE, ID CHAR(36), +6 cột W_*, index watermark, không FK) → migration 002 (2.894 dòng, cấm sửa tay) |
| `TechPos.AdminSeed` | Tạo user web đầu tiên (BCrypt cost 12) gán role vào W_USER/W_USER_ROLE |
| `TechPos.DataCompare` (x86) | **Parity check** COUNT từng bảng chung FB↔MySQL; exit code = số bảng lệch (mục tiêu 0) |

### 4.5 Unit tests — TechPos.UnitTests (xUnit + FluentAssertions)

**14 file / 104 test method**, toàn bộ thuần không DB:

- `PosCalculatorTests` (8) · `CostCalculatorTests` (11 — FIFO/BQ liên hoàn/BQ cuối kỳ)
- `CashVoucherValidatorTests` (9) · `DocsValidatorTests` (11) · `SalesDocValidatorTests` (11)
- `HrValidatorTests` (16) · `CatalogCrudTests` (14) · `EmailTests` (5 — anti-XSS + SMTP fail-safe)
- `MirrorWriterHashTests` (3) · `PdfBuilderTests` (3) · `ReportingTests` (3) · `PagingTests` (3) · `ApiResultTests` (4) · `CatalogServiceTests` (3)

---

## 5. FRONTEND REACT

**Stack:** React 19 + Vite 7 + TypeScript 5.9 (strict) + Ant Design 6 + Axios + Zustand 5 + dayjs. AG Grid được cài nhưng **chưa import** (đang dùng AntD Table toàn UI). Dev server cổng **5173**, proxy `/api` → `http://localhost:5005`.

### 5.1 Cấu trúc src

```text
src/
├── main.tsx                       # createRoot + StrictMode
├── app/App.tsx                    # Router + ConfigProvider(vi_VN) + ProtectedRoute + ShellRoute
├── app/brand.ts                   # BRAND tokens vàng gold #d4a017 + antdThemeConfig
├── api/                           # 16 module axios client (http.ts + theo domain)
├── features/                      # 34 page component theo domain
├── layouts/ResponsiveLayout.tsx   # Desktop (Sider 220px) / Mobile (<768px: Drawer + bottom-nav 5 mục)
├── stores/                        # zustand: authStore · posCartStore · notificationStore
└── styles/global.css              # Segoe UI, .sticky-action-bar POS mobile
```

### 5.2 State & HTTP client

- **authStore**: token/user khởi tạo từ localStorage (`techpos_token`, `techpos_refresh`, `techpos_user`); `login()` → `POST /auth/login`; `hasPermission()` — SUPER_ADMIN bypass, ngược lại check mảng perms.
- **posCartStore**: giỏ POS bán lẻ (giữ qua tab, reset sau thanh toán).
- **notificationStore**: badge unread **poll 30 giây** (`GET /notifications/unread-count`) — **chưa dùng SignalR client** (không có package `@microsoft/signalr`; SignalR server-side đã sẵn sàng).
- **http.ts**: axios baseURL `/api` timeout 30s; request interceptor gắn Bearer token; response 401 → clear token + redirect `/login` (**chưa có auto refresh-token flow**).

### 5.3 Routing (28 route + login)

| Nhóm | Route |
|---|---|
| Trang chủ | `/` Dashboard |
| Danh mục | `/catalog` (3 tab KH/NCC/Kho), `/catalog/products` |
| Bán hàng | `/pos/retail`, `/pos/wholesale`, `/pos/returns`, `/sales/quotation`, `/sales/order`, `/sales/contract`, `/sales/cod`, `/sales/vehicle-delivery` |
| Kho hàng | `/inventory` (+Thẻ kho), `/inventory/all-warehouses`, `/inventory/cost-recalc`, `/inventory/receipts`, `/inventory/issues`, `/inventory/transfers`, `/inventory/supplier-returns`, `/inventory/stocktakes`, `/purchases/orders` |
| Quỹ | `/cashbook` |
| Công nợ | `/debt` |
| Nhân sự | `/hr/employees`, `/hr/intro-letters`, `/hr/payroll`, `/hr/attendance`, `/hr/bonus` |
| Báo cáo | `/reports` (Report Center 10 tab) |
| Hệ thống | `/notifications`, `/history` (perm ADMIN.AUDIT.VIEW), `/emails` (admin), `/sync-admin` (SUPER_ADMIN/ADMIN/IT) |
| Auth | `/login` |

- Guard: `ProtectedRoute` check token; RBAC nằm ở (a) ẩn/hiện menu theo role trong layout, (b) `hasPermission` trong từng page. Không có route-level guard chung.

### 5.4 Trang chính

- **Dashboard**: doanh thu hôm nay/tháng, số chứng từ, tồn SKU, tồn quỹ; chứng từ trong ngày (tag Đã/Chưa xác minh theo `VERIFIED`); card Sync (heartbeat, row errors); reload 30s.
- **SyncAdminPage**: heartbeat/cursors, logs, row-errors (requeue/ignore/dead-letter), toggle policy bảng, lệnh reconcile/retry/extract-media.
- **POS bán lẻ**: posCartStore, check vượt tồn, khách đưa/trả lại/còn nợ, in hoá đơn PDF sau bán. Bán sỉ cùng pattern (BS26/00001).
- **Kho**: tồn kho + **Thẻ kho (ledger)**; nhập (PN) có phân bổ thanh toán; xuất; chuyển; kiểm kê tự điều chỉnh; xuất trả NCC; ma trận tồn tất cả kho; tính lại giá vốn.
- **Quỹ**: phiếu thu/chi (PT/PC) với **phân bổ công nợ theo hoá đơn (allocations)**; tồn quỹ theo ngày; in phiếu PDF.
- **Báo cáo**: 10 tab (bán hàng ngày / mặt hàng / thu chi ngày / thu chi lý do / XNT / tồn kho / công nợ KH / công nợ NCC / tồn quỹ / KQKD); export Excel qua blob; **mọi PDF do API sinh**.
- **Emails admin**: Templates / Events / Queue (retry/cancel) / SMTP settings + test-send.
- **Design system**: primary vàng gold `#d4a017`, sider dark `#1f1a08`, borderRadius 6, locale `vi_VN`, chưa có dark mode; responsive breakpoint 768px (mobile bottom-nav 5 mục).

### 5.5 Ghi chú kỹ thuật frontend

- Mỗi page tự fetch; pattern loading/error/Alert + `message.error`; `fmtVnd` định nghĩa lặp cục bộ (~10 page, chưa gom util).
- Chưa có: test, ESLint/Prettier, lazy-loading route, i18n (hard-code tiếng Việt), hooks/types/utils riêng.

---

## 6. CƠ SỞ DỮ LIỆU

### 6.1 Firebird 2.5 Embedded (nguồn — DEMO.FDB)

- **86 bảng, 0 view, 0 trigger, 0 generator**; PK = `ID` GUID VARCHAR(36); cột chuẩn `STATUS, USERCREATEDID, TIMECREATED, USERMODIFIEDID, TIMEMODIFIED`.
- Phân loại: **T:34 nghiệp vụ · S:20 hệ thống · D:32 danh mục**. DMATHANG nhiều cột nhất (59).
- **TDONHANG = bảng đa năng ~9 nghiệp vụ** (bán lẻ/sỉ, nhập/xuất/chuyển kho, kiểm kê, trả NCC, khách trả…) phân biệt bằng `LOAI/MODE/LOAIGIA/LOAITHANHTOAN` — rủi ro số 1 của dự án. Đã xác minh DEMO: `LOAI=1` → PN Nhập kho, `LOAI=5` → KTL Kiểm kê, `MODE=-1`, `STATUS=30` = hoạt động, 0 = nghi soft-delete.
- `TTHUCHI`: LOAI 0 = thu, 1 = chi (đã xác minh).
- fbembed.dll **2.5.5 (ODS 11.2, 32-bit)** ⇒ mọi process đụng FB build **x86**; thư mục cài ACL chống ghi ⇒ sync đọc **bản copy** DEMO.FDB.
- Không FK thật; báo cáo desktop nằm trong `SREPORT.SQL` (BLOB).

### 6.2 MySQL 8 mirror (techposweb)

- **86 bảng mirror** (migration 002, DDL auto-generated từ discovery 23/09/2026 — 2.894 dòng, cấm sửa tay): giữ tên + cột Firebird UPPERCASE, mỗi bảng +6 cột kỹ thuật `W_SYNC_AT, W_SYNC_REV, W_ORIGIN (default 'FB'), W_ROW_HASH, W_DELETED, W_DELETED_AT`.
- **36 bảng W_** (web-only): migration 001 (31) + 003 (W_SYNC_COMMAND, W_DOC_TYPE_MAP) + 015 (W_OUTBOX, W_CONFLICT, W_SYNC_OUTBOX_WHITELIST). Nhóm chính:
  - Sync meta: W_SYNC_TABLE_POLICY / CURSOR / LOG / ROW_ERROR / TOMBSTONE / HEARTBEAT / COMMAND
  - Auth/RBAC: W_USER, W_ROLE, W_PERMISSION, W_ROLE_PERMISSION, W_USER_ROLE, W_USER_SCOPE, W_REFRESH_TOKEN
  - Nghiệp vụ web: W_KHOLEDGER (+ENTRY_SEQ), W_MEDIA, W_EMAIL_QUEUE/TEMPLATE/EVENT/SETTING/LOG, W_NOTIFICATION, W_AUDIT_LOG, W_OUTBOX, W_CONFLICT, W_DOC_SEQUENCE, W_APP_SETTING, W_FEATURE_FLAG, W_REPORT_CACHE, W_CHANGELOG, W_MIGRATION
- Quy tắc: tiền = **DECIMAL(15,2) cấm FLOAT/DOUBLE**; utf8mb4; index watermark `(TIMEMODIFIED, ID)`; không suy FK; ID CHAR(36).

### 6.3 Migrations (15 file — idempotent, apply theo tên)

| # | File | Nội dung |
|---|---|---|
| 001 | init_web_tables | 31 bảng W_ nền tảng (sync meta, auth, audit, email, notification, W_KHOLEDGER…) |
| 002 | mirror_schema | **86 bảng mirror** auto-generated (cấm sửa tay) |
| 003 | sync_command | W_SYNC_COMMAND + W_DOC_TYPE_MAP (NHAP_KHO/LOAI=1, KIEM_KE/LOAI=5 **VERIFIED=1**) |
| 004 | media_column | W_MEDIA.COLUMN_NAME + unique key |
| 005 | pos_sale | Bán lẻ (BAN_LE, VERIFIED=0), sequence BL, index UX_OTONKHO_ITEM_KHO |
| 006 | ledger_entry_seq | W_KHOLEDGER.ENTRY_SEQ + backfill |
| 007 | catalog_crud_perms | 10 perms danh mục (35–44) + flag ENABLE_CATALOG_WRITE |
| 008 | phase4_docs | Bán sỉ/Trả KH/Xuất/Chuyển/Trả NCC (5 dòng map VERIFIED=0), perms 45–48 |
| 009 | phase4_orders_delivery | Báo giá/Đặt hàng/Đặt mua/Hợp đồng/COD/Xe — bảng riêng, 7 prefix, perms 49–55 |
| 010 | cost_recalc | Perm INVENTORY.COST_RECALC (56) |
| 011 | cashbook | 4 tổ hợp thu/chi × KH/NCC (VERIFIED=0), prefix PT/PC, perm 57 |
| 012 | hr | 11 perms HR (58–68), sequence GIAYGIOITHIEU |
| 013 | reports | Perm REPORT.PRINT (69) |
| 014 | email_templates | **19 template** + SMTP settings mặc định (password qua env/DPAPI) |
| 015 | two_way_outbox | W_OUTBOX + W_CONFLICT + W_SYNC_OUTBOX_WHITELIST (GĐ7, mặc định OFF) |

### 6.4 Seed

`001_roles_permissions_flags.sql` (chạy sau migration 001/002):

- **9 roles**: SUPER_ADMIN, ADMIN, MANAGER, SALES, CASHIER, WAREHOUSE, ACCOUNTANT, HR, VIEWER.
- **34 permissions gốc** → migration 007–013 bổ sung tới tổng **69 perms** (pattern `MODULE.RESOURCE.ACTION`).
- Mapping role→perm: ADMIN/SUPER_ADMIN toàn bộ; MANAGER theo module; còn lại theo danh sách cụm.
- **7 feature flags**: ENABLE_WEB_TRANSACTION=0, ENABLE_TWO_WAY_SYNC=0, ENABLE_EMAIL=1, ENABLE_SIGNALR=1, ENABLE_COD=1, ENABLE_PRODUCTION=0, ENABLE_ADVANCED_REPORT=0.
- **19 email events** (ORDER_CREATED … SYSTEM_ERROR).
- Admin user tạo bằng tool `TechPos.AdminSeed` (không nằm trong seed SQL).

### 6.5 Quy ước mã chứng từ (từ dữ liệu desktop)

| Loại | Mẫu |
|---|---|
| Phiếu nhập / xuất kho | PN/PX + YY/5 số (PN18/00001) |
| Bán sỉ / Bán lẻ | BS+YY/5 số · 8 chữ số (26000001) |
| Kiểm kê | KTL + YY/5 số |
| Phiếu thu/chi | PT/PC + YY |
| Báo giá/Đặt hàng/Đặt mua/Hợp đồng | BG/DH/DN/HD + YY/5 số |
| COD / KQ COD | COD/KQ + YY/4 số |
| Giao xe | XE |
| Giấy giới thiệu | GS + YY |
| Mã danh mục | AMC0001 (mặt hàng), KH00001 (khách), NCC00001 (NCC) |

---

## 7. LUỒNG HOẠT ĐỘNG END-TO-END

### 7.1 Sync 1 chiều (Firebird → MySQL) — mặc định GĐ0–6

```text
[TechPosSync Worker — mỗi 5 giây]
1. Đọc W_SYNC_TABLE_POLICY → bảng đến hạn (TRANSACTION 30s / DICTIONARY 120s / OTHER 600s)
2. WatermarkSyncEngine:
   - SELECT rows FB WHERE (TIMEMODIFIED, ID) > (cursor.LAST_MODIFIED, cursor.LAST_ID) LIMIT batch
   - Mỗi row: MirrorWriter dò cột chung FB↔MySQL → ComputeHash MD5 → so W_ROW_HASH
     → khác hash: UPSERT mirror (W_SYNC_AT=now, W_ORIGIN='FB', W_ROW_HASH=hash)
     → giống hash: skip
   - Row lỗi → W_SYNC_ROW_ERROR (payload JSON); phần batch còn lại vẫn ghi; cursor chỉ advance khi 0 lỗi
3. Heartbeat → W_SYNC_HEARTBEAT
4. Retry row lỗi: backoff 1/5/15/30 phút × 4 → DEAD_LETTER
[02:00] ReconcileEngine (W2): COUNT/ID-set/hash từng bảng
   → thiếu: upsert bổ sung; hash khác: resync; dư (hard-delete FB): soft-delete mirror + W_SYNC_TOMBSTONE
[02:30] MediaExtractor: BLOB 12 cột → media-store/ + W_MEDIA
```

### 7.2 Bán lẻ web (Phase 4.1)

```text
UI RetailSalePage (posCartStore)
  → thêm dòng hàng (check tồn từ /api/inventory)
  → thanh toán: khachDua → traLai / conNo (PosCalculator: decimal, round away-from-zero)
  → POST /api/pos/sales
     [PosSaleService + Validator]
     → W_DOC_TYPE_MAP (BAN_LE) → sinh số phiếu BLyy/xxxxx (W_DOC_SEQUENCE)
     → TRANSACTION 1 lần ghi:
        TDONHANG (LOAI=2, W_ORIGIN='WEB') + TDONHANGCHITIET
        + OTONKHO (snapshot tồn) + W_KHOLEDGER (GIA_VON)
        + TCHITIETTHANHTOAN (nếu thu tiền/công nợ) + W_AUDIT_LOG
  → UI in hoá đơn PDF (InvoicePdfBuilder A5)
```

### 7.3 Chứng từ kho (nhập/xuất/chuyển/kiểm kê/trả NCC)

- Mỗi loại ghi **TDONHANG + CHITIET + W_KHOLEDGER + OTONKHO trong 1 transaction**, `W_ORIGIN='WEB'`.
- Xuất: kiểm tra đủ tồn trước khi cho xuất (400 nếu thiếu).
- Kiểm kê (KTL): chênh lệch → tự điều chỉnh tăng/giảm.
- Giá vốn dòng xuất: `W_KHOLEDGER.GIA_VON`; chỉ dòng web mới có `TDONHANGCHITIET.GIAVON`.
- Tính lại giá vốn (POST /api/inventory/cost-recalc): chọn phương pháp + "từ ngày" → CostCalculator → ghi lại giá vốn ledger.

### 7.4 Quỹ & công nợ

```text
Phiếu thu PTyy / chi PCyy (4 tổ hợp THU/CHI × KH/NCC qua W_DOC_TYPE_MAP)
  → TTHUCHI (LOAI 0=thu, 1=chi) + phân bổ trả nợ TCHITIETTHANHTOAN (chỉ chứng từ WEB)
  → Tồn quỹ = Σ thu − Σ chi (tách tiền mặt / chuyển khoản)
Công nợ KH = SUM(DKHACHHANG.CONNO) · NCC = SUM(DNHACUNGCAP.CONLAI)
ScheduledJobsWorker (30 phút): hoá đơn nợ đến hạn/quá hạn (NGAY + SONGAYCHONO ≤ +3 ngày)
  → W_NOTIFICATION nhóm DEBT + deep-link /debt
```

### 7.5 Lương (Phase 5.3)

```text
Chấm công: TBANGLUONGCHITIET (NV + ngày + ca; chặn trùng, chặn ngày tương lai)
Bảng lương BLyy/xxxxx — 1 bảng/tháng (409 nếu trùng):
  LUONGCA = Σ(SL công × TILELUONG/100 × LUONGCA_NV) + THUONG + PHUCAP − PHAT − TAMUNG
  TILELUONG lưu dạng % (100 = nguyên ca, 50 = nửa ca)
CACHTINHLUONG: 0 = theo ca, 1 = theo tháng
Tạm ứng = phiếu chi PC với LATAMUNG=1
```

### 7.6 Email (fire-and-forget)

```text
Nghiệp vụ phát event (19 events) → EmailService.EnqueueFromEventAsync
  → W_EMAIL_QUEUE (render template W_EMAIL_TEMPLATE, escape HTML anti-XSS)
  → EmailWorker (BackgroundService, poll 20s, claim atomic 10 items) → SMTP từng người nhận
  → lỗi: retry backoff 1/5/15/30 phút × 4 → FAILED
  → LỖI EMAIL KHÔNG BAO GIỜ FAIL TRANSACTION NGHIỆP VỤ (quy tắc vàng 7)
```

### 7.7 Sync 2 chiều (GĐ7 — hiện OFF)

```text
Web ghi mirror (W_ORIGIN='WEB') → W_OUTBOX (chỉ bảng trong W_SYNC_OUTBOX_WHITELIST)
  → TwoWaySyncEngine (nếu ENABLE_TWO_WAY_SYNC=true): claim PENDING→RUNNING
  → INSERT/UPDATE Firebird chỉ cột trong WRITE_COLS whitelist
  → xung đột: so TIMEMODIFIED 2 phía — FB mới hơn ⇒ giữ FB + W_CONFLICT (OPEN)
  → API /api/sync/conflicts/{id}/resolve (FB|WEB) xử tay
```

### 7.8 Auth

```text
POST /api/auth/login (username + password)
  → BCrypt verify; sai 5 lần → khóa 15 phút
  → JWT HS256 (claim sub, roles, perms) 15 phút + refresh token (SHA256 hash DB) 7 ngày
  → FE lưu localStorage; mọi request gắn Bearer; 401 → clear + redirect /login
  → RequirePermission check claim perms; SUPER_ADMIN bypass
```

---

## 8. DANH MỤC CHỨC NĂNG

### 8.1 Map API endpoint ↔ trang UI

| Nhóm API | Endpoint chính | Trang UI |
|---|---|---|
| Auth | POST /auth/login, GET /auth/me | LoginPage |
| Dashboard | GET /dashboard/summary | DashboardPage |
| Sản phẩm | /products (CRUD + barcode + import) | ProductsPage |
| Danh mục | /customers, /suppliers, /warehouses (CRUD + import) | CatalogPage (3 tab) |
| POS | POST /pos/sales, /pos/wholesale, /pos/returns, /pos/quote | RetailSalePage, WholesalePage, ReturnsPage |
| Chứng từ BH | /quotations, /orders, /purchase-orders, /contracts, /cod/*, /vehicle-deliveries | QuotationPage, SalesOrderPage, PurchaseOrdersPage, ContractsPage, CodPage, VehicleDeliveriesPage |
| Kho | /inventory, /inventory/ledger, /inventory/all-warehouses, /inventory/cost-recalc, /receipts, /inventory/issues, /inventory/transfers, /inventory/supplier-returns, /stocktakes | InventoryPage (+Thẻ kho), AllWarehousesPage, CostRecalcPage, ReceiptsPage, IssuesPage, TransfersPage, SupplierReturnsPage, StocktakesPage |
| Quỹ | /cash/* (thu/chi, lý do, tồn quỹ) | CashbookPage |
| Công nợ | /debts/customers, /debts/suppliers | DebtsPage |
| Nhân sự | /employees, /attendance, /bonus-penalties, /payroll (+PDF), /salary-advance, /intro-letters | 5 trang HR |
| Báo cáo | /reports/* (10 JSON + 13 Excel + 3 PDF + barcode) | ReportsPage (10 tab) |
| Sync | /sync/status, /sync/reconcile, /sync/row-errors, /sync/tables, /sync/outbox, /sync/conflicts | SyncAdminPage |
| Email/Notify | /notifications, /emails (templates/events/queue/settings/test) | NotificationsPage, EmailsPage |
| Hệ thống | /history, /health | HistoryPage, health cho monitoring |

### 8.2 Report Center (10 báo cáo)

1. Bán hàng theo ngày
2. Mặt hàng bán chạy
3. Thu chi theo ngày
4. Thu chi theo lý do
5. Xuất – Nhập – Tồn (XNT)
6. Tồn kho (Excel-only)
7. Công nợ khách hàng
8. Công nợ nhà cung cấp
9. Tồn quỹ luỹ kế
10. Kết quả kinh doanh (KQKD)

Excel export 13 nguồn dữ liệu; PDF: hoá đơn A5, phiếu thu/chi A5, phiếu kho A5 (5 loại), bảng lương A4; mã vạch CODE_128 PNG.

---

## 9. LOGIC NGHIỆP VỤ QUAN TRỌNG

### 9.1 Tính tiền POS (PosCalculator)
- Toàn bộ decimal; làm tròn **away-from-zero**.
- Tiền hàng = Σ(SL × đơn giá − giảm giá dòng); tổng = tiền hàng − giảm giá hoá đơn + thuế + phí vận chuyển; thanh toán → tiền thừa hoặc tạo công nợ.

### 9.2 Giá vốn (CostCalculator — 11 unit test)
- **FIFO**: xuất theo tầng nhập trước trước.
- **BQ liên hoàn (gia quyền)**: BQ = (tồn đầu × giá đầu + nhập mới) / (tồn + nhập mới) sau mỗi lần nhập.
- **BQ cuối kỳ**: BQ = (giá trị đầu kỳ + nhập kỳ) / (SL đầu kỳ + SL nhập kỳ), áp cho cả kỳ.
- Kết quả ghi `W_KHOLEDGER.GIA_VON` (mọi dòng xuất) + `TDONHANGCHITIET.GIAVON` (chỉ dòng web).

### 9.3 Bảng đa năng TDONHANG & W_DOC_TYPE_MAP
- Một bảng FB cho ~9 nghiệp vụ; web định nghĩa map `W_DOC_TYPE_MAP(DOC_CODE, LOAI, MODE, PREFIX, VERIFIED)`.
- **Chỉ ghi được khi `VERIFIED=1`** (quy tắc vàng 5 — không hard-code LOAI/MODE chưa xác minh). Hiện chỉ NHAP_KHO (LOAI=1) và KIEM_KE (LOAI=5) VERIFIED=1; BAN_LE/BS/TL/XK/CK/XT + 4 tổ hợp thu/chi đang VERIFIED=0 **chờ DB production** (trên DEMO đã verify tay qua query nên chạy được).
- Báo giá/đặt hàng/đặt mua/hợp đồng/COD/giao xe **dùng bảng riêng** nên không phụ thuộc map.

### 9.4 Chứng từ web vs FB-origin
- Mọi ghi web đánh dấu `W_ORIGIN='WEB'`; dòng `W_ORIGIN='FB'` **không cho sửa/xóa** từ web (sync sẽ đè).
- Sync 2 chiều chỉ đụng bảng trong whitelist; conflict xử lý qua W_CONFLICT.

### 9.5 Soft-delete & audit
- Xóa danh mục = `W_DELETED=1, W_DELETED_AT=now` (thùng rác khôi phục được).
- Mọi thao tác ghi → `W_AUDIT_LOG` (old/new JSON, IP, UA, userId, correlationId).

### 9.6 Số phiếu
- `W_DOC_SEQUENCE` cấp số theo prefix (BL/PN/PX/KTL/PT/PC/BG/DH/DN/HD/COD/KQ/XE/GIAYGIOITHIEU) — format `PREFIX + YY/xxxxx`.

---

## 10. BẢO MẬT & PHÂN QUYỀN RBAC

### 10.1 Authentication
- JWT HS256, access 15 phút / refresh 7 ngày (SHA256 hash trong DB, rotate).
- BCrypt cost 12; khóa tài khoản sau 5 lần sai liên tiếp (15 phút).
- JWT secret qua env `TECHPOS_JWT_SECRET` (không hard-code, không commit).
- Refresh endpoint hiện **501** (chưa hoàn thiện auto-refresh phía FE).

### 10.2 RBAC — 9 roles × 69 permissions

| Role | Perms | Phạm vi |
|---|---|---|
| SUPER_ADMIN | 69 | Toàn quyền + bypass mọi check |
| ADMIN | 69 | Toàn quyền |
| MANAGER | 59 | Theo module |
| SALES | 21 | Bán hàng, KH |
| CASHIER | 11 | POS, quỹ |
| WAREHOUSE | 15 | Kho |
| ACCOUNTANT | 21 | Quỹ, công nợ, báo cáo |
| HR | 14 | Nhân sự |
| VIEWER | 7 | Chỉ xem (403 khi ghi) |

- Permission pattern: `MODULE.RESOURCE.ACTION` (SALES / INVENTORY / CASHBOOK / DEBT / REPORT / HR / ADMIN / EMAIL / SYNC).
- Server: `[RequirePermission]` trên action; claim `perms` ';'-separated trong JWT; SUPER_ADMIN đi qua hết.
- Scope: `W_USER_SCOPE` (ALL/STORE/WAREHOUSE).
- FE: ẩn menu theo role + `hasPermission` trong page; **route-level guard chung chưa có**.

### 10.3 Bảo mật khác
- ExceptionMiddleware không trả raw DB error; header `X-Trace-Id` cho tracing.
- Template email escape HTML chống XSS (có unit test).
- MySQL bind 127.0.0.1; SMTP password qua env/DPAPI; appsettings.Production không commit.
- 9 tài khoản test theo vai trò (admin/Admin@123 cho SUPER_ADMIN; chi tiết `docs/TAI-KHOAN-HE-THONG.md` — nên đổi mật khẩu khi bàn giao).

---

## 11. EMAIL & NOTIFICATION

- **19 email events** (ORDER_CREATED … SYSTEM_ERROR) + **19 template** seed (migration 014) với biến `{{CUSTOMER_NAME}}`, `{{ORDER_NUMBER}}`…
- Kiến trúc: Event → W_EMAIL_QUEUE → EmailWorker (20s) → SMTP; retry 1/5/15/30 phút × 4 → FAILED; lỗi không fail nghiệp vụ.
- SMTP cấu hình runtime qua trang /emails + Test Email; password qua env/DPAPI.
- **Notification Center**: W_NOTIFICATION 7 nhóm (ALL/SALES/INVENTORY/DEBT/COD/SYSTEM/EMAIL); SignalR `NotificationHub` server-side sẵn sàng (JWT query-string) nhưng **FE hiện poll 30s** (chưa cài client SignalR); ScheduledJobsWorker tạo notification DEBT tự động mỗi 30 phút.


---

## 12. TRIỂN KHAI & VẬN HÀNH (Windows, không Docker)

### 12.1 4 thành phần triển khai

| Thành phần | Kiểu | Kiến trúc | Cổng | Ghi chú |
|---|---|---|---|---|
| TechPosSync | Windows Service | **x86** (fbembed 32-bit) | — | Cùng máy DEMO.FDB; đọc bản copy |
| TechPosApi | Windows Service | x64 | 5005 | Serve SPA + SignalR `/hubs/notifications` |
| MySQL 8 | MySQL80 service | — | 3306 bind 127.0.0.1 | DB `techposweb` utf8mb4 |
| SPA | file tĩnh | — | (cùng 5005) | frontend/techpos-web/dist |

### 12.2 Deploy scripts (deploy/powershell)

| Script | Mục đích |
|---|---|
| `setup-database.ps1` | Tạo DB `techposweb` (utf8mb4) → chạy migrations theo tên → seed → kiểm đếm bảng. Không đụng Firebird. |
| `install-web-api.ps1` | Publish API win-x64 → `C:\TechPosApi`, tạo appsettings.Production.json placeholder (cổng 5005), cài Windows Service `TechPosApi`. |
| `install-sync-service.ps1` | Publish sync **win-x86**, copy fbembed + ICU từ `Program Files (x86)\TechPos\SM4\v4.5`, cài service `TechPosSync`. |
| `backup-mysql.ps1` | mysqldump 23:30 (Task Scheduler) → zip `D:\Backup`, giữ 14 ngày. |

### 12.3 RUNBOOK.md (7 phần)

1. **Cài lần đầu:** setup-database → install-web-api (sửa connection string + JWT secret tay) → AdminSeed (admin đầu tiên) → install-sync-service (copy DEMO.FDB ra thư mục riêng).
2. **Backup/Phục hồi:** mysqldump 23:30/đêm giữ 14 ngày, RPO ≤ 15 phút; Firebird backup .gbk ≥ 3 bản/3 nơi (do hãng quản lý).
3. **Monitoring:** heartbeat lag > 5 phút cảnh báo; parity DataCompare; email FAILED > 10/ngày; conflict OPEN > 0; disk < 20%.
4. **Sự cố thường gặp:** FDB access-denied (bản copy bị giữ), JWT secret đổi → 401 hàng loạt, migration failed (xoá dòng W_MIGRATION + restart), SMTP chưa cấu hình, SignalR bị chặn → FE tự dùng poll 30s.
5. **Nâng cấp:** backup → publish mới → check `/api/health` → deploy sync → parity.
6. **Rollback:** publish lại version cũ `C:\Releases\{version}`; desktop không bao giờ bị động vào.
7. **Backup script** `backup-mysql.ps1` (dump + zip + dọn cũ, đã verify dump 12.1 MB).

---

## 13. PHỤ THUỘC

### 13.1 NuGet (gom nhóm)

| Nhóm | Package (phiên bản) |
|---|---|
| Data | Dapper 2.1.89 · MySqlConnector 2.6.2 · FirebirdSql.Data.FirebirdClient 10.3.4 |
| Web/API | Microsoft.AspNetCore.OpenApi 10.0.8 · Swashbuckle.AspNetCore 10.2.1 · AspNetCore.Authentication.JwtBearer 10.0.8 · Serilog.AspNetCore 9.0.0 (+ Sinks.File/Console) |
| Validation/Auth | FluentValidation 12.1.x · System.IdentityModel.Tokens.Jwt 8.14.0 · BCrypt.Net-Next 4.0.3 |
| Reporting | QuestPDF 2025.7.0 · ClosedXML 0.105.1 · ZXing.Net 0.16.11 (+ Windows.Compatibility) |
| Hosting/Worker | Microsoft.Extensions.Hosting + Hosting.WindowsServices 10.0.8 · Options 10.0.0 |
| Test | xunit 2.9.3 · xunit.runner.visualstudio 3.1.4 · Microsoft.NET.Test.Sdk 18.0.1 · FluentAssertions 8.7.1 |

### 13.2 npm (frontend/techpos-web)

| Nhóm | Package (phiên bản) |
|---|---|
| UI | antd ^6.0.2 · @ant-design/icons ^6.1.0 |
| Grid | ag-grid-community ^36.2.0, ag-grid-react ^36.2.0 *(chưa dùng trong src)* |
| HTTP | axios ^1.13.2 |
| State | zustand ^5.0.9 |
| Routing | react-router-dom ^7.9.5 |
| Core | react/react-dom ^19.2.0 · dayjs ^1.11.19 |
| Dev | vite ^7.2.7 · @vitejs/plugin-react ^5.1.2 · typescript ~5.9.3 · @types/react(-dom) ^19 |

### 13.3 Nền tảng & runtime bên ngoài

- .NET 10 SDK/runtime (win-x64 cho API, win-x86 cho Sync/Extractor/DataCompare)
- MySQL 8 (utf8mb4); Firebird 2.5 Embedded fbembed.dll 2.5.5 (ODS 11.2, 32-bit) + ICU 3.0 (copy từ thư mục cài desktop)
- Node.js + Vite 7 cho build FE; SMTP server cho email; mysqldump cho backup
- Windows-only: System.Drawing (barcode), Windows Service hosting — **không Docker**

---

## 14. KIỂM THỬ & CHẤT LƯỢNG

| Tầng | Trạng thái |
|---|---|
| Unit test (xUnit) | 104 method / 14 file — **tất cả xanh** (calculator, validator, hash, PDF, Excel, email) |
| Parity check | `TechPos.DataCompare` tự động — 04/10: **FB-origin mirror khớp 100%** (86 bảng, TDONHANG 44.560.845 đ = 0 lệch); 14 bảng "dư" đều WEB-origin (đúng thiết kế) |
| Build gates | dotnet build 0 error · dotnet test xanh · npm run build OK |
| Còn thiếu | E2E Playwright (login→bán lẻ→in hoá đơn→tồn kho), load test (k6/NBomber), parity liên tục 14 ngày trên DB production (điều kiện ADR-12) |
| Integration/E2E | Thiết kế trong TESTING-STRATEGY.md (7 tầng) — chưa implement |

Log chạy thật (W_SYNC_LOG): 6.245 incremental · 86 reconcile · 12 media · **0 errors**; parity COUNT 86/86 bảng = 0 lệch.

---

## 15. TRẠNG THÁI DỰ ÁN & VIỆC CÒN LẠI

### 15.1 Trạng thái phase

| Phase | Nội dung | Trạng thái |
|---|---|---|
| 0 | DB discovery Firebird (86 bảng thật) | ✅ Xong |
| 1 | Solution + MySQL foundation + Backend + Frontend skeleton | ✅ Xong |
| 2 | Sync Service (Watermark, Reconcile, Row Error, Tombstone) | ✅ Chạy thật toàn diện (6.245 incremental, 0 error) |
| 3 | Auth, RBAC, Dashboard | ✅ JWT + RBAC 9 roles/69 perms, Dashboard số liệu thật |
| 4 | Danh mục, Bán hàng, Kho | ✅ 30/09/2026 (56 perms; Sản xuất chờ flag + DB production) |
| 5 | Quỹ, Công nợ, Nhân sự | ✅ 30/09/2026 (13 perms HR, migration 012) |
| 6 | Reports, PDF, Excel, Barcode | ✅ 03/10/2026 (10 báo cáo + Excel + PDF + barcode) |
| 7 | Email, Notification, SignalR + hạ tầng 2 chiều | ✅ 04/10/2026 (outbox/conflict; 2 chiều OFF) |
| 8 | Testing | 🟡 Unit 104/104 xanh + parity tự động; thiếu E2E + load test |
| 9 | Deployment Windows | ✅ 04/10/2026 (RUNBOOK + 4 script) |

### 15.2 Việc còn lại (cần DB production / xác nhận của hãng)

1. **Xác minh W_DOC_TYPE_MAP trên DB production** → UPDATE VERIFIED=1 cho BAN_LE/BS/TL/XK/CK/XT + 4 tổ hợp thu/chi (quy tắc vàng 5).
2. **GĐ7 bật 2 chiều** khi hãng đồng ý ghi Firebird: `ENABLE_TWO_WAY_SYNC=true` + seed whitelist; chạy thử 20 tình huống xung đột trước khi mở.
3. **Sản xuất** (nếu bật ENABLE_PRODUCTION): verify bảng TSANXUAT/TSANXUATCHITIET trên DB production.
4. **HR production**: kiểm tra tập giá trị TBANGLUONGCHITIET.TRANGTHAI và DNHANVIEN.CACHTINHLUONG (DEMO có giá trị lạ 30/60).
5. **Phase 8 còn lại:** E2E Playwright (login → bán lẻ → in hoá đơn → tồn kho), load test (k6/NBomber), **parity 6 báo cáo = 0 lệch liên tục 14 ngày** trên production trước cutover.
6. **UAT + đào tạo** theo GĐ8: đào tạo theo vai trò, tài liệu thao tác, hotline 2 tuần, chốt ngày cắt chuyển.

---

## 16. QUY TẮC VÀNG & RỦI RO

### 16.1 7 quy tắc vàng (bất biến)

1. **Không sửa desktop / Firebird.** Sync chỉ READ (trừ outbox 2 chiều có whitelist khi bật GĐ7).
2. **Không sinh schema MySQL từ tài liệu** — bắt buộc discovery RDB$ trên DB thật (FirebirdExtractor → SchemaGenerator).
3. Bảng mirror giữ nguyên tên + cột Firebird (UPPERCASE); bảng web-only prefix `W_`.
4. Tiền = DECIMAL, cấm FLOAT/DOUBLE.
5. TDONHANG đa năng — không hard-code LOAI/MODE chưa xác minh (VERIFIED gate).
6. `ENABLE_TWO_WAY_SYNC = false` mặc định; Firebird = MASTER, MySQL = MIRROR ở GĐ đầu.
7. Email/Notification qua queue — lỗi email không fail transaction nghiệp vụ.

### 16.2 Rủi ro chính (16 rủi ro — RISKS.md, rút ra nổi bật)

| Mức | Rủi ro |
|---|---|
| Critical | Danh mục thiếu `DKHUVUC`, `CNHANVIEN` (SQL WebProcessor trỏ DB khác) |
| Critical | TDONHANG đa năng — tập LOAI/MODE chưa biết đủ trên production |
| Critical | FB Embedded chỉ file cục bộ (ODS 11.2) — không share mạng |
| High | Update không bump TIMEMODIFIED → sync lệch (reconcile bù) |
| High | Hard delete FB không dấu vết → tombstone |
| High | SREPORT.SQL (BLOB) dễ mất — đã có MediaExtractor |
| High | Bật 2 chiều sinh xung đột 2 nguồn ghi → flag OFF mặc định + whitelist |
| Security | teamviewer.exe + credential .dat trong thư mục cài desktop |

### 16.3 Quy tắc code (quy tắc code.txt — áp cho cả dự án)

- Layer bắt buộc Controller → Service → Repository; DTO ↔ Entity qua Mapper; không trả Entity ra Controller.
- Không dùng RuntimeException-style — dùng exception domain + global handler; thông báo lỗi tiếng Việt.
- Không hard-code secret; `.env` trong .gitignore; không tạo file documentation ngoài phạm vi yêu cầu (trừ khi được yêu cầu như lần này).
- Scope control: chỉ sửa phần được yêu cầu, reuse trước khi thêm mới.

---

## 17. CẤU TRÚC THƯ MỤC REPO

```text
HoanNgocWeb/
├── BAO-CAO-TONG-HOP-DU-AN.md      ← (file này)
├── README.md                       ← trạng thái phase + kết quả verify từng đợt
├── DANH-MUC-CHUC-NANG.md           ← 10 nhóm / 76 mục / ~41 màn hình (từ 51 ảnh desktop)
├── DOI-CHIEU-CSDL-VA-CHUC-NANG.md  ← đối chiếu 86 bảng ↔ chức năng (coverage ~84%)
├── TechPos_SM4_Database_Structure.md (2.064 dòng, 86 bảng — tham khảo, không phải chân lý)
├── TU-DIEN-DU-LIEU-TECHPOS-SM4.md  ← 581 cột / 67 bảng metadata cache + nhãn tiếng Việt
├── KHAO-SAT-THU-MUC-CAI-DAT-TECHPOS-SM4.md ← fbembed 32-bit, module web hãng, rủi ro
├── KE-HOACH-TRIEN-KHAI-WEB-MYSQL-SYNC.md   ← kế hoạch tổng v1.0, 9 giai đoạn, 7 mục tiêu
├── Quy tắc code.txt                ← quy tắc kiến trúc/lớp/DTO/exception/secret
├── backend/
│   ├── TechPosWeb.slnx             ← 9 src + 1 test + 4 tools (net10.0)
│   ├── src/ (Api · Application · Domain · Data.Firebird · Data.MySql ·
│   │         Reporting · Shared · Sync.Core · Sync.Service)
│   ├── tests/TechPos.UnitTests/
│   └── tools/ (AdminSeed · DataCompare · FirebirdExtractor · SchemaGenerator)
├── frontend/techpos-web/           ← React 19 + Vite + TS + AntD 6 (34 page, 28 route)
├── database/
│   ├── migrations/ (001–015, idempotent)
│   └── seed/001_roles_permissions_flags.sql
├── deploy/
│   ├── powershell/ (setup-database · install-web-api · install-sync-service · backup-mysql)
│   └── RUNBOOK.md
├── docs/
│   ├── architecture/ (ARCHITECTURE · FRONTEND-STRUCTURE · RBAC-DESIGN)
│   ├── database/ (DATABASE-STRATEGY · PHASE0-FINDINGS · discovery/ — 86 JSON + SCHEMA_DISCOVERY.md)
│   ├── sync/SYNC-STRATEGY.md · api/API-STRUCTURE.md · email/EMAIL-ARCHITECTURE.md
│   ├── testing/TESTING-STRATEGY.md · ROADMAP.md · RISKS.md · TAI-KHOAN-HE-THONG.md
├── Giao diện HN/                   ← 51 ảnh tham chiếu UI desktop
└── .tmp-fb/                        ← bản copy DEMO.FDB cho sync (READ-ONLY)
```

---

## PHỤ LỤC A — CON SỐ TỔNG HỢP

| Chỉ số | Giá trị |
|---|---|
| Bảng Firebird / mirror MySQL | 86 / 86 |
| Bảng W_* web-only | 36 |
| Roles / Permissions / Feature flags | 9 / 69 / 7 |
| Controllers / Route prefix / HTTP endpoint | 26 / 34 / 136 |
| Service classes / Validators | ~20 / 23 (FluentValidation) |
| Repository Dapper | 28 |
| Unit tests | 104 (14 file) — 107/107 xanh theo README |
| Pages React / Routes | 34 page / 28 route |
| Email events / templates | 19 / 19 |
| Migrations | 15 (idempotent) |
| Báo cáo Report Center / Excel export / PDF builder | 10 / 13 nguồn / 4 builder |
| Sync policy | TRANSACTION 30s · DICTIONARY 120s · OTHER 600s · LEDGER 300s · reconcile 02:00 · media 02:30 |
| Port | API 5005 · dev FE 5173 · MySQL 3306 (localhost) · desktop web hãng 8080 (cần chú ý xung đột) |

## PHỤ LỤC B — SỰ KHÁC BIỆT TIỀN ĐỒNG BỘ (verify 04/10/2026)

- Parity 86/86 bảng COUNT = **0 lệch**.
- Parity tiền TDONHANG: FB **44.560.845** = MySQL **44.560.845.00** — **0 đồng lệch**.
- 14 bảng "dư" phía MySQL đều là WEB-origin (đúng ADR-02 GĐ0–6).
- W_SYNC_LOG thực tế: 6.245 incremental · 86 reconcile · 12 media · **0 errors**.

---

## PHỤ LỤC C — TÀI LIỆU THAM CHIẾU TRONG REPO

| Tài liệu | Vị trí |
|---|---|
| Kiến trúc tổng thể | `docs/architecture/ARCHITECTURE.md` |
| Frontend structure | `docs/architecture/FRONTEND-STRUCTURE.md` |
| RBAC design | `docs/architecture/RBAC-DESIGN.md` |
| Database strategy + Phase 0 findings | `docs/database/DATABASE-STRATEGY.md`, `docs/database/PHASE0-FINDINGS.md` |
| Discovery schema 86 bảng | `docs/database/discovery/SCHEMA_DISCOVERY.md` + `tables/*.json` |
| Sync strategy | `docs/sync/SYNC-STRATEGY.md` |
| API structure | `docs/api/API-STRUCTURE.md` |
| Email architecture | `docs/email/EMAIL-ARCHITECTURE.md` |
| Testing strategy | `docs/testing/TESTING-STRATEGY.md` |
| Roadmap / Risks / Tài khoản | `docs/ROADMAP.md` · `docs/RISKS.md` · `docs/TAI-KHOAN-HE-THONG.md` |
| Danh mục chức năng desktop | `DANH-MUC-CHUC-NANG.md` |
| Đối chiếu CSDL ↔ chức năng | `DOI-CHIEU-CSDL-VA-CHUC-NANG.md` |
| Cấu trúc 86 bảng + từ điển dữ liệu | `TechPos_SM4_Database_Structure.md` · `TU-DIEN-DU-LIEU-TECHPOS-SM4.md` |
| Kế hoạch triển khai v1.0 | `KE-HOACH-TRIEN-KHAI-WEB-MYSQL-SYNC.md` |
| Runbook vận hành | `deploy/RUNBOOK.md` |


