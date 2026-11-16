import { http, ApiEnvelope } from './http';
import type { PageResult } from './customers';

// ===================== Phase 8 — CSKH =====================

export interface CustomerCare {
  id: string;
  customerId: string;
  tenKhach: string | null;
  dienThoai: string | null;
  email: string | null;
  careType: string;
  content: string | null;
  result: string | null;
  nextCareAt: string | null;
  assigneeUserId: string | null;
  assigneeName: string | null;
  createdBy: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface CustomerCareQuery {
  search?: string;
  careType?: string;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

export interface CustomerCareInput {
  customerId: string;
  careType: string;
  content?: string | null;
  result?: string | null;
  nextCareAt?: string | null;
  assigneeUserId?: string | null;
}

export interface CustomerFeedback {
  id: string;
  customerId: string | null;
  tenKhach: string | null;
  invoiceId: string | null;
  soHoaDon: string | null;
  rating: number;
  content: string | null;
  status: string;
  response: string | null;
  createdBy: string | null;
  createdAt: string;
  respondedBy: string | null;
  respondedAt: string | null;
}

export interface CustomerFeedbackQuery {
  search?: string;
  status?: string;
  rating?: number;
  page?: number;
  pageSize?: number;
}

export interface CustomerFeedbackInput {
  customerId?: string | null;
  invoiceId?: string | null;
  rating: number;
  content?: string | null;
}

export interface Message {
  id: string;
  channel: string;
  customerId: string | null;
  tenKhach: string | null;
  recipient: string;
  subject: string | null;
  body: string | null;
  status: string;
  error: string | null;
  createdBy: string | null;
  createdAt: string;
  sentAt: string | null;
}

export interface MessageQuery {
  channel?: string;
  status?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface MessageSendInput {
  channel: string;
  customerId?: string | null;
  recipient?: string | null;
  subject?: string | null;
  body: string;
}

export interface LoyaltySummary {
  customerId: string;
  tenKhach: string | null;
  dienThoai: string | null;
  points: number;
  totalEarned: number;
  totalRedeemed: number;
  lastTxnAt: string | null;
}

export interface LoyaltyTxn {
  id: string;
  customerId: string;
  txnType: string;
  points: number;
  referenceType: string | null;
  referenceId: string | null;
  note: string | null;
  createdBy: string | null;
  createdAt: string;
}

function toQuery<T extends object>(params: T): string {
  const q = new URLSearchParams();
  for (const [k, raw] of Object.entries(params)) {
    const v = raw as string | number | undefined;
    if (v !== undefined && v !== '') q.set(k, String(v));
  }
  return q.toString();
}

// ---------- Chăm sóc ----------

export async function getCare(params: CustomerCareQuery = {}): Promise<PageResult<CustomerCare>> {
  const res = await http.get<ApiEnvelope<PageResult<CustomerCare>>>(`/crm/care?${toQuery(params)}`);
  return res.data.data;
}

export async function createCare(input: CustomerCareInput): Promise<CustomerCare> {
  const res = await http.post<ApiEnvelope<CustomerCare>>('/crm/care', input);
  return res.data.data;
}

export async function updateCare(id: string, input: Partial<CustomerCareInput>): Promise<CustomerCare> {
  const res = await http.put<ApiEnvelope<CustomerCare>>(`/crm/care/${id}`, input);
  return res.data.data;
}

export async function deleteCare(id: string): Promise<void> {
  await http.delete(`/crm/care/${id}`);
}

// ---------- Phản hồi ----------

export async function getFeedback(params: CustomerFeedbackQuery = {}): Promise<PageResult<CustomerFeedback>> {
  const res = await http.get<ApiEnvelope<PageResult<CustomerFeedback>>>(`/crm/feedback?${toQuery(params)}`);
  return res.data.data;
}

export async function createFeedback(input: CustomerFeedbackInput): Promise<CustomerFeedback> {
  const res = await http.post<ApiEnvelope<CustomerFeedback>>('/crm/feedback', input);
  return res.data.data;
}

export async function respondFeedback(id: string, response: string, status?: string | null): Promise<CustomerFeedback> {
  const res = await http.post<ApiEnvelope<CustomerFeedback>>(`/crm/feedback/${id}/respond`, { response, status });
  return res.data.data;
}

// ---------- Tin nhắn ----------

export async function getMessages(params: MessageQuery = {}): Promise<PageResult<Message>> {
  const res = await http.get<ApiEnvelope<PageResult<Message>>>(`/crm/messages?${toQuery(params)}`);
  return res.data.data;
}

export async function sendMessage(input: MessageSendInput): Promise<Message> {
  const res = await http.post<ApiEnvelope<Message>>('/crm/messages', input);
  return res.data.data;
}

// ---------- Điểm thân thiết ----------

export async function getLoyalty(params: { search?: string; page?: number; pageSize?: number } = {}): Promise<PageResult<LoyaltySummary>> {
  const res = await http.get<ApiEnvelope<PageResult<LoyaltySummary>>>(`/crm/loyalty?${toQuery(params)}`);
  return res.data.data;
}

export async function getLoyaltyTxns(customerId: string): Promise<LoyaltyTxn[]> {
  const res = await http.get<ApiEnvelope<LoyaltyTxn[]>>(`/crm/loyalty/${customerId}/transactions`);
  return res.data.data;
}

export async function adjustLoyalty(customerId: string, points: number, note?: string | null): Promise<{ applied: number; newPoints: number }> {
  const res = await http.post<ApiEnvelope<{ applied: number; newPoints: number }>>('/crm/loyalty/adjust', { customerId, points, note });
  return res.data.data;
}
