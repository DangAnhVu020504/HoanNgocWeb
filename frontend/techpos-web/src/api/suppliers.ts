import { http, ApiEnvelope } from './http';

export interface Supplier {
  id: string;
  name: string;
  maNhaCungCap: string | null;
  diaChi: string | null;
  dienThoai: string | null;
  email: string | null;
  website: string | null;
  note: string | null;
  status: number;
  nhomNhaCungCap: string | null;
  timeCreated: string | null;
  timeModified: string | null;
}

export interface SupplierGroup {
  id: string;
  name: string;
  status: number;
}

export interface SupplierListParams {
  search?: string;
  groupId?: string;
  activeOnly?: boolean;
  page?: number;
  pageSize?: number;
  sort?: string;
}

export interface PageResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export async function getSuppliers(params: SupplierListParams = {}): Promise<PageResult<Supplier>> {
  const q = new URLSearchParams();
  if (params.search) q.set('search', params.search);
  if (params.groupId) q.set('groupId', params.groupId);
  if (params.activeOnly !== undefined) q.set('activeOnly', String(params.activeOnly));
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 20));
  if (params.sort) q.set('sort', params.sort);
  const res = await http.get<ApiEnvelope<PageResult<Supplier>>>(`/suppliers?${q.toString()}`);
  return res.data.data;
}

export async function getSupplier(id: string): Promise<Supplier> {
  const res = await http.get<ApiEnvelope<Supplier>>(`/suppliers/${id}`);
  return res.data.data;
}

export async function getSupplierGroups(): Promise<SupplierGroup[]> {
  const res = await http.get<ApiEnvelope<SupplierGroup[]>>('/suppliers/groups');
  return res.data.data;
}

// ---------- Phase 4 CRUD ----------

export interface SupplierCreateInput {
  name: string;
  maNhaCungCap?: string | null;
  diaChi?: string | null;
  dienThoai?: string | null;
  email?: string | null;
  website?: string | null;
  note?: string | null;
  nhomNhaCungCapId?: string | null;
}

export interface SupplierUpdateInput {
  name?: string | null;
  diaChi?: string | null;
  dienThoai?: string | null;
  email?: string | null;
  website?: string | null;
  note?: string | null;
  nhomNhaCungCapId?: string | null;
  status?: number | null;
}

export async function createSupplier(input: SupplierCreateInput): Promise<{ id: string; name: string }> {
  const res = await http.post<ApiEnvelope<{ id: string; name: string }>>('/suppliers', input);
  return res.data.data;
}

export async function updateSupplier(id: string, input: SupplierUpdateInput): Promise<Supplier> {
  const res = await http.put<ApiEnvelope<Supplier>>(`/suppliers/${id}`, input);
  return res.data.data;
}

export async function deleteSupplier(id: string): Promise<boolean> {
  const res = await http.delete<ApiEnvelope<boolean>>(`/suppliers/${id}`);
  return res.data.data;
}
