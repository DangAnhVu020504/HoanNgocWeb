import { http, ApiEnvelope } from './http';
import type { PageResult } from './customers';

// ===================== Phase 5.1 — Quỹ =====================

export interface CashReason {
  id: string;
  name: string;
  laLyDoThu: boolean;
}

export async function getCashReasons(): Promise<CashReason[]> {
  const res = await http.get<ApiEnvelope<CashReason[]>>('/cashbook/reasons');
  return res.data.data;
}

export interface CashBalance {
  tongThu: number;
  tongChi: number;
  tonQuy: number;
  tongThuChuyenKhoan: number;
  tongChiChuyenKhoan: number;
  tonChuyenKhoan: number;
}

export async function getCashBalance(params: { from?: string; to?: string } = {}): Promise<CashBalance> {
  const q = new URLSearchParams();
  if (params.from) q.set('from', params.from);
  if (params.to) q.set('to', params.to);
  const res = await http.get<ApiEnvelope<CashBalance>>(`/cashbook/balance?${q.toString()}`);
  return res.data.data;
}

export interface CashVoucher {
  id: string;
  soPhieu: string;
  ngay: string | null;
  loai: number;
  tenDoiTuong: string | null;
  dienGiai: string | null;
  thu: number;
  chi: number;
  chuyenKhoan: boolean;
  laPhieuThuCongNo: boolean;
  ghiChu: string | null;
  khachHang: string | null;
  nhaCungCap: string | null;
  lyDo: string | null;
  timeCreated: string | null;
}

export interface CashVoucherListParams {
  isThu?: boolean;
  search?: string;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

export async function getCashVouchers(params: CashVoucherListParams = {}): Promise<PageResult<CashVoucher>> {
  const q = new URLSearchParams();
  if (params.isThu !== undefined) q.set('isThu', String(params.isThu));
  if (params.search) q.set('search', params.search);
  if (params.from) q.set('from', params.from);
  if (params.to) q.set('to', params.to);
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 20));
  const res = await http.get<ApiEnvelope<PageResult<CashVoucher>>>(`/cashbook/vouchers?${q.toString()}`);
  return res.data.data;
}

export interface CashVoucherAllocationInput {
  donHangId: string;
  soTien: number;
}

export interface CashVoucherPayload {
  doiTuong: 'KH' | 'NCC';
  khachHangId: string | null;
  nhaCungCapId: string | null;
  nhanVienId: string | null;
  lyDoId: string | null;
  tien: number;
  chuyenKhoan: boolean;
  ghiChu: string | null;
  allocations: CashVoucherAllocationInput[] | null;
}

export interface CashVoucherResult {
  id: string;
  soPhieu: string;
  tien: number;
  daGanNo: number;
}

export async function createCashReceipt(req: CashVoucherPayload): Promise<CashVoucherResult> {
  const res = await http.post<ApiEnvelope<CashVoucherResult>>('/cashbook/receipts', req);
  return res.data.data;
}

export async function createCashPayment(req: CashVoucherPayload): Promise<CashVoucherResult> {
  const res = await http.post<ApiEnvelope<CashVoucherResult>>('/cashbook/payments', req);
  return res.data.data;
}