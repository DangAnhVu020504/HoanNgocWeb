# DANH MỤC CHỨC NĂNG — PHẦN MỀM QUẢN LÝ BÁN HÀNG KHÔNG CÔNG NỢ v4.5
> Nguồn dữ liệu: 51 ảnh chụp màn hình trong `Giao diện HN\` (chụp 16/09/2026).
> Mục đích: checklist chức năng để viết lại bản web. Tổng: **10 nhóm menu — 76 mục menu — ~41 màn hình nghiệp vụ**.

## 1. KHUNG ỨNG DỤNG (dùng chung mọi màn hình)

| # | Thành phần | Mô tả |
|---|---|---|
| 1.1 | Thanh menu chính | 10 nhóm: HỆ THỐNG, BÁN HÀNG, CHĂM SÓC KH, KHO HÀNG, QUỸ, CÔNG NỢ, NHÂN SỰ, QUẢN TRỊ, BÁO CÁO, TRỢ GIÚP |
| 1.2 | Thanh công cụ nhanh | 12 nút: Bán lẻ, QLý bán lẻ, Bán sỉ, QLý bán sỉ, Doanh thu, Mặt hàng bán, Chi tiết hoạt động, Tổng hợp KQKD, Tồn kho, Ghi chú, Báo cáo, Thoát |
| 1.3 | MDI + thanh tab tài liệu | Mỗi phiếu/danh mục mở thành 1 tab (Hóa đơn bán lẻ, Tạo hóa đơn bán sỉ, Quản lý nhập kho…) + nút `Mở nhanh` |
| 1.4 | Trang chủ (Dashboard) | 9 khối chức năng, 41 lối vào nhanh (xem mục 3) |
| 1.5 | Thanh trạng thái | Phiên bản dùng thử (hết hạn N ngày) · CSDL: DEMO.FDB · Tài khoản: Admin · Tự động tải lại · Gõ tiếng Việt (Telex) |

### 1.6 Chức năng dùng chung cho mọi danh sách / phiếu
- **CRUD**: Thêm mới (Insert) · Chỉnh sửa (F4) · Xóa (Del) · Thùng rác (xóa mềm / khôi phục)
- **Lọc & tải**: Lọc (F3) · Refresh / Tải dữ liệu (F5) · Lọc dữ liệu (F3) · Hiển thị tất cả · lọc theo khoảng ngày (Từ ngày – Đến ngày)
- **Dữ liệu**: Xuất excel · Thêm excel / Nhập từ excel · In (Ctrl+P) · Tổng (cộng cột số) · P.tích (phân tích) · In báo cáo · In mã vạch
- **Cây lọc bên trái**: Nhóm mặt hàng · Kho hàng · Nhân viên · Nhóm khách hàng (Khách lẻ / TV Bạc / TV Đồng / TV Vàng) · Nhóm NCC (Hà Nội / Sài Gòn) · Đơn vị chuyển phát (VIETTEL / VNPT) · mục *Chưa thiết lập* + *Thùng rác*
- **Panel phải — TỔNG CỘNG**: Tiền hàng · Giảm giá (%) · Tiền thuế (%) · Phí vận chuyển · Đổi trả · Tổng cộng · Tiền thanh toán (bản bán lẻ thêm cột *Số lượng*, *Thành toán*)
- **Panel dưới — tab chi tiết liên quan**: Thông tin · Phiếu thu · Phiếu chi · Phiếu thu công nợ · <loại chứng từ> chi tiết · Đơn hàng chi tiết · Giao hàng theo xe
- **Footer lưu vết**: Khởi tạo / Khởi tạo bởi · Sửa đổi gần nhất / Sửa đổi bởi
- **Xem in**: Print · Save · thanh công cụ in · 1 of 1 · zoom 100% · Close

## 2. CHI TIẾT 10 NHÓM CHỨC NĂNG (76 mục menu)

### 2.1 HỆ THỐNG — 6 chức năng
| STT | Chức năng | Phím tắt | Ghi chú |
|---|---|---|---|
| 1 | Cơ sở dữ liệu → Tạo mới cơ sở dữ liệu | | tạo DB mới |
| 2 | Cơ sở dữ liệu → Đổi mật khẩu đăng nhập | | |
| 3 | Cơ sở dữ liệu → Sao lưu cơ sở dữ liệu | | backup Firebird |
| 4 | Cơ sở dữ liệu → Phục hồi cơ sở dữ liệu | | restore |
| 5 | Đăng xuất khỏi hệ thống | Ctrl+Shift+L | |
| 6 | Thoát khỏi hệ thống | Ctrl+Q | |

### 2.2 BÁN HÀNG — 16 chức năng
| STT | Chức năng | Phím tắt | Màn hình đã có ảnh |
|---|---|---|---|
| 1 | Danh mục cửa hàng | | ✔ (dialog Cửa hàng, cây TRỤ SỞ CHÍNH) |
| 2 | Danh mục mặt hàng | Ctrl+M | ✔ (Mã hàng, Tên, Nhóm, ĐVT, Giá bán lẻ, Giá nhập, Giá bán sỉ; nút In mã vạch) |
| 3 | Danh mục khách hàng | Ctrl+K | ✔ (Mã khách, Tên, Địa chỉ, Điện thoại, Email, Nhóm KH, Địa chỉ giao hàng, Người đại diện, Chức vụ, Số ngày cho nợ) |
| 4 | Danh mục đặt khuyến mại | | |
| 5 | Báo giá ▸ (tạo mới / quản lý) | | ✔ Quản lý báo giá bán hàng (Ngày, Số phiếu, Tên khách, Địa chỉ, Điện thoại, Tiền hàng, Tiền giảm giá, Tổng cộng) |
| 6 | Đặt hàng ▸ | | ✔ Quản lý khách đặt hàng |
| 7 | Hợp đồng ▸ | | |
| 8 | Bán lẻ ▸ | | ✔ HÓA ĐƠN BÁN HÀNG (lưới chọn hàng: Giá bán lẻ, ĐVT, Giá bán sỉ, ĐVT chẵn, Giá lẻ USD, Giá sỉ USD; nút Mở đặt hàng, Lưu tạm, Mở lại, Tăng số 1, Giảm số 1, Tạo hóa đơn mới, Thanh toán F12, Thống kê F10; tab TẤP HÓA – KHÁCH LẤY HÀNG – KHÁCH TRẢ HÀNG) |
| 9 | Bán sỉ ▸ | | ✔ TẠO HÓA ĐƠN BÁN SỈ (Số phiếu BS26/00001) + ✔ Quản lý hóa đơn bán sỉ (PHIẾU BÁN SỈ) |
| 10 | Trả lại ▸ | | ✔ Quản lý khách trả lại (Số phiếu, Ngày, Khách hàng, Tổng cộng, Tiền giảm giá, Tỉ lệ giảm giá, Tiền hàng, Trả lại kho, Nhân viên, Điền giải, Tiền thanh toán, Ghi chú, Tiền thuế, Tỉ lệ thuế, Đã xuất hóa đơn, Số hóa đơn, Đặt hàng nhà cung cấp) |
| 11 | Giao hàng ▸ | | ✔ Quản lý gửi hàng COD (COD16/0001) + ✔ Quản lý kết quả gửi COD (KQ16/0001) + KQ Giao |
| 12 | Lưu vết hoạt động | | ✔ (Giờ, Ngày, Số đơn hàng, Nội dung, Tài khoản; lọc Ngày/Tài khoản/Số phiếu/Nội dung; nút Xóa lưu vết) |
| 13 | Thống kê mặt hàng bán | Ctrl+L | ✔ (Mã hàng, Tên, SL, ĐVT, Đơn giá, Thành tiền bán, Giảm giá %, Giá vốn, Thành tiền nhập, Lãi, Tỉ lệ lãi; tổng: Tổng tiền bán / Tổng tiền nhập / Tổng tiền lãi / Tổng tiền giảm giá) |
| 14 | Thống kê doanh thu | Ctrl+T | ✔ (Số phiếu, Ngày, Giá thành toán(?)*, Khách hàng, Tiền hàng, Tiền giảm giá, Đổi trả, Tổng cộng, Thành toán bộ*, Mã khách, Tỉ lệ giảm giá, Điền giải, Khách đưa, Trả lại, Số phiếu đặt hàng, Giao hàng, Nhân viên, Phí vận chuyển, Địa chỉ, Tiền mặt, Tiền thẻ, Chuyển khoản, Điện thoại, Ghi chú, Tiền thuế, Tỉ lệ thuế) |
| 15 | Chi tiết hoạt động ngày | | ✔ (bản in: NHẬP HÀNG + TIỀN MẶT / CHUYỂN KHOẢN / TIỀN THẺ / KHÁCH HÀNG NỢ / NỢ NHÀ CUNG CẤP) |
| 16 | Tổng hợp kết quả kinh doanh | | ✔ (Doanh thu; Chi phí: Định phí [Tiền nhà, Điện thoại, Nước, Điện, Lương NV, Lương vệ sỹ, Thưởng NV, Chi lương NV], Biến phí, Chi khác [VPP/in ấn, Xây dựng–sửa chữa–thiết kế, Đồ dùng dụng cụ, Vận chuyển, Ngoại giao, Chi khác, Đặt trước]; LÃI/LỖ; cột %DT, %CP, Tăng giảm so với tháng trước, KQKD tháng trước) |

### 2.3 CHĂM SÓC KH — 4 chức năng
| STT | Chức năng | Màn hình đã có ảnh |
|---|---|---|
| 1 | Chăm sóc khách hàng | ✔ (Số phiếu, Ngày, Nhân viên, Giờ, Trình trạng gọi, Khách hàng, Ghi chú; lọc theo nhân viên) |
| 2 | Gửi tin nhắn tới khách hàng | |
| 3 | Khách hàng thân thiết | |
| 4 | Phân hồi của khách hàng | |

### 2.4 KHO HÀNG — 12 chức năng
| STT | Chức năng | Phím tắt | Màn hình đã có ảnh |
|---|---|---|---|
| 1 | Danh mục kho hàng | | ✔ (dialog Kho hàng; cây Kho thành phẩm / Kho vật tư) |
| 2 | Danh mục tài khoản ngân hàng | | (dùng làm cột "Tài khoản ngân hàng" của phiếu) |
| 3 | Sản xuất ▸ | | (tab "Phiếu sản xuất" xuất hiện ở danh mục MH/NCC) |
| 4 | Đặt mua hàng ▸ | | ✔ Quản lý đặt hàng nhà cung cấp (Số phiếu, Ngày, NCC, Tổng cộng, Phí vận chuyển, Tiền/Tỉ lệ giảm giá, Tiền/Tỉ lệ thuế, Tiền hàng, Ghi chú) |
| 5 | Nhập kho ▸ | | ✔ Quản lý nhập kho (PN18/00001: Số phiếu, Ngày, NCC, Kho nhập, Nhân viên nhập, Tiền hàng, Tiền/Tỉ lệ giảm giá, Tổng cộng, Cửa hàng, Còn lại, Thanh toán, Tài khoản ngân hàng, Số hóa đơn, Đã xuất hóa đơn) |
| 6 | Xuất kho ▸ | | ✔ Quản lý xuất kho (Số phiếu, Ngày, Kho xuất, Nhân viên xuất, Tiền hàng, Tiền/Tỉ lệ giảm giá, Tiền/Tỉ lệ thuế, Tổng cộng, Phí vận chuyển) |
| 7 | Xuất trả lại nhà cung cấp ▸ | | ✔ Quản lý xuất trả lại nhà cung cấp |
| 8 | Chuyển kho ▸ | | ✔ Quản lý chuyển kho (Số phiếu, Ngày, Điền giải, Kho xuất, Kho nhập, Nhân viên xuất, Nhân viên nhập, Tổng cộng…) |
| 9 | Kiểm kê ▸ | | ✔ Quản lý kiểm kê (Số phiếu, Ngày, Kho hàng, Điền giải, Nhân viên, Ghi chú, Cửa hàng, Còn lại, Thanh toán, Tài khoản ngân hàng, Đã xuất hóa đơn, Số hóa đơn) |
| 10 | Tính lại giá vốn | | ✔ (dialog: phương pháp Nhập trước xuất trước / Bình quân cuối kỳ / Bình quân gia quyền (liên hoàn) / Đích danh + "Tính từ ngày" + Thực hiện) |
| 11 | Tồn kho | Ctrl+Shift+T | ✔ (Mã hàng, Mặt hàng, Giá trị, Tồn, Giá bán, Tồn 2 đvt, ĐVT, Tồn tối thiểu, Giá trị vốn, Giá vốn, Hãng sx; lọc Kho hàng, Tìm F3, checkbox "Chỉ hiển thị mặt hàng có tồn kho <> 0"; nút In báo cáo, Xuất excel; tab **Thẻ kho**: Số phiếu, Ngày, Điền giải, SL nhập, SL xuất, Ghi chú, ĐVT, Giảm giá %, Đơn giá, Tồn, Thành tiền) |
| 12 | Tồn tất cả các kho | | |

### 2.5 QUỸ — 8 chức năng
| STT | Chức năng | Phím tắt | Màn hình đã có ảnh |
|---|---|---|---|
| 1 | Danh mục lý do thu chi | | |
| 2 | Tạo phiếu thu | Ctrl+Shift+T | |
| 3 | Tạo phiếu chi | Ctrl+Shift+C | |
| 4 | Danh mục phiếu thu | | |
| 5 | Danh mục phiếu chi | | |
| 6 | Danh sách phiếu thu nhiều đối tượng | | |
| 7 | Danh sách phiếu chi nhiều đối tượng | | |
| 8 | Tổn quỹ | | ✔ (Số phiếu, Ngày, Đối tượng, Thu, Chi, Thu–Chi, Tồn quỹ, …) |

### 2.6 CÔNG NỢ — 3 chức năng
| STT | Chức năng | Màn hình đã có ảnh |
|---|---|---|
| 1 | Công nợ khách hàng | ✔ Tên khách hàng, Mã khách, Địa chỉ, Điện thoại, Email, Mã số thuế, Công nợ; lọc Nhóm KH (Khách lẻ/TV Bạc/TV Đồng/TV Vàng); nút Lọc dữ liệu (F3), Refresh, Hiển thị tất cả, **Thanh toán**, **In tổng hợp nợ**; dòng TỔNG; lưới chi tiết phát sinh: Số phiếu, Ngày, Tổng cộng, Điền giải, Tiền thanh toán, Lũy kế, Ghi chú |
| 2 | Công nợ nhà cung cấp | ✔ Tương tự (cây HÀ NỘI / SÀI GÒN) |
| 3 | Theo dõi nợ | ✔ (Số phiếu ĐN16/0004, Ngày, Khách hàng, Nhân viên, Ghi chú; lọc theo nhân viên) |

### 2.7 NHÂN SỰ — 6 chức năng
| STT | Chức năng | Màn hình đã có ảnh |
|---|---|---|
| 1 | Danh mục nhân viên | ✔ (dialog Nhân viên: Nguyễn Minh Trang, Nguyễn Tú Oanh, Phạm Huyền Trang, Phạm Quế Chi, Thukho, Tống Uyên My, Trần Thùy Hằng) |
| 2 | Giấy giới thiệu | |
| 3 | Tạm ứng lương | ✔ (mục trong khối SỔ QUỸ TIỀN MẶT ở Dashboard) |
| 4 | Thưởng phạt | |
| 5 | Chấm công | |
| 6 | Tính lương | |

### 2.8 QUẢN TRỊ — 9 chức năng
| STT | Chức năng | Ghi chú |
|---|---|---|
| 1 | Thư viện ảnh... | quản lý ảnh dùng chung (ảnh sản phẩm/nhân viên/logo) |
| 2 | Dữ liệu ban đầu ▸ | nhập số dư đầu kỳ |
| 3 | Chi tiết doanh thu | |
| 4 | Khóa sổ | chốt kỳ kế toán |
| 5 | Xóa dữ liệu | |
| 6 | Công cụ nhà phát triển | |
| 7 | Lịch sử tác hệ thống | audit log |
| 8 | Người dùng và phân quyền | tài khoản + quyền |
| 9 | Cài hình toàn hệ thống | tham số/thiết lập chung |

### 2.9 BÁO CÁO — 9 nhóm báo cáo (cửa vào Trung tâm báo cáo)
`BÁO CÁO QUỸ` · `BÁO CÁO DANH MỤC` · `BÁO CÁO BÁN HÀNG` · `BÁO CÁO ĐẶT HÀNG` · `BÁO CÁO KHO HÀNG` · `BÁO CÁO CÔNG NỢ` · `BÁO CÁO QUẢN TRỊ` · `BÁO CÁO BIỂU ĐỒ` · `BÁO CÁO GỬI HÀNG`

**Trung tâm báo cáo (màn hình "Báo cáo")** — cây báo cáo + khu tham số + hướng dẫn 4 bước:
- Tab lọc cây: **Tất cả / Thường dùng / Tìm kiếm**; công cụ: **Thiết kế mẫu · Tùy chỉnh tham số báo cáo · Xem dữ liệu thô**
- Nhánh QUỸ (mở sẵn — 7 báo cáo): DANH SÁCH PHIẾU THU THEO NGÀY · DANH SÁCH PHIẾU THU THEO LÝ DO THU CHI · DANH SÁCH PHIẾU CHI THEO NGÀY · DANH SÁCH PHIẾU CHI THEO LÝ DO THU CHI · TỔNG HỢP THU CHI THEO NGÀY · TỔNG HỢP THU CHI THEO LÝ DO · BÁO CÁO TỒN QUỸ
- Nhánh KHO HÀNG (mở sẵn): BÁO CÁO NHẬP HÀNG · BÁO CÁO KIỂM KÊ · BÁO CÁO THEO HSD · BÁO CÁO XUẤT KHÁC · BÁO CÁO CHUYỂN KHO · …
- Tham số mẫu của báo cáo: Ngày (Từ/Đến) · Lý do thu chi · Cửa hàng · nút **Hiển thị**
- Báo cáo **biểu đồ**: BIỂU ĐỒ DOANH SỐ THEO NHÓM HÀNG HÓA · BIỂU ĐỒ DOANH THU NGÀY TRONG THÁNG (legend Tháng 1→12) · BIỂU ĐỒ DOANH THU THÁNG TRONG NĂM (legend Năm nay / Năm ngoái)
- Báo cáo đã có ảnh bản in: TỔNG HỢP BÁN HÀNG THEO NGÀY (STT, Số phiếu, Nhân viên bán, Thu ngân, Tiền hàng, Đổi trả, Giảm giá, Tiền thuế, Tổng cộng, Tiền mặt, Thẻ) · BÁO CÁO TỔNG HỢP XUẤT NHẬP TỒN (Tồn đầu / Nhập / Xuất / Tồn theo SL & Giá trị) · TỔNG HỢP CÔNG NỢ KHÁCH HÀNG · TỔNG HỢP CÔNG NỢ NHÀ CUNG CẤP · TỔNG HỢP KẾT QUẢ KINH DOANH · CHI TIẾT HOẠT ĐỘNG TRONG NGÀY

### 2.10 TRỢ GIÚP — 3 chức năng
| STT | Chức năng |
|---|---|
| 1 | Hỗ trợ từ tư vấn viên (remote) |
| 2 | Đăng ký bản quyền... |
| 3 | Thông tin phần mềm |

## 3. TRANG CHỦ (DASHBOARD) — 9 khối, 41 lối vào nhanh
| Khối | Các nút chức năng |
|---|---|
| DANH MỤC (6) | Cửa hàng · Mặt hàng · Khách hàng · Kho hàng · Nhân viên · Nhà cung cấp |
| SỔ QUỸ TIỀN MẶT (4) | Phiếu thu · Phiếu chi · Tồn quỹ · Tạm ứng lương |
| BÁO CÁO HÀNG NGÀY (4) | Thống kê doanh thu · Thống kê mặt hàng · Chi tiết hoạt động · Tổng hợp kết quả kinh doanh |
| BÁN HÀNG (7) | Báo giá · Đặt hàng · Khách trả lại · Giao COD · Bán lẻ · Bán sỉ · KQ giao |
| KHO HÀNG (8) | Nhập hàng · Chuyển kho · Kiểm kê kho · Tính lại giá vốn · Đặt hàng · Xuất khác · Xuất trả NCC · Tồn kho |
| BÁO CÁO KHÁC (4) | Tổng hợp bán hàng theo ngày · Báo cáo tổng hợp xuất nhập tồn · Báo cáo công nợ khách hàng · Báo cáo công nợ nhà cung cấp |
| CÔNG NỢ (3) | Công nợ khách hàng · Công nợ nhà cung cấp · Theo dõi nợ |
| CHỨC NĂNG KHÁC (2) | Chăm sóc KH · Lưu vết bán lẻ |
| BÁO CÁO BIỂU ĐỒ (3) | Doanh số theo nhóm · Doanh thu ngày trong tháng · Doanh thu tháng trong năm |

## 4. BẢNG ÁNH XẠ 51 ẢNH → MÀN HÌNH CHỨC NĂNG
| Ảnh (giờ chụp 16/09/2026) | Màn hình |
|---|---|
| 145154 | Trang chủ (Dashboard) |
| 145203 | Bán lẻ — HÓA ĐƠN BÁN HÀNG |
| 145210 | Tổng hợp kết quả kinh doanh (bản in) |
| 145218 | Kho hàng — Tồn kho (+ tab Thẻ kho) |
| 145226 | Chi tiết hoạt động trong ngày (bản in) |
| 145234 | Quản lý bán lẻ (+ panel Hóa đơn, Danh sách bill hủy) |
| 145240 | Tạo hóa đơn bán sỉ (BS26/00001) |
| 145247 | Quản lý hóa đơn bán sỉ (PHIẾU BÁN SỈ) |
| 145319, 145406 | Thống kê doanh thu |
| 145514 | Thống kê mặt hàng bán |
| 145522 | Biểu đồ doanh số theo nhóm hàng hóa |
| 145638 | Trung tâm báo cáo (cây báo cáo + tham số) |
| 145756…145903 | 10 menu chính + menu con HỆ THỐNG > Cơ sở dữ liệu |
| 150144 | Danh mục mặt hàng |
| 150206 | Danh mục cửa hàng (dialog) |
| 150212 | Danh mục kho hàng (dialog) |
| 150223 | Danh mục nhân viên (dialog) |
| 150231 | Danh mục khách hàng |
| 150239 | Danh mục nhà cung cấp |
| 150302 | Quản lý báo giá bán hàng |
| 150727 | Quản lý gửi hàng COD (COD16/0001) |
| 150737 | Quản lý kết quả gửi COD (KQ16/0001) |
| 150752 | Quản lý nhập kho |
| 150828 | Quản lý đặt hàng nhà cung cấp |
| 150837 | Quản lý chuyển kho |
| 150849 | Quản lý xuất kho |
| 150859 | Quản lý kiểm kê |
| 151016 | Tính lại giá vốn (dialog) |
| 151033 | Báo cáo bán hàng theo ngày (bản in) |
| 151040 | Báo cáo tổng hợp xuất nhập tồn (bản in) |
| 151051 | Tổng hợp công nợ khách hàng (bản in) |
| 151059 | Tổng hợp công nợ nhà cung cấp (bản in) |
| 151110 | Công nợ khách hàng |
| 151120 | Công nợ nhà cung cấp |
| 151131 | Theo dõi nợ (ĐN16/0004) |
| 151143 | Chăm sóc khách hàng |
| 151153 | Lưu vết hoạt động |
| 151223 | Biểu đồ doanh thu ngày trong tháng |
| 151240 | Biểu đồ doanh thu tháng trong năm |

## 5. PHÍM TẮT CẦN GIỮ KHI LÀM BẢN WEB
| Phím | Chức năng | Phím | Chức năng |
|---|---|---|---|
| F3 | Lọc / Tìm / Lọc dữ liệu | Ctrl+M | Danh mục mặt hàng |
| F4 | Chỉnh sửa (bản ghi) | Ctrl+K | Danh mục khách hàng |
| F5 | Refresh / Tải dữ liệu | Ctrl+L | Thống kê mặt hàng bán |
| F7 | Mức giá | Ctrl+T | Thống kê doanh thu |
| F8 | Xóa dòng chi tiết | Ctrl+P | In phiếu |
| F9 | Tồn kho | Ctrl+Q | Thoát khỏi hệ thống |
| F10 | Thống kê | Ctrl+Shift+T | Tồn kho / Tạo phiếu thu |
| F11 | Lịch sử mua | Ctrl+Shift+C | Tạo phiếu chi |
| F12 | Thanh toán / Xem (báo cáo) | Ctrl+Shift+L | Đăng xuất |
| Insert | Thêm mới | Ctrl+Shift+P | Xem (báo cáo) |
| Delete | Xóa bản ghi | | |

## 6. QUY TẮC MÃ CHỨNG TỪ (quan sát từ dữ liệu demo)
| Loại | Mẫu | Ví dụ |
|---|---|---|
| Phiếu nhập kho | PN + YY / 5 số | PN18/00001 |
| Phiếu xuất kho | PX + YY / 5 số | PX18/00001 |
| Hóa đơn bán sỉ | BS + YY / 5 số | BS26/00001 |
| Báo giá / Đặt hàng | tiền tố riêng + YY / 5 số | — |
| Kiểm kê | KTL + YY / 5 số | KTL18/00001, KTL17/00001 |
| Gửi COD | COD + YY / 4 số | COD16/0001 |
| Kết quả gửi COD | KQ + YY / 4 số | KQ16/0001 |
| Theo dõi nợ | ĐN + YY / 4 số | ĐN16/0004 |
| Hóa đơn bán lẻ | 8 chữ số | 26000001 |
| Mã danh mục | tiền tố + STT | AMC0001 (mặt hàng), KH00001 (khách), NCC00001 (NCC) |

## 7. CHỨC NĂNG CÓ TRONG MENU NHƯNG CHƯA CÓ ẢNH MÀN HÌNH
Hợp đồng · Sản xuất (phiếu sản xuất) · Danh mục đặt khuyến mại · Danh mục lý do thu chi · Tạo phiếu thu / Tạo phiếu chi · Danh mục phiếu thu / phiếu chi · Danh sách phiếu thu – phiếu chi nhiều đối tượng · Danh mục tài khoản ngân hàng · Gửi tin nhắn tới khách hàng · Khách hàng thân thiết · Phân hồi của khách hàng · Giấy giới thiệu · Thưởng phạt · Chấm công · Tính lương · Tồn tất cả các kho · (form tạo) Xuất trả lại NCC · Xóa dữ liệu · Công cụ nhà phát triển · Lịch sử tác hệ thống · Người dùng và phân quyền · Cài hình toàn hệ thống · Thư viện ảnh · Dữ liệu ban đầu · Chi tiết doanh thu · Khóa sổ · Hỗ trợ từ tư vấn viên · Đăng ký bản quyền · Thông tin phần mềm.

## 8. GHI CHÚ NGHIỆP VỤ / KỸ THUẬT
- **Miền nghiệp vụ**: bán hàng – kho – quỹ – công nợ cho cửa hàng vàng bạc & quà tặng (Công ty TNHH Ngọc Châu Âu, 455 Sư Vạn Hạnh P.12 Q.10 TP.HCM) – CSDL Firebird `DEMO.FDB`, chạy phiên bản dùng thử (19 ngày).
- **Đa giá & đa tiền tệ**: mỗi mặt hàng có Giá bán lẻ (VND), Giá bán sỉ (VND), Giá lẻ USD, Giá sỉ USD, Giá nhập, Giá vốn, Tồn tối thiểu, ĐVT + ĐVT chẵn; có nút **In mã vạch**.
- **Đa kho – đa cửa hàng**: chứng từ kho đều có Kho xuất / Kho nhập / Cửa hàng / Còn lại / Thanh toán / Tài khoản ngân hàng / Đã xuất hóa đơn / Số hóa đơn / Đặt hàng nhà cung cấp.
- **Dòng chi tiết hàng hóa**: Số lượng, Đơn giá, Giảm giá %, Tiền giảm, ĐVT, Thành tiền, **Hạn sử dụng**, **Seri**, Giá gần nhất.
- **Xóa mềm**: mọi danh mục đều có nút/nhánh **Thùng rác**.
- **Lưu vết**: footer *Khởi tạo – Khởi tạo bởi* và *Sửa đổi gần nhất – Sửa đổi bởi* ở mọi danh mục; module "Lưu vết hoạt động" ghi theo tài khoản/đơn (Giờ, Ngày, Số đơn hàng, Nội dung, Tài khoản) và có nút *Xóa lưu vết*.
- **COD**: tích hợp đơn vị chuyển phát VIETTEL / VNPT, có phiếu kết quả gửi COD.
- **Hạng khách hàng**: Khách lẻ / TV Bạc / TV Đồng / TV Vàng, chức năng *Khách hàng thân thiết*, *Tặng giảm điểm*, *Số ngày cho nợ*.
- **Lỗi chính tả bản cũ cần sửa khi làm lại**: "Thành toán" → *Thanh toán*; "Điện giải"/"Điền giải" → *Diễn giải*; "Bã xuất hóa đơn đỏ" → *Đã xuất hóa đơn đỏ*; "Bill hũy" → *Bill hủy*; "Trình trạng gọi" → *Tình trạng gọi*; "Tổn quỹ" → *Tồn quỹ*; "Trích nhiên viên" → *Trực nhiên viên*; "Thành toán bộ"/"Giá thành toán" → *Thành tiền*.
- **Chưa xác minh 100%**: nhánh *BÁO CÁO KHO HÀNG* trong Trung tâm báo cáo bị panel tham số che một số dòng tên báo cáo.

> 📌 **Đối chiếu với CSDL thực tế**: xem file `DOI-CHIEU-CSDL-VA-CHUC-NANG.md` — kết quả kiểm tra 86 bảng Firebird (`TechPos_SM4_Database_Structure.md`) ↔ 76 mục chức năng: **~84% mục đã có bảng**, thiếu 12 mục (phiếu sản xuất, log gửi tin, số dư đầu kỳ, ledger kho, bảng giá, log xóa dữ liệu, module TRỢ GIÚP…), thừa hạ tầng engine low-code + BLOB ảnh.


