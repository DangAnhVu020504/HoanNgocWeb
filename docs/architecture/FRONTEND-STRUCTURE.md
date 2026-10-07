# FRONTEND STRUCTURE — React routing, responsive, UX desktop

## 1. Tech stack

Vite + React 18 + TypeScript · Ant Design 5 · AG Grid Community · TanStack Query v5 · Zustand ·
React Router v6 · dayjs (locale `vi`) · axios · @microsoft/signalr · zxing (barcode scan).

## 2. Routing structure

```text
/login                          Đăng nhập
/forgot-password
/                               Dashboard (Trang chủ)
/sales
  /sales/retail                 Bán lẻ (POS)  ★ màn hình quan trọng nhất
  /sales/wholesale              Bán sỉ
  /sales/returns                Khách trả lại
  /sales/quotation              Báo giá
  /sales/order                  Đặt hàng
  /sales/contract               Hợp đồng
  /sales/delivery               Giao hàng
  /sales/cod                    Giao COD
  /sales/activity               Lưu vết hoạt động
  /sales/stats/items            Thống kê mặt hàng bán
  /sales/stats/revenue          Thống kê doanh thu
  /sales/stats/activity-day     Chi tiết hoạt động ngày
  /sales/stats/kqkd             Tổng hợp KQKD
/catalog                        Danh mục: products, customers, suppliers, stores,
                                promotions, warehouses, banks, staff…
/inventory                      Nhập kho / Xuất kho / Chuyển kho / Kiểm kê /
                                Xuất trả NCC / Sản xuất / Tính lại giá vốn /
                                Tồn kho / Tồn tất cả kho / Thẻ kho
/cashbook                       Lý do thu chi, tạo phiếu thu/chi, danh mục
                                phiếu thu/chi, thu/chi nhiều đối tượng, tồn quỹ
/debt                           Công nợ KH / Công nợ NCC / Theo dõi nợ
/hr                             Nhân viên, giấy giới thiệu, tạm ứng,
                                thưởng phạt, chấm công, tính lương
/crm                            Chăm sóc KH, gửi tin nhắn, KH thân thiết, phản hồi
/reports                        Report Center (9 nhóm, tham số, xuất PDF/Excel/CSV/in)
/admin                          Người dùng & phân quyền, audit, cài đặt hệ thống,
                                feature flags, thư viện ảnh, dữ liệu ban đầu,
                                khóa sổ, lịch sử tác hệ thống
/sync-admin                     Quản lý đồng bộ (ADMIN+)
/notifications                  Notification Center
/emails                         Email Templates / Queue / History / Settings (ADMIN+)
/help                           TRỢ GIÚP (hướng dẫn, about, license)
/profile                        Tài khoản cá nhân
```

Menu sidebar = đúng 10 nhóm desktop: HỆ THỐNG, BÁN HÀNG, CHĂM SÓC KH, KHO HÀNG, QUỸ,
CÔNG NỢ, NHÂN SỰ, QUẢN TRỊ, BÁO CÁO, TRỢ GIÚP (76 mục theo `DANH-MUC-CHUC-NANG.md`).

## 3. Data layer

- `api/http.ts`: axios + interceptor (JWT, refresh token tự động, envelope unwrap, toast lỗi).
- `features/<module>/hooks/*.ts`: TanStack Query — `useXxxList` (server pagination),
  `useXxxDetail`, mutations + invalidation đúng key.
- `stores/`: `authStore` (token, user, permissions), `uiStore` (breakpoint, sidebar), `posCartStore` (giỏ bán lẻ).
- Lọc/pagination giữ state trên URL query string (shareable, refresh giữ nguyên).

## 4. POS (Bán lẻ) responsive spec

Desktop:
```text
┌ Search barcode/hàng · KH · NV ─────────────────────────────┐
│ AG Grid: Mã│Tên│ĐVT│SL│Đơn giá│Giảm│Thành tiền          │
│ Panel phải: Tiền hàng/Giảm giá/Thuế/Vận chuyển/Tổng cộng/  │
│ Khách đưa/Tiền thừa + [Lưu][Thanh toán F12][In][Lưu tạm]   │
└────────────────────────────────────────────────────────────┘
```
Mobile (một tay): `Search → Product cards (Tên/Mã/Tồn/Giá/ĐVT [+]) → Cart bottom drawer →
Summary → [Thanh toán] sticky`. Barcode = camera (zxing) hoặc scanner HID.

## 5. Keyboard (mục 28)

Toàn app: Tab order chuẩn, Enter xác nhận, Esc đóng dialog, Arrow keys trong grid.
Bản đồ phím desktop cũ: F3 lọc, F4 sửa, F5 tải lại, F7 dòng mới, F8 xóa dòng, F9 tồn kho,
F10 thống kê, F11 lịch sử mua, F12 thanh toán, Insert thêm, Del xóa, Ctrl+M mặt hàng,
Ctrl+K khách, Ctrl+T tạo phiếu thu, Ctrl+Shift+C phiếu chi, Ctrl+P in, Ctrl+Q thoát(→đăng xuất menu),
Ctrl+Shift+L đăng xuất. Implement qua `useHotkeys` map, disabled trên mobile.