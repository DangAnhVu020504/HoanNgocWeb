import { http, ApiEnvelope } from './http';
import type { PageResult } from './customers';

export interface Warehouse {
  id: string;
  name: string;
  note: string | null;
  status: number;
  parentId: string | null;
  parentName: string | null;
  cuaHang: string | null;
  choPhepAmKho: boolean;
  timeCreated: string | null;
  timeModified: string | null;
}

export interface WarehouseListParams {
  search?: string;
  activeOnly?: boolean;
  page?: number;
  pageSize?: number;
  sort?: string;
}

export async function getWarehouses(params: WarehouseListParams = {}): Promise<PageResult<Warehouse>> {
  const q = new URLSearchParams();
  if (params.search) q.set('search', params.search);
  if (params.activeOnly !== undefined) q.set('activeOnly', String(params.activeOnly));
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 20));
  if (params.sort) q.set('sort', params.sort);
  const res = await http.get<ApiEnvelope<PageResult<Warehouse>>>(`/warehouses?${q.toString()}`);
  return res.data.data;
}

export async function getWarehouse(id: string): Promise<Warehouse> {
  const res = await http.get<ApiEnvelope<Warehouse>>(`/warehouses/${id}`);
  return res.data.data;
}

// ---------- Phase 4 CRUD ----------

export interface WarehouseCreateInput {
  name: string;
  note?: string | null;
  parentId?: string | null;
  cuaHangId?: string | null;
  choPhepAmKho?: boolean;
}

export interface WarehouseUpdateInput {
  name?: string | null;
  note?: string | null;
  parentId?: string | null;
  cuaHangId?: string | null;
  choPhepAmKho?: boolean | null;
  status?: number | null;
}

export async function createWarehouse(input: WarehouseCreateInput): Promise<{ id: string; name: string }> {
  const res = await http.post<ApiEnvelope<{ id: string; name: string }>>('/warehouses', input);
  return res.data.data;
}

export async function updateWarehouse(id: string, input: WarehouseUpdateInput): Promise<Warehouse> {
  const res = await http.put<ApiEnvelope<Warehouse>>(`/warehouses/${id}`, input);
  return res.data.data;
}

export async function deleteWarehouse(id: string): Promise<boolean> {
  const res = await http.delete<ApiEnvelope<boolean>>(`/warehouses/${id}`);
  return res.data.data;
}
