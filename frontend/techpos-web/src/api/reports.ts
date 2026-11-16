import { http } from './http';

// ===================== Phase 6 — Report Center =====================
// Data endpoints trả JSON; export trả file (xlsx); in trả PDF (blob).

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

// ---------- Data (JSON) ----------

export interface SalesByDayRowDto {
  ngay: string;
  soHoaDon: number;
  tienHang: number;
  tienGiamGia: number;
  tongCong: number;
  conNo: number;
}

export interface SalesByItemRowDto {
  matHangId: string;
  code: string | null;
  name: string;
  donViTinh: string | null;
  soLuong: number;
  thanhTien: number;
}

export interface CashByDayRowDto { ngay: string; thu: number; chi: number }
export interface CashByReasonRowDto { lyDo: string; thu: number; chi: number }

export interface RevenueDetailRowDto {
  donHangId: string;
  soPhieu: string;
  ngay: string | null;
  khachHangId: string | null;
  khachHang: string | null;
  dienThoai: string | null;
  nhanVien: string | null;
  kho: string | null;
  tienHang: number;
  tienGiamGia: number;
  tienThue: number;
  phiVanChuyen: number;
  tongCong: number;
  thanhToan: number;
  conNo: number;
  loai: number | null;
}

export interface XntRowDto {
  matHangId: string;
  code: string | null;
  name: string;
  donViTinh: string | null;
  tonDauKy: number;
  slNhap: number;
  slXuat: number;
  tonCuoi: number;
  giaVon: number;
  giaTriTon: number;
}

async function getData<T>(url: string, params: Record<string, string | number | undefined> = {}): Promise<T[]> {
  const q = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) if (v !== undefined && v !== '') q.set(k, String(v));
  const res = await http.get<{ data: T[] }>(`/reports/${url}${q.toString() ? `?${q.toString()}` : ''}`);
  return res.data.data;
}

export const getSalesByDay = (p: { from?: string; to?: string } = {}) => getData<SalesByDayRowDto>('sales-by-day', p);
export const getSalesByItem = (p: { from?: string; to?: string; top?: number } = {}) => getData<SalesByItemRowDto>('sales-by-item', p);
export const getRevenueDetail = (p: { from?: string; to?: string } = {}) => getData<RevenueDetailRowDto>('revenue-detail', p);

export async function getCashByDay(p: { from?: string; to?: string } = {}): Promise<CashByDayRowDto[]> {
  const res = await http.get<{ data: CashByDayRowDto[] }>('/reports/cash-by-day', { params: p });
  return res.data.data;
}

export async function getCashByReason(p: { from?: string; to?: string } = {}): Promise<CashByReasonRowDto[]> {
  const res = await http.get<{ data: CashByReasonRowDto[] }>('/reports/cash-by-reason', { params: p });
  return res.data.data;
}

export async function getXnt(p: { from?: string; to?: string; khoId?: string } = {}): Promise<XntRowDto[]> {
  const res = await http.get<{ data: XntRowDto[] }>('/reports/xnt', { params: p });
  return res.data.data;
}

export interface CustomerDebtRowDto {
  khachHangId: string;
  maKhach: string | null;
  tenKhach: string;
  dienThoai: string | null;
  soHoaDon: number;
  congNo: number;
  songayChoNo: number;
}

export interface SupplierDebtRowDto {
  nhaCungCapId: string;
  maNhaCungCap: string | null;
  tenNhaCungCap: string;
  dienThoai: string | null;
  soPhieu: number;
  congNo: number;
}

export interface CashBalanceRowDto { ngay: string; thu: number; chi: number; tonCuoiNgay: number }

export interface KqkdDto {
  doanhThu: number;
  giamGia: number;
  giaVon: number;
  chiPhi: number;
  loiNhuan: number;
}

export async function getCustomerDebts(p: { search?: string } = {}): Promise<CustomerDebtRowDto[]> {
  const res = await http.get<{ data: CustomerDebtRowDto[] }>('/reports/customer-debts', { params: p });
  return res.data.data;
}

export async function getSupplierDebts(p: { search?: string } = {}): Promise<SupplierDebtRowDto[]> {
  const res = await http.get<{ data: SupplierDebtRowDto[] }>('/reports/supplier-debts', { params: p });
  return res.data.data;
}

export async function getCashBalance(p: { from?: string; to?: string } = {}): Promise<CashBalanceRowDto[]> {
  const res = await http.get<{ data: CashBalanceRowDto[] }>('/reports/cash-balance', { params: p });
  return res.data.data;
}

export async function getKqkd(p: { from?: string; to?: string } = {}): Promise<KqkdDto> {
  const res = await http.get<{ data: KqkdDto }>('/reports/kqkd', { params: p });
  return res.data.data;
}

// ---------- Export Excel (blob download) ----------

async function downloadFile(url: string, fallbackName: string): Promise<void> {
  const res = await http.get(url, { responseType: 'blob' });
  const dispo: string = typeof res.headers['content-disposition'] === 'string' ? res.headers['content-disposition'] : '';
  const m = /filename\*=UTF-8''([^;]+)/i.exec(dispo) ?? /filename="?([^";]+)"?/i.exec(dispo);
  const name = m ? decodeURIComponent(m[1]) : fallbackName;
  const blobUrl = URL.createObjectURL(res.data as Blob);
  const a = document.createElement('a');
  a.href = blobUrl;
  a.download = name;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(blobUrl);
}

export async function exportExcel(
  path: string, params: Record<string, string | undefined> = {}, fileName = 'bao-cao.xlsx'
): Promise<void> {
  const q = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) if (v) q.set(k, v);
  await downloadFile(`/reports/${path}${q.toString() ? `?${q.toString()}` : ''}`, fileName);
}

// ---------- Print (PDF): Xem (mở tab mới) + Tải về máy ----------

async function openRemotePdf(url: string): Promise<void> {
  const res = await http.get(url, { responseType: 'blob' });
  const blobUrl = URL.createObjectURL(new Blob([res.data as Blob], { type: 'application/pdf' }));
  window.open(blobUrl, '_blank');
  setTimeout(() => URL.revokeObjectURL(blobUrl), 60_000);
}

/** Xem PDF trong tab mới. url theo baseURL của http (/reports/...). */
export function viewPdf(url: string): void {
  void openRemotePdf(url).catch(() => undefined);
}

/** Tải PDF về máy — tên file lấy từ Content-Disposition, thiếu thì dùng fallbackName. */
export function downloadPdfFile(url: string, fallbackName: string): void {
  void downloadFile(url, fallbackName).catch(() => undefined);
}

export function printInvoicePdf(donHangId: string): void {
  viewPdf(`/reports/invoices/${donHangId}/pdf`);
}

export function downloadInvoicePdf(donHangId: string): void {
  downloadPdfFile(`/reports/invoices/${donHangId}/pdf`, 'hoa-don.pdf');
}

export function printCashVoucherPdf(id: string): void {
  viewPdf(`/reports/cash-vouchers/${id}/pdf`);
}

export function downloadCashVoucherPdf(id: string): void {
  downloadPdfFile(`/reports/cash-vouchers/${id}/pdf`, 'phieu-thu-chi.pdf');
}

/** In phiếu kho (nhập/xuất/chuyển/kiểm kê/trả NCC) A5 — mở tab mới. */
export function printStockVoucherPdf(donHangId: string): void {
  viewPdf(`/reports/stock-vouchers/${donHangId}/pdf`);
}

export function downloadStockVoucherPdf(donHangId: string): void {
  downloadPdfFile(`/reports/stock-vouchers/${donHangId}/pdf`, 'phieu-kho.pdf');
}

/** In bảng lương tháng A4 — mở tab mới. */
export function printPayrollPdf(id: string): void {
  viewPdf(`/payroll/${id}/pdf`);
}

export function downloadPayrollPdf(id: string): void {
  downloadPdfFile(`/payroll/${id}/pdf`, 'bang-luong.pdf');
}

// ---------- Chứng từ bán hàng (báo giá / đặt hàng / đặt mua NCC / hợp đồng) ----------

export function printQuotationPdf(id: string): void {
  viewPdf(`/reports/quotations/${id}/pdf`);
}

export function downloadQuotationPdf(id: string): void {
  downloadPdfFile(`/reports/quotations/${id}/pdf`, 'bao-gia.pdf');
}

export function printOrderPdf(id: string): void {
  viewPdf(`/reports/orders/${id}/pdf`);
}

export function downloadOrderPdf(id: string): void {
  downloadPdfFile(`/reports/orders/${id}/pdf`, 'dat-hang.pdf');
}

export function printPurchaseOrderPdf(id: string): void {
  viewPdf(`/reports/purchase-orders/${id}/pdf`);
}

export function downloadPurchaseOrderPdf(id: string): void {
  downloadPdfFile(`/reports/purchase-orders/${id}/pdf`, 'dat-mua-ncc.pdf');
}

export function printContractPdf(id: string): void {
  viewPdf(`/reports/contracts/${id}/pdf`);
}

export function downloadContractPdf(id: string): void {
  downloadPdfFile(`/reports/contracts/${id}/pdf`, 'hop-dong.pdf');
}

// ---------- Phiếu gửi hàng (gửi COD / kết quả COD / giao xe) ----------

export function printCodShipmentPdf(id: string): void {
  viewPdf(`/reports/cod-shipments/${id}/pdf`);
}

export function downloadCodShipmentPdf(id: string): void {
  downloadPdfFile(`/reports/cod-shipments/${id}/pdf`, 'gui-cod.pdf');
}

export function printCodResultPdf(id: string): void {
  viewPdf(`/reports/cod-results/${id}/pdf`);
}

export function downloadCodResultPdf(id: string): void {
  downloadPdfFile(`/reports/cod-results/${id}/pdf`, 'ket-qua-cod.pdf');
}

export function printVehicleDeliveryPdf(id: string): void {
  viewPdf(`/reports/vehicle-deliveries/${id}/pdf`);
}

export function downloadVehicleDeliveryPdf(id: string): void {
  downloadPdfFile(`/reports/vehicle-deliveries/${id}/pdf`, 'giao-xe.pdf');
}

/** Xem / tải PDF chứng từ trong Lịch sử — pdfPath do backend tính (đã có prefix /api). */
export function viewHistoryPdf(pdfPath: string): void {
  viewPdf(pdfPath.replace(/^\/api/, ''));
}

export function downloadHistoryPdf(pdfPath: string, soPhieu: string): void {
  downloadPdfFile(pdfPath.replace(/^\/api/, ''), `${soPhieu}.pdf`);
}

/** Mở PNG tem mã vạch CODE_128 của mặt hàng (tab mới). */
export function openBarcode(productId: string): void {
  void (async () => {
    const res = await http.get(`/reports/barcode/${productId}`, { responseType: 'blob' });
    const url = URL.createObjectURL(res.data as Blob);
    window.open(url, '_blank');
    setTimeout(() => URL.revokeObjectURL(url), 60_000);
  })();
}

export { fmtVnd };
