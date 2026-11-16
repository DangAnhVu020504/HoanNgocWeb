import { http, ApiEnvelope } from './http';

// ===================== Phase 8 — Cài đặt hệ thống =====================

export interface AppSetting {
  key: string;
  value: string | null;
  updatedAt: string | null;
}

export interface CompanyInfo {
  name: string;
  taxCode: string | null;
  phone: string | null;
  email: string | null;
  address: string | null;
  website: string | null;
  bankAccount: string | null;
  bankName: string | null;
  invoiceFooter: string | null;
}

export async function getSettings(): Promise<AppSetting[]> {
  const res = await http.get<ApiEnvelope<AppSetting[]>>('/settings');
  return res.data.data;
}

export async function getCompanyInfo(): Promise<CompanyInfo> {
  const res = await http.get<ApiEnvelope<CompanyInfo>>('/settings/company');
  return res.data.data;
}

export async function saveSettings(settings: Record<string, string | null>): Promise<void> {
  await http.put('/settings', { settings });
}
