# ARCHITECTURE — TechPosWeb

## 1. Architecture Diagram (tổng thể)

```text
                         ┌──────────────────────────────┐
                         │      Desktop TechPos SM4     │
                         │      Application.exe         │  (KHÔNG SỬA)
                         └──────────────┬───────────────┘
                                        │
                                        ▼
                         ┌──────────────────────────────┐
                         │  Firebird 2.5 Embedded       │
                         │  DEMO.FDB  (MASTER - GĐ đầu) │
                         └──────────────┬───────────────┘
                                        │ READ ONLY (fbembed, cùng máy)
                                        ▼
                         ┌──────────────────────────────┐
                         │  TechPos.Sync.Service        │
                         │  .NET 10 Worker Service      │
                         │  ─ Watermark incremental     │
                         │  ─ Scheduler theo nhóm bảng  │
                         │  ─ Reconcile 02:00 hằng ngày │
                         │  ─ BLOB/Media extractor 02:30│
                         │  ─ Row-error Dead Letter     │
                         │  ─ Health check + heartbeat  │
                         └──────────────┬───────────────┘
                                        │
                                        ▼
                         ┌──────────────────────────────┐
                         │          MySQL 8             │
                         │  ① Mirror tables (tên FB)    │
                         │  ② W_* web-only tables       │
                         │     (auth, RBAC, audit,      │
                         │      email, notification,    │
                         │      sync meta, ledger)      │
                         └──────────────┬───────────────┘
                                        │
                                        ▼
                         ┌──────────────────────────────┐
                         │  TechPos.Api (ASP.NET Core)  │
                         │  JWT Auth · RBAC · Audit     │
                         │  Business modules · Reports  │
                         │  Email worker · Notification │
## 3. Component Diagram (frontend)

```text
techpos-web (Vite + React + TS)
├── app/            providers, router, query client, theme, signalr client
├── features/       auth, dashboard, sales, pos, inventory, cashbook, debt,
│                   hr, admin, reports, notifications, email, sync-admin
├── components/     ui cơ sở: DataTable(AG Grid), FormKit, PageHeader,
│                   BarcodeScanner, MoneyInput, VnDateRange, PrintPreview
├── layouts/        DesktopLayout(sidebar) / MobileLayout(drawer+bottomnav)
├── api/            axios instance + TanStack Query hooks per resource
├── stores/         Zustand: auth, ui (sidebar/breakpoint), pos-cart
└── styles/         design tokens, breakpoints, AntD theme
```

## 4. Project Folder Structure (tạo trong Phase 1)

```text
TechPosWeb/
├── backend/
│   ├── TechPosWeb.slnx          Solution .NET
│   ├── src/
│   │   ├── TechPos.Sync.Service/     Worker Service, Windows Service host
│   │   ├── TechPos.Sync.Core/        Watermark engine, scheduler, reconcile, row-error
│   │   ├── TechPos.Data.MySql/       Dapper repos, migration runner, entities
│   │   ├── TechPos.Data.Firebird/    read-only Fb data access, schema discovery
│   │   ├── TechPos.Api/              Web API host
│   │   ├── TechPos.Application/      Services + validation + use cases
│   │   ├── TechPos.Domain/           Entities, enums, domain rules
│   │   ├── TechPos.Reporting/        PDF/Excel/Barcode
│   │   └── TechPos.Shared/           DTO, ApiResult, settings, constants
│   ├── tools/
│   │   ├── TechPos.FirebirdExtractor/  Phase 0: RDB$RELATIONS discovery → JSON/MD
│   │   ├── TechPos.SchemaGenerator/    discovery JSON → MySQL DDL mirror
│   │   └── TechPos.DataCompare/        parity tool (COUNT/SUM/CHECKSUM)
│   └── tests/    (Unit, Integration, Sync, Parity, E2E)
├── frontend/techpos-web/            React app (Vite + TS + AntD + AG Grid)
├── docs/                            (architecture, database, sync, api, reports, email, deployment)
├── database/ (migrations, seed)
└── deploy/     (powershell, RUNBOOK.md)
```

## 5. Responsive Layout Strategy

| Breakpoint | Layout | Thành phần chính |
|---|---|---|
| ≥1200 (desktop) | Fixed sidebar (collapsible) + topbar + content | AG Grid full, multi-column form, MDI-like tabs |
| 768–1199 (tablet) | Collapsible sidebar (overlay), 2-col form | Table responsive, filter drawer |
| <768 (mobile) | Topbar + Drawer menu + Bottom Navigation (Trang chủ · Bán · Kho · 🔔 · Tài khoản) | Product cards, cart bottom-sheet, sticky action bar, accordion thay table |

Quy tắc:
- Mọi màn hình danh sách có 2 render mode: `grid` (desktop/tablet) và `cards` (mobile) — chọn theo breakpoint, không thu nhỏ table.
- Màn hình Bán lẻ: desktop = table + panel tổng; mobile = Search → Product cards → Cart (bottom drawer) → Summary → Thanh toán sticky.
- Giữ phím tắt desktop (F3/F4/F5/F7/F8/F9/F10/F11/F12/Insert/Del/Ctrl+M/K/T/P/Q…); mobile không phụ thuộc phím tắt.
- Bottom navigation chỉ 5 mục cố định: Trang chủ, Bán hàng, Kho, Thông báo, Tài khoản.

## 6. Design System (mục 45)

- Phong cách: Modern Business ERP — clean, dense-but-readable, Vietnamese-first.
- Màu: Red (primary, kế thừa thương hiệu cũ) / White / Neutral gray / Dark text. Không lạm dụng gradient.
- Button: Primary / Secondary / Danger / Success / Warning. Icon có ý nghĩa (AntD).
- AntD theme token override trong `app/theme.ts`; dark text `#1f1f1f`, primary `#cf1322`.
- Ngày/tiền: dayjs locale `vi`, tiền format `vi-VN` (`1.234.567 ₫`), DECIMAL ở backend.
                         │  SignalR hub                 │
                         └──────────────┬───────────────┘
                          ┌─────────────┴───────────────┐
                          ▼                             ▼
              ┌─────────────────────┐       ┌─────────────────────┐
              │  React Web          │       │  SMTP Provider      │
              │  Desktop/Tablet/    │       │  (Email Queue →     │
              │  Mobile             │       │   retry backoff)    │
              └─────────────────────┘       └─────────────────────┘
```

## 2. Component Diagram (backend)

```text
TechPos.Api  ─────────────► TechPos.Application ───► TechPos.Domain
     │                              │
     │                              ├─► TechPos.Data.MySql      (Dapper + MySqlConnector)
     │                              └─► TechPos.Reporting       (QuestPDF / ClosedXML / ZXing)
     │
     ├── TechPos.Shared          (DTO contracts, enums, ApiResult, constants, settings)
     └── SignalR NotificationHub

TechPos.Sync.Service ─► TechPos.Sync.Core ─┬─► TechPos.Data.Firebird (FbConnection, ADO 2.5.x)
                                           ├─► TechPos.Data.MySql    (Dapper, mirror upsert)
                                           └─► TechPos.Shared
```

- **TechPos.Domain**: Entity thuần (mirror entities + web entities), enum, quy tắc tiền/tồn/công nợ.
- **TechPos.Application**: Use-case services (Bán lẻ, Kho, Quỹ, Công nợ…), FluentValidation, KHÔNG có SQL.
- **TechPos.Data.MySql**: Repository + SQL, migration runner. Không business logic.
- **TechPos.Data.Firebird**: chỉ SELECT; chạy cùng máy với FDB (Embedded), connection read-only.
- **TechPos.Reporting**: export PDF/Excel/CSV/barcode; nhận DTO, không truy DB trực tiếp.
- **TechPos.Api**: controller mỏng → Application service; central error handling; audit filter.