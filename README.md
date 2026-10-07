# TechPosWeb — Web Application cho TechPos SM4 v4.5

Web ERP quản lý bán hàng / kho / quỹ / công nợ / nhân sự / báo cáo, thay thế & song song với
desktop TechPos SM4 v4.5 (Firebird 2.5 Embedded) **không sửa mã nguồn desktop**.

```text
Firebird 2.5 Embedded (DEMO.FDB)
        ↓  (READ ONLY)
   Sync Service (.NET 10 Windows Service, Watermark + Reconcile)
        ↓
     MySQL 8 (mirror + W_* web tables)
        ↓
   ASP.NET Core Web API (.NET 10, JWT, RBAC, Audit, Email, Notification)
        ↓
    React Web Application (Vite + TypeScript + Ant Design + AG Grid)
```

## Cấu trúc dự án

```text
HoanNgocWeb/
├── backend/                     ← Backend (.NET 10)
│   ├── TechPosWeb.slnx              Solution (9 src + 1 test + 4 tools)
│   ├── src/                         TechPos.Api · Application · Domain ·
│   │                                Data.Firebird · Data.MySql · Sync.Core ·
│   │                                Sync.Service · Reporting · Shared
│   ├── tests/                       TechPos.UnitTests
│   └── tools/                       AdminSeed · DataCompare · FirebirdExtractor · SchemaGenerator
├── frontend/                    ← Frontend (React + Vite + TS + AntD + AG Grid)
│   └── techpos-web/
├── database/                    ← migrations/ + seed/ (MySQL)
├── deploy/                      ← powershell/ + RUNBOOK.md
├── docs/                        ← tài liệu kiến trúc / db / sync / api / email / testing
└── Giao diện HN/                ← ảnh tham chiếu UI desktop
```

## Trạng thái dự án

| Phase | Nội dung | Trạng thái |
|---|---|---|
| 0 | DB discovery Firebird (RDB$RELATIONS) + bộ thiết kế | ✅ **Xong** — 86 bảng thật trích từ DEMO.FDB (`docs/database/discovery/`), findings: `docs/database/PHASE0-FINDINGS.md` |
| 1 | Solution + MySQL foundation + Backend + Frontend skeleton | ✅ **Xong** — build ✓ test ✓ web build ✓ MySQL `techposweb` (86 mirror + 31 W_ tables) ✓ login JWT ✓ |
| 2 | Sync Service (Watermark, Reconcile, Row Error, Tombstone) | ✅ **Chạy thật toàn diện** — Watermark incremental + Reconcile (W2) + Media/BLOB extractor + Row Error Dead Letter + heartbeat. Log W_SYNC_LOG: 6.245 incremental · 86 reconcile · 12 media · **0 errors** |
| 3 | Auth, RBAC, Dashboard | ✅ **Đóng khoảng trống** — Dashboard & Sync Admin UI gọi API thật (số liệu từ mirror MySQL); JWT + RBAC (9 roles/34 perms) hoạt động |
| 4 | Danh mục, Bán hàng, Kho | ✅ **Hoàn tất (30/09/2026)** — CRUD 4 danh mục; **bán lẻ (4.1) · bán sỉ (4.2) · trả lại KH (4.3) · nhập kho (4.4) · xuất kho + xuất trả NCC (4.5) · chuyển kho (4.6) · kiểm kê (4.7) · tồn tất cả các kho + tính lại giá vốn FIFO/BQ liên hoàn/BQ cuối kỳ (4.8) · báo giá/đặt hàng/đặt mua NCC/hợp đồng (4.9) · giao COD/giao xe (4.10)** đủ DB→Repo→Service→API→UI; 56 perms. Riêng **Sản xuất** chờ flag `ENABLE_PRODUCTION` (không có trong 86 bảng DEMO) |
| 5 | Quỹ, Công nợ, Nhân sự | ✅ **Hoàn tất (30/09/2026)** — 5.1 Quỹ (phiếu thu PT/chi PC + phân bổ trả nợ + tồn quỹ) · 5.2 Công nợ KH/NCC · **5.3 Nhân sự: danh mục NV (CRUD) · giấy giới thiệu (GS) · tạm ứng lương (TTHUCHI.LATAMUNG) · thưởng phạt (TTHUONGPHAT) · chấm công (TBANGLUONGCHITIET, chống trùng) · tính lương tháng (TBANGLUONG+TONGHOP, chặn trùng tháng)** — 13 perms HR, migration 012 |
| 6 | Reports, PDF, Excel, Barcode | ✅ **Hoàn tất (03/10/2026)** — Report Center **10 báo cáo** (bán hàng ngày/mặt hàng/thu chi ngày/thu chi lý do/XNT/tồn kho/**công nợ KH**/**công nợ NCC**/**tồn quỹ luỹ kế**/**KQKD**) + Excel export 13 nguồn; **PDF: hoá đơn A5 · phiếu thu/chi A5 · phiếu kho A5 (nhập/xuất/chuyển/kiểm kê/trả NCC — `StockVoucherPdfBuilder`) · bảng lương A4 (`PayrollPdfBuilder`)**; mã vạch CODE_128 PNG. Verify thật: PDF phiếu kho KTL26/00001 (41.7 KB) + bảng lương (35.7 KB) + Excel tồn quỹ OK |
| 7 | Email, Notification, SignalR | ✅ **Hoàn tất (04/10/2026)** — **EmailQueue fire-and-forget** (lỗi SMTP không fail transaction — mục 23) + `EmailWorker` BackgroundService poll 20s, **retry backoff 1/5/15/30 phút tối đa 4 lần**; 19 email events + 19 templates seed (migration 014); **SignalR `NotificationHub`** `/hubs/notifications` (JWT qua query string) + Notification Center API/UI (badge unread poll 30s) + trang `/emails` Admin (queue retry/cancel, templates editor, events toggle, cấu hình SMTP + Test Email). Verify thật: templates=19 · unread-count/mark-read/mark-all OK · negotiate hub 200 · worker sống sau SMTP fail · health UP |
| 8 | Testing (Unit/Integration/Parity/E2E) | 🟡 Unit **107/107 xanh** (render template/escape XSS/SMTP fail-fast) + **parity check tự động** (`TechPos.DataCompare`) — còn thiếu E2E Playwright + load test |
| 9 | Deployment Windows (IIS/Kestrel, Windows Service, không Docker) | ✅ **Deploy đủ (04/10/2026)** — `setup-database.ps1` · `install-sync-service.ps1` (x86) · `install-web-api.ps1` · `backup-mysql.ps1` (verify dump 12.1 MB) + **`deploy/RUNBOOK.md`** (cài đặt/backup/restore/monitoring/sự cố/upgrade/rollback); publish win-x64/x86 verify OK |

### ✅ Kết quả phiên 04/10/2026 (bổ sung)

```text
✅ PARITY 04/10                     → FB-origin mirror khớp 100% (86 bảng, TDONHANG 44.560.845 đ = 0 lệch);
                                      14 bảng "dư" đều là dữ liệu WEB-origin (đúng ADR-02 GĐ0–6)
✅ GĐ7 HẠ TẦNG 2 CHIỀU              → migration 015: W_OUTBOX + W_CONFLICT + W_SYNC_OUTBOX_WHITELIST;
                                      TwoWaySyncEngine trong SyncWorker (no-op khi ENABLE_TWO_WAY_SYNC=false —
                                      quy tắc vàng 6); conflict so TIMEMODIFIED giữ bản FB (§7.3);
                                      API /api/sync/outbox + /conflicts + /conflicts/{id}/resolve (FB|WEB)
✅ Phase 6.6 LỊCH NHẮC TỰ ĐỘNG      → ScheduledJobsWorker (30 phút): scan hoá đơn nợ đến hạn/quá hạn
                                      (NGAY + SONGAYCHONO) → W_NOTIFICATION nhóm DEBT + deep-link /debt —
                                      verify thật: tạo notification "Nợ quá hạn: Anh Ba — 500.000 đ" đúng
✅ Phase 9 DEPLOY ĐỦ                → RUNBOOK.md (cài đặt/backup-restore/monitoring/sự cố/upgrade/rollback)
                                      + backup-mysql.ps1 (dump+zip+dọn cũ — verify dump 12.1 MB OK)
                                      + install-web-api.ps1 (Windows Service)
```

```text
✅ dotnet build TechPosWeb.slnx        → Build succeeded, 0 error, exit=0
✅ dotnet test (TechPos.UnitTests)     → 7/7 Passed, exit=0
✅ npm run build (techpos-web)         → built, dist/index.html (Dashboard + Sync Admin thật)
✅ TechPos.FirebirdExtractor           → 86 bảng thật + 86 file JSON discovery (READ-ONLY)
✅ GET /api/health                     → {"success":true,"data":{"status":"UP",...}}
✅ POST /api/auth/login (admin)        → success=true, roles=SUPER_ADMIN, perms=34, JWT 1492 ký tự
✅ MySQL techposweb                    → 86 bảng mirror + 33 bảng W_* + seed RBAC (0 error)
✅ TechPos.SchemaGenerator             → 002_mirror_schema.sql (86 bảng, DDL sinh từ discovery)
✅ Sync Service chạy thật              → 86 bảng sync Firebird → MySQL, 0 row error
✅ W_SYNC_LOG (sync thật)              → 6.245 incremental · 86 reconcile · 12 media · 0 errors
✅ GET /api/dashboard/summary          → counts/cash/stock/docTypes THẬT từ mirror MySQL
✅ GET /api/sync/status                → heartbeat RUNNING + 30 cursors + 20 logs + row-errors
✅ POST /api/sync/reconcile            → enqueue RECONCILE_ALL OK
✅ Migration idempotent               → 004_media_column.sql apply OK (sửa lỗi Duplicate column)
✅ ⭐ PARITY FIREBIRD ↔ MYSQL          → "Tổng lệch: 0" (COUNT 86/86 bảng)
✅ ⭐ PARITY TIỀN (TDONHANG)           → FB 44.560.845 = MySQL 44.560.845.00 (0 đồng lệch)
✅ Query xác minh TDONHANG             → LOAI=1/PN=Nhập kho, LOAI=5/KTL=Kiểm kê, MODE=-1
✅ Query xác minh STATUS               → 30 = hoạt động (khớp hằng số WebProcessor)
✅ ⭐ CRUD DANH MỤC (28/09/2026)       → CREATE/UPDATE/DELETE 4 danh mục OK trên API thật:
  DMATHANG/DKHACHHANG/DNHACUNGCAP/DKHOHANG ghi W_ORIGIN='WEB', soft-delete,
  audit 12 thao tác vào W_AUDIT_LOG, trùng mã chặn 409, self-parent chặn 400,
  VIEWER bị chặn 403 khi ghi — SUPER_ADMIN/ADMIN đủ quyền (44 perms)
✅ ⭐ CHỨNG TỪ PHASE 4 (30/09/2026)    → Bán sỉ (BS), Trả lại KH (TL), Xuất kho (XK),
  Chuyển kho (CK), Xuất trả NCC (XT): mỗi loại ghi TDONHANG+CHITIET+W_KHOLEDGER+OTONKHO
  trong 1 transaction (W_ORIGIN='WEB'), kiểm tra đủ tồn trước khi xuất; build ✓ unit 60/60 ✓ web build ✓
  ⚠ W_DOC_TYPE_MAP mới seed VERIFIED=0 (chờ xác minh LOAI/MODE production — quy tắc vàng 5)
✅ ⭐ 4.9–4.10 (30/09/2026)             → Báo giá (BG), Đặt hàng KH (DH), Đặt mua NCC (DN),
  Hợp đồng (HD), Gửi COD (COD), Kết quả COD (KQ), Giao xe (XE): bảng riêng
  (TBAOGIA/TDATHANG/TDATHANGNCC/THOPDONG/TGUIHANGCOD/TKETQUAGUICOD/TGIAOHANGTHEOXE),
  W_ORIGIN='WEB', HEAD+CHI TIẾT trong 1 transaction; lookup ĐV chuyển phát/xe/đơn web
✅ ⭐ 4.8 (30/09/2026)                   → Tồn tất cả các kho (ma trận MH×kho, GET /api/inventory/all-warehouses)
  + Tính lại giá vốn FIFO/BQ liên hoàn/BQ cuối kỳ (POST /api/inventory/cost-recalc):
  ghi W_KHOLEDGER.GIA_VON (mọi dòng xuất) + TDONHANGCHITIET.GIAVON (chỉ dòng web),
  Đích danh chặn rõ (chưa hỗ trợ web); CostCalculator thuần + 11 unit test
✅ ⭐ 5.1–5.2 (30/09/2026)               → Quỹ: phiếu thu PT/chi PC (4 tổ hợp THU/CHI×KH/NCC qua W_DOC_TYPE_MAP,
  phân bổ trả nợ TCHITIETTHANHTOAN chỉ trên chứng từ WEB, tồn quỹ tách tiền mặt/chuyển khoản) +
  Công nợ KH (SUM CONNO) / NCC (SUM CONLAI); UI CashbookPage + DebtsPage
✅ ⭐ 5.3 NHÂN SỰ (30/09/2026)           → CRUD nhân viên (CACHTINHLUONG 0=ca/1=tháng), giấy giới thiệu GSyy/xxxxx,
  tạm ứng lương = phiếu chi PC LATAMUNG=1, thưởng phạt (chặn cả thưởng+phạt cùng lúc),
  chấm công TBANGLUONGCHITIET (chặn trùng NV+ngày+ca, chặn ngày tương lai),
  tính lương tháng BLyy/xxxxx: LUONGCA=Σ(SL công×TILELUONG/100×LUONGCA NV) + THUONG + PHUCAP
  − PHAT − TAMUNG; 1 bảng/tháng (409 nếu trùng); TILELUONG lưu dạng % (100=nguyên ca) —
  đã verify trên mirror DEMO (Ca nửa ngày=50, Hành chính=100)
✅ FIX Phase 5.3 (30/09/2026)           → GetPayrollAsync fail "Cannot perform runtime binding on a null
  reference" (DapperRow dynamic không có CLR property) — thay bằng typed DTO PayrollHeadRow;
  TILELUONG nhân thẳng 100 lần (6.000.000 thay vì 60.000) — chia 100 trong SQL + calculator
✅ FIX bug thẻ kho (28/09 log)          → GET /api/inventory/ledger từng fail
  "Unknown column 'l.ENTRY_AUTO'" (cột không tồn tại trong W_KHOLEDGER) — đã sửa ORDER BY
```

### Quyết định kỹ thuật đã chốt khi chạy thật

1. **fbembed.dll 32-bit** ⇒ `TechPos.Sync.Service`, `TechPos.FirebirdExtractor`, `TechPos.DataCompare` build `PlatformTarget=x86`.
2. **MySqlConnector `GuidFormat=None`** trong connection string — giữ ID là `string` (GUID-as-text), tránh lỗi convert `Guid` ↔ `string` của Dapper.
3. **Không mở DB gốc trong Program Files** (ACL chống ghi, Embedded yêu cầu read-write) ⇒ sync đọc **bản copy** `.tmp-fb\DEMO.FDB`.
4. Sync policy phủ **tất cả 86 bảng**: TRANSACTION 30s · DICTIONARY 120s · OTHER 600s.
5. `W_USER_SCOPE.SCOPE_ID` NOT NULL DEFAULT `''` (không PK cột NULL); chữ hoa cho tên bảng mirror.

### Việc cần làm tiếp (tới cắt chuyển — GĐ7–8)

> **Trạng thái:** Phase 0–7 + 9 đã hoàn tất và verify trên DEMO. Phần còn lại là những việc
> **bắt buộc phải có DB production / sự xác nhận của hãng / người dùng thật** — không thể làm trên DEMO.

1. **Xác minh `W_DOC_TYPE_MAP` trên DB production** rồi `UPDATE ... SET VERIFIED=1` cho BAN_LE/BAN_SI/TRA_LAI_KH/XUAT_KHO/CHUYEN_KHO/XUAT_TRA_NCC + 4 tổ hợp phiếu thu/chi — hiện seed VERIFIED=0 nên API ghi TDONHANG/TTHUCHI còn chặn (quy tắc vàng 5). Báo giá/đặt hàng/hợp đồng/COD/NHÂN SỰ dùng bảng riêng nên đã ghi được ngay.
2. **GĐ7 bật 2 chiều** (khi hãng đồng ý ghi Firebird): `ENABLE_TWO_WAY_SYNC=true` + `INSERT W_SYNC_OUTBOX_WHITELIST` (danh mục, `SQUICKNOTE`, nhắc nợ) — hạ tầng đã sẵn sàng (migration 015 + TwoWaySyncEngine); chạy thử nghiệm 20 tình huống xung đột (§7.4) rồi mới mở.
3. **Sản xuất** (nếu bật `ENABLE_PRODUCTION`): verify bảng `TSANXUAT/TSANXUATCHITIET` trên DB production — không có trong 86 bảng DEMO.
4. **Phase 5.3 chú ý production**: kiểm tra tập giá trị `TBANGLUONGCHITIET.TRANGTHAI` và `DNHANVIEN.CACHTINHLUONG` (DEMO có giá trị lạ 30/60 — quy tắc vàng 5).
5. **Phase 8 còn lại**: E2E Playwright (login → bán lẻ → in hoá đơn → tồn kho), load test (k6/NBomber), **parity 6 báo cáo = 0 lệch liên tục 14 ngày** trên DB production trước cutover (điều kiện ADR-12).
6. **UAT + đào tạo** theo GĐ8: đào tạo theo vai trò, tài liệu thao tác, hotline 2 tuần, chốt ngày cắt chuyển.

## Tài liệu

| Tài liệu | Vị trí |
|---|---|
| Kiến trúc tổng thể + component + folder structure + responsive | `docs/architecture/ARCHITECTURE.md` |
| Database strategy + ERD dự kiến + quy tắc MySQL | `docs/database/DATABASE-STRATEGY.md` |
| Sync Firebird→MySQL (watermark, reconcile, error) | `docs/sync/SYNC-STRATEGY.md` |
| API structure + response contract | `docs/api/API-STRUCTURE.md` |
| Frontend routing + responsive + design system | `docs/architecture/FRONTEND-STRUCTURE.md` |
| Email + Notification architecture | `docs/email/EMAIL-ARCHITECTURE.md` |
| RBAC + Authentication | `docs/architecture/RBAC-DESIGN.md` |
| Roadmap | `docs/ROADMAP.md` |
| Risk list | `docs/RISKS.md` |
| Testing strategy | `docs/testing/TESTING-STRATEGY.md` |

## Nguồn tham khảo nghiệp vụ (trong repo này)

- `DANH-MUC-CHUC-NANG.md` — 10 nhóm menu / 76 mục / ~41 màn hình
- `DOI-CHIEU-CSDL-VA-CHUC-NANG.md` — đối chiếu 86 bảng Firebird ↔ chức năng
- `TechPos_SM4_Database_Structure.md` — cấu trúc 86 bảng (tham khảo, **không phải chân lý cuối**)
- `TU-DIEN-DU-LIEU-TECHPOS-SM4.md` — 581 cột / 67 bảng + nhãn tiếng Việt từ cache metadata
- `KHAO-SAT-THU-MUC-CAI-DAT-TECHPOS-SM4.md` — khảo sát thư mục cài đặt, rủi ro
- `KE-HOACH-TRIEN-KHAI-WEB-MYSQL-SYNC.md` — kế hoạch tổng thể v1.0

## Quy tắc vàng

1. **Không sửa desktop / Firebird.** Sync chỉ READ.
2. **Không sinh schema MySQL từ tài liệu.** Phải chạy `RDB$RELATIONS` / `RDB$RELATION_FIELDS` / `RDB$FIELDS` trên DB thật trước (tool `tools/TechPos.FirebirdExtractor`).
3. Bảng mirror giữ nguyên tên + tên cột Firebird (UPPERCASE); bảng web-only prefix `W_`.
4. Tiền = DECIMAL, không FLOAT/DOUBLE.
5. `TDONHANG` là bảng đa năng — không hard-code giá trị `LOAI`/`MODE` chưa xác minh.
6. `ENABLE_TWO_WAY_SYNC = false` mặc định; Firebird = MASTER, MySQL = MIRROR ở giai đoạn đầu.
7. Email/Notification chạy qua queue, lỗi email không làm fail transaction nghiệp vụ.