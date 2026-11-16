import { http, ApiEnvelope } from './http';
import type { PageResult } from './customers';

// ===================== Phase 8 — Thư viện media =====================

export interface MediaItem {
  id: string;
  path: string;
  hash: string | null;
  size: number | null;
  mimeType: string | null;
  referenceType: string | null;
  columnName: string | null;
  referenceId: string | null;
  createdAt: string;
  url: string;
}

export interface MediaQueryParams {
  search?: string;
  referenceType?: string;
  mimeType?: string;
  page?: number;
  pageSize?: number;
}

export async function getMedia(params: MediaQueryParams = {}): Promise<PageResult<MediaItem>> {
  const q = new URLSearchParams();
  if (params.search) q.set('search', params.search);
  if (params.referenceType) q.set('referenceType', params.referenceType);
  if (params.mimeType) q.set('mimeType', params.mimeType);
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 24));
  const res = await http.get<ApiEnvelope<PageResult<MediaItem>>>(`/media?${q.toString()}`);
  return res.data.data;
}

export async function getMediaReferenceTypes(): Promise<string[]> {
  const res = await http.get<ApiEnvelope<string[]>>('/media/reference-types');
  return res.data.data;
}

/** Tải file media kèm Bearer token (không dùng được trực tiếp trong <img src>). */
export async function fetchMediaBlob(url: string): Promise<Blob> {
  const path = url.replace(/^\/api/, '');
  const res = await http.get(path, { responseType: 'blob' });
  return res.data as Blob;
}
