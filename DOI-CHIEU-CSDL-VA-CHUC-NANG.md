# ĐỐI CHIẾU CSDL FIREBIRD (TechPos SM4 v4.5) ↔ DANH MỤC CHỨC NĂNG

> Nguồn đối chiếu: `TechPos_SM4_Database_Structure.md` (86 bảng, Firebird Embedded, `DEMO.FDB` / `TEMPLATE.FDB`)
> Đối tượng kiểm tra: `DANH-MUC-CHUC-NANG.md` (10 nhóm menu — 76 mục — ~41 màn hình, từ 51 ảnh chụp)
>
> ⚠️ **CẢNH BÁO (bổ sung 22/09/2026):** danh mục **86 bảng là CHƯA ĐẦY ĐỦ**. Khi bóc chuỗi từ `WebProcessor.EXE` (module web kèm bản cài) đã thấy các truy vấn dùng bảng **không có** trong danh mục: **`DKHUVUC`**, **`CNHANVIEN`** (cùng nhiều bảng khác của sản phẩm TechPos Billiards). Toàn bộ tên bảng trong danh mục chỉ có tiền tố `D/O/S/T` ⇒ **phải liệt kê lại toàn bộ đối tượng qua `RDB$RELATIONS`** (PoC **P7** ở §11.3 kế hoạch) trước khi sinh schema MySQL. Chi tiết: `KHAO-SAT-THU-MUC-CAI-DAT-TECHPOS-SM4.md` §4.3.

## 1. TỔNG QUAN CSDL

| Nhóm | Tiền tố | Số bảng | Vai trò |
|---|---|---|---|
| Giao dịch / nghiệp vụ | `T…` | **34** | Chứng từ: đơn hàng, đặt hàng, thu chi, kho, lương, COD, hợp đồng, lưu vết… |
| Hệ thống / cấu hình | `S…` | **20** | Người dùng, phân quyền, menu, form/grid/báo cáo metadata, config, ảnh |
| Danh mục (dictionary) | `D…` | **32** | Mặt hàng, KH, NCC, kho, cửa hàng, nhân viên, ĐVT, khuyến mại, lý do thu chi… |
| | | **86** | |

Đặc điểm chung: mọi bảng đều có `ID VARCHAR(36)` (GUID), `STATUS INTEGER NOT NULL`, `USERCREATEDID/TIMECREATED/USERMODIFIEDID/TIMEMODIFIED`; các bảng danh mục/cây có thêm `PARENTID/PARENTDIR/SORTORDER/ITEMTYPE/AUTOID/SIMAGEID`.

**Phát hiện quan trọng:** hệ thống **không có bảng chứng từ riêng cho từng loại phiếu**. `TDONHANG` là bảng chứng từ **đa năng**, phân biệt bằng cột `LOAI`/`MODE`/`LOAIGIA`/`LOAITHANHTOAN`:
`TDONHANG` có đồng thời `DKHOXUATID` + `DKHONHAPID` + `DNHANVIENXUATID` + `DNHANVIENNHAPID` + `DNHACUNGCAPID` + `DCUAHANGID` + `TGUIHANGCODID` + `TKETQUAGUICODID` + `TDATHANGNCCID` ⇒ dùng chung cho **bán lẻ, bán sỉ, khách trả lại, nhập kho, xuất kho, chuyển kho, kiểm kê, xuất trả NCC, xuất vật tư (sản xuất)**.
Chi tiết `TDONHANGCHITIET` có `SLNHAP/SLXUAT/SLTHUCTE/SLHETHONG/KHUYENMAI/XUATVATTU/HANSUDUNG/SERI/GIAVON/GIAGANNHAT` ⇒ xác nhận dùng cho cả nhập/xuất/kiểm kê/kho.

## 2. ÁNH XẠ 76 MỤC CHỨC NĂNG → BẢNG DỮ LIỆU

Ký hiệu: ✔ đủ bảng | ◐ có nhưng thiếu, hoặc dùng chung bảng nên cần xác minh | ✘ không có bảng

### 2.1 HỆ THỐNG (6 mục)
| Mục menu | Bảng / cơ chế | |
|---|---|---|
| Tạo mới cơ sở dữ liệu | tạo file `.FDB` (ngoài DB) | không cần bảng |
| Đổi mật khẩu đăng nhập | `SUSER.PASSWORD` | ✔ |
| Sao lưu cơ sở dữ liệu | gbak (ngoài DB) | không cần bảng |
| Phục hồi cơ sở dữ liệu | gbak -r (ngoài DB) | không cần bảng |
| Đăng xuất | phiên làm việc; `STRACKING` ghi log | ◐ |
| Thoát | — | ✘ |

### 2.2 BÁN HÀNG (16 mục) — **16/16 ✔**
| Mục menu | Bảng |
|---|---|
| 1. Danh mục cửa hàng | `DCUAHANG` (`DIACHI`, `DIENTHOAI`, `CODE`, `DKHOHANGID`, `STEMPLATEID`) |
| 2. Danh mục mặt hàng | `DMATHANG` (`ANH`, `COHANSUDUNG`, `COSERI`, `TONTOITHIEU`, `GIAVON`, `HOAHONG`, `MACDINHGIAMGIA/MACDINHGIAMTIEN`, `DDOITACKYGUIID`, `DHANGSANXUATID`, `DNHOMMATHANGID`, `DDONVITINHID` + `DDONVITINHCHANID`, `DDONVITINHLONID`) |
| 3. Danh mục khách hàng | `DKHACHHANG` (`MAKHACH`, `DIACHI`, `DIENTHOAI`, `EMAIL`, `MASOTHUE`, `DTINHTHANHID`, `FACEBOOK`, `DIACHIGIAOHANG`, `NGUOIDAIDIEN`, `CHUCVU`, `SONGAYCHONO`, `NGAYSINH`, `GIABAN`, `DIEMTICHLUYBANDAU`, `DNHANVIENID`, `DNHOMKHACHHANGID`) |
| 4. Danh mục đặt khuyến mại | `DDOTKHUYENMAI` + `DDOTKHUYENMAICHITIET` + `DLOAIHINHKHUYENMAI` (mua/tặng: `SOLUONGMUA`, `SOLUONGTANG`, `DMATHANGTANGID`, `GIATRIDONHANG`) |
| 5. Báo giá ▸ | `TBAOGIA` + `TBAOGIACHITIET` |
| 6. Đặt hàng ▸ | `TDATHANG` + `TDATHANGCHITIET` (+`TDATHANGNCCCHITIET.TDATHANGID` = đặt mua NCC theo đơn khách) |
| 7. Hợp đồng ▸ | `THOPDONG` + `THOPDONGCHITIET` + `DLOAIHOPDONG` + `DTRANGTHAIHOPDONG` (`NGAYKY/TUNGAY/DENNGAY/NGAYNGHIEMTHU/NGAYTHANHLY/DATCOC`) |
| 8. Bán lẻ ▸ | `TDONHANG` + `TDONHANGCHITIET` (`LANIN`, `VOUCHER`, `DIEM`, `KHACHDUA`, `TRALAI`, `DOITRA`) + `TLUUTAM`/`TLUUTAMCHITIET` (Lưu tạm) |
| 9. Bán sỉ ▸ | `TDONHANG` với `LOAI` (bán sỉ) — **không có bảng riêng** |
| 10. Trả lại ▸ | `TDONHANG` (`TDONHANGTRAID`, `DOITRA`) + `TDONHANGHUY` + `TDONHANGHUYCHITIET` (bill hủy) |
| 11. Giao hàng ▸ | `TGUIHANGCOD`, `TKETQUAGUICOD`, `DDONVICHUYENPHAT`, `TGIAOHANGTHEOXE(+CHITIET)`, `DXEGIAOHANG` |
| 12. Lưu vết hoạt động | `TLUUVET` (`GIO`, `SODONHANG`, `TAIKHOAN`) + `STRACKING` (`LOAI`, `IP`, `DOITUONG`, `REFID`) |
| 13. Thống kê mặt hàng bán | `TDONHANGCHITIET` (`DONGIABAOCAO`, `GIAVON`, `GIATRINHAP`, `GIATRIXUAT`, `TILEGIAMGIA`) + `SREPORT` |
| 14. Thống kê doanh thu | `TDONHANG` (`TIENHANG/TIENGIAMGIA/DOITRA/TONGCONG/TIENTHANHTOAN/KHACHDUA/TRALAI/PHIVANCHUYEN/TIENTHUE/TILETHUE/CONLAI`) |
| 15. Chi tiết hoạt động ngày | `TDONHANG` + `TTHUCHI` + `SREPORT` (bản in) |
| 16. Tổng hợp kết quả kinh doanh | `DCHITIEUDOANHTHU` (`THANG1..THANG12`, `NAM`) + `DCHITIEUCHITIET` + `DNHOMCHITIEUDOANHTHU` (Định phí/Biến phí/Chi khác) |

### 2.3 CHĂM SÓC KH (4 mục) — **3/4**
| Mục menu | Bảng | |
|---|---|---|
| Chăm sóc khách hàng | `TCHAMSOC` (`GIO`, `DTINHTRANGGOIID`, `DKHACHHANGID`, `DNHANVIENID`) + `DTINHTRANGGOI` | ✔ |
| Gửi tin nhắn tới khách hàng | `SSMSTEMPLATE` (chỉ là **mẫu tin**); `SUSER.SMTP/SSL/PORT/PASS` | ✘ **thiếu bảng hàng đợi/nhật ký gửi tin** |
| Khách hàng thân thiết | `DNHOMKHACHHANG` (`TILEGIAMGIA`, `DIEMTICHLUY`), `TTANGGIAMDIEM` (`DIEMTANG/DIEMGIAM/LYDO`), `DKHACHHANG.DIEMTICHLUYBANDAU`, `TGIAMTHEONHOM` | ✔ |
| Phân hồi của khách hàng | `DYKIENKHACHHANG` + `DNHOMYKIENKHACHHANG` | ✔ |


### 2.4 KHO HÀNG (12 mục) — **11/12**
| Mục menu | Bảng | |
|---|---|---|
| Danh mục kho hàng | `DKHOHANG` (`CHOPHEPAMKHO` = cho phép âm kho, `DCUAHANGID`) | ✔ |
| Danh mục tài khoản ngân hàng | `DTAIKHOANNGANHANG` (chỉ có `NAME`) | ◐ thiếu số TK/chủ TK/ngân hàng |
| Sản xuất ▸ | `DDINHLUONG` (định lượng NVL/BOM), `TDONHANGCHITIET.XUATVATTU`, `DHANGSANXUAT` (**chỉ là hãng sản xuất**) | ✘ **thiếu bảng phiếu sản xuất + chi tiết** |
| Đặt mua hàng ▸ | `TDATHANGNCC` + `TDATHANGNCCCHITIET` (`GIAMUAGANNHAT`) | ✔ (thiếu cột thanh toán/công nợ) |
| Nhập kho ▸ | `TDONHANG` (`LOAI`, `DNHACUNGCAPID`, `DKHONHAPID`, `DNHANVIENNHAPID`, `THANHTOAN`, `CONLAI`, `DTAIKHOANNGANHANGID`, `DAXUATHOADON`, `SOHOADON`) | ◐ dùng chung |
| Xuất kho ▸ | `TDONHANG` (`DKHOXUATID`, `DNHANVIENXUATID`) | ◐ dùng chung |
| Xuất trả lại NCC ▸ | `TDONHANG` (`LOAI`, `DNHACUNGCAPID`) | ◐ dùng chung |
| Chuyển kho ▸ | `TDONHANG` (`DKHOXUATID` + `DKHONHAPID` + 2 nhân viên) | ◐ dùng chung |
| Kiểm kê ▸ | `TDONHANG` + `TDONHANGCHITIET.SLTHUCTE` / `SLHETHONG` | ✔ |
| Tính lại giá vốn | `TDONHANGCHITIET.GIAVON` (lưu kết quả); **không có bảng lưu phương pháp** (có thể nằm trong `SCONFIG`) | ◐ |
| Tồn kho (Ctrl+Shift+T) / Thẻ kho | `OTONKHO` (`DKHOHANGID`, `DMATHANGID`, `TON`) + `TDONHANGCHITIET.SLNHAP/SLXUAT` | ✔ (snapshot, không có sổ kho) |
| Tồn tất cả các kho | `OTONKHO` nhóm theo `DKHOHANGID` | ✔ |

### 2.5 QUỸ (8 mục) — **8/8 ✔**
| Mục menu | Bảng |
|---|---|
| Danh mục lý do thu chi | `DLYDOTHUCHI` (`LALYDOTHU`, `LOAILYDO`) + `DLOAIQUY` |
| Tạo phiếu thu (Ctrl+Shift+T) | `TTHUCHI` (`LOAI`, `THU`, `CHI`, `CHUNGTUGOC`, `DIENGIAI`, `LOAIDOITUONG`) |
| Tạo phiếu chi (Ctrl+Shift+C) | `TTHUCHI` (`LOAI` = chi) |
| Danh mục phiếu thu | `TTHUCHI` + `DLYDOTHUCHI` |
| Danh mục phiếu chi | `TTHUCHI` + `DLYDOTHUCHI` |
| Phiếu thu nhiều đối tượng | `TTHUCHIHANGLOAT` (`LAPHIEUTHU`, `TONGCONG`) + `TTHUCHI.TTHUCHIHANGLOATID` |
| Phiếu chi nhiều đối tượng | `TTHUCHIHANGLOAT` (`LOAI`) + `TTHUCHI.TTHUCHIHANGLOATID` |
| Tồn quỹ | `TTHUCHI` (`THU`, `CHI`) + `TCHITIETTHANHTOAN` |

### 2.6 CÔNG NỢ (3 mục) — **3/3 ✔**
| Mục menu | Bảng |
|---|---|
| Công nợ khách hàng | `TDONHANG` (`CONNO`, `TIENTHANHTOAN`, `DATHANHTOAN`, `NOCU`, `CONLAI`) + `TCHITIETTHANHTOAN` (`TDONHANGID`, `TTHUCHIID`, `SOTIEN`) |
| Công nợ nhà cung cấp | `TDONHANG` (phiếu nhập: `DNHACUNGCAPID`, `THANHTOAN`, `CONLAI`) + `TTHUCHI.DNHACUNGCAPID` (+`TDATHANGNCC` — **thiếu cột thanh toán**) |
| Theo dõi nợ (ĐN16/0004) | `TDOINO` (`DKHACHHANGID`, `DNHANVIENID`) — **thiếu bảng chi tiết** |

### 2.7 NHÂN SỰ (6 mục) — **6/6 ✔**
| Mục menu | Bảng |
|---|---|
| Danh mục nhân viên | `DNHANVIEN` (`CACHTINHLUONG`, `LUONGCA`, `LUONGTHANG`, `PHUCAP`, `NGHITHU7`, `NGHICHUNHAT`, `DIACHI`, `DIENTHOAI`) |
| Giấy giới thiệu | `TGIAYGIOITHIEU` (`HIEULUCDEN`, `KINHGUI`, `ONGBA`, `DUOCCUDEN`, `VEVIEC`, `NGUOILIENHE`) |
| Tạm ứng lương | `TTHUCHI.LATAMUNG` + `TBANGLUONGTONGHOP.TAMUNG` + `TBANGLUONGID` |
| Thưởng phạt | `TTHUONGPHAT` (`THUONG`, `PHAT`) + `DLYDOTHUONGPHAT` |
| Chấm công | `DCALAMVIEC` (`TILELUONG` = tỉ lệ lương ca) + `TBANGLUONGCHITIET` (`NGAY`, `DCALAMVIECID`, `TRANGTHAI`) |
| Tính lương | `TBANGLUONG` (`THANG`, `NAM`) + `TBANGLUONGCHITIET` + `TBANGLUONGTONGHOP` (`LUONGCA`, `TONGLUONG`, `THUONG`, `PHAT`, `TAMUNG`, `PHUCAP`, `THUCNHAN`) |

### 2.8 QUẢN TRỊ (9 mục) — **7/9**
| Mục menu | Bảng | |
|---|---|---|
| Thư viện ảnh… | `SIMAGE` (BLOB) + `DMATHANG.ANH` | ✔ |
| Dữ liệu ban đầu ▸ | `DKHACHHANG.DIEMTICHLUYBANDAU`, `OTONKHO`, `TDONHANG.NOCU` (rời rạc) | ✘ **thiếu bảng số dư đầu kỳ** (tồn/công nợ/điểm) |
| Chi tiết doanh thu | `DCHITIEUDOANHTHU` + `DCHITIEUCHITIET` + `DNHOMCHITIEUDOANHTHU` + `DTINHTHANH` | ✔ |
| Khóa sổ | `TKHOASO` | ✔ |
| Xóa dữ liệu | — (thao tác hàng loạt) | ✘ |
| Công cụ nhà phát triển | `STABLEDESC`, `SCOLUMN`, `SFORM`, `SGRID`, `SMENU`, `SFUNCTION` (engine low-code) | ✔ |
| Lịch sử tác hệ thống | `STRACKING` (`TAIKHOAN`, `IP`, `DOITUONG`, `GIO`, `REFID`, `STABLEDESCID`) | ✔ |
| Người dùng và phân quyền | `SUSER` (`ISADMIN`, `SGROUPUSERID`, `DNHANVIENID`, `DEFAULTFUNCID`, `CARDCODE`), `SGROUPUSER`, `SGROUPROLE` (`SFUNCTIONID`, `MODE`), `SFUNCTION`, `SMENU`, `TNGUOIDUNGTHEOCUAHANG` | ✔ |
| Cài hình toàn hệ thống | `SCONFIG` (`TEXTVALUE/INTVALUE/DECIMALVALUE/DATETIMEVALUE/BLOBVALUE`, `TAB`, `HEADER`, `FOOTER`, `SHOWONREPORT`) + `SCONFIGGROUP` (`MAUHOADON` = mẫu hóa đơn) | ✔ |

### 2.9 BÁO CÁO (9 nhóm) — **9/9 ✔**
| Thành phần UI | Bảng |
|---|---|
| Cây báo cáo 9 nhóm | `SREPORT` (`PARENTID/PARENTDIR/ITEMTYPE/AUTOID`) |
| Tab **Thường dùng** | `SREPORT.THUONGDUNG` |
| Tab **Tìm kiếm** | `SREPORT.NAME/CODE` |
| **Tùy chỉnh tham số báo cáo** | `SREPORT.PARAMS` (BLOB) + `FILTERCONFIG` |
| **Xem dữ liệu thô** | `SREPORT.RAWMODE` + `SQL`/`SQLMODE` |
| **Thiết kế mẫu** | `SREPORTTEMPLATE` (`TEMPLATE`, `STEMPLATEID`, `STEMPLATELANDSCAPEID`) + `STEMPLATE` (`TEMPLATE`, `REPORTBASE`, `SFORMID`, `BARCODE`, `BARCODECOL`) |
| Phân quyền xem báo cáo | `SREPORTROLE` (`SGROUPUSERID`, `SREPORTID`, `MODE`) |
| **Báo cáo biểu đồ** | `SANALYSIS` (`CONFIG`, `ANALYSISTYPE`, `COTTONG`) |
| Báo cáo con (Tổng hợp bán hàng theo ngày, XNT, công nợ KH/NCC, KQKD, chi tiết hoạt động ngày) | dữ liệu từ `TDONHANG(+CHITIET)`, `TDATHANGNCC`, `TTHUCHI`, `OTONKHO`, `DCHITIEU*` — **định nghĩa SQL nằm trong `SREPORT.SQL` (BLOB), cần export riêng khi port** |

### 2.10 TRỢ GIÚP (3 mục) — **0/3 ✘**
Hỗ trợ từ tư vấn viên · Đăng ký bản quyền… · Thông tin phần mềm → **không có bảng nào** (thông tin bản quyền/phiên bản hiện lưu ở file cấu hình hoặc registry của Windows, không nằm trong Firebird).


## 3. TỔNG HỢP: **THIẾU** (bảng/cột cần bổ sung khi làm bản web)

| # | Chức năng thiếu dữ liệu | Đề xuất bổ sung |
|---|---|---|
| 1 | **Sản xuất** (menu "Sản xuất ▸", tab "Phiếu sản xuất") | `TSANXUAT` (số phiếu, ngày, kho NVL, kho thành phẩm, mẻ/lô, nhân viên, chi phí, trạng thái) + `TSANXUATCHITIET` (NVL xuất theo `DDINHLUONG`, thành phẩm nhập, SL, đơn giá, giá vốn) |
| 2 | **Gửi tin nhắn tới KH** (chỉ có mẫu `SSMSTEMPLATE`) | `TTINNHAN` (đối tượng, mẫu, nội dung, kênh SMS/Email/Zalo, thời điểm, trạng thái, mã lỗi) + `TTINNHANCHITIET` |
| 3 | **Dữ liệu ban đầu** (số dư đầu kỳ) | `TDUDAUKY` (kỳ, loại: tồn kho/công nợ KH/công nợ NCC/điểm) + `TDUDAUKYCHITIET` (đối tượng, giá trị) |
| 4 | **Sổ kho / Thẻ kho** — `OTONKHO` chỉ là snapshot `TON`, không có ledger ⇒ báo cáo XNT và thẻ kho phải quét lại `TDONHANGCHITIET` | `TNKHOHANG` (ngày, kho, mặt hàng, loại chứng từ, ID chứng từ, SL nhập, SL xuất, tồn lũy kế, đơn giá, giá vốn) — vừa là ledger vừa nuôi `OTONKHO` |
| 5 | **Theo dõi nợ / Đổi nợ** — `TDOINO` quá mỏng (chỉ KH + NV + ngày) | `TDOINOCHITIET` (phiếu nợ cũ, phiếu nợ mới, số tiền đổi) |
| 6 | **Giá đa tiền tệ** (UI có "Giá lẻ USD / Giá sỉ USD") — DB chỉ có slot `GIABAN`, `GIABAN2…GIABAN10`, `SOLUONG3…SOLUONG10` | `DTIGIA` (tỉ giá theo ngày) + cột `TIENTE`/`TYGIA` trên bảng giá, hoặc chuẩn hóa `DBANGGIA`/`DBANGGIACHITIET` (theo KH/nhóm KH/ĐVT/số lượng/tiền tệ) |
| 7 | **Bảng giá theo số lượng** (F7 "Mức giá") đang nhồi 10 slot cột | `DBANGGIA` + `DBANGGIACHITIET` (thay `GIABAN2..10` + `SOLUONG3..10`) |
| 8 | **Chuỗi trạng thái dạng số** không có bảng tra: `TDONHANG.LOAI/MODE/LOAIGIA/LOAITHANHTOAN`, `TTHUCHI.LOAI/LOAIDOITUONG`, `STATUS` | `DLOAICHUNGTU`, `DLOAITHANHTOAN`, `DLOAIGIA`, `DLOAIDOITUONG`, `DTRANGTHAI` (xóa mềm/đã dùng) |
| 9 | **Cấu hình tính giá vốn** (dialog "Tính lại giá vốn") | `SCONFIG` key `PHUONGPHAPGIAVON` + `TUNGAY`, hoặc bảng `DCAUHINHGIAVON` |
| 10 | **Công nợ nhà cung cấp** — `TDATHANGNCC` không có `TIENTHANHTOAN`/`CONNO` | Thêm cột thanh toán/còn nợ vào `TDATHANGNCC` (giống `TDONHANG`) |
| 11 | **Hồ sơ nhân viên đầy đủ** (CCCD, ngày vào, chức vụ, bộ phận) | Bổ sung cột vào `DNHANVIEN` hoặc `DNHANVIEN_HOSO` + `DCHUCVU`, `DPHONGBAN` |
| 12 | **Tài khoản ngân hàng** chỉ có `NAME` | Bổ sung `SOTAIKHOAN`, `CHUTAIKHOAN`, `DNGANHANGID`, `CHINHANH` vào `DTAIKHOANNGANHANG` |
| 13 | **Đa phương thức thanh toán trong 1 phiếu** (Tiền mặt/Tiền thẻ/Chuyển khoản trên cùng 1 bill) — hiện chỉ 1 khoản `TIENTHANHTOAN`/phiếu | `TTHANHTOAN` (phiếu, phương thức, số tiền, tài khoản) liên kết `TDONHANG` |
| 14 | **Trợ giúp / bản quyền** (3 mục menu) | `SLICENSE` (bản quyền, hạn dùng, máy) + `STRỢGIÚP`/`STHONGTINPHANMEM` |
| 15 | **Audit theo cột** — `STRACKING` chỉ ghi ở mức bản ghi | `SCHANGELOG` (bảng, khóa, hành động, JSON giá trị cũ/mới) |
| 16 | **FK treo**: `STRACKING.STRACKINGTYPEID` trỏ tới bảng **không tồn tại** trong dump | Thêm `SSTRACKINGTYPE` hoặc bỏ cột |
| 17 | **Hủy phiếu không liên kết gốc**: `TDONHANGHUY` lưu dạng text (`KHACHHANG`, `NHANVEN`, `THUNGAN`) | Thêm `TDONHANGID` + `LYDOHUYID` vào `TDONHANGHUY`, `TDONHANGHUYCHITIET` |
| 18 | **Xóa dữ liệu** (QUẢN TRỊ) không có log | `SXOADULIEU` (bảng bị xóa, khoảng thời gian, người thực hiện) |
| 19 | ⚠️ **Danh mục bảng chưa đầy đủ** (không phải "tính năng thiếu" mà là **dữ liệu liệu kê thiếu**): SQL của hãng trong `WebProcessor.EXE` dùng `DKHUVUC` (khu vực), `CNHANVIEN` (nhân viên/thu ngân) — **không tồn tại trong dump 86 bảng**; ngoài ra còn 2 tham số hoá `@FromDate/@ToDate` và hằng số `STATUS = 30` | Chạy **liệt kê lại toàn bộ** `RDB$RELATIONS`/`RDB$RELATION_FIELDS` trên `DEMO.FDB` (PoC P7) rồi **sinh lại tài liệu cấu trúc** trước khi lập ánh xạ MySQL (bước GĐ1). **Không** dùng danh mục 86 bảng làm nguồn duy nhất nữa |

## 4. TỔNG HỢP: **THỪA** (không cần port, hoặc nên thay thế khi làm web)

| Thành phần | Lý do |
|---|---|
| `SGRID` (+ cột `MOBILE`), `SMENU.VIEWONTS/VIEWONMOBI/VIEWONWEB/BOLD/ITALIC/IMAGE/TOOLBAR/TOOLBARINDEX/SHORTCUT/TOOLBARTITLTE`, `STABLEDESC.SYNCOPTION` | Hạ tầng engine low-code + đa nền tảng (desktop/mobile/web/TS) của bản cũ; web mới hard-code ~41 màn hình |
| `STABLEDESC.CODE/DESIGNCODE/CLIENTCODE/SERVERCODE/TSCODE/TSLAYOUT/TSDESIGNCODE/CODE`, `SFORM.CODE/DESIGNCODE/AELAYOUT/CLIENTCODE/SERVERCODE/TSCODE/TSLAYOUT/TSDESIGNCODE/CLASSNAME/FORMTYPE/LOAI`, `SCOLUMN.CONTROLTYPE/FORMULA/TAG/REFTABLEID`, `SREPORT.MASTERVIEW/DETAILVIEW/DETAIL2VIEW/CLASSNAME/CLIENTCODE/SERVERCODE/SQLMODE/RAWMODE` | Code sinh động + layout dạng BLOB nhúng trong DB — không port được sang web; thay bằng component/route + template thật |
| Ảnh lưu BLOB trong DB: `SIMAGE.IMAGE`, `DMATHANG.ANH`, `SMENU.IMAGE`, `SCONFIGGROUP.IMAGE`, `STABLEDESC.IMAGE/IMAGE32`, `SREPORT.IMAGE32`, `STEMPLATE.TEMPLATE(BLOB)`, `SREPORTTEMPLATE.TEMPLATE(BLOB)` | Nên chuyển sang object storage + URL (DB chỉ giữ path) |
| `TBANGLUONG.CHITIET` (BLOB) | Trùng chức năng với `TBANGLUONGCHITIET` (đã chuẩn hóa) — dữ liệu chấm công bị nhân đôi |
| `DMATHANG.GIABAN2…GIABAN10`, `SOLUONG3…SOLUONG10` | 10 slot giá cứng nhắc; thay bằng bảng giá (`DBANGGIA`) — giữ lại chỉ để tương thích ngược |
| `OTONKHO` (chỉ có `TON`) | Không đủ cho "Thẻ kho/Tồn đầu–Nhập–Xuất–Tồn"; nên thay bằng ledger `TNKHOHANG` + view tổng hợp |
| `SQUICKNOTE`, `SANALYSIS`, `SGROUPROLE`, `SREPORTROLE`, `SFUNCTION` | Vẫn cần nhưng nên đơn giản hóa (bảng quyền 1 cấp thay vì 3 bảng role) |
| `TNGUOIDUNGTHEOCUAHANG`, `SUSER.DKHOHANGID`, `SUSER.DEFAULTFUNCID` | Cột/cơ chế phân quyền theo cửa hàng/kho — web nên mô hình lại thành 1 bảng `USER_SCOPE` |


## 5. KẾT LUẬN ĐỘ BAO PHỦ

| Nhóm menu | Số mục | Đủ bảng | Thiếu/không bảng | Tỉ lệ |
|---|---|---|---|---|
| HỆ THỐNG | 6 | 1 (+5 thao tác ngoài DB) | 0 cần bảng | n/a |
| BÁN HÀNG | 16 | 16 | 0 | 100% |
| CHĂM SÓC KH | 4 | 3 | 1 (log gửi tin) | 75% |
| KHO HÀNG | 12 | 11 | 1 (phiếu sản xuất) | 92% |
| QUỸ | 8 | 8 | 0 | 100% |
| CÔNG NỢ | 3 | 3 | 0 (TDOINO cần chi tiết) | 100% |
| NHÂN SỰ | 6 | 6 | 0 | 100% |
| QUẢN TRỊ | 9 | 7 | 2 (dữ liệu ban đầu, xóa dữ liệu) | 78% |
| BÁO CÁO | 9 | 9 | 0 | 100% |
| TRỢ GIÚP | 3 | 0 | 3 | 0% |
| **TỔNG** | **76** | **64** | **12** | **~84%** (≈91% nếu trừ 6 mục HỆ THỐNG vốn không cần bảng) |

**Nhận xét chung:**
1. CSDL **đáp ứng đúng và khá đầy đủ** nghiệp vụ đã liệt kê: bán lẻ/bán sỉ/trả lại/COD/hợp đồng/kho/quỹ/công nợ/lương/khuyến mại/điểm/báo cáo đều có bảng.
2. **Không có bảng nào thừa hoàn toàn về nghiệp vụ** — các bảng "thừa" đều là hạ tầng engine low-code, BLOB ảnh và cột dư.
3. **Rủi ro lớn nhất khi port sang web**: `TDONHANG` "đa năng" (phân biệt bằng `LOAI`/`MODE`) — chỉ 1 bảng cho ~9 loại chứng từ. Cần **xác minh tập giá trị** bằng SQL dưới đây trước khi thiết kế lại, tốt nhất là tách thành: `PhieuBanLe`, `PhieuBanSi`, `PhieuNhapKho`, `PhieuXuatKho`, `PhieuChuyenKho`, `PhieuKiemKe`, `PhieuTraNCC`, `PhieuTraKhach` — hoặc giữ 1 bảng + cột `LOAICHUNGTU` có bảng tra cứu.
4. **Báo cáo** không nằm ở tầng DB mà ở `SREPORT.SQL` (BLOB) — cần **export toàn bộ bảng `SREPORT` (SQL/PARAMS/FILTERCONFIG)** trước khi bỏ bản cũ, nếu không sẽ mất định nghĩa ~50+ báo cáo.
5. **Không có khóa ngoại thật** trong dump (chỉ `NOT NULL`) — quan hệ chỉ theo quy ước đặt tên `…ID`; web mới nên khai báo FK + index cho mọi cột `…ID` để tránh dữ liệu mồ côi khi chuyển đổi.
6. Dữ liệu dùng `STATUS INTEGER` cho xóa mềm (mọi danh mục có "Thùng rác" trong UI) — cần biết quy ước `STATUS` (1 = hoạt động, 0/-1 = xóa?) trước khi migrate.

## 6. SQL KIỂM CHỨNG (chạy trên Firebird để chốt các điểm còn nghi ngờ)

```sql
-- 1) TDONHANG đa năng: xác định tập LOAI/MODE và số lượng từng loại
SELECT LOAI, MODE, LOAIGIA, COUNT(*) AS SO_PHIEU,
       MIN(NAME) AS PHIEU_DAU, MAX(NAME) AS PHIEU_CUOI
FROM TDONHANG GROUP BY LOAI, MODE, LOAIGIA ORDER BY 1,2,3;

-- 2) Phân biệt chứng từ kho: bản ghi có Kho nhập / Kho xuất / cả hai
SELECT CASE WHEN DKHOXUATID IS NOT NULL AND DKHONHAPID IS NOT NULL THEN 'CHUYEN KHO'
            WHEN DKHONHAPID IS NOT NULL THEN 'NHAP'
            WHEN DKHOXUATID IS NOT NULL THEN 'XUAT' END AS LOAI_KHO,
       LOAI, COUNT(*) FROM TDONHANG GROUP BY 1,2;

-- 3) Tiền tố số phiếu (PN/PX/BS/COD/KTL/ĐN…) để suy ra loại chứng từ
SELECT SUBSTRING(NAME FROM 1 FOR 3) AS TIEN_TO, COUNT(*) FROM TDONHANG GROUP BY 1 ORDER BY 2 DESC;

-- 4) TTHUCHI: thu hay chi, theo đối tượng nào, có gắn công nợ không
SELECT LOAI, LOAIDOITUONG, CHUYENKHOAN, LATAMUNG, LAPHIEUTHUCONGNO, KHONGTHAYDOICONGNO, COUNT(*)
FROM TTHUCHI GROUP BY 1,2,3,4,5,6;
SELECT ID, NAME, LALYDOTHU, LOAILYDO FROM DLYDOTHUCHI;   -- lý do thu / chi

-- 5) Kiểm tra bảng/bảng tra còn thiếu
SELECT RDB$RELATION_NAME FROM RDB$RELATIONS
WHERE RDB$SYSTEM_FLAG = 0 ORDER BY 1;

-- 6) Export định nghĩa báo cáo (BẮT BUỘC trước khi bỏ app cũ)
SELECT ID, NAME, PARENTDIR, SQL, PARAMS, FILTERCONFIG, THUONGDUNG, RAWMODE FROM SREPORT;

-- 7) Export metadata low-code (menu/cột/form) để dựng lại UI + phân quyền
SELECT ID, NAME, PARENTID, PARENTDIR, ITEMTYPE, SORTORDER, STABLEDESCID,
       SFORMID, SREPORTID, SFUNCTIONID, OPERATION, MENUCODE, HIDEINMENU FROM SMENU ORDER BY SORTORDER;
SELECT * FROM SCOLUMN;  SELECT * FROM SGROUPROLE;  SELECT * FROM SUSER;
```

## 7. VIỆC CẦN LÀM TIẾP
1. Chạy 7 query trên để chốt: bảng tra `LOAI`/`MODE`, tiền tố số phiếu, quy ước `STATUS`, và **dump `SREPORT` + `SMENU` + `SCOLUMN`**.
2. Chốt danh sách bảng mới cần thêm (mục 3) — 18 hạng mục, trong đó 4 ưu tiên cao: **phiếu sản xuất**, **ledger kho (`TNKHOHANG`)**, **bảng giá (`DBANGGIA`)**, **log gửi tin**.
3. Sau đó mới thiết kế schema web (mục tiêu: ~60 bảng nghiệp vụ + ~10 bảng hệ thống, có FK/index đầy đủ, bỏ BLOB/engine low-code).

