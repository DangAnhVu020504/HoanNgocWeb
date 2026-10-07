import { http, ApiEnvelope } from './http';
import type { PageResult } from './customers';

export interface InventoryItem {
  matHangId: string;
  code: string | null;
  name: string;
  donViTinh: string | null;
  khoId: string | null;
  kho: string | null;
  ton: number;
  giaVon: number;
  giaBan: number;
  giaTriTon: number;
  status: number;
}

export interface InventoryListParams {
  search?: string;
  khoId?: string;
  nonZeroOnly?: boolean;
  page?: number;
  pageSize?: number;
  sort?: string;
}

export async function getInventory(params: InventoryListParams = {}): Promise<PageResult<InventoryItem>> {
  const q = new URLSearchParams();
  if (params.search) q.set('search', params.search);
  if (params.khoId) q.set('khoId', params.khoId);
  if (params.nonZeroOnly !== undefined) q.set('nonZeroOnly', String(params.nonZeroOnly));
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 50));
  if (params.sort) q.set('sort', params.sort);
  const res = await http.get<ApiEnvelope<PageResult<InventoryItem>>>(`/inventory?${q.toString()}`);
  return res.data.data;
}

export interface LedgerEntry {
  entryDate: string;
  soPhieu: string | null;
  docType: string | null;
  dienGiai: string | null;
  slNhap: number;
  slXuat: number;
  giaVon: number | null;
  tonLuyKe: number;
}

export async function getLedger(
  matHangId: string, params: { khoId?: string; from?: string; to?: string } = {}
): Promise<LedgerEntry[]> {
  const q = new URLSearchParams();
  if (params.khoId) q.set('khoId', params.khoId);
  if (params.from) q.set('from', params.from);
  if (params.to) q.set('to', params.to);
  const res = await http.get<ApiEnvelope<LedgerEntry[]>>(`/inventory/ledger/${matHangId}?${q.toString()}`);
  return res.data.data;
}

export interface ReceiptLineInput {
  matHangId: string;
  slNhap: number;
  donGia: number;
  note?: string | null;
}

export interface ReceiptPayload {
  khoNhapId: string;
  nhaCungCapId: string | null;
  nhanVienId: string | null;
  lines: ReceiptLineInput[];
  tienGiamGia: number;
  tienThue: number;
  phiVanChuyen: number;
  daThanhToan: number;
  ghiChu: string | null;
}

export interface ReceiptResult {
  donHangId: string;
  soPhieu: string;
  tienHang: number;
  tienGiamGia: number;
  tongCong: number;
  daThanhToan: number;
  conLai: number;
}

export async function createReceipt(req: ReceiptPayload): Promise<ReceiptResult> {
  const res = await http.post<ApiEnvelope<ReceiptResult>>('/receipts', req);
  return res.data.data;
}

export interface StocktakeLineInput {
  matHangId: string;
  slThucTe: number;
}

export interface StocktakePayload {
  khoId: string;
  lines: StocktakeLineInput[];
  ghiChu: string | null;
}

export interface StocktakeResult {
  donHangId: string;
  soPhieu: string;
  tienTang: number;
  tienGiam: number;
  linesAdjusted: number;
}

export async function createStocktake(req: StocktakePayload): Promise<StocktakeResult> {
  const res = await http.post<ApiEnvelope<StocktakeResult>>('/stocktakes', req);
  return res.data.data;
}

// ===================== Phase 4.5 — Xuất kho =====================

export interface StockIssueLineInput {
  matHangId: string;
  soLuong: number;
  donGia: number;
  note?: string | null;
}

export interface StockIssuePayload {
  khoXuatId: string;
  nhanVienId: string | null;
  dienGiai: string | null;
  lines: StockIssueLineInput[];
  ghiChu: string | null;
}

export interface StockIssueResult {
  donHangId: string;
  soPhieu: string;
  tienHang: number;
  soDong: number;
}

export async function createStockIssue(req: StockIssuePayload): Promise<StockIssueResult> {
  const res = await http.post<ApiEnvelope<StockIssueResult>>('/inventory/issues', req);
  return res.data.data;
}

// ===================== Phase 4.6 — Chuyển kho =====================

export interface StockTransferLineInput {
  matHangId: string;
  soLuong: number;
  note?: string | null;
}

export interface StockTransferPayload {
  khoXuatId: string;
  khoNhapId: string;
  nhanVienXuatId: string | null;
  nhanVienNhapId: string | null;
  lines: StockTransferLineInput[];
  ghiChu: string | null;
}

export interface StockTransferResult {
  donHangId: string;
  soPhieu: string;
  soDong: number;
  tongSoLuong: number;
}

export async function createStockTransfer(req: StockTransferPayload): Promise<StockTransferResult> {
  const res = await http.post<ApiEnvelope<StockTransferResult>>('/inventory/transfers', req);
  return res.data.data;
}

// ===================== Phase 4.5 — Xuất trả NCC =====================

export interface SupplierReturnLineInput {
  matHangId: string;
  soLuong: number;
  donGia: number;
  note?: string | null;
}

export interface SupplierReturnPayload {
  khoXuatId: string;
  nhaCungCapId: string;
  nhanVienId: string | null;
  lines: SupplierReturnLineInput[];
  tienGiamGia: number;
  daThanhToan: number;
  ghiChu: string | null;
}

export interface SupplierReturnResult {
  donHangId: string;
  soPhieu: string;
  tienHang: number;
  tienGiamGia: number;
  tongCong: number;
  daThanhToan: number;
  conLai: number;
}

export async function createSupplierReturn(req: SupplierReturnPayload): Promise<SupplierReturnResult> {
  const res = await http.post<ApiEnvelope<SupplierReturnResult>>('/inventory/supplier-returns', req);
  return res.data.data;
}

// ===================== Phase 4.8 — Tồn tất cả các kho =====================

export interface WarehouseStockCell {
  khoId: string;
  kho: string | null;
  ton: number;
}

export interface AllWarehouseStock {
  matHangId: string;
  code: string | null;
  name: string;
  donViTinh: string | null;
  tongTon: number;
  giaVon: number;
  giaTriTon: number;
  stocks: WarehouseStockCell[];
}

export async function getAllWarehouseStock(params: {
  search?: string; nonZeroOnly?: boolean; page?: number; pageSize?: number;
} = {}): Promise<PageResult<AllWarehouseStock>> {
  const q = new URLSearchParams();
  if (params.search) q.set('search', params.search);
  if (params.nonZeroOnly !== undefined) q.set('nonZeroOnly', String(params.nonZeroOnly));
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 50));
  const res = await http.get<ApiEnvelope<PageResult<AllWarehouseStock>>>(`/inventory/all-warehouses?${q.toString()}`);
  return res.data.data;
}

// ===================== Phase 4.8 — Tính lại giá vốn =====================

export interface CostRecalcResult {
  phuongPhap: string;
  tuNgay: string;
  soMatHang: number;
  soDongXuat: number;
  tongGiaTriXuat: number;
}

export async function recalculateCost(phuongPhap: string, tuNgay: string): Promise<CostRecalcResult> {
  const res = await http.post<ApiEnvelope<CostRecalcResult>>('/inventory/cost-recalc', {
    phuongPhap, tuNgay,
  });
  return res.data.data;
}
