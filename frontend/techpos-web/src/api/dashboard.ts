import { http, ApiEnvelope } from './http';

export interface DashboardDocType {
  LOAI: number;
  DOC_TYPE: string;
  PREFIX: string | null;
  VERIFIED: number;
}

export interface DashboardDocBucket {
  LOAI: number;
  N: number;
  TienHang: number | string;
  TongCong: number | string;
}

export interface DashboardCounts {
  Products: number;
  Customers: number;
  Suppliers: number;
  Employees: number;
  Warehouses: number;
}

export interface DashboardStock {
  SkuCount: number;
  TotalQty: number | string;
}

export interface DashboardCash {
  Thu: number | string;
  Chi: number | string;
}

export interface DashboardSummary {
  todayDocs: DashboardDocBucket[];
  monthDocs: DashboardDocBucket[];
  counts: DashboardCounts;
  stock: DashboardStock;
  cash: DashboardCash;
  docTypes: DashboardDocType[];
}

export async function getDashboardSummary(): Promise<DashboardSummary> {
  const res = await http.get<ApiEnvelope<DashboardSummary>>('/dashboard/summary');
  return res.data.data;
}
