import { http, ApiEnvelope } from './http';
import type { PageResult } from './customers';

// ===================== Phase 7 — Notification Center =====================

export interface NotificationDto {
  id: string;
  group: string;               // SALES | INVENTORY | DEBT | COD | SYSTEM | EMAIL
  icon: string | null;
  title: string;
  description: string | null;
  actionUrl: string | null;
  createdAt: string;
  isRead: boolean;
}

export async function getNotifications(
  p: { page?: number; pageSize?: number; group?: string } = {}
): Promise<PageResult<NotificationDto>> {
  const q = new URLSearchParams();
  q.set('page', String(p.page ?? 1));
  q.set('pageSize', String(p.pageSize ?? 20));
  if (p.group && p.group !== 'ALL') q.set('group', p.group);
  const res = await http.get<ApiEnvelope<PageResult<NotificationDto>>>(`/notifications?${q.toString()}`);
  return res.data.data;
}

export async function getUnreadCount(): Promise<number> {
  const res = await http.get<ApiEnvelope<number>>('/notifications/unread-count');
  return res.data.data;
}

export async function markNotificationRead(id: string): Promise<void> {
  await http.post(`/notifications/${id}/read`);
}

export async function markAllNotificationsRead(): Promise<void> {
  await http.post('/notifications/mark-all-read');
}

// ===================== Phase 7 — Email Admin =====================

export interface EmailTemplate {
  id: string;
  code: string;
  name: string;
  subject: string;
  bodyHtml: string | null;
  bodyText: string | null;
  isActive: boolean;
}

export interface EmailEvent {
  id: string;
  code: string;
  name: string;
  templateCode: string | null;
  enabled: boolean;
  recipientRule: string | null;
}

export interface EmailQueueItem {
  id: string;
  eventType: string;
  subject: string;
  status: string;              // PENDING | PROCESSING | SENT | FAILED | CANCELLED
  retryCount: number;
  nextRetryAt: string | null;
  lastError: string | null;
  createdAt: string;
  sentAt: string | null;
  soNguoiNhan: number;
}

export interface EmailSettings {
  [key: string]: string | null | undefined;
}

export async function getEmailTemplates(): Promise<EmailTemplate[]> {
  const res = await http.get<ApiEnvelope<EmailTemplate[]>>('/emails/templates');
  return res.data.data;
}

export async function updateEmailTemplate(code: string, req: {
  subject: string; bodyHtml: string | null; bodyText: string | null; isActive: boolean;
}): Promise<void> {
  await http.put(`/emails/templates/${code}`, req);
}

export async function getEmailEvents(): Promise<EmailEvent[]> {
  const res = await http.get<ApiEnvelope<EmailEvent[]>>('/emails/events');
  return res.data.data;
}

export async function setEmailEventEnabled(code: string, enabled: boolean): Promise<void> {
  await http.put(`/emails/events/${code}/enabled`, enabled);
}

export async function getEmailQueue(
  p: { status?: string; page?: number; pageSize?: number } = {}
): Promise<PageResult<EmailQueueItem>> {
  const q = new URLSearchParams();
  if (p.status) q.set('status', p.status);
  q.set('page', String(p.page ?? 1));
  q.set('pageSize', String(p.pageSize ?? 20));
  const res = await http.get<ApiEnvelope<PageResult<EmailQueueItem>>>(`/emails/queue?${q.toString()}`);
  return res.data.data;
}

export async function retryEmailQueueItem(id: string): Promise<boolean> {
  const res = await http.post<ApiEnvelope<boolean>>(`/emails/queue/${id}/retry`);
  return res.data.data;
}

export async function cancelEmailQueueItem(id: string): Promise<boolean> {
  const res = await http.post<ApiEnvelope<boolean>>(`/emails/queue/${id}/cancel`);
  return res.data.data;
}

export async function getEmailSettings(): Promise<EmailSettings> {
  const res = await http.get<ApiEnvelope<EmailSettings>>('/emails/settings');
  return res.data.data;
}

export async function saveEmailSettings(req: {
  host: string; port: number; useSsl: boolean; username: string | null;
  password: string | null; fromEmail: string; fromName: string | null;
}): Promise<void> {
  await http.post('/emails/settings', req);
}

export async function testSendEmail(req: {
  to: string; name: string | null; subject: string; bodyHtml: string | null;
}): Promise<{ success: boolean; message?: string | null }> {
  const res = await http.post<ApiEnvelope<boolean>>('/emails/test-send', req);
  return { success: res.data.success, message: res.data.message };
}
