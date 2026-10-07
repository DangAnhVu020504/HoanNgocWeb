# KẾ HOẠCH TRIỂN KHAI BẢN WEB (MySQL) + SERVICE ĐỒNG BỘ FIREBIRD ↔ MYSQL

> Phiên bản 1.0 — 22/09/2026
> Nguồn: khảo sát trực tiếp máy `ANH-VU-HANSOME` + 3 tài liệu trong repo (`DANH-MUC-CHUC-NANG.md`, `DOI-CHIEU-CSDL-VA-CHUC-NANG.md`, `TechPos_SM4_Database_Structure.md`).
> Mục tiêu: website đầy đủ chức năng tương đương **TechPos SM4 v4.5** dùng **MySQL 8.0**, kèm **service đồng bộ Firebird → MySQL** để desktop và web chạy song song, số liệu khớp 100%.

---

## 0. HIỆN TRẠNG & BẰNG CHỨNG KHẢO SÁT (đã kiểm tra thực tế)

| Hạng mục | Bằng chứng | Ý nghĩa cho kế hoạch |
|---|---|---|
| Ứng dụng desktop | `C:\Program Files (x86)\TechPos\SM4\v4.5\Application.exe` (~32 KB) + ~40 DLL .NET (`No1Lib.*`, `DbMapping.DLL`, `ComponentFactory.Krypton.*`) | App .NET WinForms, engine low-code "No1Lib", UI MDI + tab |
| Engine CSDL | `fbembed.dll` = **WI-V2.5.5.26952** + `icudt30/icuin30/icuuc30.dll` (ICU 3.0) | **Firebird 2.5 Embedded, ODS 11.2** ⇒ FB 3/4/5 KHÔNG mở được; phải dùng client/engine dòng 2.5 |
| Provider .NET của app | `FirebirdSql.Data.FirebirdClient.DLL` v**2.5.2.0** | Sync service nên dùng cùng dòng provider để chắc chắn tương thích ODS 11.2 |
| CSDL nghiệp vụ | `Database\DEMO.FDB` = **19.5 MB** | Dữ liệu nhỏ ⇒ nạp đầu kỳ nhanh, có thể full-load mỗi đêm |
| CSDL mẫu | `Data\TEMPLATE.FDB` | DB trắng để test tự động, không đụng dữ liệu thật |
| Sao lưu | `Data\Backup\bak_09_15_2026.gbk`, `bak_09_16_2026.gbk` (~9 MB, gbak) | Đã có backup tự động + là **nguồn dự phòng cho phương án replica** |
| Cấu hình kết nối | `Data\data.dat`: `KhachHang=demo`, `MaDangKy=650342159-60317`, `Module=Billiards`, **`KetNoiMayChu=false`**, `Port=8080`; `Data\conn.dat` | App **hỗ trợ 2 chế độ: Embedded (file) và kết nối máy chủ** ⇒ chuyển sang Firebird Server không cần sửa app |
| Bản quyền | `Data\BanQuyen.dat`, `TrangThai=Thành công, 123.24.152.19` | Bản quyền kích hoạt online, có rủi ro hết hạn/đổi máy ⇒ phải chốt quyền sử dụng với hãng |
| Module web của hãng | `WebService.exe`, `WebProcessor.EXE`, `res\{ui,js,css,img}`, `No1Lib.Sys.Ts.DLL`; schema có `SMENU.VIEWONWEB/VIEWONMOBI`, `SGRID.MOBILE` | Hãng **đã có kiến trúc web/mobile** ⇒ phải so sánh "mua module web của hãng" vs "tự viết" |
| Báo cáo / Excel / tem | `FastReport*.DLL`, `Aspose.Cells.DLL`, `Data\Bartend\*.btw` + `BartenderPrint.exe` | Bản web cần thay tương đương: PDF/Excel in tem mã vạch |
| Sản phẩm anh em | `C:\Program Files (x86)\HOANNGOC\SHOP\v5.0` (`BanQuyen.dat`) | Có thể tham chiếu nghiệp vụ/kiến trúc bản v5.0 |
| Máy dev | .NET SDK **10.0.300**; Node **v25.4.0** / npm 11.7.0; Python 3.10.11; git 2.52; **MySQL Server 8.0.45** (service `MySQL80` đang chạy) + Workbench/Shell/Router | Stack khả dụng: .NET 10 + MySQL 8. **Không có Docker**, không có Firebird Server |
| Tài liệu nghiệp vụ | 51 ảnh chụp + `DANH-MUC-CHUC-NANG.md` (10 nhóm / **76 mục menu** / ~41 màn hình); `DOI-CHIEU-CSDL-VA-CHUC-NANG.md` (**86 bảng**, 12 mục thiếu, hạ tầng thừa) | Ranh giới phạm vi và danh sách việc cần bổ sung đã rõ |

**Kết luận khảo sát:** có **3 ràng buộc cứng** chi phối toàn bộ kế hoạch:
1. **FB 2.5 Embedded** — mọi kết nối phải qua *đường dẫn file cục bộ* (không TCP), và **bỏ qua `security2.fdb`** ⇒ bảo vệ dữ liệu gốc phải bằng **quyền NTFS read-only**, không bằng tài khoản DB.
2. **Không có CDC/Change Views** (chỉ từ FB 4.0) ⇒ phải tự xây cơ chế phát hiện thay đổi (watermark + đối chiếu định kỳ, hoặc trigger ghi log).
3. **Không sửa được app desktop** (là sản phẩm đóng) ⇒ mọi thay đổi phải nằm ngoài: MySQL, sync service, web app.

---

## 1. MỤC TIÊU — PHẠM VI — NGUYÊN TẮC

### 1.1 Mục tiêu
| # | Mục tiêu | Chỉ số đo được |
|---|---|---|
| M1 | Web hoá **toàn bộ 76 mục menu / ~41 màn hình** của SM4 v4.5 | Checklist 76/76 mục chạy được trên web (kiểm tra bằng UAT) |
| M2 | **Số liệu khớp 100%** với desktop trong giai đoạn chạy song song | 6 báo cáo đối chiếu lệch **0 đồng / 0 dòng** |
| M3 | Đồng bộ gần thời gian thực | Giao dịch mới xuất hiện trên web ≤ **30 giây**; danh mục ≤ **2 phút** |
| M4 | Dữ liệu tập trung, không còn phụ thuộc 1 máy | MySQL trên server nội bộ + backup hằng đêm, RPO ≤ 15 phút |
| M5 | Bổ sung 12 chức năng mà bản desktop còn thiếu bảng | 18 hạng mục ở mục 3 của `DOI-CHIEU-CSDL-VA-CHUC-NANG.md` được đưa vào web |
| M6 | Bảo mật & phân quyền tốt hơn bản cũ | JWT + hash mật khẩu + RBAC + audit log từng thao tác |
| M7 | Truy cập đa thiết bị | Chạy tốt trên Chrome/Edge, tablet (bán hàng tại quầy), responsive |

### 1.2 Trong phạm vi (In-scope)
- 10 nhóm chức năng: HỆ THỐNG, BÁN HÀNG (16), CHĂM SÓC KH (4), KHO HÀNG (12), QUỸ (8), CÔNG NỢ (3), NHÂN SỰ (6), QUẢN TRỊ (9), BÁO CÁO (9 nhóm), TRỢ GIÚP.
- Dashboard 9 khối / 41 lối vào nhanh.
- In ấn: hoá đơn, phiếu nhập/xuất/chuyển/kiểm kê, phiếu thu/chi, bảng lương, tem mã vạch, 6+ báo cáo bản in.
- Đồng bộ dữ liệu 1 chiều Firebird → MySQL (và 2 chiều có kiểm soát ở giai đoạn 9, tuỳ chọn).

### 1.3 Ngoài phạm vi (Out-of-scope — cố ý bỏ)
| Bỏ | Lý do | Thay thế trên web |
|---|---|---|
| Engine low-code (`STABLEDESC`, `SFORM`, `SGRID`, `SCOLUMN`, `SMENU` cấu hình động, code sinh động trong BLOB) | Không port được, là hạ tầng của bản desktop | Route/component viết tay (~41 màn hình) |
| "Công cụ nhà phát triển" (sửa form/grid/SQL ngay trong app) | Rủi ro cao, không cần cho người dùng cuối | Trang Admin cấu hình giới hạn |
| "Tạo mới/Phục hồi CSDL" bằng gbak từ UI | Web không quản lý file .FDB | Script vận hành trên server (mục 8) |
| Trình thiết kế báo cáo FastReport (`Thiết kế mẫu`) | Phức tạp, ít dùng | Quản lý mẫu in dạng template có sẵn (Word/HTML/PDF) |
| "Xem dữ liệu thô" mở cho mọi user | Rủi ro bảo mật | Chỉ vai trò Admin/IT, có log |

### 1.4 Nguyên tắc thiết kế (bất biến trong suốt dự án)
1. **Không sửa DB gốc trong GĐ0–GĐ6**: sync service chỉ **đọc** `DEMO.FDB`; bảo vệ bằng quyền NTFS read-only của tài khoản service (vì FB Embedded bỏ qua `security2.fdb`).
2. **Một nguồn sự thật tại mỗi thời điểm**: GĐ0–GĐ8 = Firebird là master (web chỉ đọc chứng từ); GĐ9 mới bàn giao master cho MySQL.
3. **Không có "im lặng sai số"**: mọi mẻ đồng bộ đều ghi log + có job đối chiếu đêm; lệch ⇒ cảnh báo, chặn phát hành.
4. **Bảng nguồn giữ nguyên tên & cột** (UPPER) để SQL/đối chiếu 1-1 với Firebird; chỉ bảng mới của web dùng tiền tố `W_`.
5. **Tiền tệ dùng `DECIMAL` đúng scale gốc**, không dùng `FLOAT`; mọi so sánh tiền dùng exact match.
6. **Tiếng Việt UTF-8 (`utf8mb4`)** toàn hệ thống, kiểm thử bắt buộc 100% chuỗi có dấu khớp bản cũ.
7. **Mọi bảng web đều soft-delete + audit** (thay `STRACKING` chỉ ghi mức bản ghi).
8. **Ứng dụng chạy nội bộ LAN, HTTPS**, không expose ra Internet (trừ khi mở API cho app mobile sau này).

---

## 2. KIẾN TRÚC TỔNG THỂ

### 2.1 Sơ đồ khối
```
┌──────────────────────── MÁY QUẦY (desktop hiện hữu) ─────────────────────────┐
│  Application.exe (WinForms, No1Lib)                                          │
│        │ (Firebird local protocol = đường dẫn file, KHÔNG TCP)                │
│        ▼                                                                     │
│  DEMO.FDB  (Firebird 2.5.5 Embedded / ODS 11.2, 19.5 MB)                     │
│        ▲                                                                     │
│        │ đọc READ-ONLY (fbembed.dll 2.5.5 + ICU 3.0, NTFS read-only)         │
└────────┼─────────────────────────────────────────────────────────────────────┘
         │
┌────────┴───────────── MÁY CHỦ NỘI BỘ (Windows Server / Win 11 Pro) ──────────┐
│  ┌──────────────────────────────┐        ┌────────────────────────────────┐  │
│  │ SyncService (Windows Service)│        │  MySQL 8.0.45 (service MySQL80)│  │
│  │ .NET 10 Worker               │──upsert▶│  utf8mb4, InnoDB, binlog ON    │  │
│  │  - watermark incremental     │        │  86 bảng gốc + ~20 bảng mới    │  │
│  │  - reconcile 02:00 hằng đêm  │◀─cursor─│  bảng điều khiển SYNC_*        │  │
│  │  - healthcheck /metrics      │        └───────────┬────────────────────┘  │
│  └──────────────────────────────┘                    │ Dapper/EF Core        │
│                                                      ▼                       │
│  ┌───────────────────────────────────────────────────────────────────────┐   │
│  │ WebApi (ASP.NET Core 10, REST /api/v1, JWT, RBAC, SignalR)            │   │
│  │  + ReportingEngine (QuestPDF/FastReport .NET)  + Excel (ClosedXML)    │   │
│  └───────────────────────────────┬───────────────────────────────────────┘   │
│                                  │ HTTPS 443                                 │
│  ┌───────────────────────────────▼───────────────────────────────────────┐   │
│  │ Web SPA (React 19 + Vite + TS + Ant Design) — 41 màn hình, MDI→Tab    │   │
│  └───────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────────┘
```


### 2.2 Thành phần & công nghệ

| # | Thành phần | Công nghệ chọn | Ghi chú |
|---|---|---|---|
| C1 | Sync Service | .NET 10 Worker Service + `FirebirdSql.Data.FirebirdClient` (dòng 2.5.x) + `MySqlConnector` | Đăng ký Windows Service, tự khởi động cùng máy |
| C2 | Firebird client bundle | `fbembed.dll` 2.5.5.26952 + `icudt30/icuin30/icuuc30.dll` + `firebird.msg/conf` | Bắt buộc đúng dòng 2.5 (ODS 11.2) |
| C3 | CSDL đích | **MySQL 8.0.45** (đã cài) InnoDB, `utf8mb4_0900_ai_ci` | Bind `127.0.0.1`, binlog bật cho PITR |
| C4 | Web API | ASP.NET Core 10 Minimal API + Dapper (truy vấn nóng) + EF Core (CRUD danh mục) | Versioning `/api/v1`, JSON camelCase |
| C5 | Web UI | React 19 + TypeScript + Vite + Ant Design v5 + TanStack Query/Table + AG Grid (lưới lớn) | Ant Design hợp UI nghiệp vụ VN, có sẵn Table/Form/DatePicker VN |
| C6 | Xác thực | JWT access (15 phút) + refresh token (7 ngày, xoay vòng) + BCrypt/Argon2id | Không sync mật khẩu cũ |
| C7 | Báo cáo PDF | QuestPDF (thuần C#, không cần designer) hoặc FastReport .NET (đã quen từ bản cũ) | In hoá đơn A5/A4 + phiếu kho |
| C8 | Excel | ClosedXML (đọc/ghi .xlsx) | Thay Aspose.Cells |
| C9 | Mã vạch | ZXing.Net (sinh) + Bartender (nếu giữ máy in tem hiện tại) | In tem qua PDF/lệnh máy in |
| C10 | Realtime | SignalR (thông báo "có dữ liệu mới", nhắc nợ, COD) | Tùy chọn |
| C11 | Logging | Serilog → file + bảng `W_SYSLOG`; correlation id xuyên suốt | Thay `STRACKING` |
| C12 | Migration DB | DbUp (script SQL đánh số `0001_xxx.sql`) | Áp dụng cho staging/prod theo CI |

### 2.3 Luồng dữ liệu & độ trễ mục tiêu

| Luồng | Tần suất | Độ trễ mục tiêu | Cơ chế |
|---|---|---|---|
| Chứng từ mới (bán lẻ/sỉ, nhập/xuất, thu/chi) | 30 giây | ≤ 30 s | Watermark `TIMEMODIFIED/TIMECREATED` + trigger (nếu có) |
| Danh mục (mặt hàng, KH, NCC, kho…) | 2 phút | ≤ 2 phút | Watermark |
| Xóa cứng / sửa "lén" (ngoài app) | 02:00 hằng đêm | ≤ 24 h | Reconcile đối chiếu ID + checksum |
| Tồn kho, công nợ, tồn quỹ (bảng dẫn xuất) | 5 phút | ≤ 5 phút | Materialized view/tính lại trên MySQL |
| Báo cáo nặng (XNT, KQKD, công nợ tổng hợp) | Theo yêu cầu | ≤ 5 s (cache 60 s) | Bảng tổng hợp `W_RPT_*` refresh theo job |

### 2.4 Sơ đồ triển khai (topology tối thiểu)
| Máy | Vai trò | Cấu hình đề xuất |
|---|---|---|
| PC quầy (hiện hữu) | Chạy `Application.exe` + `DEMO.FDB` (hoặc chỉ `Application.exe` nếu nâng cấp lên Firebird Server) | Giữ nguyên, chỉ cấp thêm quyền NTFS cho tài khoản service đọc file |
| **Server nội bộ (mới)** | MySQL + SyncService + WebApi + IIS/Kestrel + lưu media | 4–8 core, 16–32 GB RAM, SSD 512 GB, Windows Server 2019/2022 (hoặc Win 11 Pro), UPS |
| Trạm làm việc (Thu ngân, Thủ kho, Kế toán, Chủ) | Trình duyệt Chrome/Edge (PC, tablet) | Mạng LAN 1 Gbps, có Wi-Fi |
| Ngoài văn phòng (chủ doanh nghiệp) | Xem báo cáo qua VPN/ZeroTier hoặc Cloudflare Tunnel + xác thực 2 lớp | Không mở trực tiếp cổng 443 ra Internet |

### 2.5 Danh mục tổ chức mã nguồn (đề xuất)
```
HoanNgocWeb/
├─ docs/                       # 3 tài liệu phân tích + kế hoạch này
├─ src/
│  ├─ Hn.Sync.Service/         # Windows Service đồng bộ (C1)
│  ├─ Hn.Sync.Core/            # engine sync, mapping, watermark, reconcile
│  ├─ Hn.Data.MySql/           # DbUp scripts (0001_init.sql…), repository Dapper
│  ├─ Hn.Api/                  # ASP.NET Core Web API (C4)
│  ├─ Hn.Reporting/            # QuestPDF/FastReport, Excel, barcode (C7-C9)
│  └─ Hn.Shared/               # DTO, enum, hằng số, helpers tiếng Việt
├─ web/                        # React + Vite + TS (C5)
├─ tools/
│  ├─ Hn.SchemaGen/            # sinh DDL MySQL từ metadata Firebird (RDB$)
│  ├─ Hn.DataCompare/          # công cụ đối chiếu số liệu FB ↔ MySQL
│  └─ Hn.FbExtract/            # export SREPORT/SMENU/SCOLUMN/SUSER → JSON/Excel
├─ infra/                      # script cài đặt, PowerShell deploy, backup, monitoring
└─ tests/                      # unit/integration/parity tests
```


---

## 3. CÁC QUYẾT ĐỊNH KIẾN TRÚC (ADR)

### ADR-01 — Cách Sync Service truy cập `DEMO.FDB` ⭐ (quan trọng nhất)
**Bối cảnh:** app dùng FB 2.5 **Embedded**; README Firebird 2.5 ghi rõ: *"The database file can be accessed by multiple client programs. The database consistency in this case is guaranteed internally (by the shared lock table)"* và *"Client access can be only via the local protocol, i.e. NOT a TCP/IP connection string that includes the server name localhost or IP address"*.

| Phương án | Ưu | Nhược | Đánh giá |
|---|---|---|---|
| **A. Embedded read-only song song** (service dùng chính `fbembed.dll` 2.5.5, mở bằng đường dẫn file, `ReadOnly=true` + NTFS read-only) | Không đụng gì tới app/hãng; triển khai trong 1 ngày; độ trễ thấp nhất | Phụ thuộc hành vi embedded; không dùng tài khoản/mật khẩu DB; nếu app khoá file ở chế độ độc quyền (ví dụ đang backup/sweep) có thể lỗi tạm thời | ✅ **CHỌN cho GĐ0–GĐ4** (kèm PoC bắt buộc) |
| **B. Nâng lên Firebird 2.5.9 Server (SuperServer/Classic)** trên cùng máy, app chuyển sang `KetNoiMayChu=true` (app đã hỗ trợ), service kết nối `localhost/3050` | Có xác thực DB + tài khoản read-only riêng; ổn định nhất; hỗ trợ nhiều client chuẩn | Phải cài FB Server 2.5.9 + đổi cấu hình app (cần hãng xác nhận); cần backup/restore để đổi ODS? (không bắt buộc, 2.5↔2.5 tương thích) | ✅ **CHỌN cho GĐ5 trở đi** (nếu hãng cho phép) |
| **C. Replica từ file backup `.gbk`** (gbak restore ra `SYNC.FDB` rồi đọc) | Rủi ro bằng 0 cho dữ liệu gốc; không cần quyền gì | Trễ tới 24 h; không đáp ứng M3 | ⚠️ **Chỉ dùng làm phương án dự phòng** (fallback tự động khi A/B lỗi) |
| D. Trigger + đọc bảng log, hoặc đọc trực tiếp bằng SQL `INSERT` từ app | Kiểm soát chặt | Đòi hỏi sửa DB/app của hãng | ❌ Không chọn |

**Quyết định:** triển khai **A** ngay (có PoC), chuẩn bị đường lui sang **B**, và **C** là chế độ "an toàn" tự động bật khi A/B lỗi liên tục.
**Hệ quả:** (1) sync service bắt buộc chạy **trên cùng máy** với file `DEMO.FDB` (hoặc truy cập qua share UNC cùng account — cần test); (2) mọi câu lệnh sync phải **chỉ SELECT**; (3) phải có healthcheck phát hiện lỗi mở file.

### ADR-02 — Chiều đồng bộ & quyền ghi
| Giai đoạn | Chiều | Web được ghi gì |
|---|---|---|
| GĐ0–GĐ6 | Firebird → MySQL (1 chiều) | **Không ghi nghiệp vụ**; chỉ ghi bảng web-only (`W_*`) + ghi chú/đánh dấu |
| GĐ7 | + MySQL → Firebird cho **một số bảng cho phép** (danh mục, ghi chú, `SQUICKNOTE`, khách hàng, nhắc nợ) | Danh mục do web tạo phải chèn được vào Firebird ⇒ cần quyền ghi (tài khoản NTFS read-write) + `ORIGIN='WEB'` |
| GĐ8–GĐ9 | Web trở thành master cho chứng từ mới; desktop chỉ tra cứu | Toàn quyền web; đóng băng desktop |

**Quyết định:** giữ **1 chiều** cho tới khi 6 báo cáo đối chiếu đạt 0 đồng lệch **liên tục 14 ngày**; sau đó mở ghi 2 chiều theo whitelist bảng.
**Hệ quả:** web phải hiển thị rõ "nguồn: desktop" và chặn nút Sửa/Xóa trên chứng từ gốc ở giai đoạn 1.

### ADR-03 — Phát hiện thay đổi (vì FB 2.5 không có Change Views)
| Cơ chế | Mô tả | Chọn |
|---|---|---|
| **W1. Watermark** | `WHERE TIMEMODIFIED > :cursor OR TIMECREATED > :cursor ORDER BY TIMEMODIFIED, ID` phân trang, lưu `(last_modified, last_id)` | ✅ Bắt buộc |
| **W2. Reconcile đêm** | Đối chiếu **tập ID + checksum theo lô** giữa FB và MySQL cho **cả 86 bảng** ⇒ phát hiện hard delete & bản ghi bị sửa mà không cập nhật `TIMEMODIFIED` (ví dụ sửa trực tiếp bằng SQL) | ✅ Bắt buộc |
| **W3. Trigger log** (`ZZB_SYNCLOG` ghi mọi INSERT/UPDATE/DELETE theo bảng, dạng append-only) | Chính xác tuyệt đối, phát hiện cả hard delete tức thời; chi phí: **phải sửa DB gốc** (tạo bảng + ~50 trigger) | ⏸️ Tùy chọn GĐ5, chỉ khi được phép và sau khi backup |
| W4. Theo dõi dung lượng/khoá file | Không đủ tin cậy | ❌ |

**Quyết định:** W1 + W2 trong GĐ2; đề xuất W3 khi nghiệp vụ cho phép (kèm script tạo & xoá trigger để rollback nhanh).

### ADR-04 — Khoá & ID
- Giữ nguyên **`ID VARCHAR(36)` GUID** của Firebird → MySQL `CHAR(36) CHARACTER SET ascii` (tiết kiệm, so sánh nhanh).
- Thêm `ID_BIN BINARY(16)` (generated từ `ID`) cho các bảng nóng để join nhanh + PK clustered nếu cần; không thay thế `ID`.
- **Không tự sinh ID trên web** trong GĐ0–GĐ7 ⇒ tránh trùng GUID. Nếu phải sinh (bảng mới), dùng **UUIDv7** + ghi vào `W_IDREGISTRY`.
- Mọi bảng giữ PK `ID`; thêm unique index trên cột nghiệp vụ (`NAME`/`CODE`/`MAKHACH`…) để chống trùng khi migrate.

### ADR-05 — Xóa dữ liệu
- Firebird: soft-delete qua `STATUS` (UI có "Thùng rác") → MySQL mirror đúng `STATUS` + thêm `W_DELETED TINYINT(1)`, `W_DELETED_AT`.
- Hard delete ở Firebird: ghi nhận bằng `W_SYNCTOMBSTONE(TABLE_NAME, ID, DETECTED_AT, SOURCE)`, chuyển bản ghi MySQL sang trạng thái "đã xoá vĩnh viễn" (không xoá vật lý, để bảo toàn tham chiếu lịch sử); UI ẩn.
- Người dùng web xoá = set `W_DELETED=1` + audit; **không** xoá ở Firebird trong GĐ1–6.

### ADR-06 — Ảnh & BLOB
| Nguồn | Xử lý |
|---|---|
| `DMATHANG.ANH`, `SIMAGE.IMAGE`, `SMENU.IMAGE`, `SCONFIGGROUP.IMAGE`, `STABLEDESC.IMAGE/IMAGE32`, `SREPORT.IMAGE32`, `STEMPLATE.TEMPLATE`, `SREPORTTEMPLATE.TEMPLATE` | Job `BlobExtractor` xuất ra `\\server\media\{table}\{id}.{ext}` (hoặc MinIO nếu muốn), MySQL chỉ giữ `W_MEDIA_PATH`, `W_MEDIA_HASH`, `W_MEDIA_SIZE`; DB gốc không bị chỉnh sửa |
| `TBANGLUONG.CHITIET` (BLOB chứa XML/JSON chấm công) | Parse → JSON trong cột `W_PAYROLL_DATA JSON` + bảng chi tiết đã chuẩn hoá `TBANGLUONGCHITIET` |
| `SREPORT.SQL/PARAMS/FILTERCONFIG` (BLOB text) | Export ngay GĐ0 ra `.sql`/`.json`, lưu vào `docs/reports/` — **đây là tài sản tri thức, mất là không tái tạo được** |
| `DMATHANG` ảnh nhỏ dạng byte | Nếu ảnh < 64 KB có thể giữ `LONGBLOB` tạm; khuyến nghị vẫn tách file |


### ADR-07 — Stack bản web
- **Backend:** .NET 10 (ASP.NET Core) — SDK 10.0.300 đã có; cùng ngôn ngữ với sync service ⇒ **1 solution, share DTO/enum/validation**; tái sử dụng kiến thức .NET của đội (`FirebirdSql.Data.FirebirdClient`, `MySqlConnector`, Dapper/EF Core đều chạy tốt trên .NET 10).
- **Frontend:** React 19 + TypeScript + Vite + **Ant Design v5** (Table/Form/DatePicker VN, Modal, Tree) + **AG Grid Community** cho lưới > 10.000 dòng; TanStack Query cho cache; Zustand cho state nhẹ; dayjs + locale `vi`.
- **Tại sao không Node/PHP:** PHP không có trên máy; Node chỉ dùng để build front-end. Dùng .NET cho backend giúp tái dùng engine báo cáo/Excel/barcode trong 1 runtime, và **sync service + API dùng chung tầng dữ liệu** (đối chiếu số liệu dễ hơn).
- **Không dùng Docker** (máy chưa cài) — chạy native Windows Service + IIS/Kestrel; tùy chọn bật Docker Desktop sau nếu muốn CI dễ hơn.

### ADR-08 — Xác thực & phân quyền
| Hạng mục | Thiết kế |
|---|---|
| Tài khoản | Đồng bộ từ `SUSER` (`USERNAME`, `DNHANVIENID`, `ISADMIN`, `SGROUPUSERID`) nhưng **KHÔNG đồng bộ `PASSWORD`**; mật khẩu mới hash `Argon2id`; lần đăng nhập đầu tiên buộc đổi mật khẩu (hoặc cấp mật khẩu tạm qua quản trị) |
| Phiên | JWT HS256/RS256, `access` 15 phút + `refresh` 7 ngày (xoay vòng, thu hồi được), lưu `W_USERTOKEN` |
| Quyền | Bảng chuẩn hoá `W_PERMISSION(CODE)`, `W_ROLE`, `W_ROLE_PERMISSION`, `W_USER_ROLE` — **map 1-1 từ `SFUNCTION`/`SGROUPROLE`/`SREPORTROLE`** để không mất phân quyền hiện có |
| Phạm vi dữ liệu | `W_USER_SCOPE(USER_ID, DCUAHANGID, DKHOHANGID)` thay `TNGUOIDUNGTHEOCUAHANG` + `SUSER.DKHOHANGID` |
| Menu động | Dựng menu web từ `SMENU` (đã export) + `W_PERMISSION` ⇒ giữ đúng trải nghiệm & phân quyền |
| Bảo mật | Bắt buộc HTTPS, rate-limit đăng  nhập, lockout 5 lần/15 phút, log IP + thiết bị, 2FA (TOTP) tùy chọn cho Admin |

### ADR-09 — Báo cáo, in ấn, Excel, mã vạch
- **Hai lớp báo cáo:**
  1. **Báo cáo vận hành** (danh sách phiếu thu/chi theo ngày/lý do, nhập hàng, kiểm kê, chuyển kho, tồn kho, thẻ kho, công nợ, KQKD, bán hàng theo ngày, XNT, chi tiết hoạt động trong ngày) → viết lại bằng SQL MySQL, tham số hoá, cache 60 s.
  2. **Báo cáo tuỳ biến** → giữ khái niệm "cấu hình báo cáo" trong `W_REPORT` (SQL template an toàn, whitelist bảng/cột, không cho user viết SQL tự do trừ Admin).
- **Nguồn chân lý để viết lại:** export `SREPORT.SQL` (đã có 7 SQL kiểm chứng + mục 6 tài liệu đối chiếu) — mỗi báo cáo phải có **test đối chiếu số với bản desktop** trước khi công nhận.
- **In ấn:** QuestPDF (hoá đơn A5/A4, phiếu kho, phiếu thu/chi, bảng lương) + template HTML→PDF nếu cần; mã vạch ZXing.Net; giữ Bartender cho tem nếu máy in tem hiện hữu vẫn dùng.
- **Excel:** ClosedXML cho Import/Export (`Thêm excel`, `Xuất excel` giữ đúng cột như bản cũ để người dùng không phải học lại).

### ADR-10 — Phạm vi dữ liệu & lưu trữ
- Nạp **toàn bộ** 86 bảng (~19.5 MB) — không cần lọc theo năm.
- `STRACKING`/`TLUUVET` (log) dễ phình to ⇒ giữ ở MySQL 24 tháng, archive ra bảng `W_ARCHIVE_*` hoặc file Parquet/CSV nén.
- Ảnh/tệp: lưu ngoài DB (ADR-06), sao lưu riêng.

### ADR-11 — 18 hạng mục còn thiếu (từ tài liệu đối chiếu)
- Tạo **mới trong MySQL** và **không sync ngược** về Firebird (vì Firebird không có bảng tương ứng): `W_SANXUAT(+CT)`, `W_KHOLEDGER` (sổ kho nhập–xuất–tồn), `W_BANGGIA(+CT)`, `W_TIGIA`, `W_TINNHAN(+CT)` (log SMS/Zalo/Email), `W_DUDAUKY(+CT)`, `W_DOINOCHITIET`, `W_THANHTOAN` (đa phương thức), `W_CHANGELOG`, `W_XOADULIEU`, `W_LICENSE`, `W_TRACKINGTYPE`… cộng các **cột bổ sung** cho `DTAIKHOANNGANHANG` (số TK/chủ TK/ngân hàng), `DNHANVIEN` (hồ sơ nhân sự), `TDATHANGNCC` (thanh toán/còn nợ).
- **Bảng tra cứu chuỗi trạng thái** (`LOAI`, `MODE`, `LOAIGIA`, `LOAITHANHTOAN`, `LOAIDOITUONG`, `STATUS`) phải được **khảo sát từ dữ liệu thật** ở GĐ0 rồi hằng hoá thành enum trong code + bảng tra trong DB.
- `W_KHOLEDGER` là **xương sống mới** thay `OTONKHO` (chỉ có snapshot `TON`): mỗi dòng chứng từ kho ghi 1 dòng ledger (ngày, kho, mặt hàng, loại, ID chứng từ, SL nhập, SL xuất, tồn luỹ kế, đơn giá, giá vốn) ⇒ phục vụ "Thẻ kho", XNT, tính lại giá vốn (FIFO/bình quân/đích danh) **chuẩn xác** thay vì quét lại toàn bộ đơn hàng.

### ADR-12 — Chiến lược cắt chuyển (cutover)
| Bước | Thời lượng | Điều kiện chuyển tiếp |
|---|---|---|
| Song song 2 hệ thống (web đọc + desktop ghi) | 4 tuần | Đối chiếu 6 báo cáo = 0 lệch, 14 ngày liên tục |
| Web ghi thêm danh mục/ghi chú (2 chiều whitelist) | 2 tuần | Không xung đột, không mất dữ liệu khi test 20 tình huống |
| **Web thành master chứng từ**, desktop chỉ tra cứu (read-only) | 2 tuần | UAT + đào tạo xong, có hotline |
| Ngừng desktop (giữ bản cũ để tra cứu lịch sử, DB đóng băng) | — | Backup cuối + lưu 3 bản `.gbk` ở 3 nơi |


---

## 4. THIẾT KẾ TẦNG ĐỒNG BỘ (SYNC) — CHI TIẾT

### 4.1 Mô hình dữ liệu điều khiển (thuộc MySQL)
```sql
-- Cấu hình từng bảng cần đồng bộ
CREATE TABLE W_SYNCTABLE (
  TABLE_NAME      VARCHAR(64)  NOT NULL PRIMARY KEY,
  PRIMARY_TABLE   VARCHAR(64)  NULL,          -- bảng cha (thứ tự phụ thuộc)
  SYNC_MODE       ENUM('INCREMENTAL','FULL','REF','NONE') NOT NULL DEFAULT 'INCREMENTAL',
  WATERMARK_COL   VARCHAR(64)  NULL,          -- TIMEMODIFIED / TIMECREATED / NULL
  BATCH_SIZE      INT          NOT NULL DEFAULT 1000,
  PRIORITY        TINYINT      NOT NULL DEFAULT 5,
  IS_ENABLED      TINYINT(1)   NOT NULL DEFAULT 1,
  LAST_FULL_AT    DATETIME(3)  NULL,
  NOTE            VARCHAR(255) NULL
) ENGINE=InnoDB;

-- Con trỏ incremental (an toàn khi trùng timestamp: lưu cả ID)
CREATE TABLE W_SYNCCURSOR (
  TABLE_NAME      VARCHAR(64) NOT NULL PRIMARY KEY,
  LAST_TIMESTAMP  DATETIME(3) NULL,
  LAST_ID         CHAR(36)    NULL,
  ROWS_APPLIED    BIGINT      NOT NULL DEFAULT 0,
  UPDATED_AT      DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3)
) ENGINE=InnoDB;

-- Mỗi lần chạy
CREATE TABLE W_SYNCRUN (
  RUN_ID        CHAR(36)    NOT NULL PRIMARY KEY,
  STARTED_AT    DATETIME(3) NOT NULL,
  FINISHED_AT   DATETIME(3) NULL,
  MODE          ENUM('INCREMENTAL','FULL','RECONCILE','BLOB') NOT NULL,
  STATUS        ENUM('RUNNING','OK','WARN','FAILED') NOT NULL DEFAULT 'RUNNING',
  TABLES_OK     INT NOT NULL DEFAULT 0,
  TABLES_FAILED INT NOT NULL DEFAULT 0,
  ROWS_READ     BIGINT NOT NULL DEFAULT 0,
  ROWS_WRITTEN  BIGINT NOT NULL DEFAULT 0,
  ROWS_DELETED  BIGINT NOT NULL DEFAULT 0,
  DURATION_MS   INT NULL,
  MESSAGE       TEXT NULL
) ENGINE=InnoDB;

-- Chi tiết từng bảng trong mỗi lần chạy + lỗi từng dòng (dead letter)
CREATE TABLE W_SYNCBATCHLOG (
  ID BIGINT AUTO_INCREMENT PRIMARY KEY,
  RUN_ID CHAR(36) NOT NULL, TABLE_NAME VARCHAR(64) NOT NULL,
  PHASE ENUM('READ','WRITE','VERIFY') NOT NULL,
  BATCH_NO INT NOT NULL, ROWS INT NOT NULL,
  FROM_TS DATETIME(3) NULL, TO_TS DATETIME(3) NULL,
  MS INT NULL, ERROR_TEXT TEXT NULL,
  INDEX IX_SBL (RUN_ID, TABLE_NAME, BATCH_NO)
) ENGINE=InnoDB;

-- Phát hiện xoá cứng / sửa ngoài luồng
CREATE TABLE W_SYNCTOMBSTONE (
  TABLE_NAME VARCHAR(64) NOT NULL, ID CHAR(36) NOT NULL,
  DETECTED_AT DATETIME(3) NOT NULL, SOURCE ENUM('RECONCILE','TRIGGER') NOT NULL,
  HANDLED TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (TABLE_NAME, ID)
) ENGINE=InnoDB;
```

### 4.2 Cột kỹ thuật bổ sung trên **mọi** bảng dữ liệu MySQL
| Cột | Kiểu | Ý nghĩa |
|---|---|---|
| `W_SYNC_AT` | `DATETIME(3)` | Thời điểm bản ghi được đồng bộ lần cuối |
| `W_SYNC_REV` | `BIGINT` | Số lần bản ghi bị ghi đè (phát hiện "giật" dữ liệu) |
| `W_ORIGIN` | `VARCHAR(8)` | `FB` (từ Firebird) / `WEB` (tạo trên web) / `SYS` |
| `W_ROW_HASH` | `CHAR(32)` | MD5 của các cột nghiệp vụ (dùng cho reconcile nhanh) |
| `W_DELETED` | `TINYINT(1)` | Ẩn khỏi UI (khác `STATUS` gốc của Firebird) |

### 4.3 Thuật toán incremental (pseudo)
```
for each table in order by PRIORITY/TABLE_NAME (topological: cha trước, con sau):
    cursor = W_SYNCCURSOR[table]                     # (last_ts, last_id)
    loop:
        rows = FB: SELECT * FROM {table}
               WHERE (TIMEMODIFIED > :last_ts)
                  OR (TIMECREATED  > :last_ts)
                  OR (TIMEMODIFIED = :last_ts AND ID > :last_id)
               ORDER BY TIMEMODIFIED, ID
               ROWS :batch_size                      # Firebird 2.5: FIRST/SKIP
        if rows.empty: break
        hash = md5(each row's business columns)
        # 1) bỏ qua bản ghi không đổi (giảm 70-90% I/O ghi)
        existing = MY: SELECT ID, W_ROW_HASH FROM {table} WHERE ID IN (:ids)
        upsert = rows where hash != existing.hash
        MY: BEGIN; INSERT ... ON DUPLICATE KEY UPDATE ... ; COMMIT;   # transaction theo batch
        W_SYNCCURSOR[table] = (last_row.TIMEMODIFIED, last_row.ID)
    delete-detect (reconcile only): so tập ID FB vs MY → ghi W_SYNCTOMBSTONE
    update W_SYNCBATCHLOG + W_SYNCRUN
```
**Xử lý cạnh (bắt buộc có test):**
| Tình huống | Cách xử lý |
|---|---|
| Nhiều bản ghi cùng `TIMEMODIFIED` | Điều kiện `(TIMEMODIFIED = ts AND ID > last_id)` + sắp xếp theo `ID` |
| Bản ghi bị sửa với `TIMEMODIFIED` **nhỏ hơn** con trỏ (đồng hồ máy lùi, sửa trực tiếp) | Reconcile đêm phát hiện qua `W_ROW_HASH` |
| Mẻ đồng bộ bị dừng giữa chừng (crash) | Con trỏ chỉ cập nhật **sau** khi commit batch ⇒ replay an toàn (idempotent upsert) |
| Bảng có `ID` là khoá nhưng thiếu `TIMEMODIFIED` | Dùng `SYNC_MODE=FULL` (bảng nhỏ) hoặc `SYNC_MODE=REF` (chỉ nạp khi có yêu cầu) |
| Bảng ghi rất nhanh (TDONHANG giờ cao điểm) | Batch 500 + chạy 30 s/lần + chỉ lấy thêm nếu con trỏ đã tiến |
| Firebird đang backup/sweep ⇒ lỗi mở file | Retry 3 lần (5 s/15 s/60 s); quá 3 lần ⇒ chuyển sang chế độ **C (replica .gbk)** và cảnh báo |


### 4.4 Thứ tự phụ thuộc bảng (topological — nạp cha trước con)
| # | Nhóm | Bảng |
|---|---|---|
| 1 | Cấu hình hệ thống | `SCONFIGGROUP`, `SCONFIG`, `SSMSTEMPLATE`, `SGROUPUSER`, `SUSER`, `SFUNCTION`, `SGROUPROLE`, `SREPORT`, `SREPORTROLE` |
| 2 | Danh mục cấp 1 (không phụ thuộc) | `DTINHTHANH`, `DLOAIQUY`, `DLYDOTHUCHI`, `DLYDOTHUONGPHAT`, `DLOAIHINHKHUYENMAI`, `DLOAIHOPDONG`, `DTRANGTHAIHOPDONG`, `DDONVITINH`, `DNHOMMATHANG`, `DHANGSANXUAT`, `DDOITACKYGUI`, `DNHOMKHACHHANG`, `DNHOMNHACUNGCAP`, `DNHOMCHITIEUDOANHTHU`, `DNHOMYKIENKHACHHANG`, `DDONVICHUYENPHAT`, `DTINHTRANGGOI`, `DKHOHANG` |
| 3 | Danh mục cấp 2 | `DCUAHANG`, `DNHANVIEN`, `DKHACHHANG`, `DNHACUNGCAP`, `DMATHANG` (phụ thuộc ĐVT/nhóm/hãng), `DCALAMVIEC`, `DXEGIAOHANG`, `DTAIKHOANNGANHANG` |
| 4 | Danh mục phụ (con của cấp 2/3) | `DDINHLUONG`, `DDOTKHUYENMAI(+CT)`, `DCHITIEUDOANHTHU(+CT)`, `DYKIENKHACHHANG`, `DTINHTHANH`, `SIMAGE` |
| 5 | Chứng từ cha | `TDONHANG` (+`TDONHANGCHITIET`), `TDATHANGNCC(+CT)`, `TDATHANG(+CT)`, `TBAOGIA(+CT)`, `THOPDONG(+CT)`, `TTHUCHI(+CT)`, `TTHUCHIHANGLOAT`, `TGUIHANGCOD`, `TKETQUAGUICOD`, `TGIAOHANGTHEOXE(+CT)`, `TLUUTAM(+CT)`, `TDONHANGHUY(+CT)`, `TBANGLUONG(+CT/`TONGHOP`)`, `TTHUONGPHAT`, `TGIAYGIOITHIEU`, `TCHAMSOC`, `TGIAMTHEONHOM`, `TTANGGIAMDIEM`, `TDOINO`, `TLUUVET`, `STRACKING`, `TNGUOIDUNGTHEOCUAHANG`, `TKHOASO`, `SQUICKNOTE`, `OTONKHO` |
| 6 | Nhóm bị khoá/chỉ dùng nội bộ | `STABLEDESC`, `SCOLUMN`, `SFORM`, `SGRID`, `SMENU`, `SIMAGE`, `STEMPLATE`, `SREPORTTEMPLATE`, `SANALYSIS` — **`SYNC_MODE=NONE`** (không port UI) nhưng **vẫn export 1 lần** để bảo toàn cấu hình/phân quyền/mẫu in |

### 4.5 Trường hợp đặc biệt phải xử lý riêng
| Đối tượng | Vấn đề | Cách xử lý |
|---|---|---|
| `TDONHANG` (đa năng: bán lẻ/sỉ/trả lại/nhập/xuất/chuyển/kiểm kê/xuất trả NCC/xuất vật tư) | Không tách bảng ⇒ UI web dễ sai | GĐ0 **khảo sát tập giá trị `LOAI`/`MODE`/`LOAIGIA`/`LOAITHANHTOAN`** bằng SQL; tạo view `V_DONHANG_BANLE`, `V_PHIEUNHAPKHO`, `V_PHIEUXUATKHO`, `V_CHUYENKHO`, `V_KIEMKE`, `V_TRANCC` + cột `W_DOC_TYPE` tính sẵn để lọc nhanh |
| `TDONHANGCHITIET.SLNHAP/SLXUAT/SLTHUCTE/SLHETHONG/XUATVATTU` | Một dòng chi tiết phục vụ nhiều nghiệp vụ | Giữ nguyên + dẫn xuất vào `W_KHOLEDGER` theo `W_DOC_TYPE` |
| `OTONKHO` (chỉ có `TON`) | Không đủ cho "Thẻ kho"/XNT | Thay bằng ledger `W_KHOLEDGER`; `OTONKHO` chỉ dùng để **đối chiếu** (số tồn cuối cùng phải khớp tuyệt đối) |
| `TBANGLUONG.CHITIET` (BLOB) vs `TBANGLUONGCHITIET` | Dữ liệu chấm công lưu 2 nơi | Parse BLOB → `W_PAYROLL_DATA JSON`; đối chiếu với bảng chi tiết; cảnh báo nếu lệch |
| `SUSER.PASSWORD` | Không thể/chưa nên migrate mật khẩu | Tạo mật khẩu tạm, buộc đổi lần đầu; lưu `W_MUSTCHANGE=1` |
| `STRACKING` (audit cũ, thường 100k+ dòng) | Bảng lớn, tăng nhanh | Sync theo `GIO`/`NGAY` (nếu có) hoặc `SYNC_MODE=INCREMENTAL` theo `TIMECREATED`; archive 24 tháng |
| `TLUUVET`, `TTHUCHI`, `TDONHANG` trong nhiều năm | Khối lượng lớn dần | Partition theo năm (`PARTITION BY RANGE (YEAR(NGAY))`) ở MySQL — chuẩn bị từ GĐ1 |
| Charset tiếng Việt (DB 2.5 có thể `charset NONE`/`WIN1258`) | Nguy cơ mojibake | GĐ0 kiểm tra `RDB$CHARACTER_SET_NAME`; sync đọc theo đúng charset rồi ghi `utf8mb4`; **test tự động 100% giá trị chuỗi có dấu** (tên KH, mặt hàng, diễn giải) |
| Số tiền `NUMERIC(x,2)` | Sai số nếu đổi sang float | `DECIMAL(x,2)`/`DECIMAL(x,4)` đúng scale gốc; test tổng tiền từng phiếu = 0 lệch |


### 4.6 Bảo đảm tính đúng & idempotent
- Ghi bằng `INSERT ... ON DUPLICATE KEY UPDATE` (không dùng `REPLACE INTO` để tránh mất cột `W_*` và gây DELETE+INSERT làm tăng auto-increment).
- Mỗi batch = **1 transaction MySQL**; con trỏ cập nhật **cùng transaction** với dữ liệu (dùng bảng `W_SYNCCURSOR` trong cùng `BEGIN…COMMIT`) ⇒ không bao giờ mất mẻ hoặc nhảy cóc.
- Khoá chống chạy chồng: `W_SYNCLOCK` (1 dòng) hoặc `GET_LOCK()` MySQL.
- Ghi lỗi từng dòng vào `W_SYNCROWERROR(TABLE_NAME, ID, ERROR, PAYLOAD JSON)`; **không** dừng cả mẻ vì 1 dòng lỗi (dead-letter), nhưng phải cảnh báo nếu > 10 dòng lỗi/bảng/ngày.

### 4.7 Hiệu năng & lịch chạy
| Job | Lịch | Ghi chú |
|---|---|---|
| Incremental chứng từ | mỗi **30 giây** | `TDONHANG`, `TDONHANGCHITIET`, `TTHUCHI`, `TCHITIETTHANHTOAN`, `TDATHANGNCC*`, `TGUIHANGCOD*`, `TKETQUAGUICOD`, `TBANGLUONG*`, `TLUUVET` |
| Incremental danh mục | mỗi **2 phút** | `DMATHANG`, `DKHACHHANG`, `DNHACUNGCAP`, `DNHANVIEN`, `DKHOHANG`, `DCUAHANG`, `DDINHLUONG`, `DDOTKHUYENMAI*`, `DKHOHANG`… |
| Full load bảng nhỏ | mỗi **10 phút** | `SCONFIG`, `SCONFIGGROUP`, `SGROUPROLE`, `SREPORTROLE`, `TNGUOIDUNGTHEOCUAHANG`, `DLOAI*`, `DTINHTHANH`, `DNHOM*` |
| Ledger kho & bảng tổng hợp | mỗi **5 phút** | Build `W_KHOLEDGER` cho chứng từ mới + refresh `W_RPT_*` |
| Reconcile toàn bộ | **02:00** hằng đêm | Đối chiếu COUNT + checksum + tập ID của 86 bảng; xuất báo cáo lệch |
| Blob/ảnh extractor | **02:30** hằng đêm | Chỉ xử lý ảnh mới (theo hash) |
| Kiểm tra dung lượng & vacuum | **03:00** Chủ nhật | `OPTIMIZE TABLE` bảng log, archive |

### 4.8 Giám sát & cảnh báo
- Healthcheck tổng hợp: `GET /health/sync` trả JSON `{fbConnected, mySqlConnected, lastRunAt, lagSeconds, failedTables[], pendingTombstones[]}`.
- Trang **/admin/sync** trong web: bảng 86 bảng × (con trỏ, số dòng, lần chạy cuối, trạng thái, lag), nút "Đồng bộ ngay", "Reconcile bảng này".
- Cảnh báo (email/Zalo/Telegram – ngưỡng cấu hình): mất kết nối FB > 3 lần retry; lag > 5 phút; reconcile có lệch; dung lượng đĩa < 20%.
- Ghi log chuẩn Serilog: `sync.run`, `sync.table`, `sync.row.error` kèm `RunId`/`TableName`/`BatchNo` để truy vết.

### 4.9 Bảo mật tầng sync (quan trọng vì FB Embedded bỏ qua `security2.fdb`)
1. Tài khoản Windows chuyên dụng `svc_hnsync` — **không** thuộc Administrators.
2. Quyền NTFS trên thư mục chứa `DEMO.FDB`: `svc_hnsync` = **Read & Execute** (bỏ Write/Delete) ⇒ kể cả code lỗi cũng không ghi được DB gốc.
3. Chuỗi kết nối Firebird: `ServerType=Embedded;Database=<đường dẫn>;Charset=<charset gốc>;ReadOnly=true` + `NoGarbageCollect=true` để **không** kích hoạt sweep (tránh ghi vào file/đụng app).
4. MySQL: user `hn_sync` chỉ có quyền trên schema `hoanngoc`; user `hn_api` cho ứng dụng; **bind-address 127.0.0.1**; bật `binlog` + `audit_log`.
5. Không mở cổng Firebird/MySQL ra LAN; web chỉ qua 443.


---

## 5. THIẾT KẾ CSDL MYSQL

### 5.1 Quy ước đặt tên
| Đối tượng | Quy ước | Ví dụ |
|---|---|---|
| Bảng gốc (mirror Firebird) | **Giữ nguyên tên, UPPER** | `DMATHANG`, `TDONHANGCHITIET` |
| Bảng mới của web | Tiền tố `W_` + UPPER | `W_KHOLEDGER`, `W_KHACHHANGNO` |
| Bảng điều khiển sync | `W_SYNC*` | `W_SYNCTABLE`, `W_SYNCRUN` |
| Cột gốc | Giữ nguyên tên, UPPER | `GIABAN`, `TIMEMODIFIED` |
| Cột kỹ thuật | Tiền tố `W_` | `W_SYNC_AT`, `W_ORIGIN` |
| View | `V_` + tên nghiệp vụ | `V_TONKHO`, `V_CONNOKHACHHANG` |
| Bảng tổng hợp cho báo cáo | `W_RPT_` | `W_RPT_XNT_THANG` |
| Index | `IX_<bảng>_<cột>`, unique `UX_`, FK `FK_` | `IX_TDONHANG_NGAY` |

> **Lưu ý bắt buộc:** vì giữ tên bảng/cột UPPER như Firebird, mọi câu SQL MySQL phải **bọc backtick** (`` ` ``) — điều này giúp câu SQL chỉ khác nhau ở dấu nháy, dịch qua lại giữa 2 hệ dễ và giảm sai sót khi đối chiếu.

### 5.2 Bảng ánh xạ kiểu dữ liệu Firebird 2.5 → MySQL 8.0
| Firebird 2.5 | MySQL 8.0 | Ghi chú |
|---|---|---|
| `VARCHAR(n)` | `VARCHAR(n)` + `utf8mb4` | Nếu `n > 191` và có index ⇒ dùng index tiền tố; hoặc đổi sang `TEXT` + generated hash cho tìm kiếm |
| `VARCHAR(36)` (GUID) | `CHAR(36) CHARACTER SET ascii COLLATE ascii_bin` | Tiết kiệm, so khớp nhanh, đúng ngữ nghĩa GUID |
| `CHAR(n)` | `CHAR(n)` | |
| `SMALLINT` | `SMALLINT` | |
| `INTEGER` | `INT` | `STATUS` giữ `INT` để bảo toàn mọi giá trị lạ |
| `BIGINT` | `BIGINT` | |
| `NUMERIC(p,s)` / `DECIMAL(p,s)` | `DECIMAL(p,s)` **đúng p,s gốc** | Tiền tệ: bắt buộc exact; kiểm tra cả những cột "số lượng" |
| `DOUBLE PRECISION` | `DOUBLE` | Chỉ với trọng lượng/tỉ lệ |
| `FLOAT` | `FLOAT` | |
| `DATE` | `DATE` | |
| `TIME` | `TIME(3)` | FB lưu 1/10000 s, làm tròn ms |
| `TIMESTAMP` | `DATETIME(3)` | Dùng cho `TIMECREATED/TIMEMODIFIED/NGAY` |
| `BLOB SUB_TYPE 0` | `LONGBLOB` | Ảnh/tệp ⇒ chuyển ra media (ADR-06) |
| `BLOB SUB_TYPE 1` (text) | `LONGTEXT` + `utf8mb4` | `SREPORT.SQL`, `TBANGLUONG.CHITIET` (XML) |
| `DOMAIN` (kiểu người dùng) | Bỏ domain, dùng kiểu gốc | Tra `RDB$FIELDS` để lấy kiểu thật |
| `COMPUTED BY (...)` | MySQL **generated column** (nếu biểu thức tương thích) hoặc tính trong API | Liệt kê từ `RDB$RELATION_FIELDS`; nếu không tương thích ⇒ tính ở tầng C# |
| `IDENTITY`/generator | Không dùng (ID là GUID) | Nếu bảng log cần số tự tăng ⇒ `BIGINT AUTO_INCREMENT` riêng |
| Charset `NONE`/`WIN1258` | `utf8mb4` | Phải chuyển ở tầng sync, có test |

### 5.3 Charset & collation
- Server/DB: `utf8mb4` + `utf8mb4_0900_ai_ci` (MySQL 8.0.45 hỗ trợ) — sắp xếp/tìm kiếm tiếng Việt có dấu đúng.
- Với cột so khớp chính xác (mã, GUID, hash): dùng `ascii_bin`/`utf8mb4_bin`.
- Kiểm thử bắt buộc: danh sách `NAME`/`DIENGIAI` có dấu phải giống **byte-for-byte** sau khi chuẩn hoá NFC.

### 5.4 Index & khoá (chiến lược)
| Nhóm | Index bắt buộc |
|---|---|
| Mọi bảng | PK `(ID)`; `ID_BIN` (nếu tạo) unique |
| Bảng có watermark | `IX_<T>_SYNC (TIMEMODIFIED, ID)`, `IX_<T>_CRE (TIMECREATED, ID)` |
| Chứng từ | `IX_<T>_NGAY (NGAY)`, `IX_<T>_LOAI (LOAI, NGAY)`, `IX_<T>_KH (DKHACHHANGID, NGAY)`, `IX_<T>_KHO (DKHONHAPID, NGAY)`, `IX_<T>_KHOX (DKHOXUATID, NGAY)` |
| Chi tiết chứng từ | `IX_<CT>_PARENT (TDONHANGID)`, `IX_<CT>_MH (DMATHANGID)` |
| Danh mục | Unique `UX_<T>_NAME (NAME)`, `UX_<T>_CODE (CODE)` (nếu nghiệp vụ cho phép) |
| Tìm kiếm nhanh | `FULLTEXT` trên `DMATHANG(NAME, MASANCO)`, `DKHACHHANG(NAME, DIENTHOI)`, `DNHACUNGCAP(NAME)` |
| FK | Khai báo FK thật cho mọi `…ID` (**bản Firebird không có FK**) ⇒ cần bật `FOREIGN_KEY_CHECKS=0` khi nạp ban đầu và bật lại sau, có job kiểm tra mồ côi |


### 5.5 Ví dụ DDL (mẫu chuẩn để sinh hàng loạt)
```sql
-- Danh mục mặt hàng (rút gọn các cột tiêu biểu; bản đầy đủ sinh tự động từ metadata)
CREATE TABLE `DMATHANG` (
  `ID`              CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `NAME`            VARCHAR(255) NOT NULL,
  `CODE`            VARCHAR(64)  NULL,
  `MASANCO`         VARCHAR(64)  NULL,
  `DNHOMMATHANGID`  CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `DDONVITINHID`    CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `DDONVITINHLONID` CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `DDONVITINHCHANID`CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `DHANGSANXUATID`  CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `GIABAN`          DECIMAL(15,2) NULL,
  `GIABANCHAN`      DECIMAL(15,2) NULL,
  `GIABANLON`       DECIMAL(15,2) NULL,
  `GIABAN1`         DECIMAL(15,2) NULL,   -- … GIABAN2..GIABAN10 giữ nguyên để tương thích
  `GIABANSILON`     DECIMAL(15,2) NULL,
  `GIABANSICHAN`    DECIMAL(15,2) NULL,
  `GIANHAP`         DECIMAL(15,2) NULL,
  `GIAVON`          DECIMAL(18,4) NULL,
  `TONTOITHIEU`     DECIMAL(18,4) NULL,
  `COHANSUDUNG`     SMALLINT NULL,
  `COSERI`          SMALLINT NULL,
  `ANH`             LONGBLOB NULL,          -- giai đoạn 1; sau đó chuyển sang W_MEDIA_PATH
  `STATUS`          INT NOT NULL DEFAULT 1,
  `USERCREATEDID`   CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `TIMECREATED`     DATETIME(3) NULL,
  `USERMODIFIEDID`  CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `TIMEMODIFIED`    DATETIME(3) NULL,
  -- cột kỹ thuật web
  `W_SYNC_AT`       DATETIME(3) NULL,
  `W_SYNC_REV`      BIGINT NOT NULL DEFAULT 0,
  `W_ORIGIN`        VARCHAR(8) NOT NULL DEFAULT 'FB',
  `W_ROW_HASH`      CHAR(32) NULL,
  `W_DELETED`       TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`ID`),
  KEY `IX_DMATHANG_SYNC` (`TIMEMODIFIED`,`ID`),
  KEY `IX_DMATHANG_CRE`  (`TIMECREATED`,`ID`),
  KEY `IX_DMATHANG_NHOM` (`DNHOMMATHANGID`),
  KEY `IX_DMATHANG_DVT`  (`DDONVITINHID`),
  KEY `IX_DMATHANG_HANG` (`DHANGSANXUATID`),
  FULLTEXT KEY `FT_DMATHANG` (`NAME`,`MASANCO`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
```
```sql
-- Chứng từ đa năng: thêm cột dẫn xuất W_DOC_TYPE để lọc/route theo nghiệp vụ
CREATE TABLE `TDONHANG` (
  `ID` CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
  `NAME` VARCHAR(64) NULL,              -- số phiếu: PN18/00001, BS26/00001, 26000001…
  `NGAY` DATETIME(3) NULL,
  `LOAI` INT NULL, `MODE` INT NULL, `LOAIGIA` INT NULL, `LOAITHANHTOAN` INT NULL,
  `W_DOC_TYPE` VARCHAR(24) GENERATED ALWAYS AS (
      CASE
        WHEN `DKHOXUATID` IS NOT NULL AND `DKHONHAPID` IS NOT NULL THEN 'CHUYENKHO'
        WHEN `SLTHUCTE_IS_USED_CHECK` = 1 THEN 'KIEMKE'   -- (giữ ví dụ; thực tế suy từ LOAI)
        WHEN `DKHONHAPID` IS NOT NULL THEN 'NHAPKHO'
        WHEN `DKHOXUATID` IS NOT NULL THEN 'XUATKHO'
        WHEN `TDONHANGTRAID` IS NOT NULL OR `DOITRA` <> 0 THEN 'TRAKHACH'
        ELSE 'BANHANG'
      END) STORED,
  `DKHACHHANGID` CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `DNHACUNGCAPID` CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `DKHONHAPID` CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `DKHOXUATID` CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `DCUAHANGID` CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `DNHANVIENID` CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `DNHANVIENNHAPID` CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `DNHANVIENXUATID` CHAR(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
  `TIENHANG` DECIMAL(15,2) NULL, `TIENGIAMGIA` DECIMAL(15,2) NULL,
  `TILETHUE` DECIMAL(9,4) NULL,  `TIENTHUE` DECIMAL(15,2) NULL,
  `PHIVANCHUYEN` DECIMAL(15,2) NULL, `DOITRA` DECIMAL(15,2) NULL,
  `TONGCONG` DECIMAL(15,2) NULL, `TIENTHANHTOAN` DECIMAL(15,2) NULL,
  `CONLAI` DECIMAL(15,2) NULL, `CONNO` DECIMAL(15,2) NULL, `NOCU` DECIMAL(15,2) NULL,
  `STATUS` INT NOT NULL DEFAULT 1,
  `TIMECREATED` DATETIME(3) NULL, `TIMEMODIFIED` DATETIME(3) NULL,
  `W_SYNC_AT` DATETIME(3) NULL, `W_SYNC_REV` BIGINT NOT NULL DEFAULT 0,
  `W_ORIGIN` VARCHAR(8) NOT NULL DEFAULT 'FB', `W_ROW_HASH` CHAR(32) NULL,
  `W_DELETED` TINYINT(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`ID`),
  KEY `IX_TDONHANG_SYNC` (`TIMEMODIFIED`,`ID`),
  KEY `IX_TDONHANG_NGAY` (`NGAY`),
  KEY `IX_TDONHANG_TYPE_NGAY` (`W_DOC_TYPE`,`NGAY`),
  KEY `IX_TDONHANG_KH` (`DKHACHHANGID`,`NGAY`),
  KEY `IX_TDONHANG_KHONHAP` (`DKHONHAPID`,`NGAY`),
  KEY `IX_TDONHANG_KHOXUAT` (`DKHOXUATID`,`NGAY`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
```
> ⚠️ `W_DOC_TYPE` ở trên là **ví dụ minh hoạ**; biểu thức thật phải chốt sau khi **khảo sát tập giá trị `LOAI`/`MODE`** ở GĐ0 (xem mục 7 – GĐ0, việc 1.3). Nếu không đủ tin cậy ⇒ tính ở tầng C# thay vì generated column.


### 5.6 View & bảng dẫn xuất (thay thế các chỗ thiếu của bản cũ)
| View/bảng | Thay thế cho | Công thức chính |
|---|---|---|
| `W_KHOLEDGER` (bảng thật, có index) | `OTONKHO` + "Thẻ kho" + XNT | Ghi 1 dòng/1 dòng chi tiết chứng từ kho: `(NGAY, DKHOHANGID, DMATHANGID, W_DOC_TYPE, TDONHANGID, SLNHAP, SLXUAT, DONGIA, GIAVON, TON_LUYKE)` |
| `V_TONKHO` | "Tồn kho", "Tồn tất cả các kho" | `SUM(SLNHAP - SLXUAT)` theo `(kho, mặt hàng)` từ `W_KHOLEDGER` + join `DMATHANG` lấy giá bán/giá vốn/tồn tối thiểu/ĐVT |
| `V_THEKHO` | tab "Thẻ kho" | Lọc `W_KHOLEDGER` theo kho + mặt hàng + khoảng ngày, tính `TON` luỹ kế |
| `V_CONNOKHACHHANG` | "Công nợ khách hàng" | `TDONHANG` (chưa thanh toán) + `TCHITIETTHANHTOAN` + `DKHACHHANG` (hạn nợ, nhóm) |
| `V_CONNONCC` | "Công nợ nhà cung cấp" | `TDONHANG` phiếu nhập + `TTHUCHI` chi cho NCC + `TDATHANGNCC` |
| `V_TONQUY` | "Tồn quỹ" | `TTHUCHI(THU, CHI)` luỹ kế theo ngày + `TCHITIETTHANHTOAN` |
| `V_HOADON_BANLE`, `V_HOADON_BANSI`, `V_PHIEUNHAP`, `V_PHIEUXUAT`, `V_CHUYENKHO`, `V_KIEMKE`, `V_TRANCC` | tách nghiệp vụ từ `TDONHANG` | Lọc theo `W_DOC_TYPE` + tiền tố số phiếu (`PN/PX/BS/KTL/COD/ĐN`) |
| `W_RPT_BANHANG_NGAY`, `W_RPT_XNT_THANG`, `W_RPT_KQKD` | 3 báo cáo bản in | Job tổng hợp 5 phút/lần; tránh truy vấn nặng khi xem báo cáo |

### 5.7 Bảng/cột **mới** cần tạo (18 hạng mục từ tài liệu đối chiếu)
| # | Bảng/cột mới | Phục vụ chức năng | Ưu tiên |
|---|---|---|---|
| 1 | `W_SANXUAT` + `W_SANXUATCHITIET` | "Sản xuất ▸ Phiếu sản xuất" (đang **không có bảng nào**) | P1 |
| 2 | `W_KHOLEDGER` | Sổ kho/Thẻ kho/XNT/Tính lại giá vốn chính xác | P1 |
| 3 | `W_BANGGIA` + `W_BANGGIACHITIET` | Bảng giá theo KH/nhóm/ĐVT/số lượng/tiền tệ (thay `GIABAN2..10` + `SOLUONG3..10`) | P1 |
| 4 | `W_TINNHAN` + `W_TINNHANCHITIET` | "Gửi tin nhắn tới KH" (hiện chỉ có mẫu `SSMSTEMPLATE`) | P2 |
| 5 | `W_DUDAUKY` + `W_DUDAUKYCHITIET` | "Dữ liệu ban đầu" (tồn/công nợ/điểm đầu kỳ) | P1 |
| 6 | `W_DOINOCHITIET` | "Theo dõi nợ" (`TDOINO` quá mỏng) | P2 |
| 7 | `W_TIGIA` | Giá lẻ/sỉ USD (bản cũ không có tỉ giá) | P2 |
| 8 | `W_THANHTOAN` | Đa phương thức trong 1 bill (tiền mặt/thẻ/CK) | P2 |
| 9 | `W_CHANGELOG` | Audit theo cột (giá trị cũ/mới) | P1 |
| 10 | `W_XOADULIEU` | Log chức năng "Xóa dữ liệu" | P3 |
| 11 | `W_LICENSE`, `W_APPINFO` | Module TRỢ GIÚP (3 mục, bản cũ 0 bảng) | P3 |
| 12 | `W_TRACKINGTYPE` | Sửa FK treo `STRACKING.STRACKINGTYPEID` | P3 |
| 13 | Cột mới `DTAIKHOANNGANHANG`: `SOTAIKHOAN`, `CHUTAIKHOAN`, `DNGANHANGID`, `CHINHANH` | Tài khoản ngân hàng dùng đúng nghĩa | P2 |
| 14 | Cột mới `DNHANVIEN`: `CCCD`, `NGAYVAO`, `DCHUCVUID`, `DPHONGBANID`, `NGANHANG`, `STK` (+ `W_CHUCVU`, `W_PHONGBAN`) | Hồ sơ nhân sự | P2 |
| 15 | Cột mới `TDATHANGNCC`: `TIENTHANHTOAN`, `CONNO`, `DATHANHTOAN`, `DTAIKHOANNGANHANGID` | Công nợ NCC theo đặt mua | P2 |
| 16 | Cột mới `TDONHANGHUY`: `TDONHANGID`, `LYDOHUYID` | Hủy bill liên kết gốc (đang lưu text) | P2 |
| 17 | Bảng tra `W_LOAICHUNGTU`, `W_LOAITHANHTOAN`, `W_LOAIGIA`, `W_LOAIDOITUONG`, `W_TRANGTHAI` | Giải mã mọi chuỗi trạng thái dạng số | P1 |
| 18 | `W_SYSLOG`, `W_USERTOKEN`, `W_PERMISSION`, `W_ROLE*`, `W_USER_SCOPE`, `W_MEDIA` | Hạ tầng web (audit, phiên, phân quyền, media) | P1 |

### 5.8 Quản lý thay đổi schema
- Dùng **DbUp** với thư mục `src/Hn.Data.MySql/Scripts/` (`0001_init_generated.sql`, `0002_sync_tables.sql`, `0003_web_tables.sql`, `0004_indexes_fk.sql`, `0005_views.sql`, …).
- Script sinh tự động từ `tools/Hn.SchemaGen` (đọc `RDB$RELATION_FIELDS`, `RDB$FIELDS`, `RDB$INDEX_SEGMENTS` → phát sinh `CREATE TABLE`), **sau đó review bằng tay** rồi ghi vào repo (không sinh runtime).
- Bảng `schemaversions` (của DbUp) theo dõi; CI chạy migrate lên staging trước khi lên prod.
- Mọi thay đổi schema **chỉ thêm, không sửa/xoá cột cũ** trong 6 tháng đầu để tránh gãy tương thích với dữ liệu đồng bộ.


---

## 6. THIẾT KẾ ỨNG DỤNG WEB

### 6.1 Kiến trúc API
| Nhóm endpoint | Ví dụ | Ghi chú |
|---|---|---|
| Danh mục CRUD | `GET/POST/PUT/DELETE /api/v1/mathang`, `/khachhang`, `/nhacungcap`, `/nhanvien`, `/khohang`, `/cuahang`, `/dvt`, `/nhommathang`, `/lydothuchi`, `/dotkhuyenmai`… | 13 danh mục chính theo `DANH-MUC-CHUC-NANG.md` mục 3 |
| Chứng từ | `/api/v1/donhang?type=BANLE&from=&to=&kho=&khach=&page=`, `/api/v1/thuchi`, `/api/v1/dathangncc`, `/api/v1/kiemke`, `/api/v1/chuyenkho` | Trả về **master + detail + tổng cộng** trong 1 response |
| Ghi chứng từ | `POST /api/v1/donhang` (payload giống cấu trúc `TDONHANG` + `TDONHANGCHITIET`) | GĐ1–6 chỉ cho `W_*`; GĐ7 mới bật whitelist |
| Lưới dữ liệu | `POST /api/v1/grid/{resource}/query` (filter/sort/page/tổng cột) | Chuẩn hoá 1 endpoint cho mọi màn hình danh sách |
| Báo cáo | `POST /api/v1/reports/{code}/run` → `jobId`; `GET /api/v1/reports/{code}/export?format=pdf|xlsx` | Cache + phân quyền theo `SREPORTROLE` map sang `W_PERMISSION` |
| Nhập/Xuất Excel | `POST /api/v1/{resource}/import`, `GET /api/v1/{resource}/export` | ClosedXML; validate & trả file lỗi từng dòng |
| Đồng bộ | `GET /api/v1/admin/sync/status`, `POST /api/v1/admin/sync/run`, `POST /api/v1/admin/sync/reconcile` | Chỉ Admin/IT |
| Healthcheck | `/health`, `/health/sync`, `/metrics` | Cho giám sát |
| Realtime | `hub/notify` (SignalR): có dữ liệu mới, nhắc nợ đến hạn, COD cập nhật | Tùy chọn |

**Chuẩn chung:** DTO theo `snake/UPPER` gốc (`NAME`, `NGAY`, `TONGCONG`) để frontend dùng thẳng tên cột quen thuộc ⇒ giảm lỗi mapping và giúp người dùng đối chiếu với bản cũ; lỗi trả về theo `ProblemDetails` + `errorCode` (thông báo tiếng Việt do backend trả); phân trang `page/pageSize` với `totalRows`.

### 6.2 Phân quyền & menu động
- Map `SGROUPUSER` → `W_ROLE`; `SGROUPROLE(SFUNCTIONID, MODE)` → `W_ROLE_PERMISSION(PERMISSION_CODE, CAN_VIEW/CAN_EDIT/CAN_DELETE/CAN_PRINT)`.
- Ma trận quyền theo nghiệp vụ (đề xuất 6 vai trò mặc định): **Admin**, **Kế toán trưởng**, **Thu ngân/Bán hàng**, **Thủ kho**, **Kinh doanh/CSKH**, **Chủ/Quản lý (chỉ xem báo cáo)**.
- `W_USER_SCOPE` giới hạn cửa hàng/kho: mọi query tự động thêm điều kiện scope (không tin client).
- Menu trái dựng động theo quyền, giữ đúng 10 nhóm + 76 mục như bản cũ (đối chiếu bằng checklist UAT).
- Bổ sung so với bản cũ: **đăng nhập 2 thiết bị/hủy phiên**, **lịch sử đăng nhập**, **buộc đổi mật khẩu**, **khoá tài khoản tạm thời**.

### 6.3 Trải nghiệm người dùng (giữ thói quen bản cũ = giảm kháng cự)
| Đặc điểm bản desktop | Cách tái hiện trên web |
|---|---|
| MDI + thanh tab tài liệu | Router + "Workspace tab" (mở nhiều màn hình dạng tab, `Mở nhanh` = command palette Ctrl+K) |
| Phím tắt F3/F4/F5/F8/F9/F10/F11/F12, Ctrl+P/Q/M/K | Bảng `W_SHORTCUT` cấu hình được; mặc định giống bản cũ; hiển thị gợi ý phím |
| Panel phải "TỔNG CỘNG" | Cột tổng cộng dính bên phải, tự tính như bản cũ (Tiền hàng, Giảm giá %, Thuế %, Phí VC, Đổi trả, Tổng cộng, Tiền thanh toán) |
| Panel dưới tab chi tiết (Thông tin/Phiếu thu/Phiếu chi/… chi tiết) | Tab dưới lưới, lazy-load từng tab |
| Cây lọc trái (nhóm hàng, kho, NV, nhóm KH, NCC, ĐVCB, Chưa thiết lập, Thùng rác) | Tree component + badge số lượng, giữ nguyên nhãn |
| Footer "Khởi tạo – Khởi tạo bởi / Sửa đổi gần nhất" | Footer tự động trên mọi form (đọc `TIMECREATED/USERCREATEDID/…`) |
| Máy quét mã vạch tại quầy | Ô "Quét/tìm hàng" auto-focus, Enter thêm dòng; hỗ trợ máy quét HID |
| Gõ tiếng Việt Telex | Trình duyệt/Windows IME (không cần tự viết); loại bỏ lỗi chính tả cũ: *Thành toán→Thanh toán*, *Điền/Điện giải→Diễn giải*, *Bill hũy→Bill hủy*, *Tổn quỹ→Tồn quỹ*, *Trình trạng gọi→Tình trạng gọi* |
| In nhanh (Ctrl+P) | Nút In → xem trước PDF trên tab mới → in máy in nhiệt/A4 |

### 6.4 Danh sách màn hình theo ưu tiên (rút gọn — ma trận chi tiết ở §11.1 và §11.4)
| Ưu tiên | Màn hình |
|---|---|
| **P1 (MVP)** | Đăng nhập; Dashboard; Danh mục: Mặt hàng, Khách hàng, NCC, Nhân viên, Kho hàng, ĐVT, Nhóm hàng; Bán lẻ (hoá đơn); Bán sỉ; Nhập kho; Tồn kho + Thẻ kho; Thu/Chi (phiếu thu, phiếu chi, tồn quỹ); Công nợ KH/NCC; 6 báo cáo chính; Người dùng & phân quyền; Đồng bộ (admin) |
| **P2** | Báo giá, Đặt hàng, Hợp đồng, Trả lại, Giao COD + KQ giao, Chuyển kho, Kiểm kê, Xuất trả NCC, Tính lại giá vốn, Khuyến mại, Khách hàng thân thiết/điểm, Chăm sóc KH, Gửi tin nhắn, Chấm công, Tính lương, Thưởng phạt, Tạm ứng, Giấy giới thiệu, Xe giao hàng |
| **P3** | Sản xuất, Lưu vết hoạt động, Chi tiết doanh thu, Khóa sổ, Thư viện ảnh, Dữ liệu ban đầu, Xóa dữ liệu, Trợ giúp/bản quyền, In mã vạch, Xuất/nhập Excel nâng cao, biểu đồ |

### 6.5 Báo cáo & in ấn (map từ 9 nhóm của bản cũ)
| Nhóm | Báo cáo phải có (tối thiểu) |
|---|---|
| QUỸ | Danh sách phiếu thu theo ngày / theo lý do; Danh sách phiếu chi theo ngày / theo lý do; Tổng hợp thu chi theo ngày / theo lý do; Báo cáo tồn quỹ |
| KHO HÀNG | Nhập hàng; Kiểm kê; Theo HSD; Xuất khác; Chuyển kho; Tồn kho; Thẻ kho; **Tổng hợp xuất–nhập–tồn** |
| BÁN HÀNG | **Tổng hợp bán hàng theo ngày**; Thống kê doanh thu; Thống kê mặt hàng bán; Chi tiết hoạt động trong ngày; Bán sỉ |
| ĐẶT HÀNG | Đặt hàng KH; Đặt mua NCC |
| CÔNG NỢ | **Tổng hợp công nợ KH**; **Tổng hợp công nợ NCC**; Theo dõi nợ |
| QUẢN TRỊ | **Tổng hợp kết quả kinh doanh**; Lịch sử tác hệ thống; Nhật ký đồng bộ; Lịch sử đăng nhập |
| BIỂU ĐỒ | Doanh số theo nhóm hàng; Doanh thu ngày trong tháng; Doanh thu tháng trong năm (Chart.js/Recharts + ECharts) |
| GỬI HÀNG | Giao COD; Kết quả giao COD; Giao hàng theo xe |
| DANH MỤC | Danh sách mặt hàng; khách hàng; NCC; nhân viên (có in tem mã vạch) |


---

## 7. LỘ TRÌNH TRIỂN KHAI (9 GIAI ĐOẠN)

### GĐ0 — Khảo sát chốt dữ liệu & PoC kết nối (1 tuần)
| # | Việc | Kết quả |
|---|---|---|
| 0.1 | Chạy 7 SQL kiểm chứng (mục 6 của `DOI-CHIEU-CSDL-VA-CHUC-NANG.md`): tập `LOAI/MODE`, tiền tố số phiếu, quy ước thu/chi, danh sách bảng, export `SREPORT` | Bảng giá trị thật của mọi cột trạng thái |
| 0.2 | **Export** `SREPORT` (SQL/PARAMS/FILTERCONFIG), `SMENU`, `SCOLUMN`, `SFORM`, `STABLEDESC`, `SGROUPUSER`, `SGROUPROLE`, `SFUNCTION`, `SUSER` (bỏ `PASSWORD`) → JSON/Excel vào `docs/reports/` | Tài sản cấu hình + định nghĩa 50+ báo cáo, không thể tái tạo |
| 0.3 | Kiểm tra `SELECT RDB$CHARACTER_SET_NAME FROM RDB$DATABASE` + test 200 chuỗi có dấu | Chốt charset, tránh mojibake |
| 0.4 | Đếm số dòng từng bảng (script) | Ước lượng tải + lịch sync |
| 0.5 | **PoC-1:** console .NET 10 + `fbembed.dll` 2.5.5 mở `DEMO.FDB` **read-only** trong khi `Application.exe` đang chạy và đang ghi hoá đơn | Chốt ADR-01 (A, hoặc buộc sang B) |
| 0.6 | **PoC-2:** cài Firebird 2.5.9 Server bản portable trên máy test, copy `DEMO.FDB`, bật `KetNoiMayChu=true` cho bản app trên máy test | Khẳng định đường nâng cấp B |
| 0.7 | **PoC-3:** `gbak -c` từ `bak_*.gbk` → `SYNC.FDB` rồi đọc | Phương án dự phòng C khả thi |
| 0.8 | Backup `DEMO.FDB` + toàn bộ `Data\Backup\*.gbk` ra 2 nơi (NAS + ổ ngoài) | An toàn dữ liệu |
| 0.9 | Làm việc với hãng TechPos: (a) dùng Firebird Server có phá bản quyền không, (b) có module web chính thức không (`WebService.exe`?), (c) quyền đọc DB cho bên thứ ba | Ghi nhận quyết định **mua hay tự viết** |
| 0.10 | Chốt ADR-07/08/09 + stack với chủ dự án | ADR được duyệt nội bộ |
**Deliverable:** `docs/GD0-KET-QUA-KHAO-SAT.md`, `docs/reports/*.json`, PoC chạy được.
**Nghiệm thu:** PoC đọc `DEMO.FDB` song song ≥ 30 phút không lỗi; export đủ 9 bảng hệ thống; charset chốt.

### GĐ1 — Sinh schema MySQL + nạp dữ liệu đầu (1.5 tuần)
| # | Việc | Kết quả |
|---|---|---|
| 1.1 | `Hn.SchemaGen`: đọc `RDB$RELATION_FIELDS`/`RDB$FIELDS`/`RDB$INDICES` → sinh `0001_init_generated.sql` | DDL 86 bảng đúng kiểu + charset |
| 1.2 | Script `0002_sync_tables.sql` (§4.1), `0003_web_tables.sql` (§5.7), `0004_indexes_fk.sql`, `0005_views.sql`, `0006_partitions.sql` | Schema đầy đủ index/FK |
| 1.3 | Lệnh `initial-load` trong sync service: nạp 86 bảng theo thứ tự phụ thuộc (§4.4) | MySQL có dữ liệu thật |
| 1.4 | `Hn.DataCompare`: đối chiếu COUNT, SUM tiền, MAX(TIMEMODIFIED), tập ID theo lô | Báo cáo khớp 100% |
| 1.5 | Kiểm mồ côi FK; tạo view nghiệp vụ; build `W_KHOLEDGER` lần đầu, đối chiếu `OTONKHO` | Chênh lệch = 0 |
**Deliverable:** `infra/mysql/*.sql`, script nạp, báo cáo đối chiếu.
**Nghiệm thu:** `Hn.DataCompare` 0 lệch trên 86 bảng; `V_TONKHO` khớp `OTONKHO` từng mặt hàng/kho.

### GĐ2 — Sync Service v1 + giám sát (2.5 tuần)
| # | Việc | Kết quả |
|---|---|---|
| 2.1 | Worker Service: đọc `W_SYNCTABLE`, vòng lặp incremental, batch, `ON DUPLICATE KEY UPDATE`, con trỏ an toàn | Đồng bộ 30 s/lần |
| 2.2 | Retry/backoff, dead-letter `W_SYNCROWERROR`, lock chống chạy chồng | Không mất dữ liệu khi lỗi |
| 2.3 | Job **reconcile** đêm (W2) + ghi `W_SYNCTOMBSTONE` | Phát hiện hard delete/sửa ngoài luồng |
| 2.4 | Build `W_KHOLEDGER` + `W_RPT_*` theo job 5 phút | Báo cáo chạy nhanh |
| 2.5 | Blob extractor ảnh + parse `TBANGLUONG.CHITIET` | Media ngoài DB |
| 2.6 | `/health/sync`, trang `/admin/sync`, cảnh báo email/Zalo | Vận hành được |
| 2.7 | Đăng ký Windows Service + tài khoản đọc-only + hướng dẫn backup | Chạy 24/7 tự động |
**Deliverable:** `Hn.Sync.Service` (script cài đặt PS1/MSI) + dashboard sync.
**Nghiệm thu:** thao tác thật trên desktop (bán 1 hoá đơn, sửa KH, xoá mặt hàng, xoá cứng 1 dòng) → MySQL phản ánh đúng trong ≤ 30 s (hoặc đêm đó với hard delete); chạy 72 giờ không lỗi; reconcile 0 lệch.


### GĐ3 — Nền tảng web: xác thực, phân quyền, lưới, CRUD danh mục (4 tuần)
| # | Việc |
|---|---|
| 3.1 | Solution ASP.NET Core 10 + DbUp + Dapper/EF Core + Serilog + Swagger |
| 3.2 | Đăng nhập/refresh/logout, đổi mật khẩu, lockout 5 lần, `W_USERTOKEN` |
| 3.3 | RBAC: `W_PERMISSION/W_ROLE/W_ROLE_PERMISSION/W_USER_SCOPE` + migrate quyền từ `SGROUPROLE/SFUNCTION/SREPORTROLE` |
| 3.4 | Khung SPA: layout, menu động, workspace tab, breadcrumb, tiếng Việt, dark/light |
| 3.5 | Bộ component chuẩn: DataGrid server-side (paging/sort/filter/tổng cột/xuất Excel), Form generator, dialog CRUD, Thùng rác, footer lưu vết, tìm kiếm F3 |
| 3.6 | 13 danh mục chính: mặt hàng, KH, NCC, nhân viên, kho, cửa hàng, ĐVT, nhóm hàng, nhóm KH, nhóm NCC, lý do thu chi, tỉnh thành, đơn vị chuyển phát |
| 3.7 | Audit `W_CHANGELOG` + màn hình lịch sử thao tác |
| 3.8 | Import/Export Excel cho danh mục + in mã vạch mặt hàng |
**Nghiệm thu:** UAT 13/13 màn hình danh mục; phân quyền 6 vai trò đúng; ≥ 40 unit test + 15 integration test.

### GĐ4 — Bán hàng & Kho (MVP nghiệp vụ) (5 tuần)
| # | Việc |
|---|---|
| 4.1 | **Bán lẻ**: lưới chọn hàng (đa giá/2 ĐVT/USD), F-keys, thanh toán F12 (đa phương thức = `W_THANHTOAN`), in hoá đơn, "Lưu tạm/Mở lại", huỷ bill + `TDONHANGHUY` |
| 4.2 | **Bán sỉ**: hoá đơn BS, giá sỉ, ĐVT chẵn/lẻ, chiết khấu, in phiếu |
| 4.3 | Trả lại KH (khách trả hàng, đổi hàng) |
| 4.4 | **Nhập kho**: NCC, kho nhập, nhân viên, thuế/phí VC, thanh toán + công nợ, số hoá đơn, đã xuất hoá đơn |
| 4.5 | **Xuất kho** và **Xuất trả lại NCC** |
| 4.6 | **Chuyển kho** (kho xuất → kho nhập, nhân viên xuất/nhập) |
| 4.7 | **Kiểm kê** (SL thực tế vs SL hệ thống, chênh lệch, xử lý chênh lệch) |
| 4.8 | **Tồn kho / Thẻ kho / Tồn tất cả các kho** trên `W_KHOLEDGER`; **Tính lại giá vốn** (FIFO, bình quân cuối kỳ, bình quân liên hoàn, đích danh) |
| 4.9 | Báo giá · Đặt hàng KH · Đặt mua NCC · Hợp đồng (trạng thái, nghiệm thu, thanh lý, đặt cọc) |
| 4.10 | Giao COD: tạo phiếu gửi, kết quả giao, giao hàng theo xe |
**Nghiệm thu:** nhập 1 ngày giao dịch thật (≥ 50 hoá đơn, 5 phiếu kho) trên web; **6 báo cáo đối chiếu = 0 lệch** so với desktop cùng ngày; in hoá đơn/tem đúng mẫu.

### GĐ5 — Quỹ · Công nợ · Nhân sự · Chăm sóc KH (4 tuần)
| # | Việc |
|---|---|
| 5.1 | Quỹ: danh mục lý do thu chi, phiếu thu, phiếu chi, phiếu thu/chi **nhiều đối tượng** (`TTHUCHIHANGLOAT`), tồn quỹ, tạm ứng lương |
| 5.2 | Công nợ: công nợ KH (thanh toán, in tổng hợp nợ), công nợ NCC, theo dõi nợ/đổi nợ (`W_DOINOCHITIET`) |
| 5.3 | Nhân sự: danh mục nhân viên (hồ sơ mở rộng), giấy giới thiệu, chấm công (ca + tỉ lệ lương), tính lương, thưởng phạt |
| 5.4 | CSKH: chăm sóc KH (tình trạng gọi), gửi tin nhắn (`W_TINNHAN` + SMS/Zalo/Email), khách hàng thân thiết (điểm, tặng/giảm điểm, giảm theo nhóm), phản hồi KH |
| 5.5 | Khuyến mại: đợt khuyến mại (mua/tặng), áp dụng tự động khi bán |
| 5.6 | Quản trị: thư viện ảnh, dữ liệu ban đầu (`W_DUDAUKY`), chi tiết doanh thu, khoá sổ, xoá dữ liệu (có log), lịch sử tác hệ thống, người dùng & phân quyền, cài hình toàn hệ thống (`SCONFIG` → UI cấu hình chuẩn hoá) |
**Nghiệm thu:** đối chiếu tồn quỹ & công nợ ngày bất kỳ = 0 lệch; bảng lương tháng thử khớp bản desktop.

### GĐ6 — Báo cáo · Dashboard · In ấn (3.5 tuần)
| # | Việc |
|---|---|
| 6.1 | 35–50 báo cáo (mục 6.5) + engine tham số + cache |
| 6.2 | Báo cáo biểu đồ (doanh số theo nhóm hàng, doanh thu ngày/tháng) bằng ECharts |
| 6.3 | Bản in chuẩn: hoá đơn A5/A4, phiếu kho, phiếu thu/chi, bảng lương, tem mã vạch, công nợ |
| 6.4 | Xuất Excel mọi lưới + nhập Excel (danh mục, mặt hàng, KH) |
| 6.5 | Dashboard 9 khối / 41 lối vào + KPI hôm nay (doanh thu, số bill, tồn quỹ, công nợ, hàng sắp hết, COD đang giao) |
| 6.6 | Lịch in tự động (nhắc nợ đến hạn, hàng hết hạn sử dụng) — tính năng mới |
**Nghiệm thu:** 100% báo cáo trong checklist đối chiếu được với bản desktop trên cùng kỳ dữ liệu.


### GĐ7 — Đồng bộ 2 chiều có whitelist (2 tuần)
| # | Việc |
|---|---|
| 7.1 | Whitelist bảng web được ghi ngược: danh mục, ghi chú (`SQUICKNOTE`), KH, nhắc nợ, thu chi (tuỳ chọn) |
| 7.2 | Cơ chế đẩy: outbox `W_OUTBOX(TABLE_NAME, ID, OP, PAYLOAD, STATUS)` → worker ghi vào Firebird (embedded read-write **hoặc** FB Server) → cập nhật `ORIGIN='WEB'` |
| 7.3 | Xung đột: so `TIMEMODIFIED` 2 phía; nếu desktop sửa sau ⇒ **cảnh báo** + giữ bản Firebird, ghi `W_CONFLICT` để người dùng chọn |
| 7.4 | Kiểm thử 20 tình huống xung đột/khoá ngoại trùng/trùng tên |
| 7.5 | Quy tắc sinh ID trên web: UUIDv7 + kiểm tra tồn tại ở Firebird (không trùng) |
**Nghiệm thu:** 2 tuần chạy song song không mất/không nhân bản dữ liệu; số dòng 2 bên khớp.

### GĐ8 — Di trú dữ liệu & cắt chuyển (2 tuần)
| # | Việc |
|---|---|
| 8.1 | Chọn ngày cắt chuyển (ngày nghỉ/cuối tháng); chốt kỳ khoá sổ desktop |
| 8.2 | Full re-sync lần cuối + freeze desktop (rút quyền ghi) |
| 8.3 | Đối chiếu toàn diện: 86 bảng + 6 báo cáo + tồn kho/công nợ/tồn quỹ/điểm KH/lương |
| 8.4 | Xử lý dữ liệu dở dang (hoá đơn chưa thanh toán, COD đang giao, hợp đồng mở, đơn đặt hàng) |
| 8.5 | Đào tạo người dùng theo vai trò + tài liệu thao tác (ảnh chụp màn hình web) + hotline 2 tuần |
| 8.6 | Kế hoạch lùi (rollback): giữ Firebird nguyên trạng + bản `.gbk` toàn phần; nếu sự cố lớn ⇒ mở lại desktop |
**Nghiệm thu:** UAT ký nghiệm thu từng nhóm chức năng; 3 ngày đầu chạy thật không sự cố nghiêm trọng.

### GĐ9 — Bàn giao, chạy song song & bảo trì (liên tục)
| # | Việc |
|---|---|
| 9.1 | Bàn giao mã nguồn + tài liệu (kiến trúc, ERD, API, vận hành) + video đào tạo |
| 9.2 | SaaS hoá nếu muốn (multi-tenant: thêm `W_TENANT` + `TENANT_ID`) — **thiết kế sẵn từ GĐ1 nhưng chưa bật** |
| 9.3 | Ứng dụng di động (PWA → React Native) cho chủ doanh nghiệp: báo cáo, duyệt chi, nhắc nợ |
| 9.4 | Tích hợp: hoá đơn điện tử, máy in tem, cân điện tử, Zalo OA/SMS gateway, ngân hàng (đối chiếu sao kê) |
| 9.5 | Mở API cho đối tác/khách hàng (đặt hàng online, tra cứu đơn) |
| 9.6 | Bảo trì: SLA, lịch nâng cấp MySQL/.NET, review index & hiệu năng mỗi quý |

### Bảng tổng hợp thời gian & nguồn lực
| GĐ | Nội dung | Thời lượng | Nguồn lực đề xuất |
|---|---|---|---|
| 0 | Khảo sát + PoC + backup | 1 tuần | 1 DE/SA + 1 dev .NET |
| 1 | Schema + nạp dữ liệu đầu | 1.5 tuần | 1 dev .NET (backend) |
| 2 | Sync Service v1 + giám sát | 2.5 tuần | 1 dev .NET (backend) |
| 3 | Nền tảng web + danh mục | 4 tuần | 1 backend + 1 frontend |
| 4 | Bán hàng & Kho | 5 tuần | 1 backend + 2 frontend |
| 5 | Quỹ/Nợ/HR/CSKH | 4 tuần | 1 backend + 2 frontend |
| 6 | Báo cáo/Dashboard/In | 3.5 tuần | 1 backend + 1 frontend + 1 tester |
| 7 | 2 chiều whitelist | 2 tuần | 1 backend |
| 8 | Di trú + cắt chuyển + đào tạo | 2 tuần | cả nhóm + 1 BA |
| 9 | Bàn giao & mở rộng | liên tục | 1 dev + 1 hỗ trợ |
| **TỔNG** | | **~26 tuần (≈ 6 tháng)** | 2–4 người |
> Nếu chạy **1 người bán thời gian (20 h/tuần)**: ước tính **10–12 tháng** (hệ số 2.0–2.2).
> **Đường MVP để có giá trị sớm:** GĐ0+1+2+3 + (4.1 Bán lẻ, 4.4 Nhập kho, 4.8 Tồn kho, 6.1 6 báo cáo) ≈ **10–12 tuần** ⇒ đã có "web xem số liệu + bán lẻ".

---

## 8. KIỂM THỬ & BẢO ĐẢM CHẤT LƯỢNG

### 8.1 Kim tự tháp kiểm thử
| Tầng | Nội dung | Công cụ |
|---|---|---|
| Unit | Mapping kiểu dữ liệu, tính tiền/chiết khấu/thuế (đối chiếu công thức bản cũ), tính tồn luỹ kế, watermark | xUnit + FluentAssertions |
| Integration | Sync engine với `TEMPLATE.FDB` (DB trắng của hãng) + MySQL container/database test | xUnit + Testcontainers-DB (dùng DB test đặt tên `hoanngoc_test`) |
| **Parity (đặc thù)** | 6 báo cáo + tồn kho/công nợ/tồn quỹ: chạy cùng kỳ trên Firebird (đọc trực tiếp) và MySQL, so **từng dòng** | `Hn.DataCompare` + báo cáo HTML |
| UI/E2E | Đăng nhập → bán lẻ → in hoá đơn → tra tồn kho; nhập kho; phân quyền | Playwright |
| Tải | 100 user đồng thời xem báo cáo, 20 user bán hàng | k6 / NBomber |
| Hồi quy dữ liệu | Sau mỗi migration: chạy lại toàn bộ parity | CI |

### 8.2 Tiêu chí "Definition of Done" cho mỗi màn hình
1. Có route + menu + quyền; 2. CRUD đủ (nếu là danh mục) hoặc đủ nút theo bản cũ; 3. Lưới có paging/sort/filter/xuất Excel; 4. Có footer lưu vết + audit; 5. Có unit test cho logic tính toán; 6. Đã đối chiếu số liệu với desktop; 7. Có ảnh chụp đưa vào tài liệu hướng dẫn; 8. Qua UAT của nhóm người dùng tương ứng.

### 8.3 Checklist đối chiếu số liệu (chạy hằng ngày trong giai đoạn song song)
| # | Phép đối chiếu | Tiêu chí |
|---|---|---|
| 1 | Doanh thu theo ngày / theo nhân viên / theo kho / theo nhóm hàng | lệch = 0 |
| 2 | Số hoá đơn, số phiếu nhập/xuất, số dòng chi tiết | lệch = 0 |
| 3 | Tồn kho từng mặt hàng × kho (so `OTONKHO`) | lệch = 0 |
| 4 | Tồn quỹ cuối ngày (thu − chi) | lệch = 0 |
| 5 | Công nợ KH/NCC cuối ngày | lệch = 0 |
| 6 | Bảng lương tháng: từng nhân viên | lệch = 0 |
| 7 | Điểm khách hàng thân thiết | lệch = 0 |
| 8 | Tổng số dòng 86 bảng | lệch = 0 (hoặc giải trình từng dòng lệch) |

### 8.4 Rủi ro & biện pháp
| # | Rủi ro | Khả năng | Tác động | Biện pháp |
|---|---|---|---|---|
| R1 | Firebird Embedded **không** cho phép 2 tiến trình đọc/ghi song song trong môi trường thật | Trung bình | Cao | PoC-1 ở GĐ0; nếu thất bại → **nâng lên FB 2.5.9 Server** (đường B, app đã hỗ trợ `KetNoiMayChu`), hoặc chế độ C (replica `.gbk` mỗi 5–15 phút) |
| R2 | Hãng TechPos **không cho phép** truy cập DB / phá bản quyền khi cài FB Server | Trung bình | Cao | Làm việc bằng văn bản với hãng ở GĐ0; phương án dự phòng: chỉ đọc từ `.gbk` (không đụng app), trễ chấp nhận được; hoặc **mua module web của hãng** (đã thấy `WebService.exe`) và chỉ tự làm phần bổ sung |
| R3 | Bản quyền desktop hết hạn giữa dự án | Thấp–TB | Trung bình | Gia hạn/đàm phán trước khi bắt đầu; nếu không ⇒ chuyển hẳn sang web (GĐ9) |
| R4 | Mojibake tiếng Việt (charset `NONE`/`WIN1258`) | Trung bình | Trung bình | Test charset ở GĐ0 + test tự động 200 chuỗi có dấu; nếu cần, chuẩn hoá NFC/CP1258 ở tầng sync |
| R5 | Sai lệch số tiền do `NUMERIC` vs `DECIMAL` | Thấp–TB | Cao | Giữ đúng p,s; test tổng tiền từng phiếu = 0 lệch; cấm dùng `FLOAT` cho tiền |
| R6 | Bảng `TDONHANG` đa nghiệp vụ bị hiểu sai ⇒ báo cáo sai | Cao | Cao | Khảo sát tập `LOAI/MODE` từ dữ liệu thật (GĐ0), viết view `V_*` + test parity từng nhóm |
| R7 | Hard delete ở desktop không phát hiện được | Trung bình | Trung bình | Reconcile đêm bắt buộc; tùy chọn W3 (trigger log) nếu được phép |
| R8 | Người dùng phản đối vì web "khó dùng hơn" | Cao | Cao | Nhân bản phím tắt/thói quen (§6.3), đào tạo theo vai trò, chạy song song 4 tuần, giữ desktop tra cứu |
| R9 | Dữ liệu khối lượng lớn dần (log, chứng từ nhiều năm) | Cao | Trung bình | Partition theo năm từ GĐ1, archive log 24 tháng, index cho mọi truy vấn báo cáo |
| R10 | Mất dữ liệu MySQL (máy chủ hỏng) | Thấp | Cao | Backup hằng đêm (mysqldump + binlog PITR), UPS, restore test mỗi tháng, giữ `.gbk` gốc |
| R11 | Bảo mật (web nội bộ bị lộ) | Thấp–TB | Cao | HTTPS, JWT ngắn hạn, RBAC, rate-limit, log, không expose Internet, cập nhật vá MySQL/.NET |
| R12 | Phụ thuộc 1 người làm (bus factor) | Cao | Cao | Tài liệu hoá tối đa, repo git + README từng module, script deploy tự động |


---

## 9. VẬN HÀNH, SAO LƯU & AN TOÀN DỮ LIỆU

### 9.1 Lịch sao lưu (3-2-1)
| Đối tượng | Tần suất | Công cụ | Nơi lưu | Kiểm thử phục hồi |
|---|---|---|---|---|
| MySQL `hoanngoc` | Hằng ngày 23:30 (full) + binlog (từng 15 phút) | `mysqldump --single-transaction --routines` + `mysqlbinlog` | NAS + ổ ngoài | Restore vào DB test mỗi tháng |
| Ảnh/tệp media | Hằng ngày | Robocopy/7-Zip | NAS | — |
| `DEMO.FDB` (nguồn gốc) | Hằng ngày (đọc-only copy sau khi desktop đóng, hoặc dùng gbak có sẵn) | Copy/gbak | NAS + ổ ngoài | — |
| Cấu hình hệ thống (`.dat`, `firebird.conf`) | 1 lần + khi thay đổi | Zip | NAS + repo `infra/` | — |
| Mã nguồn | mỗi commit | Git (server nội bộ hoặc GitHub private) | 2 nơi | CI build lại |

### 9.2 Vận hành hằng ngày
- Trang `/admin/sync` xem lag + lỗi; `/health` cho giám sát tự động.
- Nhật ký đồng bộ lưu 90 ngày trong `W_SYNCRUN/W_SYNCBATCHLOG`, sau đó archive.
- Xử lý sự cố mẫu (viết sẵn playbook):
  1. *MySQL down* → sync retry tự động, cảnh báo; web trả "hệ thống tạm ngưng đọc dữ liệu".
  2. *File FB bị khoá* → chuyển chế độ C, tiếp tục phục vụ dữ liệu cũ, cảnh báo IT.
  3. *Lệch số liệu báo cáo* → chạy `reconcile` bảng liên quan → nếu vẫn lệch, dùng `Hn.DataCompare` xuất file chi tiết để tìm nguyên nhân.
  4. *Restore khẩn cấp* → dừng web, restore dump gần nhất + replay binlog, dựng lại `W_KHOLEDGER`, mở lại web.

### 9.3 Tăng trưởng & bảo trì
- Tính toán dung lượng: `TDONHANG` ~ 30 MB/năm với < 100 hóa đơn/ngày ⇒ 10 năm ≈ 300 MB — **không đáng lo**; rủi ro chỉ ở log (`STRACKING`, `TLUUVET`) và ảnh.
- Mỗi quý: review slow query log, thêm index, `ANALYZE TABLE`, cập nhật thống kê.
- Mỗi năm: kiểm tra lại bản quyền hãng, xem xét bỏ hoàn toàn desktop, đánh giá mở API/mobile.

---

## 10. ƯỚC LƯỢNG CHI PHÍ (để lập ngân sách — thay số theo thực tế)

### 10.1 Hạ tầng (một lần + định kỳ)
| Hạng mục | Chi phí tham khảo | Ghi chú |
|---|---|---|
| Server nội bộ (Core i5/i7, 32 GB RAM, SSD 512 GB, UPS) | 15–30 triệu VNĐ | Nếu dùng máy trạm sẵn có ⇒ 0 (không khuyến nghị cho môi trường chạy thật) |
| NAS/ổ ngoài sao lưu | 3–6 triệu VNĐ | 2 nơi theo 3-2-1 |
| Windows Server license (nếu dùng Windows Server) | ~ 15–25 triệu VNĐ | Có thể dùng **Win 11 Pro** có sẵn để tiết kiệm |
| Tên miền nội bộ + chứng chỉ HTTPS | 0–2 triệu VNĐ/năm | Chứng chỉ nội bộ (self-signed) miễn phí |
| SMS/Zalo OA/Zalo Mini App, email | 200–500k VNĐ/tháng | Chỉ khi làm CSKH đa kênh (GĐ5) |
| **Tổng hạ tầng** | **~20–60 triệu VNĐ** | |

### 10.2 Nhân công (theo lộ trình 26 tuần)
| Phương án | Chi phí tham khảo | Ghi chú |
|---|---|---|
| Thuê ngoài trọn gói (2–4 người, 6 tháng) | **500 triệu – 1.2 tỷ VNĐ** | Giá thị trường VN 2026, tuỳ mức độ phức tạp thực tế |
| Tự làm (1 dev bán thời gian, 10–12 tháng) | Chi phí cơ hội nội bộ | Kiểm soát tốt, chậm hơn, rủi ro bus-factor cao |
| Thuê thêm 1 dev full-time hỗ trợ | 25–45 triệu VNĐ/tháng | Rút ngắn ~25–30% thời gian |
| Mua module web của hãng (nếu có) | **cần báo giá hãng** | Có thể rẻ hơn nhiều và đồng bộ "chính chủ"; nhưng phụ thuộc hãng, khó tuỳ biến |
| Phí bảo trì sau bàn giao | 10–20% giá trị dự án/năm | Bao gồm sửa lỗi, hỗ trợ, nâng cấp nhỏ |

### 10.3 Phương án "tối thiểu rủi ro" nên cân nhắc trước khi chi lớn
1. **Gọi hãng TechPos** hỏi rõ về `WebService.exe`/`WebProcessor.exe` — nếu hãng bán module web dùng MySQL/report web, so sánh giá với phương án tự làm (đặc biệt phần Báo cáo/In ấn tốn nhiều công nhất).
2. Nếu không mua: vẫn nên làm **GĐ0–GĐ2** trước (chỉ ~5 tuần) — kể cả khi dừng dự án, doanh nghiệp vẫn được lợi: **có bản sao dữ liệu trên MySQL, có backup, có công cụ đối chiếu, và mở đường dùng công cụ BI (Power BI/Metabase) ngay trên MySQL** để xem báo cáo mà không cần viết web.

---

## 11. PHỤ LỤC

### 11.1 Ma trận truy vết chức năng → bảng dữ liệu (trích; đầy đủ ở `DANH-MUC-CHUC-NANG.md` mục 2)
| Màn hình / chức năng | Bảng chính | Bảng mới/ghi chú |
|---|---|---|
| Danh mục ▸ Mặt hàng | `DMATHANG`, `DDINHLUONG`, `DHANGSANXUAT`, `DNHOMMATHANG`, `DDONVITINH` | `W_MEDIA` (ảnh), `W_BANGGIA` |
| Danh mục ▸ Khách hàng | `DKHACHHANG`, `DNHOMKHACHHANG`, `DTINHTHANH` | `W_DIEM` (nếu chưa có), `W_LOAINGANH` |
| Bán hàng ▸ Bán lẻ/bán sỉ | `TDONHANG`, `TDONHANGCHITIET`, `TCHITIETTHANHTOAN` | `W_THANHTOAN`, `W_DOC_TYPE` |
| Kho ▸ Nhập/Xuất/Chuyển/Kiểm kê | `TDONHANG`, `TDONHANGCHITIET`, `DKHOHANG` | **`W_KHOLEDGER`**, `V_TONKHO`, `V_THEKHO` |
| Quỹ ▸ Phiếu thu/chi | `TTHUCHI`, `TCHITIETTHUCHI`, `TTHUCHIHANGLOAT`, `DLYDOTHUCHI`, `DLOAIQUY` | `V_TONQUY` |
| Công nợ | `TDONHANG`, `TCHITIETTHANHTOAN`, `TDATHANGNCC`, `DNHACUNGCAP` | `V_CONNOKHACHHANG`, `V_CONNONCC`, `W_DOINOCHITIET` |
| Nhân sự ▸ Lương | `DNHANVIEN`, `TBANGLUONG`, `TBANGLUONGCHITIET`, `TBANGLUONGTONGHOP`, `DCALAMVIEC`, `TTHUONGPHAT` | `W_PAYROLL_DATA` (parse BLOB) |
| CSKH ▸ Chăm sóc KH | `TCHAMSOC`, `DKHACHHANG`, `SSMSTEMPLATE` | `W_TINNHAN`, `W_TINNHANCHITIET` |
| Quản trị ▸ Phân quyền | `SUSER`, `SGROUPUSER`, `SGROUPROLE`, `SFUNCTION`, `SREPORT`, `SREPORTROLE` | `W_PERMISSION`, `W_ROLE*`, `W_USER_SCOPE` |
| Quản trị ▸ Khóa sổ | `TKHOASO` | — (giữ nguyên hành vi chặn sửa chứng từ trước ngày khoá) |
| Hàng bị khoá nội bộ | `STABLEDESC`, `SFORM`, `SGRID`, `SCOLUMN`, `SMENU`, `STEMPLATE`, `SREPORTTEMPLATE`, `SIMAGE`, `SANALYSIS`, `TLUUVET`, `STRACKING`, `TNGUOIDUNGTHEOCUAHANG`, `OTONKHO` | Export 1 lần để bảo toàn; `SYNC_MODE=NONE` cho UI |


### 11.2 Lệnh/tiện ích cần có trong repo (để thao tác nhanh, ít sai)
| Lệnh | Mục đích |
|---|---|
| `dotnet run --project tools/Hn.FbExtract -- export-catalog` | Export `SREPORT/SMENU/SCOLUMN/SFORM/SGROUP*` |
| `dotnet run --project tools/Hn.SchemaGen -- generate-ddl` | Sinh DDL MySQL từ metadata Firebird |
| `dotnet run --project tools/Hn.FbExtract -- initial-load --tables all` | Nạp đầu kỳ |
| `dotnet run --project tools/Hn.DataCompare -- compare --all` | Đối chiếu số liệu FB ↔ MySQL |
| `dotnet run --project tools/Hn.DataCompare -- compare --report TONG_HOP_CONG_NO` | Đối chiếu 1 báo cáo |
| `dotnet run --project src/Hn.Sync.Service -- run-once` | Chạy 1 mẻ sync (debug) |
| `dotnet run --project src/Hn.Sync.Service -- reconcile` | Chạy reconcile ngay |
| `infra/deploy-api.ps1`, `infra/install-service.ps1`, `infra/backup-all.ps1` | Triển khai & sao lưu |

### 11.3 Sổ đăng ký PoC kỹ thuật (phải xong trước khi cam kết tiến độ)
| # | PoC | Điều kiện đạt |
|---|---|---|
| P1 | Đọc `DEMO.FDB` song song với app (FB 2.5 Embedded, read-only) | 30 phút ghi/đọc song song không lỗi, app không treo |
| P2 | Chuyển app sang FB 2.5.9 Server (`KetNoiMayChu=true`) | App chạy đủ chức năng, in ấn OK, không lỗi bản quyền |
| P3 | Restore từ `.gbk` → đọc & sync | Đọc được 86 bảng, số liệu khớp bản gốc |
| P4 | Charset: 200 chuỗi có dấu khớp byte-for-byte | 100% khớp |
| P5 | Tiền tệ: tổng 10 hoá đơn bất kỳ khớp 0 lệch | 100% khớp |
| P6 | Lưới 50.000 dòng hiển thị < 3 s | Đạt yêu cầu |

### 11.4 Bảng ánh xạ menu gốc → route web (mẫu — hoàn thiện ở GĐ3)
| Menu gốc | Route web đề xuất | Quyền |
|---|---|---|
| HỆ THỐNG ▸ Mở nhanh / Thoát / Đăng nhập | `Ctrl+K` command palette, `/login`, `/logout` | — |
| HỆ THỐNG ▸ Cấu hình hệ thống | `/admin/config` | Admin |
| BÁN HÀNG ▸ Bán lẻ | `/ban-hang/ban-le` | Bán hàng |
| BÁN HÀNG ▸ Bán sỉ | `/ban-hang/ban-si` | Bán hàng |
| BÁN HÀNG ▸ Trả lại | `/ban-hang/tra-lai` | Bán hàng |
| KHO HÀNG ▸ Nhập kho | `/kho/nhap-kho` | Thủ kho |
| KHO HÀNG ▸ Xuất kho / Xuất trả NCC | `/kho/xuat-kho`, `/kho/xuat-tra-ncc` | Thủ kho |
| KHO HÀNG ▸ Chuyển kho / Kiểm kê | `/kho/chuyen-kho`, `/kho/kiem-ke` | Thủ kho |
| KHO HÀNG ▸ Tồn kho / Thẻ kho / Tồn tất cả kho | `/kho/ton-kho`, `/kho/the-kho`, `/kho/ton-tat-ca` | Kho + Kế toán |
| KHO HÀNG ▸ Tính lại giá vốn | `/kho/tinh-lai-gia-von` | Kế toán trưởng |
| QUỸ ▸ Phiếu thu / Phiếu chi | `/quy/phieu-thu`, `/quy/phieu-chi` | Thu ngân/Kế toán |
| CÔNG NỢ ▸ Công nợ KH / NCC / Theo dõi nợ | `/cong-no/khach-hang`, `/cong-no/ncc`, `/cong-no/theo-doi` | Kế toán |
| NHÂN SỰ ▸ Chấm công / Tính lương / Thưởng phạt | `/nhan-su/cham-cong`, `/nhan-su/tinh-luong`, `/nhan-su/thuong-phat` | HR/Kế toán |
| QUẢN TRỊ ▸ Người dùng & phân quyền | `/admin/nguoi-dung`, `/admin/phan-quyen` | Admin |
| QUẢN TRỊ ▸ Khóa sổ | `/admin/khoa-so` | Kế toán trưởng |
| QUẢN TRỊ ▸ Đồng bộ dữ liệu (mới) | `/admin/sync` | Admin/IT |
| BÁO CÁO ▸ (9 nhóm) | `/bao-cao/{code}` | Theo `SREPORTROLE` |

### 11.5 Glossary
| Thuật ngữ | Nghĩa trong dự án |
|---|---|
| **Watermark** | Con trỏ thời gian (kèm ID) đánh dấu đã đồng bộ tới đâu — dùng cho incremental |
| **Reconcile** | Job đối chiếu định kỳ 2 CSDL để tìm bản ghi lệch/thiếu/xoá cứng |
| **Tombstone** | Bản ghi đánh dấu "đã bị xoá ở nguồn" (không xoá vật lý ở MySQL) |
| **Parity test** | Kiểm thử tính tương đương số liệu giữa hệ thống cũ và mới |
| **Outbox** | Bảng trung gian chứa thay đổi cần đẩy ngược về Firebird (GĐ7) |
| **Ledger kho** | Sổ chi tiết nhập–xuất–tồn theo từng dòng chứng từ (`W_KHOLEDGER`) |
| **ODS** | On-Disk Structure — định dạng file của Firebird (2.5 ⇒ ODS 11.2) |
| **Embedded** | Firebird nhúng: chạy trong tiến trình app, không dùng TCP, bỏ qua security DB |

---

## 12. CÂU HỎI CẦN CHỐT VỚI CHỦ DỰ ÁN (trước khi bắt đầu GĐ1)
| # | Câu hỏi | Ảnh hưởng |
|---|---|---|
| Q1 | Có chấp nhận **cài Firebird 2.5.9 Server** trên máy quầy (phương án B) nếu PoC-1 thất bại? | ADR-01; có thể phải thương lượng hãng |
| Q2 | Hãng TechPos có **bán module web** không? Giá bao nhiêu? | Có thể rút ngắn/không cần dự án |
| Q3 | Bản quyền desktop còn hạn bao lâu? Giấy phép có cho phép truy cập DB không? | Rủi ro pháp lý & kỹ thuật |
| Q4 | Sau cùng **hệ thống nào là master**? Có giữ desktop bán hàng song song lâu dài? | ADR-02, ADR-12 |
| Q5 | Cần **web ghi chứng từ** ngay GĐ4 hay chỉ **xem báo cáo** trước? | Chọn đường MVP ~3 tháng hay ~6 tháng |
| Q6 | Số người dùng đồng thời, số điểm bán/kho? | Cấu hình server, index |
| Q7 | Đã có máy chủ nội bộ / NAS chưa? | Chi phí hạ tầng |
| Q8 | Yêu cầu tích hợp ngoài (hoá đơn điện tử, Zalo OA, ngân hàng, sàn TMĐT) ở giai đoạn nào? | Phạm vi GĐ9 |
| Q9 | Ai là key user nghiệm thu từng nhóm chức năng? | Kế hoạch UAT |
| Q10 | Ngân sách & deadline mong muốn? | Chọn phương án nhân công/mua |


---

## 13. VIỆC CẦN LÀM NGAY (3 ngày đầu)
| # | Việc | Cách làm cụ thể | Ai |
|---|---|---|---|
| 1 | **Sao lưu an toàn** | Copy `DEMO.FDB` + `Data\Backup\*.gbk` ra ổ ngoài/NAS; ghi SHA256 để kiểm chứng | IT |
| 2 | **Chạy PoC-1** | Tạo `Hn.Poc.FbRead` (.NET 10 console) tham chiếu `FirebirdSql.Data.FirebirdClient` dòng 2.5, copy `fbembed.dll` + `icudt30/icuin30/icuuc30.dll` + `firebird.msg` cạnh file exe, dùng chuỗi kết nối Embedded tới `Database\DEMO.FDB`, vòng lặp `SELECT FIRST 5 * FROM DMATHANG` mỗi 10 s trong 30 phút **trong lúc dùng app bán hàng** | Dev .NET |
| 3 | **Chạy 7 SQL kiểm chứng** | Theo mục 6 của `DOI-CHIEU-CSDL-VA-CHUC-NANG.md`; ghi kết quả vào `docs/GD0-KET-QUA-KHAO-SAT.md` | Dev/BA |
| 4 | **Export cấu hình** | `SREPORT` (SQL/PARAMS/FILTERCONFIG) + `SMENU` + `SGROUPROLE` + `SFUNCTION` → `docs/reports/` | Dev |
| 5 | **Đếm dữ liệu & kiểm charset** | Script đếm COUNT 86 bảng; `SELECT RDB$CHARACTER_SET_NAME FROM RDB$DATABASE`; test 20 chuỗi có dấu | Dev |
| 6 | **Gọi hãng TechPos** | Hỏi Q2, Q3 + xác nhận có được phép truy cập DB bằng công cụ bên ngoài | Chủ dự án |
| 7 | **Chuẩn bị môi trường** | Tạo database `hoanngoc` + user `hn_sync`/`hn_api` trên MySQL 8.0.45; tạo repo git + cấu trúc thư mục (§2.5) | Dev |
| 8 | **Tạo project skeleton** | `dotnet new sln`; `dotnet new worker -o src/Hn.Sync.Service`; `dotnet new webapi -o src/Hn.Api`; `npm create vite@latest web -- --template react-ts`; commit đầu tiên | Dev |

> ⚠️ **Nhắc lại giới hạn môi trường hiện tại:** máy này **chưa có** `isql`/`gsec`/`gbak` (công cụ Firebird), **chưa có Docker**, `mysql` CLI chỉ có bản đi kèm Workbench/Shell. Vì vậy:
> - GĐ0 cần tải **Firebird 2.5.9 (Server/Classic + client tools)** để có `gbak`/`isql` cho PoC-2/PoC-3.
> - Kết nối MySQL nên qua provider `MySqlConnector` trong ứng dụng .NET thay vì dựa vào CLI.
> - `fbembed.dll` **không hỗ trợ kết nối TCP** ⇒ nếu chọn ADR-01 phương án B thì phải cài engine 2.5.9 đầy đủ (không chỉ DLL); ngược lại giữ chế độ A cho sync.

---

## 14. KẾT LUẬN NGẮN
1. **Khả thi** — dữ liệu nhỏ (19.5 MB), cấu trúc đã liệt kê đầy đủ (86 bảng), nghiệp vụ đã phân tích (76 mục menu / ~41 màn hình), môi trường có sẵn .NET 10 + MySQL 8.0.45.
2. **Điểm nghẽn duy nhất đáng lo** là **cách truy cập `DEMO.FDB`** (Firebird 2.5 Embedded) — giải quyết trong **1 tuần PoC** với 3 phương án (A: embedded song song; B: nâng lên FB 2.5.9 Server; C: replica từ `.gbk`), trong đó phương án C **luôn hoạt động** làm lưới an toàn.
3. **Rủi ro lớn hơn kỹ thuật là quan hệ với hãng TechPos** (bản quyền, quyền truy cập DB, module web đã có) ⇒ việc đầu tiên là gọi hãng, chạy song song với PoC kỹ thuật.
4. **Đường đi an toàn nhất**: làm GĐ0→GĐ2 (~5 tuần) để có bản sao MySQL + service đồng bộ + công cụ đối chiếu; kể cả khi chưa làm web, doanh nghiệp đã hưởng lợi (backup có kiểm soát, dùng BI/report trên MySQL, báo cáo ngoài giờ). Sau đó quyết định tiếp GĐ3–GĐ6 hay mua module của hãng.
5. **Để có "web dùng được"** (xem số liệu + bán lẻ + tồn kho + 6 báo cáo): **~10–12 tuần**. **Đầy đủ 100% chức năng**: **~26 tuần** với nhóm 2–4 người.

---

*Tài liệu này là kế hoạch thi công, không phải cam kết tiến độ. Mọi mốc thời gian cần xác nhận lại sau GĐ0 (PoC + trả lời 10 câu hỏi ở §12).*

