import { http, ApiEnvelope } from './http';
import type { PageResult } from './customers';

// ===================== Phase 5.3 — Nhân sự =====================

export interface Employee {
  id: string;
  name: string;
  diaChi: string | null;
  dienThoai: string | null;
  cachTinhLuong: number;      // 0 = theo ca, 1 = theo tháng
  luongCa: number;
  luongThang: number;
  phuCap: number;
  nghiThu7: boolean;
  nghiChuNhat: boolean;
  note: string | null;
  status: number;
  timeCreated: string | null;
  timeModified: string | null;
}

export async function getEmployees(params: { search?: string; activeOnly?: boolean; page?: number; pageSize?: number } = {}): Promise<PageResult<Employee>> {
  const q = new URLSearchParams();
  if (params.search) q.set('search', params.search);
  if (params.activeOnly !== undefined) q.set('activeOnly', String(params.activeOnly));
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 20));
  const res = await http.get<ApiEnvelope<PageResult<Employee>>>(`/employees?${q.toString()}`);
  return res.data.data;
}

export interface EmployeePayload {
  name: string;
  diaChi: string | null;
  dienThoai: string | null;
  cachTinhLuong: number;
  luongCa: number;
  luongThang: number;
  phuCap: number;
  nghiThu7: boolean;
  nghiChuNhat: boolean;
  note: string | null;
}

export async function createEmployee(req: EmployeePayload): Promise<{ id: string; name: string }> {
  const res = await http.post<ApiEnvelope<{ id: string; name: string }>>('/employees', req);
  return res.data.data;
}

export async function updateEmployee(id: string, req: Partial<EmployeePayload> & { status?: number }): Promise<Employee> {
  const res = await http.put<ApiEnvelope<Employee>>(`/employees/${id}`, req);
  return res.data.data;
}

export async function deleteEmployee(id: string): Promise<void> {
  await http.delete(`/employees/${id}`);
}

// ---------- Chấm công ----------

export interface Shift {
  id: string;
  name: string;
  tileLuong: number;
  note: string | null;
}

export async function getShifts(): Promise<Shift[]> {
  const res = await http.get<ApiEnvelope<Shift[]>>('/attendance/shifts');
  return res.data.data;
}

export interface Attendance {
  id: string;
  ngay: string | null;
  nhanVienId: string;
  tenNhanVien: string | null;
  caLamViecId: string | null;
  tenCa: string | null;
  tileLuong: number;
  trangThai: number;
}

export interface AttendanceListParams {
  from?: string;
  to?: string;
  nhanVienId?: string;
  page?: number;
  pageSize?: number;
}

export async function getAttendance(params: AttendanceListParams = {}): Promise<PageResult<Attendance>> {
  const q = new URLSearchParams();
  if (params.from) q.set('from', params.from);
  if (params.to) q.set('to', params.to);
  if (params.nhanVienId) q.set('nhanVienId', params.nhanVienId);
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 50));
  const res = await http.get<ApiEnvelope<PageResult<Attendance>>>(`/attendance?${q.toString()}`);
  return res.data.data;
}

export async function createAttendance(req: { nhanVienId: string; ngay: string; caLamViecId: string; trangThai?: number }): Promise<Attendance> {
  const res = await http.post<ApiEnvelope<Attendance>>('/attendance', req);
  return res.data.data;
}

// ---------- Thưởng phạt ----------

export interface BonusPenalty {
  id: string;
  ngay: string | null;
  name: string;
  nhanVienId: string | null;
  tenNhanVien: string | null;
  lyDo: string | null;
  thuong: number;
  phat: number;
  note: string | null;
  timeCreated: string | null;
}

export async function getBonusPenalties(params: { from?: string; to?: string; nhanVienId?: string; page?: number; pageSize?: number } = {}): Promise<PageResult<BonusPenalty>> {
  const q = new URLSearchParams();
  if (params.from) q.set('from', params.from);
  if (params.to) q.set('to', params.to);
  if (params.nhanVienId) q.set('nhanVienId', params.nhanVienId);
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 50));
  const res = await http.get<ApiEnvelope<PageResult<BonusPenalty>>>(`/bonus-penalties?${q.toString()}`);
  return res.data.data;
}

export async function getBonusReasons(): Promise<{ id: string; name: string }[]> {
  const res = await http.get<ApiEnvelope<{ id: string; name: string }[]>>('/bonus-penalties/reasons');
  return res.data.data;
}

export async function createBonusPenalty(req: { nhanVienId: string; thuong: number; phat: number; lyDoId: string | null; ghiChu: string | null; ngay: string | null }): Promise<{ id: string; name: string }> {
  const res = await http.post<ApiEnvelope<{ id: string; name: string }>>('/bonus-penalties', req);
  return res.data.data;
}

// ---------- Tính lương ----------

export interface PayrollLine {
  id: string;
  nhanVienId: string;
  tenNhanVien: string | null;
  cachTinhLuong: number;
  luongCa: number;
  luongThang: number;
  thuong: number;
  phat: number;
  tamUng: number;
  phuCap: number;
  tongLuong: number;
  thucNhan: number;
  soCong: number;
}

export interface Payroll {
  id: string;
  soPhieu: string;
  thang: number;
  nam: number;
  ngay: string | null;
  note: string | null;
  lines: PayrollLine[];
  tongThucNhan: number;
}

export interface PayrollSummary {
  id: string;
  soPhieu: string;
  thang: number;
  nam: number;
  ngay: string | null;
  note: string | null;
  soNhanVien: number;
  tongThucNhan: number;
}

export async function getPayrolls(params: { page?: number; pageSize?: number } = {}): Promise<PageResult<PayrollSummary>> {
  const q = new URLSearchParams();
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 20));
  const res = await http.get<ApiEnvelope<PageResult<PayrollSummary>>>(`/payroll?${q.toString()}`);
  return res.data.data;
}

export async function getPayroll(id: string): Promise<Payroll> {
  const res = await http.get<ApiEnvelope<Payroll>>(`/payroll/${id}`);
  return res.data.data;
}

export async function createPayroll(req: { thang: number; nam: number; ghiChu: string | null }): Promise<Payroll> {
  const res = await http.post<ApiEnvelope<Payroll>>('/payroll', req);
  return res.data.data;
}

export async function createSalaryAdvance(req: { nhanVienId: string; tien: number; ghiChu: string | null }): Promise<{ id: string; soPhieu: string; tien: number }> {
  const res = await http.post<ApiEnvelope<{ id: string; soPhieu: string; tien: number }>>('/payroll/advances', req);
  return res.data.data;
}

// ---------- Giấy giới thiệu ----------

export interface IntroLetter {
  id: string;
  soPhieu: string;
  ngay: string | null;
  hieuLucDen: string | null;
  kinhGui: string | null;
  diaChi: string | null;
  nguoiLienHe: string | null;
  ongBa: string | null;
  duocCuDen: string | null;
  veViec: string | null;
  note: string | null;
  timeCreated: string | null;
}

export async function getIntroLetters(params: { search?: string; page?: number; pageSize?: number } = {}): Promise<PageResult<IntroLetter>> {
  const q = new URLSearchParams();
  if (params.search) q.set('search', params.search);
  q.set('page', String(params.page ?? 1));
  q.set('pageSize', String(params.pageSize ?? 20));
  const res = await http.get<ApiEnvelope<PageResult<IntroLetter>>>(`/intro-letters?${q.toString()}`);
  return res.data.data;
}

export async function createIntroLetter(req: {
  ngay: string | null; hieuLucDen: string | null; kinhGui: string | null; diaChi: string | null;
  nguoiLienHe: string | null; ongBa: string; duocCuDen: string; veViec: string | null; ghiChu: string | null;
}): Promise<{ id: string; name: string }> {
  const res = await http.post<ApiEnvelope<{ id: string; name: string }>>('/intro-letters', req);
  return res.data.data;
}

export async function deleteIntroLetter(id: string): Promise<void> {
  await http.delete(`/intro-letters/${id}`);
}
