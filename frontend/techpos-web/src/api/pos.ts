import { http, ApiEnvelope } from './http';

export interface PosCartItem {
  matHangId: string;
  tenMatHang: string;
  donViTinh: string | null;
  donGia: number;
  soLuong: number;
  tileGiamGia: number;
  tienGiamGia: number;
}

export interface PosSaleLineInput {
  matHangId: string;
  khoHangId: string | null;
  soLuong: number;
  donGia: number;
  tileGiamGia: number;
  giamTheoTien: number;
}

export interface SaleTotals {
  tienHang: number;
  tienGiamGia: number;
  tongCong: number;
  khachDua: number;
  traLai: number;
  conNo: number;
}

export interface PosSaleResult {
  donHangId: string;
  soPhieu: string;
  tienHang: number;
  tienGiamGia: number;
  tongCong: number;
  khachDua: number;
  traLai: number;
  conNo: number;
}

export interface PosCreatePayload {
  khoId: string;
  khachHangId: string | null;
  nhanVienId: string | null;
  lines: PosSaleLineInput[];
  tienGiamGia: number;
  tileGiamGia: number;
  ghiChu: string | null;
  khachDua: number;
}

export async function quoteSale(
  lines: PosSaleLineInput[], tileGiamGia: number, tienGiamGia: number, khachDua: number
): Promise<SaleTotals> {
  const res = await http.post<ApiEnvelope<SaleTotals>>('/pos/quote', {
    lines: lines.map((l) => ({
      matHangId: l.matHangId, tenMatHang: '', donViTinh: null,
      donGia: l.donGia, soLuong: l.soLuong, tileGiamGia: l.tileGiamGia, tienGiamGia: l.giamTheoTien
    })),
    tileGiamGia, tienGiamGia, khachDua
  });
  return res.data.data;
}

export async function createSale(req: PosCreateRequest): Promise<PosSaleResult> {
  const res = await http.post<ApiEnvelope<PosSaleResult>>('/pos/sales', req);
  return res.data.data;
}

export interface PosSaleLineInput {
  matHangId: string;
  khoHangId: string | null;
  soLuong: number;
  donGia: number;
  tileGiamGia: number;
  giamTheoTien: number;
}

export interface PosCreateRequest {
  khoId: string;
  khachHangId: string | null;
  nhanVienId: string | null;
  lines: PosSaleLineInput[];
  tienGiamGia: number;
  tileGiamGia: number;
  ghiChu: string | null;
  khachDua: number;
}

// ===================== Phase 4.2 — Bán sỉ =====================

/** Tạo hóa đơn bán sỉ (BS26/00001) — dùng chung payload với bán lẻ. */
export async function createWholesale(req: PosCreateRequest): Promise<PosSaleResult> {
  const res = await http.post<ApiEnvelope<PosSaleResult>>('/pos/wholesale', req);
  return res.data.data;
}

// ===================== Phase 4.3 — Trả lại KH =====================

export interface CustomerReturnLineInput {
  matHangId: string;
  soLuong: number;
  donGia: number;
  note?: string | null;
}

export interface CustomerReturnPayload {
  khoNhanId: string;
  khachHangId: string | null;
  nhanVienId: string | null;
  lines: CustomerReturnLineInput[];
  tienGiamGia: number;
  tienTraLai: number;
  ghiChu: string | null;
}

export interface CustomerReturnResult {
  donHangId: string;
  soPhieu: string;
  tienHang: number;
  tienGiamGia: number;
  tongCong: number;
  tienTraLai: number;
}

export async function createCustomerReturn(req: CustomerReturnPayload): Promise<CustomerReturnResult> {
  const res = await http.post<ApiEnvelope<CustomerReturnResult>>('/pos/returns', req);
  return res.data.data;
}
