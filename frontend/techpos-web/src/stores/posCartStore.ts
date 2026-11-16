import { create } from 'zustand';
import type { PosCartItem } from '../api/pos';

/**
 * Giỏ hàng POS bán lẻ (mục 46, FRONTEND-STRUCTURE §3).
 * Giữ state khi chuyển tab; reset sau khi thanh toán thành công.
 */
export interface PosCartState {
  khoId: string | null;
  khachHangId: string | null;
  nhanVienId: string | null;
  dienGiai: string;
  ghiChu: string;
  lines: PosCartItem[];
  tileGiamGia: number;
  tienGiamGia: number;
  tienThue: number;
  phiVanChuyen: number;
  khachDua: number;
  setKho: (khoId: string) => void;
  setKhachHang: (id: string | null) => void;
  setNhanVien: (id: string | null) => void;
  setDienGiai: (v: string) => void;
  setGhiChu: (v: string) => void;
  addLine: (line: PosCartItem) => void;
  removeLine: (matHangId: string) => void;
  updateLine: (matHangId: string, patch: Partial<PosCartItem>) => void;
  setTileGiamGia: (v: number) => void;
  setTienGiamGia: (v: number) => void;
  setTienThue: (v: number) => void;
  setPhiVanChuyen: (v: number) => void;
  setKhachDua: (v: number) => void;
  resetCart: () => void;
}

export const usePosCartStore = create<PosCartState>()((set) => ({
  khoId: null,
  khachHangId: null,
  nhanVienId: null,
  dienGiai: '',
  ghiChu: '',
  lines: [],
  tileGiamGia: 0,
  tienGiamGia: 0,
  tienThue: 0,
  phiVanChuyen: 0,
  khachDua: 0,
  setKho: (khoId) => set({ khoId }),
  setKhachHang: (khachHangId) => set({ khachHangId }),
  setNhanVien: (nhanVienId) => set({ nhanVienId }),
  setDienGiai: (dienGiai) => set({ dienGiai }),
  setGhiChu: (ghiChu) => set({ ghiChu }),
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
  setTileGiamGia: (tileGiamGia) => set({ tileGiamGia: Math.min(100, Math.max(0, tileGiamGia)) }),
  setTienGiamGia: (tienGiamGia) => set({ tienGiamGia: Math.max(0, tienGiamGia) }),
  setTienThue: (tienThue) => set({ tienThue: Math.max(0, tienThue) }),
  setPhiVanChuyen: (phiVanChuyen) => set({ phiVanChuyen: Math.max(0, phiVanChuyen) }),
  setKhachDua: (khachDua) => set({ khachDua: Math.max(0, khachDua) }),
  resetCart: () => set({
    lines: [], khachHangId: null, nhanVienId: null, dienGiai: '', ghiChu: '',
    tileGiamGia: 0, tienGiamGia: 0, tienThue: 0, phiVanChuyen: 0, khachDua: 0,
  }),
}));