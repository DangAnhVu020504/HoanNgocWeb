import { http, ApiEnvelope } from './http';

// ===================== Phase 4.9 — Báo giá / Đặt hàng / Đặt mua NCC / Hợp đồng =====================

export interface QuoteLineInput {
  matHangId: string;
  soLuong: number;
  donGia: number;
  tileGiamGia: number;
  note?: string | null;
}

export interface CustomerDocPayload {
  khachHangId: string | null;
  tenKhach: string | null;
  diaChi: string | null;
  dienThoai: string | null;
  email: string | null;
  lines: QuoteLineInput[];
  tienThue: number;
  tienGiamGia: number;
  phiVanChuyen: number;
  ghiChu: string | null;
}

export interface QuoteResult {
  id: string;
  soPhieu: string;
  tienHang: number;
  tongCong: number;
  soDong: number;
}

export interface OrderResult extends QuoteResult { }

export async function createQuotation(req: CustomerDocPayload): Promise<QuoteResult> {
  const res = await http.post<ApiEnvelope<QuoteResult>>('/quotations', req);
  return res.data.data;
}

export async function createOrder(req: CustomerDocPayload): Promise<OrderResult> {
  const res = await http.post<ApiEnvelope<OrderResult>>('/orders', req);
  return res.data.data;
}

export interface PurchaseOrderPayload {
  nhaCungCapId: string;
  lines: QuoteLineInput[];
  tienThue: number;
  tienGiamGia: number;
  phiVanChuyen: number;
  ghiChu: string | null;
}

export interface PurchaseOrderResult extends QuoteResult { }

export async function createPurchaseOrder(req: PurchaseOrderPayload): Promise<PurchaseOrderResult> {
  const res = await http.post<ApiEnvelope<PurchaseOrderResult>>('/purchase-orders', req);
  return res.data.data;
}

export interface ContractPayload {
  khachHangId: string | null;
  tenKhach: string | null;
  nhanVienId: string | null;
  loaiHopDongId: string | null;
  trangThaiHopDongId: string | null;
  ngayKy: string | null;
  tuNgay: string | null;
  denNgay: string | null;
  lines: QuoteLineInput[];
  datCoc: number;
  tienGiamGia: number;
  ghiChu: string | null;
}

export interface ContractResult extends QuoteResult { }

export async function createContract(req: ContractPayload): Promise<ContractResult> {
  const res = await http.post<ApiEnvelope<ContractResult>>('/contracts', req);
  return res.data.data;
}

// ===================== Phase 4.10 — Giao COD / Giao hàng theo xe =====================

export interface LookupItem {
  id: string;
  name: string;
}

export interface WebOrderItem {
  id: string;
  soPhieu: string;
  ngay: string | null;
  tongCong: number;
  khachHang: string | null;
}

export async function getDeliveryPartners(): Promise<LookupItem[]> {
  const res = await http.get<ApiEnvelope<LookupItem[]>>('/cod/delivery-partners');
  return res.data.data;
}

export async function getVehicles(): Promise<LookupItem[]> {
  const res = await http.get<ApiEnvelope<LookupItem[]>>('/cod/vehicles');
  return res.data.data;
}

export async function getWebOrders(): Promise<WebOrderItem[]> {
  const res = await http.get<ApiEnvelope<WebOrderItem[]>>('/cod/orders');
  return res.data.data;
}

export interface CodShipmentPayload {
  donViChuyenPhatId: string;
  nhanVienId: string | null;
  donHangIds: string[];
  ghiChu: string | null;
}

export interface CodShipmentResult {
  id: string;
  soPhieu: string;
  soDonHang: number;
}

export async function createCodShipment(req: CodShipmentPayload): Promise<CodShipmentResult> {
  const res = await http.post<ApiEnvelope<CodShipmentResult>>('/cod/shipments', req);
  return res.data.data;
}

export interface CodResultPayload {
  donViChuyenPhatId: string | null;
  nhanVienId: string | null;
  donHangIds: string[];
  ghiChu: string | null;
}

export async function createCodResult(req: CodResultPayload): Promise<CodShipmentResult> {
  const res = await http.post<ApiEnvelope<CodShipmentResult>>('/cod/results', req);
  return res.data.data;
}

export interface VehicleDeliveryPayload {
  xeGiaoHangId: string;
  nhanVienId: string | null;
  donHangIds: string[];
  ghiChu: string | null;
}

export interface VehicleDeliveryResult {
  id: string;
  soPhieu: string;
  soDonHang: number;
}

export async function createVehicleDelivery(req: VehicleDeliveryPayload): Promise<VehicleDeliveryResult> {
  const res = await http.post<ApiEnvelope<VehicleDeliveryResult>>('/vehicle-deliveries', req);
  return res.data.data;
}