import { http, ApiEnvelope } from './http';
import type { PageResult } from './customers';

// ===================== Phase 8 — Quản trị người dùng & phân quyền =====================

export interface AdminUser {
  id: string;
  username: string;
  fullName: string | null;
  email: string | null;
  isActive: boolean;
  mustChangePassword: boolean;
  roles: string[];
  lastLoginAt: string | null;
  createdAt: string | null;
}

export interface AdminUserQuery {
  search?: string;
  activeOnly?: boolean;
  page?: number;
  pageSize?: number;
}

export interface AdminUserCreateInput {
  username: string;
  password: string;
  fullName?: string | null;
  email?: string | null;
  roles?: string[] | null;
}

export interface AdminUserUpdateInput {
  fullName?: string | null;
  email?: string | null;
  isActive?: boolean | null;
  roles?: string[] | null;
}

export interface AdminRole {
  id: string;
  code: string;
  name: string;
  permissions: string[];
  userCount: number;
}

export interface AdminPermission {
  id: string;
  code: string;
  name: string;
}

export async function getAdminUsers(params: AdminUserQuery = {}): Promise<PageResult<AdminUser>> {
  const q = new URLSearchParams();
  if (params.search) q.set('search', params.search);
  if (params.activeOnly !== undefined) q.set('activeOnly', String(params.activeOnly));
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 20));
  const res = await http.get<ApiEnvelope<PageResult<AdminUser>>>(`/admin/users?${q.toString()}`);
  return res.data.data;
}

export async function createAdminUser(input: AdminUserCreateInput): Promise<AdminUser> {
  const res = await http.post<ApiEnvelope<AdminUser>>('/admin/users', input);
  return res.data.data;
}

export async function updateAdminUser(id: string, input: AdminUserUpdateInput): Promise<AdminUser> {
  const res = await http.put<ApiEnvelope<AdminUser>>(`/admin/users/${id}`, input);
  return res.data.data;
}

export async function resetAdminPassword(id: string, newPassword: string): Promise<void> {
  await http.post(`/admin/users/${id}/reset-password`, { newPassword });
}

export async function deactivateAdminUser(id: string): Promise<void> {
  await http.delete(`/admin/users/${id}`);
}

export async function getAdminRoles(): Promise<AdminRole[]> {
  const res = await http.get<ApiEnvelope<AdminRole[]>>('/admin/roles');
  return res.data.data;
}

export async function getAdminPermissions(): Promise<AdminPermission[]> {
  const res = await http.get<ApiEnvelope<AdminPermission[]>>('/admin/permissions');
  return res.data.data;
}

export async function saveAdminRole(input: { code: string; name: string; permissions: string[] }): Promise<AdminRole> {
  const res = await http.post<ApiEnvelope<AdminRole>>('/admin/roles', input);
  return res.data.data;
}

export async function deleteAdminRole(id: string): Promise<void> {
  await http.delete(`/admin/roles/${id}`);
}
