import { http, ApiEnvelope } from './http';

export interface PageResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface Customer {
  id: string;
  name: string;
  maKhach: string | null;
  diaChi: string | null;
  diaChiGiaoHang: string | null;
  dienThoai: string | null;
  email: string | null;
  maSoThue: string | null;
  nguoiDaiDien: string | null;
  chucVu: string | null;
  facebook: string | null;
  songayChoNo: number;
  diemTichLuy: number;
  status: number;
  nhomKhachHang: string | null;
  nhanVienPhuTrach: string | null;
  timeCreated: string | null;
  timeModified: string | null;
}

export interface CustomerGroup {
  id: string;
  name: string;
  status: number;
}

export interface CustomerListParams {
  search?: string;
  groupId?: string;
  activeOnly?: boolean;
  page?: number;
  pageSize?: number;
  sort?: string;
}

export async function getCustomers(params: CustomerListParams = {}): Promise<PageResult<Customer>> {
  const q = new URLSearchParams();
  if (params.search) q.set('search', params.search);
  if (params.groupId) q.set('groupId', params.groupId);
  if (params.activeOnly !== undefined) q.set('activeOnly', String(params.activeOnly));
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 20));
  if (params.sort) q.set('sort', params.sort);
  const res = await http.get<ApiEnvelope<PageResult<Customer>>>(`/customers?${q.toString()}`);
  return res.data.data;
}

export async function getCustomer(id: string): Promise<Customer> {
  const res = await http.get<ApiEnvelope<Customer>>(`/customers/${id}`);
  return res.data.data;
}

export async function getCustomerGroups(): Promise<CustomerGroup[]> {
  const res = await http.get<ApiEnvelope<CustomerGroup[]>>('/customers/groups');
  return res.data.data;
}

// ---------- Phase 4 CRUD ----------

export interface CustomerCreateInput {
  name: string;
  maKhach?: string | null;
  diaChi?: string | null;
  diaChiGiaoHang?: string | null;
  dienThoai?: string | null;
  email?: string | null;
  maSoThue?: string | null;
  nguoiDaiDien?: string | null;
  chucVu?: string | null;
  facebook?: string | null;
  nhomKhachHangId?: string | null;
  songayChoNo?: number;
  diemTichLuy?: number;
}

export interface CustomerUpdateInput {
  name?: string | null;
  diaChi?: string | null;
  dienThoai?: string | null;
  email?: string | null;
  maSoThue?: string | null;
  nguoiDaiDien?: string | null;
  chucVu?: string | null;
  facebook?: string | null;
  diaChiGiaoHang?: string | null;
  nhomKhachHangId?: string | null;
  songayChoNo?: number | null;
  diemTichLuy?: number | null;
  status?: number | null;
}

export interface CatalogCreateResult {
  id: string;
  name: string;
}

export async function createCustomer(input: CustomerCreateInput): Promise<CatalogCreateResult> {
  const res = await http.post<ApiEnvelope<CatalogCreateResult>>('/customers', input);
  return res.data.data;
}

export async function updateCustomer(id: string, input: CustomerUpdateInput): Promise<Customer> {
  const res = await http.put<ApiEnvelope<Customer>>(`/customers/${id}`, input);
  return res.data.data;
}

export async function deleteCustomer(id: string): Promise<boolean> {
  const res = await http.delete<ApiEnvelope<boolean>>(`/customers/${id}`);
  return res.data.data;
}

