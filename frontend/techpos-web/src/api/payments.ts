import { http, ApiEnvelope } from './http';

// ===================== Thanh toán MoMo (Phase 8) =====================

export interface PaymentOrder {
  id: string;
  soPhieu: string;
  ngay: string | null;
  tongCong: number;
  conNo: number;
  khachHang: string | null;
}

export interface MoMoCreateResult {
  orderId: string;
  requestId: string;
  payUrl: string | null;
  qrCodeUrl: string | null;
  deeplink: string | null;
  resultCode: number;
  message: string | null;
}

export interface MoMoPaymentStatus {
  orderId: string;
  status: 'PENDING' | 'PAID' | 'FAILED' | string;
  amount: number;
  resultCode: number | null;
  message: string | null;
  transId: number | null;
  payUrl: string | null;
}

export async function getMoMoOrders(): Promise<PaymentOrder[]> {
  const res = await http.get<ApiEnvelope<PaymentOrder[]>>('/payments/momo/orders');
  return res.data.data;
}

export async function createMoMoPayment(payload: {
  donHangId: string;
  amount?: number | null;
  orderInfo?: string | null;
}): Promise<MoMoCreateResult> {
  const res = await http.post<ApiEnvelope<MoMoCreateResult>>('/payments/momo/create', payload);
  return res.data.data;
}

export async function getMoMoStatus(orderId: string): Promise<MoMoPaymentStatus> {
  const res = await http.get<ApiEnvelope<MoMoPaymentStatus>>(`/payments/momo/${orderId}`);
  return res.data.data;
}
