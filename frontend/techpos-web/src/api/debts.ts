import { http, ApiEnvelope } from './http';
import type { PageResult } from './customers';

// ===================== Phase 5.2 — Công nợ =====================

export interface CustomerDebt {
  khachHangId: string;
  maKhach: string | null;
  tenKhach: string;
  dienThoai: string | null;
  congNo: number;
  soHoaDon: number;
}

export async function getCustomerDebts(params: { search?: string; page?: number; pageSize?: number } = {})
  : Promise<PageResult<CustomerDebt>> {
  const q = new URLSearchParams();
  if (params.search) q.set('search', params.search);
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 50));
  const res = await http.get<ApiEnvelope<PageResult<CustomerDebt>>>(`/debts/customers?${q.toString()}`);
  return res.data.data;
}

export interface SupplierDebt {
  nhaCungCapId: string;
  maNhaCungCap: string | null;
  tenNhaCungCap: string;
  dienThoai: string | null;
  congNo: number;
  soPhieu: number;
}

export async function getSupplierDebts(params: { search?: string; page?: number; pageSize?: number } = {})
  : Promise<PageResult<SupplierDebt>> {
  const q = new URLSearchParams();
  if (params.search) q.set('search', params.search);
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 50));
  const res = await http.get<ApiEnvelope<PageResult<SupplierDebt>>>(`/debts/suppliers?${q.toString()}`);
  return res.data.data;
}