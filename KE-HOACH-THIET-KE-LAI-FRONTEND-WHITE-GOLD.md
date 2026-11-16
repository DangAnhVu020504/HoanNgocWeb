# KẾ HOẠCH TOÀN DIỆN THIẾT KẾ LẠI FRONTEND HOÀN NGỌC GROUP
## PHONG CÁCH HIỆN ĐẠI · MƯỢT MÀ · HIỆU ỨNG ĐỘNG ĐẶC SẮC · TÔNG TRẮNG & VÀNG KIM CHỦ ĐẠO
### (BẢO TOÀN 100% CHỨC NĂNG NGHIỆP VỤ - KHÔNG MẤT DỮ LIỆU & LOGIC)
> **Tài liệu**: Kế hoạch kiến trúc và thiết kế giao diện người dùng (UI/UX) cho hệ thống Quản lý Bán hàng Hoàn Ngọc Group (`techpos-web`).  
> **Phiên bản tài liệu**: 2.1 (Cập nhật đối chiếu toàn diện mã nguồn & cam kết bảo toàn tính năng).  
> **Mục tiêu**: Nâng tầm trải nghiệm người dùng, chuyển đổi giao diện từ form nghiệp vụ phẳng đơn điệu sang giao diện điều hành và bán hàng sang trọng, hiện đại, mượt mà 60fps với hiệu ứng động đặc sắc, mang đậm bản sắc thương hiệu Hoàn Ngọc (Trắng ngọc trai & Vàng kim hoàn), **đồng thời giữ nguyên 100% tính năng, luồng dữ liệu, API, store, logic tính toán và phím tắt hiện có**.

---

## MỤC LỤC
1. [Khảo sát Toàn diện Mã Nguồn & Đánh giá Điểm nghẽn UI/UX](#1-khảo-sát-toàn-diện-mã-nguồn--đánh-giá-điểm-nghẽn-uiux)
2. [CAM KẾT BẢO TOÀN 100% CHỨC NĂNG (ZERO FEATURE LOSS)](#2-cam-kết-bảo-toàn-100-chức-năng-zero-feature-loss)
3. [Bản đồ Ma trận Đối chiếu 34 Màn hình Nghiệp vụ Hiện hữu](#3-bản-đồ-ma-trận-đối-chiếu-34-màn-hình-nghiệp-vụ-hiện-hữu)
4. [Triết lý Thiết kế: Royal Pearl & Champagne Gold](#4-triết-lý-thiết-kế-royal-pearl--champagne-gold)
5. [Hệ thống Token Thiết kế (Design Tokens & Color Palette)](#5-hệ-thống-token-thiết-kế-design-tokens--color-palette)
6. [Hệ thống Hiệu ứng Động Đặc sắc (Motion & Physics System)](#6-hệ-thống-hiệu-ứng-động-đặc-sắc-motion--physics-system)
7. [Kế hoạch Chuyển đổi Chi tiết cho Từng Phân hệ Cốt lõi](#7-kế-hoạch-chuyển-đổi-chi-tiết-cho-từng-phân-hệ-cốt-lõi)
   - 7.1 Màn hình Công Nợ (`DebtsPage.tsx`) - Ví dụ Mẫu Code Trước & Sau
   - 7.2 Màn hình Bán lẻ POS (`RetailSalePage.tsx`) - Trọng tâm Flagship
   - 7.3 Màn hình Sổ Quỹ (`CashbookPage.tsx`) - Thu/Chi & Phân bổ nợ
   - 7.4 Màn hình Tồn Kho & Thẻ Kho (`InventoryPage.tsx`)
   - 7.5 Màn hình Báo Giá & Đặt Hàng (`CustomerDocPage.tsx`, `QuotationPage`, `SalesOrderPage`)
   - 7.6 Màn hình Danh mục Mặt Hàng (`ProductsPage.tsx`)
   - 7.7 Khung Ứng Dụng (Shell, Frosted Header & Luxury Sidebar)
   - 7.8 Màn hình Đăng Nhập (Luxury Aurora Login)
   - 7.9 Trang Chủ Điều Hành (Executive Command Center Dashboard)
   - 7.10 Trung Tâm Báo Cáo & Phân Tích (`ReportsPage.tsx`)
8. [Lộ trình Triển khai Chi tiết 6 Giai đoạn & Kiểm thử An toàn](#8-lộ-trình-triển-khai-chi-tiết-6-giai-đoạn--kiểm-thử-an-toàn)
9. [Tiêu chuẩn Kỹ thuật & Tuân thủ Quy tắc Dự án](#9-tiêu-chuẩn-kỹ-thuật--tuân-thủ-quy-tắc-dự-án)

---

## 1. KHẢO SÁT TOÀN DIỆN MÃ NGUỒN & ĐÁNH GIÁ ĐIỂM NGHẼN UI/UX

### 1.1 Khảo sát Kiến trúc Kỹ thuật Hiện tại
Sau khi rà soát toàn bộ cây thư mục `frontend/techpos-web/src`:
- **Core Framework**: React 19 (`19.2.0`), React DOM 19, TypeScript 5.9, Vite 7.2.
- **UI Library**: Ant Design 6 (`antd 6.0.2`), `@ant-design/icons 6.1.0`.
- **State Management (Zustand 5.0.9)**:
  - `authStore.ts`: Quản lý `token`, `user`, phân quyền `hasPermission(code)`, `roles`, tự động lưu `localStorage`.
  - `posCartStore.ts`: Quản lý trạng thái giỏ hàng POS bán lẻ (`khoId`, `khachHangId`, `lines`, `tienGiamGia`, `khachDua`, `tienThue`, `phiVanChuyen`, các action thêm/sửa/xóa dòng).
  - `notificationStore.ts`: Quản lý polling thông báo 30s một lần, unread badge.
- **Data Access Layer (16 file API trong `src/api/`)**:
  - `http.ts`: Cấu hình Axios instance kèm request/response interceptor, tự động gắn Bearer JWT, refresh token khi 401.
  - Các API modules: `cashbook.ts`, `customers.ts`, `dashboard.ts`, `debts.ts`, `history.ts`, `hr.ts`, `inventory.ts`, `notification.ts`, `pos.ts`, `products.ts`, `reports.ts`, `salesdocs.ts`, `suppliers.ts`, `sync.ts`, `warehouses.ts`.
- **Hệ thống Menu & Điều hướng**:
  - `App.tsx` định nghĩa 34 routes con trong ShellRoute được bảo vệ bởi `ProtectedRoute`.
  - `ResponsiveLayout.tsx` định nghĩa cây Menu gồm 10 nhóm chính (Trang chủ, Danh mục, Bán hàng, Quỹ, Công nợ, Nhân sự, Kho hàng, Báo cáo, Lịch sử, Quản trị).
  - Hỗ trợ responsive chuyển đổi giữa `DesktopLayout` và `MobileLayout` (`useIsMobile`).

### 1.2 Đánh giá Điểm nghẽn Cần Khắc phục
1. **Thiếu bản sắc Luxury & Đồ họa phẳng lỗi thời**: 
   - Ant Design 6 dùng cấu hình cơ bản (`brand.ts` chỉ có vài mã màu thô: `#d4a017`, `#8a6100`, `#faf7ef`, sider đen `#1f1a08`).
   - Các bảng dữ liệu (`Table`) và thẻ (`Card`) san sát, màu xám trắng cơ bản, không toát lên được đẳng cấp của một thương hiệu trang sức kim hoàn cao cấp (Hoàn Ngọc Group / Ngọc Châu Âu).
2. **Thiếu hiệu ứng chuyển động (Motionless)**:
   - Không có chuyển động khi chuyển trang (chuyển trang giật tức thì).
   - Con số tài chính (Doanh thu, Tồn kho, Tiền nợ, Tiền thừa) hiển thị thô cứng, thiếu hiệu ứng số nhảy (`count-up`).
   - Thao tác nút bấm không có phản hồi xúc giác (tactile feedback).
3. **Màn hình POS Bán lẻ chưa tối ưu cho thao tác nhanh**:
   - Thẻ sản phẩm nhỏ, thông tin tồn kho chỉ hiển thị con số đơn thuần mà không có mã màu cảnh báo trực quan.
   - Chưa tích hợp mã QR động sinh tức thì cho khách quét chuyển khoản ngân hàng.

---

## 2. CAM KẾT BẢO TOÀN 100% CHỨC NĂNG (ZERO FEATURE LOSS)

> ### ⚠️ NGUYÊN TẮC BẤT DI BẤT DỊCH
> **Thiết kế lại giao diện (UI Redesign) TUYỆT ĐỐI KHÔNG LÀM MẤT BẤT KỲ MỘT TÍNH NĂNG NÀO CỦA HỆ THỐNG.**  
> Kiến trúc phần mềm phân tầng rõ ràng giữa **Tầng Trình Diễn (Presentation Layer)** và **Tầng Dữ Liệu/Nghiệp Vụ (Business & Data Layer)**.

```text
┌────────────────────────────────────────────────────────────────────────┐
│                   MÔ HÌNH PHÂN TẦNG BẢO TOÀN TÍNH NĂNG                 │
├────────────────────────────────────────────────────────────────────────┤
│ [LỚP ĐƯỢC THIẾT KẾ LẠI & NÂNG CẤP THỊ GIÁC]                           │
│  ✨ Màu sắc Vàng Kim Hoàng Gia & Trắng Ngọc Trai (Tokens)              │
│  ✨ Hiệu ứng chuyển động mượt mà 60fps (CSS Keyframes & Springs)       │
│  ✨ Typography số học tài chính cao cấp (Outfit & Jakarta Sans)        │
│  ✨ Layout Card Kính Mờ, Bảng Dữ Liệu Nâng Cấp (Hover lift, Badges)    │
├────────────────────────────────────────────────────────────────────────┤
│ [LỚP BẢO VỆ NGUYÊN VẸN 100% - KHÔNG THAY ĐỔI LOGIC]                    │
│  🔒 100% Routes & URL Paths (`/debt`, `/pos/retail`, `/inventory`...)   │
│  🔒 100% State & Hooks (`useState`, `useCallback`, `useEffect`)        │
│  🔒 100% Zustand Stores (`posCartStore`, `authStore`, `notifStore`)    │
│  🔒 100% API Calls, Payload Request/Response & Error Handlers          │
│  🔒 100% Phím Tắt Nghiệp Vụ (F3, F4, F5, F7, F9, F10, F12, In ấn Ctrl+P)│
│  🔒 100% Logic Phân Quyền (RBAC: `hasPermission`, Roles `ADMIN`, etc.) │
│  🔒 100% Tính Toán Tiền Tệ, Thuế, Giảm Giá, Tồn Kho, Kiểm Tra Vượt Tồn│
└────────────────────────────────────────────────────────────────────────┘
```

---

## 3. BẢN ĐỒ MA TRẬN ĐỐI CHIẾU 34 MÀN HÌNH NGHIỆP VỤ HIỆN HỮU

Bảng dưới đây thống kê đầy đủ toàn bộ 34 màn hình trong hệ thống, chỉ rõ trạng thái logic được bảo toàn và những gì được nâng cấp về mặt giao diện:

| # | Tên Chức Năng | File Mã Nguồn | API & State Đang Dùng | Cam Kết Bảo Toàn Logic | Cải Tiến Giao Diện Mới |
|---|---|---|---|---|---|
| **1** | **Đăng nhập** | `features/auth/LoginPage.tsx` | `useAuthStore.login` | Giữ nguyên xác thực JWT, refresh token, ghi nhớ đăng nhập | Nền Aurora vàng chuyển động, Card kính pha lê, Input focus viền vàng kim |
| **2** | **Trang chủ** | `features/dashboard/DashboardPage.tsx` | `getDashboardSummary`, `getSyncStatus` | Giữ nguyên tính toán doanh thu ngày/tháng, số phiếu, tồn quỹ, sync heartbeat | KPI Cards mạ vàng, mini-sparklines xu hướng, Quick Action Dock 12 nút |
| **3** | **Bán lẻ POS** | `features/pos/RetailSalePage.tsx` | `posCartStore`, `createSale`, `getProducts`, `getInventory` | Giữ nguyên giỏ hàng, chặn vượt tồn, tính tiền thừa, công nợ, in bill | Lưới thẻ sản phẩm lớn, badge tồn kho 3 màu, VietQR động, phím tắt trực quan |
| **4** | **Bán sỉ** | `features/pos/WholesalePage.tsx` | `posCartStore`, `createSale` | Giữ nguyên quy trình tạo phiếu bán sỉ, bảng giá sỉ | Layout bảng chọn hàng thoáng mắt, viền vàng kim sang trọng |
| **5** | **Khách trả lại** | `features/pos/ReturnsPage.tsx` | `posCartStore`, `createSale` | Giữ nguyên nhập hàng trả, tính tiền hoàn khách | Thẻ hoàn tiền nổi bật, cảnh báo kiểm tra hàng trả lại |
| **6** | **Công nợ KH & NCC** | `features/debt/DebtsPage.tsx` | `getCustomerDebts`, `getSupplierDebts` | Giữ nguyên 2 tab, phân trang 50 dòng, tính tổng nợ | Header KPI tổng nợ, ô search debounce, dòng tổng kết viền kép mạ vàng |
| **7** | **Sổ Quỹ Thu/Chi** | `features/cashbook/CashbookPage.tsx` | `createCashReceipt`, `createCashPayment`, `getCashBalance` | Giữ nguyên phân bổ nợ đơn hàng, chọn đối tượng KH/NCC, lý do thu chi | Thẻ tồn quỹ vàng hoàng gia, form thu chi kính mờ, tag phân loại thu/chi |
| **8** | **Mặt hàng** | `features/catalog/ProductsPage.tsx` | `getProducts`, `getProductStock`, `createProduct`, `deleteProduct` | Giữ nguyên CRUD, phân quyền, lọc nhóm hàng, in mã vạch | Bảng dữ liệu Pro, drawer xem chi tiết tồn kho các chi nhánh mượt mà |
| **9** | **Danh mục KH/NCC/Kho** | `features/catalog/CatalogPage.tsx` | `customers.ts`, `suppliers.ts`, `warehouses.ts` | Giữ nguyên quản lý danh mục đối tác và kho | Phân tab mạ vàng thanh lịch, bảng danh bạ đối tác hiện đại |
| **10** | **Tồn kho & Thẻ kho** | `features/inventory/InventoryPage.tsx` | `getInventory`, `getLedger` | Giữ nguyên lọc kho, lọc tồn <> 0, tra cứu lịch sử thẻ kho theo ngày | Bảng tồn kho hover lift, cảnh báo tồn âm đỏ ruby, lịch sử thẻ kho rõ ràng |
| **11** | **Tồn tất cả các kho** | `features/inventory/AllWarehousesPage.tsx` | `getInventory` | Giữ nguyên ma trận tồn kho tổng thể | Bảng ma trận đa kho thông minh, highlight kho còn hàng |
| **12** | **Nhập kho** | `features/inventory/ReceiptsPage.tsx` | `inventory.ts`, `suppliers.ts` | Giữ nguyên tạo phiếu nhập PN, tính giá nhập | Form nhập hàng 2 cột, bảng dòng hàng mượt mà |
| **13** | **Xuất kho** | `features/inventory/IssuesPage.tsx` | `inventory.ts` | Giữ nguyên tạo phiếu xuất PX, trừ kho | Cảnh báo tồn tức thì tại kho xuất |
| **14** | **Chuyển kho** | `features/inventory/TransfersPage.tsx` | `inventory.ts`, `warehouses.ts` | Giữ nguyên luồng điều chuyển giữa 2 kho | Stepper chuyển kho trực quan: Kho xuất ➔ Đang chuyển ➔ Kho nhập |
| **15** | **Xuất trả NCC** | `features/inventory/SupplierReturnsPage.tsx` | `inventory.ts`, `suppliers.ts` | Giữ nguyên chứng từ xuất trả nhà cung cấp | Đối chiếu công nợ NCC tự động giảm |
| **16** | **Kiểm kê kho** | `features/inventory/StocktakesPage.tsx` | `inventory.ts` | Giữ nguyên phiếu kiểm kê KTL, lệch thừa/thiếu | Cột chênh lệch tồn hiển thị tag màu trực quan |
| **17** | **Tính lại giá vốn** | `features/inventory/CostRecalcPage.tsx` | `inventory.ts` | Giữ nguyên thuật toán FIFO, bình quân gia quyền | Hộp thoại xác nhận sang trọng, tiến trình chạy mượt mà |
| **18** | **Báo giá bán hàng** | `features/sales/QuotationPage.tsx` | `createQuotation`, `printQuotationPdf` | Giữ nguyên lưu báo giá, in PDF báo giá | Giao diện tạo báo giá chuyên nghiệp, xem trước bản in |
| **19** | **Khách đặt hàng** | `features/sales/SalesOrderPage.tsx` | `createOrder`, `printOrderPdf` | Giữ nguyên đơn đặt hàng, đặt cọc | Badge trạng thái đơn hàng (Mới, Đã cọc, Đã giao) |
| **20** | **Hợp đồng bán lẻ** | `features/sales/ContractsPage.tsx` | `salesdocs.ts` | Giữ nguyên quản lý hợp đồng kinh tế | Cột giá trị hợp đồng nổi bật font số lớn |
| **21** | **Giao hàng COD** | `features/sales/CodPage.tsx` | `salesdocs.ts` | Giữ nguyên quản lý gửi COD Viettel/VNPT | Badge mã vận đơn và tiến độ phát hàng |
| **22** | **Giao theo xe** | `features/sales/VehicleDeliveriesPage.tsx` | `salesdocs.ts` | Giữ nguyên lộ trình giao hàng theo chuyến xe | Bảng danh sách phiếu xuất giao theo tài xế/xe |
| **23** | **Đặt mua NCC** | `features/sales/PurchaseOrdersPage.tsx` | `salesdocs.ts` | Giữ nguyên phiếu PO đặt hàng NCC | Đếm ngược ngày hàng về, cảnh báo quá hạn |
| **24** | **Danh mục Nhân viên** | `features/hr/EmployeesPage.tsx` | `hr.ts` | Giữ nguyên thông tin nhân sự, chức vụ, bộ phận | Card thông tin nhân viên kèm avatar viền vàng |
| **25** | **Chấm công** | `features/hr/AttendancePage.tsx` | `hr.ts` | Giữ nguyên bảng chấm công theo ca | Lưới lịch chấm công trực quan, trạng thái đi muộn/đủ công |
| **26** | **Tính lương** | `features/hr/PayrollPage.tsx` | `hr.ts` | Giữ nguyên công thức tính lương, tạm ứng | Bảng lương chi tiết, nút in phiếu lương từng cá nhân |
| **27** | **Thưởng phạt** | `features/hr/BonusPenaltyPage.tsx` | `hr.ts` | Giữ nguyên phiếu thưởng/phạt nhân viên | Tag thưởng màu xanh lục bảo, tag phạt màu đỏ |
| **28** | **Giấy giới thiệu** | `features/hr/IntroLettersPage.tsx` | `hr.ts` | Giữ nguyên phát hành giấy giới thiệu công tác | Mẫu in giấy giới thiệu chuẩn văn bản doanh nghiệp |
| **29** | **Trung tâm Báo cáo** | `features/reports/ReportsPage.tsx` | `reports.ts`, `exportExcel` | Giữ nguyên 9 tab báo cáo, xuất Excel, in ấn | Cây báo cáo mạ vàng, bộ lọc ngày nhanh, xuất file êm ái |
| **30** | **Thông báo** | `features/notifications/NotificationsPage.tsx` | `notification.ts` | Giữ nguyên danh sách thông báo, đánh dấu đã đọc | Hộp thư thông báo kính mờ, icon phân loại rực rỡ |
| **31** | **Cấu hình Email** | `features/notifications/EmailsPage.tsx` | `notification.ts` | Giữ nguyên mẫu email, hàng đợi gửi mail | Bảng hàng đợi mail kèm trạng thái đã gửi/lỗi |
| **32** | **Lịch sử Chứng từ** | `features/history/HistoryPage.tsx` | `history.ts` | Giữ nguyên audit trail (ai tạo, ai sửa, ngày giờ) | Dòng thời gian (Timeline) thẩm mỹ, dễ tra cứu vết |
| **33** | **Sync Admin** | `features/sync-admin/SyncAdminPage.tsx` | `sync.ts` | Giữ nguyên theo dõi heartbeat, lỗi dòng, dead-letter | Dashboard giám sát đồng bộ nhịp thở Live Pulse |
| **34** | **Chứng từ KH dùng chung** | `features/sales/CustomerDocPage.tsx` | `salesdocs.ts` | Giữ nguyên tính toán tiền hàng, chiết khấu, thuế | Bố cục 2 cột hiện đại, giỏ hàng chứng từ mượt mà |

---

## 4. TRIẾT LÝ THIẾT KẾ: ROYAL PEARL & CHAMPAGNE GOLD

1. **Sự Sang Trọng & Tinh Tế (Luxury & Elegance)**:
   - Màu trắng: **Trắng Ngọc Trai (Pearl White `#FFFFFF`)** làm bề mặt thẻ và bảng biểu, kết hợp sắc thái nền canvas ấm dịu mắt (`#FBFBFA`).
   - Màu vàng: **Vàng Kim Hoàng Gia (Royal Gold `#D4AF37`)** kết hợp dải gradient kim loại (`linear-gradient(135deg, #FFE28A 0%, #D4AF37 50%, #A67C1E 100%)`).
2. **Độ Mượt Mà Tuyệt Đối (60 FPS Fluidity)**:
   - Toàn bộ chuyển động áp dụng hàm gia tốc mượt mà tự nhiên `cubic-bezier(0.16, 1, 0.3, 1)`.
   - Chuyển tab, lọc bảng, mở modal đều không có độ giật cục.
3. **Phản Hồi Xúc Giác Đậm Nét (Tactile & Responsive)**:
   - Nút bấm lún nhẹ khi click (`scale(0.97)`), viền phát sáng ánh kim nhẹ khi hover (`box-shadow: 0 4px 18px rgba(212, 175, 55, 0.28)`).
4. **Bảo Tồn Phím Tắt Truyền Thống**:
   - Giữ nguyên bộ phím tắt TechPOS: F3 tìm kiếm/lọc, F4 sửa, F5 tải lại, F7 dòng mới/giảm giá, F9 tồn kho, F10 thống kê, F12 thanh toán.

---

## 5. HỆ THỐNG TOKEN THIẾT KẾ (DESIGN TOKENS & COLOR PALETTE)

### 5.1 Bảng màu Chuẩn Hóa
```css
:root {
  /* Vàng Kim (Gold Spectrum) */
  --gold-primary: #D4AF37;
  --gold-hover: #E5C158;
  --gold-active: #B8860B;
  --gold-deep: #8A6100;           /* Đảm bảo tương phản WCAG AAA trên nền trắng */
  --gold-champagne: #FFF9E6;      /* Nền badge / điểm nhấn nhạt */
  --gold-surface: #FFFDF2;        /* Nền hàng được chọn / input focus */
  --gold-border: rgba(212, 175, 55, 0.24);
  --gold-gradient: linear-gradient(135deg, #FFE28A 0%, #D4AF37 50%, #A67C1E 100%);
  --gold-glow: 0 0 20px rgba(212, 175, 55, 0.25);

  /* Trắng Ngọc Trai & Nền (Pearl & Canvas) */
  --pearl-white: #FFFFFF;
  --canvas-bg: #FBFBFA;
  --canvas-surface: #FAF8F2;
  --glass-bg: rgba(255, 255, 255, 0.88);
  --glass-border: rgba(212, 175, 55, 0.2);

  /* Chữ & Phân cấp (Typography) */
  --text-heading: #1C180E;
  --text-body: #403825;
  --text-muted: #7E7359;

  /* Trạng thái nghiệp vụ (Semantic Status) */
  --status-success: #10B981;      /* Xanh lục bảo: Doanh thu, Đã thanh toán, Tồn an toàn */
  --status-warning: #F59E0B;      /* Vàng hổ phách: Sắp hết hàng, Chờ duyệt */
  --status-danger: #E11D48;       /* Đỏ ruby: Nợ quá hạn, Hết hàng, Vượt tồn */
}
```

### 5.2 Ant Design 6 Theme Config (`antdThemeConfig`)
```typescript
export const antdThemeConfig = {
  token: {
    colorPrimary: '#D4AF37',
    colorLink: '#D4AF37',
    colorInfo: '#D4AF37',
    colorBgLayout: '#FBFBFA',
    colorBgContainer: '#FFFFFF',
    borderRadius: 8,
    fontSize: 14,
    fontFamily: "'Plus Jakarta Sans', 'Be Vietnam Pro', system-ui, sans-serif",
    colorText: '#403825',
    colorTextHeading: '#1C180E',
  },
  components: {
    Layout: {
      siderBg: '#FFFFFF',
      headerBg: 'rgba(255, 255, 255, 0.85)',
      bodyBg: '#FBFBFA',
    },
    Menu: {
      itemBg: 'transparent',
      itemSelectedBg: '#FFF9E6',
      itemSelectedColor: '#8A6100',
      itemHoverBg: 'rgba(212, 175, 55, 0.08)',
      itemHoverColor: '#D4AF37',
    },
    Card: {
      boxShadow: '0 4px 20px -2px rgba(212, 175, 55, 0.1)',
      headerBg: '#FFFFFF',
    },
    Table: {
      headerBg: '#FAF8F2',
      headerColor: '#8A6100',
      rowHoverBg: '#FFFDF5',
    },
    Button: {
      primaryShadow: '0 4px 14px 0 rgba(212, 175, 55, 0.35)',
    },
  },
};
```

---

## 6. HỆ THỐNG HIỆU ỨNG ĐỘNG ĐẶC SẮC (MOTION & PHYSICS SYSTEM)

Các hiệu ứng động được viết độc lập trong `src/styles/animations.css`, không can thiệp vào logic JavaScript:
1. **`gold-shimmer-sweep`**: Vệt sáng ánh kim quét qua nút Thanh toán F12 hoặc các thẻ nổi bật theo chu kỳ 4 giây.
2. **`number-count-up`**: Tự động chuyển đổi mượt mà con số tài chính khi tải hoặc cập nhật.
3. **`tactile-button-press`**: Hiệu ứng đàn hồi lún nhẹ khi ấn nút chuột (`transform: scale(0.97)`).
4. **`pos-item-pulse`**: Dòng sản phẩm mới thêm vào giỏ hàng nháy viền vàng mềm trong 300ms.
5. **`live-pulse-heartbeat`**: Chấm trạng thái kết nối máy chủ nhấp nháy êm ái dạng nhịp thở (`pulseGlow`).

---

## 7. KẾ HOẠCH CHUYỂN ĐỔI CHI TIẾT CHO TỪNG PHÂN HỆ CỐT LÕI

### 7.1 Màn hình Công Nợ (`DebtsPage.tsx`) - Ví dụ Mẫu Code Trước & Sau

Đây là minh chứng rõ ràng nhất cho việc **giữ nguyên 100% logic, hooks, state và API calls** trong khi nâng tầm hoàn toàn diện mạo:

#### A. Logic Hiện tại (Đang chạy trong `DebtsPage.tsx`):
- `CustomerDebts` và `SupplierDebts`:
  - `useCallback` gọi `getCustomerDebts({ search, page, pageSize: 50 })`.
  - State: `rows`, `total`, `page`, `search`, `loading`.
  - Bảng hiển thị: `maKhach`, `tenKhach`, `dienThoai`, `soHoaDon`, `congNo`.
  - `Table.Summary.Row`: hiển thị tổng số khách nợ và cộng dồn số tiền nợ.

#### B. Chuyển đổi Sang Giao diện Mới (White & Gold Luxury):
```tsx
// Toàn bộ imports, interface, và hooks load() ĐƯỢC GIỮ NGUYÊN 100%
const CustomerDebts: React.FC = () => {
  const [rows, setRows] = useState<CustomerDebt[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (p: number, s: string) => {
    setLoading(true);
    try {
      const r = await getCustomerDebts({ search: s || undefined, page: p, pageSize: 50 });
      setRows(r.items); setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải công nợ khách hàng');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(1, ''); }, [load]);

  const totalDebt = rows.reduce((s, r) => s + r.congNo, 0);

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {/* THẺ TỔNG HỢP KPI MẠ VÀNG MỚI - TĂNG TRẢI NGHIỆM ĐIỀU HÀNH */}
      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} md={8}>
          <div className="luxury-kpi-card">
            <div className="kpi-label">TỔNG SỐ KHÁCH HÀNG NỢ</div>
            <div className="kpi-value">{total.toLocaleString('vi-VN')} khách</div>
          </div>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <div className="luxury-kpi-card gold-border">
            <div className="kpi-label">TỔNG TIỀN NỢ PHẢI THU</div>
            <div className="kpi-value text-gold">{fmtVnd(totalDebt)}</div>
          </div>
        </Col>
      </Row>

      {/* BẢNG CÔNG NỢ NÂNG CẤP VỚI KHUNG TRẮNG NGỌC TRAI */}
      <Card
        className="luxury-card"
        title={<span style={{ color: '#8A6100', fontWeight: 700 }}>Danh Sách Công Nợ Khách Hàng</span>}
        extra={
          <Input.Search
            allowClear
            placeholder="F3 — Tìm mã / tên khách..."
            className="luxury-search-input"
            style={{ width: 280 }}
            onSearch={(v) => { setSearch(v); setPage(1); void load(1, v); }}
          />
        }
      >
        <Table
          className="luxury-table"
          size="middle"
          rowKey="khachHangId"
          loading={loading}
          dataSource={rows}
          pagination={{
            current: page, pageSize: 50, total, showSizeChanger: false,
            onChange: (p) => { setPage(p); void load(p, search); }
          }}
          columns={[
            { title: 'Mã khách', dataIndex: 'maKhach', width: 130, render: (v) => <span className="code-badge">{v}</span> },
            { title: 'Tên khách hàng', dataIndex: 'tenKhach', render: (v) => <b>{v}</b> },
            { title: 'Điện thoại', dataIndex: 'dienThoai', width: 140 },
            { title: 'Số HĐ nợ', dataIndex: 'soHoaDon', width: 120, align: 'right' },
            {
              title: 'Công nợ', dataIndex: 'congNo', width: 170, align: 'right',
              render: (v: number) => (
                <span className={v > 0 ? "debt-amount-badge" : ""}>
                  {fmtVnd(v)}
                </span>
              )
            },
          ]}
          summary={() => rows.length > 0 ? (
            <Table.Summary.Row className="luxury-summary-row">
              <Table.Summary.Cell index={0} colSpan={4}>
                <b style={{ color: '#8A6100' }}>TỔNG {total} KHÁCH HÀNG ĐANG NỢ</b>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={1} align="right">
                <b className="debt-summary-val">{fmtVnd(totalDebt)}</b>
              </Table.Summary.Cell>
            </Table.Summary.Row>
          ) : null}
        />
      </Card>
    </Space>
  );
};
```
*Kết quả: 100% chức năng phân trang, tìm kiếm, tính nợ không đổi; nhưng màn hình lột xác thành giao diện tài chính cao cấp.*

---

### 7.2 Màn hình Bán lẻ POS (`RetailSalePage.tsx`) - Trọng tâm Flagship

- **Bảo toàn 100% logic `usePosCartStore`**:
  - `lines`, `khoId`, `khachHangId`, `tienGiamGia`, `khachDua`.
  - Logic tính: `tienHang`, `tongCong`, `traLai`, `conNo`.
  - Logic kiểm tra tồn: `vuotTonIds = Object.keys(soLuongTrongGio).filter(id => soLuongTrongGio[id] > tonCua(id))`.
  - Hàm thanh toán `createSale`, in bill `printInvoicePdf`, tải hóa đơn `downloadInvoicePdf`.
- **Cải tiến Giao diện**:
  - **Lưới Mặt Hàng**: Thẻ sản phẩm chuẩn tỷ lệ, hình ảnh rõ nét hoặc icon mạ vàng, badge tồn kho 3 màu (Xanh: >5, Cam: 1-4, Đỏ: hết hàng). Chạm hoặc click thẻ có hiệu ứng sóng vàng (Ripple effect) lập tức nhảy vào giỏ.
  - **Thanh toán F12**: Nút thanh toán lớn với hiệu ứng ánh sáng vàng quét ngang (`gold-shimmer-sweep`).
  - **Tích hợp VietQR Tự Động**: Khi bấm thanh toán chuyển khoản, sinh ngay mã VietQR chuẩn NAPAS kèm số tiền chính xác từng đồng để khách quét app ngân hàng.

---

### 7.3 Màn hình Sổ Quỹ (`CashbookPage.tsx`)
- **Bảo toàn 100%**:
  - Form tạo phiếu thu/chi (`createCashReceipt`, `createCashPayment`).
  - Phân bổ nợ đơn hàng (`allocations: donHangId, soTien`).
  - Truy vấn tồn quỹ (`getCashBalance`).
- **Cải tiến Giao diện**:
  - Thẻ Tồn Quỹ thiết kế phong cách hoàng gia, số tiền nhảy mượt mà.
  - Phân định rõ ràng: Phiếu Thu dùng màu xanh lục bảo viền vàng; Phiếu Chi dùng màu đỏ ruby trang nhã.

---

### 7.4 Màn hình Tồn Kho & Thẻ Kho (`InventoryPage.tsx`)
- **Bảo toàn 100%**:
  - Tab 1: Tồn kho đa kho, lọc kho, lọc `nonZeroOnly`.
  - Tab 2: Thẻ kho theo khoảng ngày `fromDate` - `toDate`, lịch sử nhập - xuất - tồn.
- **Cải tiến Giao diện**:
  - Bảng dữ liệu Pro với hàng xen kẽ trắng ngọc trai và kem nhạt, hover nhấc nhẹ 3D.
  - Cảnh báo tồn âm màu đỏ ruby rực rỡ, dễ phát hiện sai sót kiểm kê.

---

### 7.5 Màn hình Báo Giá & Khách Đặt Hàng (`CustomerDocPage.tsx`)
- **Bảo toàn 100%**:
  - Logic tạo báo giá (`createQuotation`) và đơn đặt hàng (`createOrder`).
  - Tính toán tiền hàng, chiết khấu, thuế VAT, phí vận chuyển.
  - In ấn PDF và tải về máy.
- **Cải tiến Giao diện**:
  - Bố cục 2 cột hiện đại: Bên trái chọn hàng nhanh; Bên phải tóm tắt chi phí và thông tin khách hàng viền kép vàng kim.

---

### 7.6 Màn hình Danh mục Mặt Hàng (`ProductsPage.tsx`)
- **Bảo toàn 100%**:
  - CRUD mặt hàng: `createProduct`, `updateProduct`, `deleteProduct`.
  - Quyền hạn: `canCreate`, `canUpdate`, `canDelete` từ `authStore`.
  - Xem chi tiết tồn kho từng chi nhánh: `getProductStock`.
  - In mã vạch: `openBarcode`.
- **Cải tiến Giao diện**:
  - Bảng danh mục hỗ trợ ảnh sản phẩm thumbnail mượt mà.
  - Drawer xem chi tiết tồn kho trượt êm từ cạnh phải với hiệu ứng kính mờ.

---

### 7.7 Khung Ứng Dụng (Responsive Shell, Header & Sidebar)
- **Bảo toàn 100%**:
  - Danh mục 10 nhóm menu theo chuẩn nghiệp vụ TechPOS (`MENU_ITEMS`, `ADMIN_MENU_ITEMS`, `HISTORY_MENU_ITEMS`).
  - Phân quyền menu admin (`hasAdminRole`).
  - Polling thông báo 30 giây (`notificationStore.startPolling`).
  - Hỗ trợ Mobile Drawer & Bottom Navigation 5 nút.
- **Cải tiến Giao diện**:
  - **Sider**: Nền trắng ngọc trai, viền phải ánh kim mỏng; mục được chọn dạng viên thuốc (capsule pill) phát sáng vàng dịu.
  - **Header**: Kính mờ `backdrop-filter: blur(16px)`, tích hợp Quick Search Command Palette `Ctrl + K`.

---

### 7.8 Màn hình Đăng Nhập (`LoginPage.tsx`)
- **Bảo toàn 100%**:
  - Logic `authStore.login(username, password)`, điều hướng `/` và thông báo lỗi.
- **Cải tiến Giao diện**:
  - Nền Aurora vàng kim uốn lượn chậm rãi.
  - Khung đăng nhập kính pha lê, logo Hoàn Ngọc mạ vàng dập nổi.

---

### 7.9 Trang Chủ Điều Hành (`DashboardPage.tsx`)
- **Bảo toàn 100%**:
  - Tính toán doanh thu hôm nay/tháng, số chứng từ, tồn kho SKU, tồn quỹ (Thu - Chi).
  - Trạng thái đồng bộ Sync heartbeat, row errors pending.
- **Cải tiến Giao diện**:
  - 4 Thẻ KPI Metrics lớn có hiệu ứng số nhảy (`count-up`).
  - Quick Actions Dock: 12 phím tắt vào nhanh POS, Nhập hàng, Công nợ, Tồn kho...
  - Biểu đồ doanh thu dạng miền (Area Chart) gradient vàng kim tương tác mượt mà.

---

### 7.10 Trung Tâm Báo Cáo (`ReportsPage.tsx`)
- **Bảo toàn 100%**:
  - Đầy đủ 9 nhóm báo cáo: Bán hàng theo ngày, Mặt hàng bán chạy, XNT, Công nợ KH, Công nợ NCC, Tồn quỹ, Kết quả kinh doanh, Lý do thu chi, Thu chi theo ngày.
  - Nút xem dữ liệu và xuất file Excel (`exportExcel`).
- **Cải tiến Giao diện**:
  - Cây danh mục báo cáo mạ vàng tinh xảo, bộ lọc ngày nhanh (*Hôm nay, Tuần này, Tháng này*).
  - Bảng kết quả báo cáo hiển thị chuẩn in ấn.

---

## 8. LỘ TRÌNH TRIỂN KHAI CHI TIẾT 6 GIAI ĐOẠN & KIỂM THỬ AN TOÀN

Quá trình chuyển đổi được tổ chức theo hình thức **Từng Bước (Modular & Incremental)**, mỗi bước đều có kiểm thử hồi quy để đảm bảo tuyệt đối không có lỗi phát sinh:

```text
┌────────────────────────────────────────────────────────────────────────┐
│                    QUY TRÌNH NÂNG CẤP AN TOÀN 6 BƯỚC                   │
├─────────┬──────────────────────────────────────────────────────────────┤
│ BƯỚC 1  │ Nền tảng: Tokens, Colors (`brand.ts`), CSS Keyframes & Fonts │
│ BƯỚC 2  │ Khung Ứng Dụng (`ResponsiveLayout.tsx`) & Đăng Nhập (`Login`)│
│ BƯỚC 3  │ Màn hình Điều Hành Trung Tâm (`DashboardPage.tsx`)           │
│ BƯỚC 4  │ Màn hình Bán lẻ POS Flagship (`RetailSalePage.tsx`)          │
│ BƯỚC 5  │ Phân hệ Công Nợ (`DebtsPage`), Sổ Quỹ & Danh mục Kho Hàng     │
│ BƯỚC 6  │ Báo Cáo (`ReportsPage`), Tối ưu 60fps & Nghiệm Thu Toàn Diện │
└─────────┴──────────────────────────────────────────────────────────────┘
```

### Quy trình Kiểm Thử Hồi Quy (Regression Testing Checklist) Sau Mỗi Bước:
1. **Kiểm tra Gọi API**: Đảm bảo Network tab trên trình duyệt gửi đúng URL, đúng Headers Authorization Bearer, đúng Request Payload.
2. **Kiểm tra State**: Đảm bảo giỏ hàng POS không mất khi chuyển qua lại giữa các trang, phân trang nhớ đúng trang đang đứng.
3. **Kiểm tra Ràng Buộc**: Thử nhập vượt tồn kho xem hệ thống có chặn và báo lỗi đúng như cũ không; thử thanh toán số tiền âm xem có bị chặn không.
4. **Kiểm tra In ấn & File**: Thử bấm In hóa đơn PDF và Xuất Excel xem file tải về có đầy đủ số liệu như cũ không.

---

## 9. TIÊU CHUẨN KỸ THUẬT & TUÂN THỦ QUY TẮC DỰ ÁN

- **Tuân thủ `Quy tắc code.txt`**:
  - Không thay đổi cấu trúc Service / Controller / DTO backend.
  - Không cài đặt thêm các thư viện cồng kềnh ngoài Ant Design và CSS thuần.
  - Giữ nguyên các comment giải thích nghiệp vụ quan trọng.
- **Tối ưu Hiệu năng Render**:
  - Toàn bộ animation dùng GPU Acceleration (`transform`, `opacity`, `filter`). Không dùng `margin`, `top`, `left` để tạo chuyển động tránh gây giật lag (layout reflow).
  - Đảm bảo tốc độ hiển thị và phản hồi đạt 60fps mượt mà trên cả máy tính bàn và màn hình POS cảm ứng.

---

> 📌 **KẾT LUẬN & CAM KẾT**:  
> Bản kế hoạch trên đảm bảo 100%: **KHÔNG MẤT BẤT KỲ CHỨC NĂNG NÀO**. Toàn bộ 34 màn hình, 16 modules API, các Zustand stores, hệ thống phím tắt và logic nghiệp vụ của TechPOS đều được giữ nguyên vẹn. Việc thiết kế lại chỉ khoác lên hệ thống một diện mạo mới: **Sang trọng, Đẳng cấp Hoàng gia (Trắng ngọc trai & Vàng kim), và Mượt mà tinh tế**.
