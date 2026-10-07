import { create } from 'zustand';
import type { PosCartItem } from '../api/pos';

/**
 * Giỏ hàng POS bán lẻ (mục 46, FRONTEND-STRUCTURE §3).
 * Giữ state khi chuyển tab; reset sau khi thanh toán thành công.
 */
export interface PosCartState {
  khoId: string | null;
  khachHangId: string | null;
  lines: PosCartItem[];
  tienGiamGia: number;
  khachDua: number;
  setKho: (khoId: string) => void;
  setKhachHang: (id: string | null) => void;
  addLine: (line: PosCartItem) => void;
  removeLine: (matHangId: string) => void;
  updateLine: (matHangId: string, patch: Partial<PosCartItem>) => void;
  setTienGiamGia: (v: number) => void;
  setKhachDua: (v: number) => void;
  resetCart: () => void;
}

export const usePosCartStore = create<PosCartState>()((set) => ({
  khoId: null,
  khachHangId: null,
  lines: [],
  tienGiamGia: 0,
  khachDua: 0,
  setKho: (khoId) => set({ khoId }),
  setKhachHang: (khachHangId) => set({ khachHangId }),
  addLine: (line) => set((s) => {
    const existing = s.lines.find((l) => l.matHangId === line.matHangId);
    if (existing) {
      return {
        lines: s.lines.map((l) =>
          l.matHangId === line.matHangId ? { ...l, soLuong: l.soLuong + line.soLuong } : l
        ),
      };
    }
    return { lines: [...s.lines, line] };
  }),
  removeLine: (matHangId) => set((s) => ({ lines: s.lines.filter((l) => l.matHangId !== matHangId) })),
  updateLine: (matHangId, patch) => set((s) => ({
    lines: s.lines.map((l) => (l.matHangId === matHangId ? { ...l, ...patch } : l)),
  })),
  setTienGiamGia: (tienGiamGia) => set({ tienGiamGia: Math.max(0, tienGiamGia) }),
  setKhachDua: (khachDua) => set({ khachDua: Math.max(0, khachDua) }),
  resetCart: () => set({ lines: [], khachHangId: null, tienGiamGia: 0, khachDua: 0 }),
}));
