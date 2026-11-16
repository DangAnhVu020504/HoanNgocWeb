import { http, ApiEnvelope } from './http';
import type { PageResult } from './customers';

export interface HistoryItem {
  id: string;
  soPhieu: string;
  loai: string;
  nhom: string;
  ngay: string | null;
  timeCreated: string;
  nguoiTao: string | null;
  tien: number | null;
  dienGiai: string | null;
  status: number;
  /** Đường dẫn PDF của chứng từ (null nếu loại không có bản in). */
  pdfPath: string | null;
}

export async function getHistory(params: { search?: string; page?: number; pageSize?: number } = {}): Promise<PageResult<HistoryItem>> {
  const q = new URLSearchParams();
  if (params.search) q.set('search', params.search);
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 20));
  const res = await http.get<ApiEnvelope<PageResult<HistoryItem>>>(`/history?${q.toString()}`);
  return res.data.data;
}
