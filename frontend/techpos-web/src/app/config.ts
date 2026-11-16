/**
 * Cấu hình tích hợp ngoài — đọc từ biến môi trường Vite (.env, đã gitignore).
 * KHÔNG hardcode tài khoản ngân hàng thật vào mã nguồn.
 *
 * Cách cấu hình: tạo file `.env` (cùng cấp package.json) với nội dung:
 *   VITE_VIETQR_BANK_BIN=970422
 *   VITE_VIETQR_ACCOUNT=<so-tai-khoan-that>
 *   VITE_VIETQR_TEMPLATE=compact2
 *   VITE_VIETQR_ADDINFO=HNPOS
 */

const env = import.meta.env;

export interface VietQrConfig {
  bankBin: string;
  account: string;
  template: string;
  addInfo: string;
}

export const VIETQR: VietQrConfig = {
  bankBin: (env.VITE_VIETQR_BANK_BIN as string | undefined) ?? '',
  account: (env.VITE_VIETQR_ACCOUNT as string | undefined) ?? '',
  template: (env.VITE_VIETQR_TEMPLATE as string | undefined) ?? 'compact2',
  addInfo: (env.VITE_VIETQR_ADDINFO as string | undefined) ?? 'HNPOS',
};

/** Đã cấu hình STK VietQR thật hay chưa (tránh sinh QR sai người nhận). */
export const isVietQrConfigured = (): boolean =>
  VIETQR.bankBin.trim() !== '' && VIETQR.account.trim() !== '';

/** Sinh URL ảnh VietQR cho số tiền (VNĐ, làm tròn). */
export const buildVietQrUrl = (amount: number): string =>
  `https://api.vietqr.io/image/${VIETQR.bankBin}-${VIETQR.account}-${VIETQR.template}.jpg` +
  `?amount=${Math.max(0, Math.round(amount))}&addInfo=${encodeURIComponent(VIETQR.addInfo)}`;