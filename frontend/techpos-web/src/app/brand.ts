import { theme } from 'antd';

/** Brand tokens — logo vàng gold HN, nền trắng chủ đạo (quyết định 05/10/2026). */
export const BRAND = {
  /** Vàng gold chủ đạo (lấy từ logo HN) */
  primary: '#d4a017',
  /** Vàng đậm cho text trên nền trắng — đảm bảo contrast đọc được */
  primaryDeep: '#8a6100',
  /** Vàng nhạt nền điểm nhấn */
  primarySoft: '#fffbe6',
  /** Trắng nền */
  bg: '#ffffff',
  bgLayout: '#faf7ef',
  border: '#f0e6c8',
} as const;

export const antdThemeConfig = {
  token: {
    colorPrimary: BRAND.primary,
    colorLink: BRAND.primary,
    colorInfo: BRAND.primary,
    colorBgLayout: BRAND.bgLayout,
    borderRadius: 6,
    fontSize: 14,
    colorText: '#433b22',
    colorTextHeading: '#2c2611',
  },
  components: {
    Layout: {
      siderBg: '#1f1a08',
      headerBg: BRAND.bg,
      bodyBg: BRAND.bgLayout,
    },
    Menu: {
      darkItemBg: 'transparent',
      darkItemSelectedBg: BRAND.primary,
      darkItemHoverBg: 'rgba(212,160,23,0.25)',
    },
    Button: {
      primaryShadow: 'none',
    },
  },
  algorithm: theme.defaultAlgorithm,
};
