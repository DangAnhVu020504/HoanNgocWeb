import { http, ApiEnvelope } from './http';

export interface Product {
  id: string;
  code: string | null;
  name: string;
  note: string | null;
  giaBan: number;
  giaBan2: number;
  giaNhap: number;
  giaVon: number;
  tonToiThieu: number;
  tonToiDa: number;
  hoaHong: number;
  status: number;
  donViTinh: string | null;
  nhomMatHang: string | null;
  hangSanXuat: string | null;
  xuatXu: string | null;
  mauSac: string | null;
  chatLieu: string | null;
  coHanSuDung: boolean;
  coSeri: boolean;
  timeCreated: string | null;
  timeModified: string | null;
}

export interface ProductGroup {
  id: string;
  name: string;
  status: number;
}

export interface ProductStock {
  wareHouseId: string;
  wareHouseName: string | null;
  ton: number;
}

export interface PageResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface ProductListParams {
  search?: string;
  groupId?: string;
  activeOnly?: boolean;
  page?: number;
  pageSize?: number;
  sort?: string;
}

export async function getProducts(params: ProductListParams = {}): Promise<PageResult<Product>> {
  const q = new URLSearchParams();
  if (params.search) q.set('search', params.search);
  if (params.groupId) q.set('groupId', params.groupId);
  if (params.activeOnly !== undefined) q.set('activeOnly', String(params.activeOnly));
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 20));
  if (params.sort) q.set('sort', params.sort);
  const res = await http.get<ApiEnvelope<PageResult<Product>>>(`/products?${q.toString()}`);
  return res.data.data;
}

export async function getProduct(id: string): Promise<Product> {
  const res = await http.get<ApiEnvelope<Product>>(`/products/${id}`);
  return res.data.data;
}

export async function getProductGroups(): Promise<ProductGroup[]> {
  const res = await http.get<ApiEnvelope<ProductGroup[]>>('/products/groups');
  return res.data.data;
}

export async function getProductStock(id: string): Promise<ProductStock[]> {
  const res = await http.get<ApiEnvelope<ProductStock[]>>(`/products/${id}/stock`);
  return res.data.data;
}

// ---------- Phase 4 CRUD ----------

export interface ProductCreateInput {
  name: string;
  code?: string | null;
  maSanCo?: string | null;
  note?: string | null;
  nhomMatHangId?: string | null;
  donViTinhId?: string | null;
  hangSanXuatId?: string | null;
  xuatXu?: string | null;
  mauSac?: string | null;
  chatLieu?: string | null;
  giaBan?: number;
  giaNhap?: number;
  tonToiThieu?: number;
  tonToiDa?: number;
  coHanSuDung?: boolean;
  coSeri?: boolean;
}

export interface ProductUpdateInput {
  name?: string | null;
  note?: string | null;
  giaBan?: number | null;
  giaNhap?: number | null;
  tonToiThieu?: number | null;
  tonToiDa?: number | null;
  nhomMatHangId?: string | null;
  donViTinhId?: string | null;
  xuatXu?: string | null;
  mauSac?: string | null;
  chatLieu?: string | null;
  coHanSuDung?: boolean | null;
  coSeri?: boolean | null;
  status?: number | null;
}

export async function createProduct(input: ProductCreateInput): Promise<{ id: string; name: string }> {
  const res = await http.post<ApiEnvelope<{ id: string; name: string }>>('/products', input);
  return res.data.data;
}

export async function updateProduct(id: string, input: ProductUpdateInput): Promise<Product> {
  const res = await http.put<ApiEnvelope<Product>>(`/products/${id}`, input);
  return res.data.data;
}

export async function deleteProduct(id: string): Promise<boolean> {
  const res = await http.delete<ApiEnvelope<boolean>>(`/products/${id}`);
  return res.data.data;
}
