# KHẢO SÁT THƯ MỤC CÀI ĐẶT — TECHPOS SM4 v4.5

> **Đường dẫn:** `C:\Program Files (x86)\TechPos\SM4\v4.5`
> **Ngày khảo sát:** 22/09/2026 · **Phương thức: CHỈ ĐỌC** (`Get-ChildItem`, `FileVersionInfo`, `icacls`, trích chuỗi từ EXE/DLL) — **không sửa, không chạy, không xoá** bất kỳ file nào.
> **Quy mô:** **137 file · ~101 MB**, 3 thư mục con: `Data\`, `Database\`, `res\` (không có file README/help/tài liệu kỹ thuật nào).

---

## 1. Bản đồ thư mục

| Nhóm | Nội dung | Số file | Dung lượng |
|---|---|---|---|
| Chương trình | `Application.exe` (loader 32 KB) + `No1Lib.*.DLL` (khung SM) | 11 | ~9,4 MB |
| Thư viện giao diện | Krypton (5 DLL), SourceGrid, FastColoredTextBox, DataVisualization | 8 | ~5,6 MB |
| In & xuất file | FastReport (4 file + XML), Aspose.Cells, Ionic.Zip, Bartender | 12 | ~14,5 MB |
| **Firebird runtime** | `fbembed.dll` 2.5.5.26952, `FirebirdSql.Data.FirebirdClient.DLL` 2.5.2.0, ICU 3.0 (3 DLL) | 5 | ~6,5 MB |
| **Module web của hãng** | `WebService.exe`, `WebProcessor.EXE`, `res\ui\*.php` (27), `res\js` (5), `res\css` (1), `res\img` (39) | 75 | ~1,1 MB |
| **Dữ liệu** | `Database\DEMO.FDB` (19,5 MB), `Data\TEMPLATE.FDB` (19,5 MB), `Data\Backup\*.gbk` ×2 (~18 MB), 10 file `.dat`, `Data\cache…dat` (5,7 MB), `.png`/`.btw` | 19 | ~68 MB |
| Khác | `firebird.log`, `sqlDesc.xml`, `TipOfTheDay.xml`, `teamviewer.exe` | 4 | ~3,1 MB |

---

## 2. Nhóm chương trình & thư viện

| File | Phiên bản | Ý nghĩa |
|---|---|---|
| `Application.exe` | 1.0.0.0, **32 KB** | Chỉ là **loader**; toàn bộ logic nằm trong `No1Lib.*` ⇒ **không thể vá/sửa app** (đúng như ADR-03: mọi thay đổi phải nằm ngoài desktop) |
| `No1Lib.Core.Dll`, `No1Lib.Sys.DLL` (5,9 MB), `No1Lib.Sys.Ts.DLL`, `No1Lib.Db.DLL`, `No1Lib.Run.dll`, `No1Lib.Utils.DLL`, `No1Lib.Designer.DLL`, `No1Lib.PluginHelper.DLL` | 1.0.0.0 | Khung nghiệp vụ + engine form/grid động (low-code) của SM4 |
| `DbMapping.DLL` | 0.0.0.0 (14/09/2018) | Ánh xạ bảng/cột DB ↔ đối tượng |
| `ComponentFactory.Krypton.*` (5 DLL) | 2018 | Bộ điều khiển giao diện (ribbon, docking, navigator) |
| `FastReport.DLL`, `.Bars`, `.Editor` + `FastReport.xml` (1,35 MB) | 2018 | Engine in báo cáo ⇒ mẫu in nằm trong `STEMPLATE`/`SREPORTTEMPLATE` (BLOB) |
| `Aspose.Cells.DLL` (5,5 MB) | 2018 | Xuất Excel |
| `SourceGrid.DLL`, `FastColoredTextBox.DLL`, `sqlDesc.xml` (4,3 KB) | 2018 | Lưới dữ liệu + editor SQL (công cụ nhà phát triển); `sqlDesc.xml` chỉ là **từ khoá tô màu SQL** |
| `System.Windows.Forms.DataVisualization.dll` | 2018 | Biểu đồ (nhóm BÁO CÁO BIỂU ĐỒ, `SANALYSIS`) |
| `Ionic.Zip.Reduced.DLL` | — | Nén/giải nén (backup `.zip`) |
| `Data\Bartend\` (`2cot.btw`, `3cot.btw`, `Empty.btw`, `bin\BartenderPrint.exe` + Interop DLL) | 2018 | **In mã vạch** qua BarTender (nút "In mã vạch" ở Danh mục mặt hàng) |
| `teamviewer.exe` (3,1 MB) | 2018 | Hãng vào máy hỗ trợ từ xa ⇒ **lưu ý bảo mật khi go-live** |
| `Icon.ico`, `Data\logo.png`, `Data\Banner.png` | — | Nhận diện thương hiệu |

## 3. Nhóm Firebird runtime

| File | Giá trị | Ý nghĩa cho dự án |
|---|---|---|
| `fbembed.dll` | **WI-V2.5.5.26952** (3,8 MB) | Engine **Embedded ODS 11.2**; chỉ truy cập file cục bộ, **không TCP** ⇒ quyết định ADR-01 phương án A |
| `FirebirdSql.Data.FirebirdClient.DLL` | **2.5.2.0** | Provider .NET app đang dùng ⇒ sync service nên dùng đúng dòng này |
| `icudt30.dll`, `icuin30.dll`, `icuuc30.dll` | ICU 3.0 | Bộ chuyển mã cho charset (VN) — **phải copy kèm** khi chạy PoC |
| `firebird.log` (2,3 KB, ghi lần cuối **16/09/2026 15:40:37**) | **7 lần ghi** (15/09 16:14 · 16:15 · 17:27; 16/09 13:47 · 15:38:08 · 15:38:35 · **15:40:37**) trên máy `ANH-VU-HANSOME`, cùng nội dung `ib_util init failed, UDFs can't be used`, dò 2 đường dẫn: `<app>\bin/ib_util.dll` và `<app>\ib_util.dll` | ⚠️ **Thiếu `ib_util.dll`**, và app **mong đợi có thư mục `bin\`** (không tồn tại) ⇒ bản cài **chưa bao giờ có UDF**. Xem phát hiện #5 và §4.5 |
| (không có) `gbak`/`isql`/`gsec`/`fbserver` | — | Xác nhận không có công cụ CLI Firebird trên máy (khớp §13 kế hoạch) |

## 4. ⭐ Module web của hãng — đã có sẵn trong bản cài

**Phát hiện quan trọng nhất của lần khảo sát này:** bản cài **đã kèm một module web hoàn chỉnh** (không phải PHP chạy bằng Apache/IIS).

### 4.1 Kiến trúc thực tế

```
Trình duyệt ──HTTP──▶ WebService.exe      (C# .NET, TcpListener, lắng nghe Port 8080)
                              │  gọi
                              ▼
                        WebProcessor.EXE  (managed, tham chiếu Microsoft.VisualBasic = classic VB.NET)
                              │  FirebirdSql.Data.FirebirdClient 2.5.2
                              ▼
                        fbembed.dll ──▶ Database\DEMO.FDB
```

- Nội dung trong `res\ui\*.php` **thực chất là template HTML** (không có thẻ `<?php` nào): engine chỉ **thay thế token** `[DATA]`, `[TENHANG]`, `[TONGDOANHTHU]`, `[KHOHANG]`, `[NHOMHANG]`… rồi trả về trình duyệt. Vì vậy **PHP runtime là không cần thiết** cho dự án.
- `WebProcessor.EXE` chứa **toàn bộ SQL nghiệp vụ dạng chuỗi UTF-16** (bóc được ở mục 4.4).
- Bằng chứng phiên bản: `FileVersionInfo` cả 2 EXE = `1.0.0.0`; chuỗi nội bộ `LoginHandler`, `IsValidLogin`, `httpHeaders`, `userName`, `passWord`, `http_url`, `TryConnect`, `SUSERRow` (đăng nhập đối chiếu bảng `SUSER`), `TcpListener`, `UTF8Encoding`.

### 4.2 Danh mục 27 trang web có sẵn trong `res\ui`

| Tệp | Chức năng | Route/đối số quan sát được |
|---|---|---|
| `login.php` | Đăng nhập (mặc định điền sẵn `Admin`) | `POST login?username=..&password=..` → `1` OK, `2` lỗi kết nối |
| `main.php` (thư mục `shop`) | Menu 14 nút báo cáo | — |
| `doanhthu.php` | Doanh thu trong ngày | `donhang?id=` |
| `chitiettheomathang.php` | Mặt hàng bán trong ngày (giá vốn) | `fromDay,fromMonth,fromYear,toDay,toMonth,toYear,nhom,laitheo` |
| `thuchi.php` | Thu chi trong ngày | — |
| `loctonghop.php` → `tonghop.php` | Báo cáo tổng hợp bán hàng | chọn kỳ rồi xem |
| `locchitiet.php` → `chitiet.php` | Báo cáo chi tiết bán hàng | — |
| `loctonghoptheothungan.php` → `tonghoptheothungan.php` | Tổng hợp theo thu ngân | — |
| `locchitiettheothungan.php` → `chitiettheothungan.php` | Chi tiết theo thu ngân | — |
| `locchitiettheomathang.php` | Báo cáo lãi lỗ | `nhom`, `laitheo` |
| `locdoanhsotheothang.php` → `doanhsotheothang.php` | Doanh số theo tháng | — |
| `loctonghopthuchitheolydo.php` → `tonghopthuchitheolydo.php` | Thu chi theo lý do | — |
| `loctonkho.php` → `tonkho.php` / `chitiettonkho.php` / `tonquy.php` | Tồn kho, chi tiết tồn kho, tồn quỹ | `nhom`, `kho` |
| `locdanhmuchanghoa.php` → `danhmuchanghoa.php` | Danh mục hàng hoá | — |
| `tonghopcongno.php`, `tonghopcongnoncc.php` | Công nợ khách hàng / nhà cung cấp | — |
| `trangthai.php` | Trạng thái hiện tại | — |
| `donhang.php` | Chi tiết đơn hàng | `id` |
| `loi.php` | Trang lỗi | — |

> Giao diện dùng **jQuery Mobile** (`jquery.js`, `jquerymobile.js`, `simpledialog.js`, `utils.js`) và 39 ảnh trong `res\img` ⇒ module này là bản **mobile web** cũ, không responsive cho desktop hiện đại.

### 4.3 ⭐ Phát hiện: **"Thuần Việt Shop"** — sản phẩm khác được nhúng trong bản cài

`res\ui\shop\main.php` có tiêu đề `<h1>Thuần Việt Shop</h1>`, và toàn bộ SQL bóc được dùng bảng/tên **khác hẳn** mô hình TechPos SM4:

- `CNHANVIEN` (nhân viên/thu ngân), `DKHUVUC` (khu vực), `DMATHANG`, `DNHOMMATHANG`, `DDONVITINH`, `DNHACUNGCAP`, `DNHOMNHACUNGCAP`, `TDONHANG`, `TDONHANGCHITIET`, `TTHUCHI`, `SUSER`, `SCONFIG`.
- Đặc trưng dữ liệu: `LoaiPhieu=0`, `DATHANHTOAN=30`, `LOAI=6` (Thẻ nạp), `LOAI=5`, `TILEGIAMGIA`, `SLXUAT`, `SLNHAP`, và hằng số trạng thái **`STATUS = 30`** ("đang dùng").
- Nguyên văn một số câu SQL tìm thấy trong `WebProcessor.EXE`:
  - `SELECT ID, NAME FROM DNHOMMATHANG WHERE STATUS = 30 ORDER BY NAME`
  - `SELECT NAME, ID, IMAGENAME FROM DKHUVUC WHERE STATUS = 30 ORDER BY NAME`
  - `SELECT ID, NAME FROM SUSER` · `SELECT TEXTVALUE FROM SCONFIG WHERE NAME = 'Application'`
  - `SELECT COUNT(*) FROM TDONHANG WHERE NGAY BETWEEN @FROMDATE AND @TODATE` (3 tham số lặp nhiều lần ⇒ có **report động** ngoài `res\ui`)
  - Khối lãi lỗ: `SUM((TDONHANGCHITIET.DONGIA - DMATHANG.GIAVON) * (COALESCE(SLXUAT,0)-COALESCE(SLNHAP,0))) AS LAI`
  - Công nợ: `COALESCE(CASE WHEN LOAI = 2 THEN -TONGCONG ELSE TONGCONG END, 0) - ... AS TONGNO FROM TDONHANG INNER JOIN DNHACUNGCAP`

**Hệ quả cho dự án:**
1. Khớp với **`data.dat` (thư mục gốc)**: `Module=**Billiards**`, và đặc biệt dòng `DuLieu=E:\KSoft\Theo Yeu Cau\**ThuanViet-QLKD**\ThuanViet-SM\bin\Debug\Database\DuLieu_2.5.fdb` ⇒ đây chính là **sản phẩm Thuần Việt** do cùng nhóm phát triển, bản cài SM4 chỉ mang theo phần web của sản phẩm đó ⇒ **không dùng được trực tiếp** cho bộ dữ liệu *Quản lý bán hàng không công nợ v4.5*.
2. ⚠️ **Danh mục 86 bảng trong `TechPos_SM4_Database_Structure.md` là CHƯA ĐẦY ĐỦ**: toàn bộ tên bảng liệt kê chỉ có tiền tố `D/O/S/T`, **không có `CNHANVIEN` và `DKHUVUC`** (và nhiều bảng Billiards khác: `DKHUVUC`, `CNHANVIEN`, có thể cả thẻ nạp/giữ xe). Điều này giải thích vì sao 86 bảng là "kết quả có hạn" của công cụ trích xuất — **khi có quyền truy cập DB phải liệt kê lại toàn bộ** (2 cách chắc chắn: `SELECT ... FROM RDB$RELATIONS` và soát kích thước file `.FDB`).
3. Đây cũng là **rủi ro danh mục/bản quyền**: cần hỏi hãng chính xác module nào đang được licence — ⚠️ **không có file `BanQuyen.dat`** trong thư mục TechPos (đã quét toàn bộ); thông tin licence thực tế nằm trong **`data.dat` (thư mục gốc): `MaDangKy=650342159-60317`, `MaBaoVe=1073`, `TrangThai='Thành công, 123.24.152.19'`** ⇒ dùng chính 3 giá trị này khi hỏi hãng (Q13).

### 4.4 Tệp cấu hình (`.dat`, `.xml`) — đã đọc và xác minh

> ⚠️ **Phân biệt LEGACY vs ĐANG DÙNG** (đây là chỗ tài liệu kế hoạch trước đây ghi nhầm đường dẫn):
> - **`data.dat` (thư mục GỐC app)** = file **cũ từ 16/03/2018**, **không** được cập nhật khi app chạy (mtime vẫn 2018) ⇒ coi như **legacy/di sản**, chỉ dùng để truy vết nguồn gốc sản phẩm.
> - **`Data\config.dat`** (128 byte, mtime **16/09/2026 15:40:37**) + **`Data\test.dat`** (9.728 byte, cùng mtime) = file **app ghi lúc chạy thật** (mtime trùng đúng `firebird.log` 16/09/2026 15:40:37 và mtime `DEMO.FDB`) ⇒ **đây mới là nguồn tham số để đọc**.
> - **`Data\conn.dat`** (108 byte) có mtime **16/03/2018 02:47** = **thời điểm cài đặt** (giống `search.dat`, `TipOfTheDay.xml`) ⇒ chỉ giữ **giá trị mặc định của bản cài**; trước khi dùng phải đối chiếu với `config.dat` đang hoạt động.

| Tệp | Khoá | Giá trị đọc được | Ghi chú |
|---|---|---|---|
| `data.dat` **(GỐC, legacy 2018)** | `Password`, `KhachHang`, `MaBaoVe`, `CapNhatGanNhat`, `TrangThai`, `HoiTruocKhiThoat`, `Port`, `KetNoiMayChu`, `DuLieu`, `MaDangKy`, `Module` | `94sQalqTn80=`, `demo`, `1073`, `09/05/2013 02:01:45`, `Thành công, 123.24.152.19`, `true`, **`8080`**, **`false`**, **`E:\KSoft\Theo Yeu Cau\ThuanViet-QLKD\ThuanViet-SM\bin\Debug\Database\DuLieu_2.5.fdb`**, `650342159-60317`, **`Billiards`** | ⭐ `DuLieu=` lộ **đường dẫn build gốc của nhà phát triển** ⇒ xác nhận nguồn gốc **Thuần Việt**; `Port=8080` khớp cổng `WebService.exe` |
| `conn.dat` (108 B, mtime **16/03/2018** = thời điểm cài, **không** đổi khi app chạy) | ⚠️ **FILE KHÔNG LƯU TÊN KHOÁ** — chỉ 6 dòng theo thứ tự ⇒ **không gán nhãn trường khi chưa PoC** | dòng 1 `version:2`, dòng 2 `Embbeded-DEMO`, dòng 3 `DEMO.FDB`, dòng 4 chuỗi mã hoá 64 ký tự, dòng 5 `True`, dòng 6 `SYSDBA` | Khớp `config.dat`: `xác thực = SYSDBA` + mật khẩu mã hoá. ⇒ **DB không đặt mật khẩu SYSDBA riêng** (tài khoản cục bộ, không qua mạng) — thuận lợi cho PoC phương án A |
| `config.dat` | (không có tên khoá) | `Embbeded-DEMO`, `DEMO.FDB`, `True`, `True`, `True`, `Admin`, `0Aqwi93xBDE=`, chuỗi mã hoá, `SYSDBA` | `Admin` = tài khoản mặc định trên màn hình đăng nhập app |
| `Data\app.dat` (mtime **19/09/2018 12:05**) | `[DATA]`, `TITLE`, `RELEASE`, `VERSIONNO`, `VERSION`, `[PRINTER]` | TITLE = `PHẦN MỀM QUẢN LÝ BÁN HÀNG KHO HÀNG CÔNG NỢ CHĂM SÓC KHÁCH HÀNG`; `RELEASE=20-09-2018`; `VERSIONNO=1`; **`VERSION=4.5.0.0`**; 18 GUID máy in | ✅ **Xác nhận đúng dòng sản phẩm v4.5.0.0** (bản có **kho + công nợ + chăm sóc KH**) ⇒ dùng đúng tên này khi hỏi hãng (Q13) |
| ❌ **(không tồn tại) `BanQuyen.dat`** | — | **Không có file này** trong `C:\Program Files (x86)\TechPos` (đã quét toàn bộ) | Trạng thái bản quyền thực tế nằm ở `data.dat`: `TrangThai=Thành công, 123.24.152.19`. **Sửa lại tài liệu kế hoạch** (trước đây ghi `Data\BanQuyen.dat`) |
| `Data\` — nhóm **trạng thái mới nhất** | `config.dat` 128 B, `test.dat` 9.728 B (**16/09/2026 15:40**); `cache10D7712804F7B9B7F9DFF6928C6E53D9.dat` **5.715.088 B** (15/09/2026 16:14) | Là **dấu vết app đang chạy thật**; cache 5,7 MB ⇒ sẽ tăng theo dữ liệu (cần lưu ý dung lượng, không ảnh hưởng CSDL) |
| `Data\` — nhóm **từ bản cài 2018** | `conn.dat` 108 B, `search.dat` 9.728 B (16/03/2018); `sconfig.dat` **372.224 B** (13/09/2018); `grid.dat` 42.620 B (14/12/2018); `dinfo.dat` 1.504 B (17/12/2018); `app.dat` 2.324 B (19/09/2018); `TEMPLATE.FDB` 19.505.152 B (19/09/2018 09:10); `TipOfTheDay.xml` 926 B; `logo.png` 7.512 B; `Banner.png` 18.717 B; `Empty.btw` 17.742 B | **Không** chứa cấu hình kết nối đang hiệu lực; `TEMPLATE.FDB` là DB trắng (an toàn để PoC) |
| `dinfo.dat` | — | **ZIP nhúng** chứa `info.txt`, `info2.txt` | Hãng đóng gói thông tin máy/đăng ký |
| `TipOfTheDay.xml`, `sqlDesc.xml` | — | XML vô hại (mẹo dùng, từ khoá tô màu SQL) | Không ảnh hưởng dự án |
| ❌ **(không tồn tại)** `DEMO.User`, `Barcode.xml` | — | Đã kiểm tra: **không có** trong thư mục gốc, `Data\`, `Database\` | Loại bỏ khỏi tài liệu (thông tin sai từ bản khảo sát trước) |
| ⚠️ `sconfig.dat` (372 KB), `test.dat` (9,7 KB), `search.dat` (9,7 KB) | — | **Bắt đầu bằng chữ ký `MZ`** (file thực thi PE); `test.dat`/`search.dat` chứa chuỗi `No1Lib`; **không** phải .NET (không có metadata `BSJB`), không có thông tin phiên bản | ⚠️ **Cấm parse** — blob nhị phân do engine No1Lib ghi (không phải INI/XML) ⇒ xem §4.7 |
| ⭐ `grid.dat` (42.620 B) | `[Grid]` + `<GUID>` = danh sách `cột / độ rộng / cờ hiển thị / thứ tự` | **Bản kiểm kê cột của mọi lưới dữ liệu**: bóc được **375 định danh** → **74 tên bảng**, chỉ **26 khớp** 86 bảng tài liệu, **48 tên ngoài tài liệu** (47 bảng thật + 1 dương tính giả) | 🔴 Xem §4.6 — bằng chứng mạnh nhất rằng **danh mục 86 bảng không dùng được để chốt schema**; PoC **P7** là bắt buộc |

> ✅ **Mật khẩu trong `conn.dat`/`config.dat` là mã hoá đối xứng của app, KHÔNG cần giải mã** — vì phương án A/B đều dùng `SYSDBA` cục bộ + `firebird.conf` allowlist. Việc giải mã chỉ có ích nếu về sau cần dùng chính credential đó (không khuyến nghị vì lý do pháp lý).

### 4.5 ⭐ Phát hiện: app có **trình soạn SQL nội bộ** (dùng được cho GĐ0) & bản cài thiếu `ib_util.dll` từ đầu

**a) `sqlDesc.xml` = bộ định nghĩa tô màu cú pháp SQL** (`<rule style="Blue">… action|add|alter|select|from|where …</rule>`, danh sách hàm `abs|cast|coalesce|…`, kiểu dữ liệu `bigint|char|decimal|…`) đi kèm `FastColoredTextBox.DLL` + `SourceGrid.DLL` ⇒ ứng dụng **có màn hình soạn/đọc câu SQL** (rất khớp với `SREPORT` — bảng báo cáo có cột `SQL`, `SQLMODE`, `RAWMODE` như đã ghi trong `DOI-CHIEU-CSDL-VA-CHUC-NANG.md`).

> ⭐ **Hệ quả hành động (rất có lợi cho GĐ0):** có thể **đọc toàn bộ SQL nghiệp vụ của hãng ngay bên trong ứng dụng**, **không cần bất kỳ kết nối ngoài nào** ⇒ vừa **tuyệt đối không đụng tới desktop**, vừa **không cần xin quyền truy cập DB** cho bước thu thập đặc tả. Nguồn đặc tả tốt nhất cho GĐ1 (§5 DDL) và GĐ6 (báo cáo) = **`SREPORT.SQL` + `STEMPLATE`** chép ra từ chính app, cộng 51 ảnh chụp màn hình.
>
> ⚠️ Điều kiện: việc chép ra hoàn toàn **không ghi vào DB** (chỉ mở màn hình xem/sửa SQL rồi copy nội dung) — cần chủ dự án đồng ý trước khi thực hiện; nếu lo ngại, chỉ **chụp ảnh màn hình** SQL thay vì copy.

**b) `firebird.log` — chi tiết đầy đủ:** 7 lần ghi, tất cả trên máy `ANH-VU-HANSOME`, cách nhau đúng bằng các lần mở ứng dụng; mỗi lần ghi 4 dòng: cảnh báo `ib_util init failed, UDFs can't be used - looks like firebird misconfigured`, kèm 2 đường dẫn được dò `<app>\bin/ib_util.dll` và `<app>\ib_util.dll`, rồi dòng tổng kết `ib_util.dll library has not been found`.

| Suy luận | Vì sao |
|---|---|
| ✅ **DB gần như chắc chắn KHÔNG phụ thuộc UDF** | Ứng dụng đã bán hàng thật ngày 15–16/09/2026 **ngay cả khi** UDF không nạp được ⇒ nếu nghiệp vụ bắt buộc UDF thì đã lỗi ngay từ đầu |
| ✅ **Cảnh báo này sẽ lặp lại khi PoC dùng `fbembed.dll` riêng** | Đây là hành vi **sẵn có của môi trường**, không phải lỗi do service của ta gây ra ⇒ ghi vào tài liệu PoC để tránh chẩn đoán sai, và **không cần cài thêm** `ib_util.dll` |
| ⚠️ **Vẫn phải kiểm tra computed column / trigger** | Phần tính toán "ẩn" trong DB (nếu có) là thứ duy nhất PoC-1 phải rà (`RDB$FIELDS`, `RDB$TRIGGERS`) trước khi chốt kiểu dữ liệu MySQL ở §5.2 kế hoạch |

**c) Ảnh hưởng nhỏ khác:** `Data\cache…dat` = **5,7 MB** và có mtime mới nhất (15/09/2026 16:14, đúng lúc app khởi động) ⇒ là **cache giao diện theo dữ liệu**, sẽ tăng dần; **không** ảnh hưởng CSDL/sync nhưng nên loại khỏi phạm vi sao lưu.

### 4.6 ⭐ `Data\grid.dat` — **bản kiểm kê cột/bảng độc lập của chính hãng**

`grid.dat` (42.620 B, mtime 14/12/2018) là file **INI** mở đầu bằng `[Grid]`; mỗi khoá là **GUID của một màn hình**, mỗi giá trị là danh sách cột theo định dạng `tênCột / độRộng / cờHiểnThị / thứTự`. Ví dụ **nguyên văn** (đã làm phẳng dấu phân cách):

```
[Grid]
<GUID>=dataGridViewTextBoxColumn1|119|True|0|dataGridViewTextBoxColumn2|118|True|1
<GUID>=CODE|76|True|0|NAME|161|True|1|DNHOMMATHANGID|113|True|2|DDONVITINHID|90|True|3|GIABAN|86|True|4|XUATXU|80|False|7|CHATLIEU|80|False|8|...
```

**Kết quả phân tích (đọc bằng máy, 22/09/2026 — có thể tái lập bằng đúng quy trình dưới đây):**

| Bước | Quy tắc / lệnh | Kết quả |
|---|---|---|
| 1 | `[regex]::Matches($s,'[A-Z][A-Z0-9_]{3,}')` trên toàn văn `grid.dat`, lấy duy nhất | **375** định danh |
| 2 | Lấy các định danh **kết thúc bằng `ID`** rồi cắt đuôi `ID`, giữ tên ≥ 4 ký tự, lấy duy nhất | **74** tên bảng |
| 3 | Đối chiếu 74 tên với **86 bảng** trong `TechPos_SM4_Database_Structure.md` | **26 trùng** |
| 4 | ❗ **Tên KHÔNG có trong tài liệu** | **48** (gồm **47 bảng thật** + 1 dương tính giả `USERTHANHTOAN` do cột `USERTHANHTOANID` — "người thu tiền", không phải bảng) |

**47 tên bảng chưa có trong tài liệu** (nguyên văn, sắp theo alphabet):

`DBENXE`, `DBOPHANGUI`, `DBOPHANNHAN`, `DDOITUONG`, `DDONVITINHCHAN`, `DDONVITINHLON`, `DHOCVIEN`, `DKHOAHOC`, `DKHONHAP`, `DKHOXUAT`, **`DKHUVUC`**, `DLOAIKHACHHANG`, `DLOAITIEN`, `DLOAIVANG`, `DLYDOTHU`, `DMATHANGTANG`, `DNGUONTHONGTIN`, `DNHANVIENCHAMSOC`, `DNHANVIENGIAO`, `DNHANVIENGIAOHANG`, `DNHANVIENNHAP`, `DNHANVIENXUAT`, `DNHANVIENXULY`, `DNHOMHOCVIEN`, `DNHOMKHOAHOC`, `DNHOMLOAIVANG`, `DNHOMMAQUOCGIA`, `DNHOMMAVACH`, `DNHOMSANPHAM`, `DNHOMSODONHANG`, `DNHOMTHEUUDAI`, `DPHANKHUC`, `DPHONGBAN`, `DSANPHAM`, `DTAIKHOAN`, `DTAIKHOANCO`, `DTAIKHOANGIAVON`, `DTAIKHOANKHO`, `DTAIKHOANNO`, `DTAIKHOANTHUE`, `DTHUESUAT`, `DTINHTRANGHONNHAN`, `DTRANGTHAI`, **`DTRANGTHAIGOI`**, `DVATTU`, `DVITRI`, `DVUVIEC`.

**Diễn giải & giới hạn (rất quan trọng — tránh kết luận quá mức):**

1. ✅ **Điều chắc chắn chứng minh được:** hệ sinh thái SM4 có **nhiều bảng ngoài danh mục 86**, trong đó có **`DKHUVUC`** (đúng bảng mà SQL của hãng trong `WebProcessor.EXE` dùng) và **`DTRANGTHAI`, `DTAIKHOAN*`** (rất khớp nhu cầu "trạng thái" + "tài khoản quỹ/kho/công nợ" của nghiệp vụ bán hàng).
2. ⚠️ **KHÔNG** được coi 47 tên này là "bảng chắc chắn có trong `DEMO.FDB`": `grid.dat` bao trùm **toàn dòng sản phẩm SM4 đa module** — thấy rõ dấu vết ngành khác: `DBENXE` (bến xe), `DHOCVIEN`/`DKHOAHOC`/`DNHOMHOCVIEN` (đào tạo), `DLOAIVANG`/`DNHOMLOAIVANG`/`DLOAITIEN` (vàng bạc – tiền), `DNHOMMAVACH` (mã vạch), `DTAIKHOANTHUE`/`DTHUESUAT` (kế toán – thuế), `DTINHTRANGHONNHAN`, `DVUVIEC` (công việc).
3. ➡️ Vì vậy **P7 vẫn là bắt buộc và không thể thay thế**: chỉ `RDB$RELATIONS` của `DEMO.FDB` mới trả lời được "bảng nào **thực có**". `grid.dat` đóng vai trò **danh sách nghi vấn để kiểm tra chéo**: nếu P7 **không** thấy `DKHUVUC`/`DTRANGTHAI` ⇒ danh mục 86 bảng có thể đúng; nếu **có** ⇒ tài liệu thiếu, phải bổ sung và sinh lại schema MySQL.
4. 💡 **Giá trị dùng ngay được**: `grid.dat` cho **tên cột + độ rộng + cờ hiển thị** của từng màn hình ⇒ dùng làm **đầu vào cho GĐ3/GĐ6** (bố cục lưới, thứ tự cột, cột mặc định ẩn) — rút ngắn thời gian BA so với việc chỉ dựa vào ảnh chụp màn hình.

### 4.7 ⚠️ Ba file `.dat` **nhị phân** trong `Data\` — **cấm parse**

| Tệp | Kích thước | Chữ ký đầu file | Nhận dạng |
|---|---|---|---|
| `sconfig.dat` | 372.224 B (mtime 13/09/2018) | `MZ` (PE) | có chuỗi `.dll`; **không** phải .NET (không có metadata `BSJB`); không có FileVersion |
| `test.dat` | 9.728 B (**mtime 16/09/2026 15:40:43** — app ghi lúc chạy) | `MZ` (PE) | chứa chuỗi `No1Lib` |
| `search.dat` | 9.728 B (mtime 16/03/2018) | `MZ` (PE) | chứa chuỗi `No1Lib` |
| `grid.dat` | 42.620 B | `[G` (**INI text**) | ✅ đọc được — xem §4.6 |
| `dinfo.dat` | 1.504 B | `GQ` + `PK` | ZIP chứa `info.txt`, `info2.txt` |

⇒ Các blob `MZ` là **dữ liệu nhị phân do engine No1Lib ghi** (không phải cấu hình văn bản). **Không** suy đoán tham số từ chúng, **không** đưa vào phạm vi sao lưu/sync. **Nguồn tham số duy nhất** cho PoC là `Data\config.dat` (đối chiếu `data.dat` chỉ để truy vết licence/module).

### 4.8 ⭐ `Data\cache10D7712804F7B9B7F9DFF6928C6E53D9.dat` — cache metadata engine (định nghĩa bảng điều khiển)

File **5.715.088 B** (5,7 MB — riêng khối `<Dataset>` là 5.711.254 ký tự), mtime **15/09/2026 16:14:13** (đúng lúc app khởi động ⇒ **app tự làm mới**, file sẽ bị ghi lại). Đây **không phải** dữ liệu nghiệp vụ mà là **cache offline của metadata engine No1Lib**, chứa một `DataSet` XML với **92 phần tử schema** — tương ứng đúng **3 bảng điều khiển** đã liệt kê trong `TechPos_SM4_Database_Structure.md`:

| Nhánh trong cache | = bảng Firebird | Cột đọc được trong cache | Ghi chú |
|---|---|---|---|
| `TableMeta` | **`STABLEDESC`** | `ID`, `REFCONFIG`, `SHOWTOTAL`, `READONLY`, `FORMAT`, `MUSTSELECTCATEGORY`, `MASTERTABLEID`, `INVOICEPRINT`, `BARCODEPRINT`, `INVOICEPARAM`, `INVOICEDETAILPARAM`, `FILTERCONFIG`, `CATEGORYCOL`, `NAME`, `AELAYOUT`, `TSLAYOUT`, `DEFAULTID`, `DESCRIPTION`, `SORTBY`, `TABLETYPE`, **`COTTANG`**, **`COTGIAM`**, `SFUNCTIONID`, `ISSYSTEM`, `USEDEFAULTAEFORM`, `GENNO`, `NOTEMPLATE`, `IMAGE`, `IMAGE32`, `AUTOREPORT`, `LASTUPDATE` | 31 cột; `COTTANG`/`COTGIAM` (cột tăng/giảm) phần lớn **chưa có** trong tài liệu cấu trúc |
| `FormMeta` | **`SFORM`** | `ID`, `NAME`, `NOTE`, `STATUS`, `USERMODIFIEDID`, `TIMEMODIFIED`, `TIMECREATED`, `USERCREATEDID`, `SORTORDER`, `PARENTID`, `PARENTDIR`, `ITEMTYPE`, `AUTOID`, `SIMAGEID`, `CODE`, `DESIGNCODE`, `FORMTYPE`, `STABLEDESCID`, `NOTEMPLATE`, `AELAYOUT`, `LOAI`, `IMAGE32`, `TABLEDESC`, `LASTTEMPLATEID`, `CLASSNAME`, `FILTERCONFIG`, `REFCONFIG`, `REPORTDESC`, `SFUNCTIONID`, `SHOWTOTAL`, `CLIENTCODE`, `SERVERCODE`, `TSCODE`, `TSLAYOUT`, `TSDESIGNCODE` | 35 cột |
| `ColumnMeta` | **`SCOLUMN`** | `ID`, `FORMULA`, `SHOWONLOOKUP`, `STABLEDESCID`, `NAME`, `CAPTION`, `FORMAT`, `REFTABLEID`, `ALLOWEMPTY`, `ALLOWDUPLICATE`, `ISSYSTEM`, `ENABLECONFIGID`, `TITLECONFIGID`, `TOOLTIP`, `SFUNCTIONID`, `MAXVALUE`, `MINVALUE`, `DEFAULTCONFIGID`, `TAG`, `CONTROLTYPE`, `AUTOREPORT`, `TABLENAME` | 22 cột |

Kiểu dữ liệu trong schema cũng đọc được: `xs:string`, `xs:int` và **`xs:base64Binary`** (= **BLOB**: `AELAYOUT`, `TSLAYOUT`, `FORMAT`, `FILTERCONFIG`, `REFCONFIG`, `INVOICEPARAM`, `INVOICEDETAILPARAM`) ⇒ xác nhận bằng chứng cho kết luận "layout nằm trong BLOB" ở mục 4 `DOI-CHIEU-CSDL-VA-CHUC-NANG.md`.

**Giá trị sử dụng / giới hạn — nói rõ để không kỳ vọng sai:**

| ✅ Dùng được cho | ❌ Không dùng được cho |
|---|---|
| **GĐ3** (render form/grid động): cho biết **chính xác tập cột engine** phải đọc từ `STABLEDESC`/`SFORM`/`SCOLUMN` và cột nào là BLOB ⇒ lập được danh sách cột **bỏ đi** khi port sang web | ❌ **Không chứa bảng nghiệp vụ** (`DMATHANG`, `TDONHANG`…) ⇒ **không giải quyết được P7**; vẫn phải liệt kê đối tượng thật từ `DEMO.FDB` |
| Kiểm chứng P7: nếu P7 trả về tên cột khác cache ⇒ tài liệu cấu trúc đã lệch, phải làm lại | ❌ Không phải đặc tả nghiệp vụ, không có dữ liệu, không suy ra được báo cáo |
| Loại file này khỏi phạm vi sao lưu/sync (5,7 MB, tự sinh lại) | ❌ Không ổn định: **app ghi lại** mỗi lần chạy ⇒ không được coi là bản gốc bất biến |

### 4.9 ⭐⭐ Trích xuất metadata **NGHIỆP VỤ** từ cache — giá trị cao nhất của đợt 3

Cache ở §4.8 **không chỉ có cấu trúc engine**: bên trong `<Dataset>` là **dữ liệu metadata đầy đủ** (chính là nội dung 3 bảng `STABLEDESC`/`SFORM`/`SCOLUMN` mà app nạp sẵn):

| Nhánh | Số bản ghi | Nội dung |
|---|---|---|
| `TableMeta` (= `STABLEDESC`) | **80** | định nghĩa **80 bảng** + cấu hình in (`INVOICEPRINT`, `BARCODEPRINT`, `INVOICEPARAM`, `INVOICEDETAILPARAM`) |
| `FormMeta` (= `SFORM`) | **69** | **69 màn hình** (tên tiếng Việt) + code (`CLIENTCODE`/`SERVERCODE`/`TSCODE`) |
| `ColumnMeta` (= `SCOLUMN`) | **581** | **581 định nghĩa cột** thuộc **67 bảng** |

#### a) Danh mục bảng **thật mà app đang điều khiển** (67 bảng) — danh sách nghi vấn cho P7

```
DCALAMVIEC  DCHITIEUCHITIET  DCHITIEUDOANHTHU  DCUAHANG  DDINHLUONG  DDOITACKYGUI  DDONVICHUYENPHAT
DDONVITINH  DDOTKHUYENMAI  DDOTKHUYENMAICHITIET  DHANGSANXUAT  DKHACHHANG  DKHOHANG  DLOAIHINHKHUYENMAI
DLOAIHOPDONG  DLOAIQUY  DLYDOTHUCHI  DLYDOTHUONGPHAT  DMATHANG  DNHACUNGCAP  DNHANVIEN
DNHOMCHITIEUDOANHTHU  DNHOMKHACHHANG  DNHOMMATHANG  DNHOMNHACUNGCAP  DNHOMYKIENKHACHHANG
DTAIKHOANNGANHANG  DTINHTHANH  DTINHTRANGGOI  DTRANGTHAIHOPDONG  DXEGIAOHANG  DYKIENKHACHHANG
OTONKHO  SUSER  TBANGLUONG  TBANGLUONGCHITIET  TBANGLUONGTONGHOP  TBAOGIA  TBAOGIACHITIET  TCHAMSOC
TCHITIETTHANHTOAN  TDATHANG  TDATHANGCHITIET  TDATHANGNCC  TDATHANGNCCCHITIET  TDOINO  TDONHANG
TDONHANGCHITIET  TDONHANGHUY  TDONHANGHUYCHITIET  TGIAMTHEONHOM  TGIAOHANGTHEOXE  TGIAOHANGTHEOXECHITIET
TGIAYGIOITHIEU  TGUIHANGCOD  THOPDONG  THOPDONGCHITIET  TKETQUAGUICOD  TKHOASO  TLUUTAM  … (đủ 67)
```

**Số cột mỗi bảng (top):** `DMATHANG` **53**, `TDONHANG` **49**, `TDONHANGCHITIET` **28**, `TTHUCHI` **25**, `DKHACHHANG` 19, `TLUUTAM` 19, `TDONHANGHUY` 19, `TBAOGIA` 18, `TDATHANG` 18, `SUSER` 16, `DCHITIEUDOANHTHU` 16, `THOPDONG` 14, `TDATHANGNCC` 13, `DDOTKHUYENMAICHITIET` 11 …

#### b) ⭐ Bản nháp schema + **nhãn tiếng Việt** cho web (ví dụ `DMATHANG` — 53 cột)

| Cột | Nhãn (CAPTION) | Kiểu điều khiển | Bảng tham chiếu (lookup) |
|---|---|---|---|
| `NAME` | Tên mặt hàng | Text | — |
| `CODE` | Mã hàng | Text | — |
| `DNHOMMATHANGID` | Nhóm mặt hàng | Lookup | `DNHOMMATHANG` |
| `DDONVITINHID` | Đơn vị tính | Lookup | `DDONVITINH` |
| `DHANGSANXUATID` | Hãng sản xuất | Lookup | `DHANGSANXUAT` |
| `DDOITACKYGUIID` | Đối tác ký gửi | Lookup | `DDOITACKYGUI` |
| `GIABAN` / `GIABAN2` | Giá bán lẻ / **Giá bán sỉ** | Number | — |
| `GIABAN3` … `GIABAN10` | Giá bán 3…10 | Number | — |
| `GIANHAP` / `GIAVON` | Giá nhập / **Giá vốn** | Number | — |
| `TONTOITHIEU` / `TONTOIDA` | Tồn tối thiểu / Tồn tối đa | Number | — |
| `ANH` | Ảnh | **Image** (cột `Image` duy nhất toàn hệ thống) | — |
| `COHANSUDUNG` / `COSERI` | Có hạn sử dụng / Có seri | CheckBox | — |
| `DDONVITINHCHANID`, `DDONVITINHLONID` | Đơn vị tính chẵn / lớn | Lookup | `DDONVITINH` |
| `XUATXU`, `CHATLIEU`, `MAUSAC`, `KIEUDANG` | Xuất xứ, Chất liệu, Màu sắc, Kiểu dáng | Text | — |

**Thống kê toàn hệ thống:** `Text` 189 · `Number` 185 · `Lookup` **110** · `NotShow` 43 · `Date` 38 · `CheckBox` 15 · `Image` 1. Có **113 cột khai báo `REFTABLEID`** ⇒ **bản đồ khoá ngoại + nhãn tra cứu** có sẵn cho MySQL và cho form web.

#### c) Cây **master–detail** (16 quan hệ, GUID đã giải mã)

`TDONHANGCHITIET→TDONHANG` · `TDATHANGCHITIET→TDATHANG` · `TDATHANGNCCCHITIET→TDATHANGNCC` · `TBAOGIACHITIET→TBAOGIA` · `THOPDONGCHITIET→THOPDONG` · `TLUUTAMCHITIET→TLUUTAM` · `TCHITIETTHANHTOAN→TTHUCHI` · `DDOTKHUYENMAICHITIET→DDOTKHUYENMAI` · `TBANGLUONGCHITIET→TBANGLUONG` · `TBANGLUONGTONGHOP→TBANGLUONG` · `TDONHANGHUYCHITIET→TDONHANGHUY` · `DCHITIEUCHITIET→DCHITIEUDOANHTHU` · `TNGUOIDUNGTHEOCUAHANG→SUSER` · `DDINHLUONG→DMATHANG` · `TGIAOHANGTHEOXECHITIET→TGIAOHANGTHEOXE`

Quan hệ **nhóm** (`CATEGORYCOL`): `DMATHANG→DNHOMMATHANGID` · `DKHACHHANG→DNHOMKHACHHANGID` · `TDONHANG→DKHOXUATID` · `TTHUCHI→DNHANVIENID` · `THOPDONG→DTRANGTHAIHOPDONGID` · `DDOTKHUYENMAI→DLOAIHINHKHUYENMAIID`.

#### d) **69 màn hình** trong cache — đối chiếu với 41 màn hình từ 51 ảnh chụp

Ánh xạ được **47 form → bảng**, ví dụ:

| Form (tên tiếng Việt trong cache) | Bảng |
|---|---|
| Hóa đơn bán lẻ · Hóa đơn bán sỉ · Quản lý hóa đơn bán hàng/bán sỉ · Phiếu nhập kho · Phiếu xuất kho · Phiếu chuyển kho · Quản lý nhập/xuất/chuyển kho · Phiếu kiểm kê · Quản lý kiểm kê · Phiếu sản xuất · Quản lý sản xuất · Khách trả lại · Quản lý khách trả lại · Phiếu xuất trả lại NCC · Quản lý xuất trả lại NCC | `TDONHANG` |
| Phiếu thu · Phiếu chi · Danh mục phiếu thu/chi · Công nợ khách hàng · Công nợ khách hàng (theo đơn) · Phiếu thu công nợ · Công nợ nhà cung cấp | `TTHUCHI` |
| Phiếu thu nhiều đối tượng · Phiếu chi nhiều đối tượng | `TTHUCHIHANGLOAT` |
| Tồn kho | `DKHOHANG` |
| Tài khoản người dùng | `SUSER` |

**22 form có tên nhưng ảnh chụp chưa thể hiện** (ứng viên **bổ sung** vào `DANH-MUC-CHUC-NANG.md`): `Tạm ứng lương`, `Xác nhận thanh toán`, `Tồn quỹ`, `Nhập số lượng`, `Chọn mặt hàng trả`, `Tính lại giá vốn`, `Khách hàng thân thiết`, `Tồn kho ban đầu`, `Mặt hàng khuyến mại`, `Chọn loại giá`, `Danh sách bill hủy`, `Lưu vết hoạt động`, `Thêm mua 1 tặng 1`, `Tính lương` / `Lương chi tiết` / `Chi lương`, `HomePage`, `Chọn hạn sử dụng`, `Chọn đơn vị tính`, `Chọn cột excel`, `Phân tích kiểm kê`, `Tồn kho theo mặt hàng`, `ThayDoiGioTao`, `Công nợ ban đầu`, `Mức giá theo mặt hàng`, `Đánh lại mã hàng`, `Chia kho`, `Lịch sử mua bán`, `Lịch sử mua hàng`, `Chọn hóa đơn trả`, `Nhập hạn sử dụng`, `Tồn kho 2`.

#### e) ❗ Ba giới hạn phải nhớ (tránh kỳ vọng sai)

| # | Giới hạn (đã kiểm chứng) | Hệ quả |
|---|---|---|
| 1 | **`FORMULA` = 0 trên 581 cột** — metadata **không chứa công thức** nào | Công thức (`THANHTIEN`, `LAI`, `TONGNO`…) nằm trong **code form** (`SFORM.CLIENTCODE`/`SERVERCODE`/`TSCODE`) và **SQL báo cáo** (`SREPORT.SQL`) ⇒ phải đọc **khi có DB**; riêng phần báo cáo đã có một phần oracle từ `WebProcessor.EXE` (§4.3) |
| 2 | **`MAXVALUE`/`MINVALUE` = 0 ở toàn bộ 453 cột có khai báo** (không có giá trị khác 0) | **Không suy ra được độ dài/ràng buộc cột** ⇒ độ dài `VARCHAR` phải lấy từ `RDB$FIELDS` trong P7 (hoặc chọn giá trị an toàn) |
| 3 | `DKHUVUC`, `CNHANVIEN` xuất hiện **0 lần** trong cache | Khẳng định thêm §4.3: chúng **không thuộc module đang cài** ⇒ P7 phải trả lời dứt khoát **có/không tồn tại** trong `DEMO.FDB`, **không thiết kế MySQL dựa trên chúng** trước khi có kết luận đó |

> **Độ tin cậy & tính bất biến:** cache được app **sinh lại mỗi lần chạy** (mtime 15/09/2026 16:14). Kết quả §4.9 thu được bằng **đọc file tĩnh, không kết nối DB** ⇒ phải **đối chiếu lại với `RDB$RELATIONS`/`RDB$RELATION_FIELDS` (P7)** trước khi dùng làm schema chính thức. Giá trị lớn nhất của nó là: **đặc tả giao diện + nhãn tiếng Việt + bản đồ khoá ngoại + danh sách nghi vấn để kiểm tra chéo P7** — *không phải* nguồn chân lý (nguồn chân lý là `DEMO.FDB`).

## 5. Ảnh hưởng tới kế hoạch `KE-HOACH-TRIEN-KHAI-WEB-MYSQL-SYNC.md`

| # | Phát hiện | Tác động | Hành động |
|---|---|---|---|
| 1 | Module web của hãng là **C# TcpListener + template token**, không phải PHP | ✅ **Tin tốt**: tự viết web bằng .NET/MySQL **hoàn toàn khả thi**, không cần PHP, không phụ thuộc runtime lạ | Giữ nguyên ADR-09 (website riêng) |
| 2 | `WebService.exe` **mở cổng 8080** cạnh app | ⚠️ Xung đột cổng: nếu hãng bật module web và ta cũng dùng 8080 ⇒ phải chọn cổng khác cho web mới | Ghi chú hạ tầng (dùng 8081/443) |
| 3 | SQL chứa `@FROMDATE`, `@TODATE`, `@FromDate`, `@ToDate` | ✅ Xác nhận **cách tham số hoá** của hãng — kiểm tra được dữ liệu đối chiếu | Bổ sung PoC-1 |
| 4 | 86 bảng liệu kê **thiếu** `DKHUVUC`, `CNHANVIEN` | 🔴 **Phải liệt kê lại toàn bộ bảng** khi có DB; §3/§4 của `DOI-CHIEU-CSDL-VA-CHUC-NANG.md` cần cập nhật | Phụ lục §16 kế hoạch |
| 5 | `firebird.log`: **7 lần ghi** cảnh báo thiếu `ib_util.dll`, app còn dò `<app>\bin\ib_util.dll` (thư mục `bin\` không tồn tại) | ✅ **Giảm lo ngại**: app đã chạy bán hàng thực tế 15–16/09 với cảnh báo này ⇒ DB **gần như chắc chắn không phụ thuộc UDF**. Cảnh báo tương tự sẽ xuất hiện khi PoC dùng `fbembed.dll` riêng — **không phải lỗi do ta gây ra** | PoC-1: chỉ cần kiểm tra **computed column/trigger** + đối chiếu định dạng số/chuỗi |
| 6 | App có `teamviewer.exe` | ⚠️ Bảo mật: rà soát khi go-live | Ghi vào §11 rủi ro |
| 7 | ACL: `DEMO.FDB` cho `BUILTIN\Users` quyền chỉ đọc — nhưng file nằm trong `Program Files (x86)` ⇒ ghi cần quyền `Administrators` | ✅ Thuận lợi cho ADR-01: service đọc bằng tài khoản thường là đủ; đặt file copy ra `C:\HoanNgocSync\` để chủ động ACL | Giữ nguyên |
| 8 | `Bartender`, `Aspose.Cells`, `FastReport` | Ngoài phạm vi web (in tem/Excel) ⇒ **không port sang web** | Bổ sung cột §5 (THỪA) của tài liệu đối chiếu |
| 9 | Tên bảng mới `TCHITIETTHANHTOAN`, `TDATHANGNCC*`, `TDOINO`, `TMOTONKHO` | Đã có trong 86 bảng ⇒ mô hình "công nợ" của bản liệu kê là hợp lệ | — |
| 10 | `data.dat` (thư mục gốc) = **legacy 2018** với `DuLieu=E:\KSoft\...\ThuanViet-QLKD\...` ⇒ củng cố **nguồn gốc Thuần Việt** của sản phẩm; cấu hình **đang hiệu lực** là `Data\config.dat` (mtime **16/09/2026 15:40**) | ⚠️ Tránh lấy sai tham số (sai module/sai chế độ kết nối) khi làm PoC | Đã sửa §0 + §12.1 kế hoạch (Q13); giữ nguyên ADR-01 |
| 11 | App có **trình soạn SQL nội bộ** (`sqlDesc.xml` + `FastColoredTextBox`) và **`SREPORT.SQL`** lưu câu SQL báo cáo (xem §4.5) | ✅ **Thu thập đặc tả ngay trong app**, không cần kết nối ngoài, không cần xin quyền truy cập DB ⇒ giảm rủi ro pháp lý ở GĐ0 và tăng tốc GĐ1/GĐ6 | Thêm việc vào §13 kế hoạch |
| 12 | Khi khảo sát: **`WebService.exe` không chạy**, cổng **8080 trống**, không tiến trình TechPos nào đang mở (chỉ có `ApplicationFrameHost` của Windows); module web của hãng là **jQuery Mobile** (bản mobile cũ) | ✅ Tự viết web **không cạnh tranh cổng/cạnh tranh chức năng** với hãng trong điều kiện hiện tại | Giữ ghi chú cổng 8081/443 (§2.4, §9.2 kế hoạch) |
| 13 | ⭐ `Data\grid.dat` là **INI `[Grid]`** chứa định nghĩa cột của mọi lưới: **375 định danh → 74 tên bảng → 26 trùng tài liệu / 48 tên ngoài tài liệu (47 bảng thật + 1 dương tính giả)** (xem §4.6) | 🔴 Danh mục 86 bảng **không dùng được để chốt schema**; **P7 phải chạy trước GĐ1**; đồng thời tận dụng làm đặc tả lưới cho GĐ3/GĐ6 | Đã siết **P7** (§11.3) + thêm **§16.4** kế hoạch |
| 14 | `Data\sconfig.dat` / `test.dat` / `search.dat` là **blob `MZ`** (PE, chứa `No1Lib`), không phải INI/XML | ⚠️ Tránh mất thời gian "dịch" các file này; nguồn tham số duy nhất = `Data\config.dat` | Thêm §4.7 + §16.4 #5 kế hoạch |
| 15 | ⭐ `Data\cache…dat` (5,7 MB) = **cache metadata engine**: typed `DataSet` gồm 3 nhánh `TableMeta`/`FormMeta`/`ColumnMeta` ⇒ **định nghĩa đầy đủ cột của `STABLEDESC` (31), `SFORM` (35), `SCOLUMN` (22)** và cột nào là BLOB (`xs:base64Binary`); có `COTTANG`/`COTGIAM` là cột **tài liệu cấu trúc chưa có** (xem §4.8) | ✅ **Tin tốt cho GĐ3**: biết chính xác tập cột engine phải đọc + danh sách cột bỏ khi port. ❌ **Không** giải quyết P7 (không có bảng nghiệp vụ) | Loại file khỏi phạm vi sao lưu/sync; dùng làm **dữ liệu kiểm chứng P7** (nếu cột P7 khác cache ⇒ tài liệu lệch) |
| 16 | ⭐⭐ Cache chứa **metadata NGHIỆP VỤ**: `TableMeta` **80**, `FormMeta` **69**, `ColumnMeta` **581 cột/67 bảng**, kèm **nhãn tiếng Việt**, `CONTROLTYPE`, **113 cột `REFTABLEID`** (lookup), **16 quan hệ master–detail** giải mã được, **`DMATHANG` có 53 cột** (xem §4.9) | ✅ **Tăng tốc GĐ1 (schema MySQL) + GĐ3 (form web)**: có sẵn bản nháp schema, nhãn trường, bản đồ khoá ngoại, đặc tả ô nhập. 🔴 **Củng cố** kết luận tài liệu cấu trúc thiếu cả cột ⇒ P7 phải liệt kê **bảng + cột + kiểu + độ dài** | Thêm PoC **P10**; siết yêu cầu **P7** (§11.3, §16.6) |
| 17 | ❗ **`FORMULA` = 0/581** và **`MAXVALUE`/`MINVALUE` = 0 ở toàn bộ 453 cột**; **69 tên form** (47 map bảng) nhưng **22 màn hình chưa có trong 51 ảnh chụp** | ⚠️ Công thức phải lấy từ **code form/SQL báo cáo** (P10); độ dài cột phải lấy từ `RDB$FIELDS` (P7); `DANH-MUC-CHUC-NANG.md` cần **bổ sung ~22 màn hình** để không bỏ sót khi phân rã/báo giá | Cập nhật `DANH-MUC-CHUC-NANG.md` + §16.6 |

## 6. Câu hỏi BỔ SUNG cho hãng TechPos (ghép vào §12 kế hoạch)

| # | Câu hỏi | Vì sao cần |
|---|---|---|
| 1 | **Module web (`WebService.exe` + `res\ui`) có thuộc licence đang dùng không?** Nếu có, giá bật và có API công khai nào để web mới gọi dữ liệu thay vì đọc `.FDB`? | Nếu hãng có API ⇒ giảm mạnh rủi ro ADR-01 (không phải đọc file DB) |
| 2 | **`data.dat` (thư mục gốc app)** ghi `Module=Billiards` + `DuLieu=E:\KSoft\Theo Yeu Cau\ThuanViet-QLKD\...` trong khi phần mềm đang dùng là "Quản lý bán hàng **không công nợ** v4.5" (**cấu hình đang dùng thật là `Data\conn.dat`+`Data\config.dat`**) — **hãng xác nhận đúng module đang cấp phép** và các bảng thực có trong `DEMO.FDB` (có `DKHUVUC`/`CNHANVIEN` không?) | 86 bảng liệu kê đã chứng minh thiếu sót; sai mô hình ⇒ vỡ toàn bộ ánh xạ MySQL |
| 3 | Hãng có **bật `WebService.exe`** trên máy khách không, dùng cổng nào, có đồng ý để web mới dùng cổng khác? | Tránh xung đột cổng/mâu thuẫn vận hành |
| 4 | Có **UDF** (thư viện `ib_util.dll`) trong DB không? DB có phụ thuộc UDF tính toán nào? | Cảnh báo tiềm ẩn khi đọc bằng `fbembed.dll` thiếu UDF |
| 5 | Xác nhận **cách tham số hoá** và tài liệu **sơ đồ quan hệ** cho các bảng trạng thái `STATUS = 30`? | Tăng tốc & bảo đảm độ chính xác bản MySQL thay vì tự đoán nghĩa cột |
| 6 | **`Data\grid.dat` có bị app ghi lại** khi người dùng tùy chỉnh lưới (kéo/thêm/ẩn cột) không? Hãng có **tài liệu danh mục bảng** (data dictionary) của module đang cấp phép? | Nếu `grid.dat` bị ghi lại ⇒ phải **sao chụp bản gốc trước** khi dùng làm đặc tả (§4.6); nếu hãng có data dictionary ⇒ giảm hẳn công đoạn P7 |

## 7. Kết luận

1. **Không cần PHP, không cần Apache/IIS** — web mới = .NET (ASP.NET Core + MySQL) đúng như `ADR-09`; nội dung `res\ui\*.php` chỉ là **template HTML** để **tham khảo nhãn/route/bố cục báo cáo**, không phải code nền tảng.
2. Lần khảo sát đã **loại bỏ được 1 rủi ro lớn** (web của hãng không phải công nghệ lạ) nhưng **phát sinh 1 rủi ro mới** (liệu kê 86 bảng là thiếu — phải liệt kê lại).
3. Không có thay đổi nào trên máy desktop: mọi thao tác đều là **đọc file** và **ghi ra ngoài** (`C:\Users\ANHVU\HoanNgocSoft\HoanNgocWeb`).
4. Việc cần làm tiếp theo **không đổi**: gọi hãng (mục 6 ở trên, đã bổ sung vào §12 kế hoạch dưới dạng Q12–Q17) + chạy **PoC-1/P7** khi được phép — 2 việc này chạy song song.
5. Đợt xác minh thứ 2 **bớt được 2 rủi ro**: (a) **UDF không bắt buộc** (app chạy bán hàng thật ngày 15–16/09 dù thiếu `ib_util.dll`); (b) **không cần quyền truy cập DB** cho bước thu thập đặc tả, vì SQL báo cáo **đọc được ngay trong ứng dụng** (§4.5a).
6. **Rủi ro còn lại vẫn là 1:** liệu kê 86 bảng **thiếu bảng** (`DKHUVUC`, `CNHANVIEN`) ⇒ **PoC-7 (liệt kê lại toàn bộ đối tượng) là điều kiện tiên quyết của GĐ1**, không được bắt đầu sinh schema MySQL trước khi xong.
7. **Đợt 3 làm rủi ro trên rõ hơn nhưng cũng cho công cụ xử lý:** `Data\grid.dat` (§4.6) là **bản kiểm kê cột/bảng do chính hãng sinh ra** — 375 định danh, 74 tên bảng suy ra được, **26 trùng tài liệu / 48 tên ngoài tài liệu** ⇒ xác nhận không thể chốt schema bằng tài liệu, đồng thời **cung cấp danh sách nghi vấn để kiểm tra chéo kết quả P7** và **đặc tả lưới dùng được cho GĐ3/GĐ6**. Quy trình đếm đã ghi lại **từng bước để tái lập** (§4.6).
8. **Ba quy tắc mới được cứng hoá** để tránh sai sót khi PoC: (a) **nguồn tham số duy nhất = `Data\config.dat`** (không dùng `data.dat` gốc, không gán nhãn trường cho `conn.dat`); (b) **cấm parse** các `.dat` nhị phân `sconfig.dat`/`test.dat`/`search.dat` (§4.7); (c) loại bỏ hoàn toàn khỏi hồ sơ 3 thông tin **không tồn tại**: `BanQuyen.dat`, `DEMO.User`, `Barcode.xml`.
9. **Đợt 5 (metadata nghiệp vụ) là kết quả giá trị nhất cho GĐ1/GĐ3:** cache cho **581 định nghĩa cột / 67 bảng** với **nhãn tiếng Việt**, **113 cột lookup**, **16 quan hệ master–detail** và **69 tên màn hình** ⇒ dựng được **bản nháp schema MySQL + đặc tả form web** trước cả khi có quyền đọc DB. Đổi lại phải nhớ: **không có công thức, không có ràng buộc/độ dài** trong cache ⇒ **P7 (cấu trúc) + P10 (công thức)** vẫn bắt buộc trước khi chốt schema.
10. **Danh mục chức năng cần cập nhật:** 22 màn hình có trong cache nhưng **không có trong 51 ảnh chụp** (`Tồn quỹ`, `Tính lại giá vốn`, `Chia kho`, `Lịch sử mua bán`, `Lập lương`, `Chọn cột excel`, `Tồn kho 2`…) ⇒ bổ sung vào `DANH-MUC-CHUC-NANG.md` để không bỏ sót khi phân rã công việc và báo giá.
