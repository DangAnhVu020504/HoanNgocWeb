import { theme } from 'antd';

/**
 * Brand Tokens — Hoàn Ngọc Group
 * Phong cách "Royal Pearl & Champagne Gold" (Trắng Ngọc Trai & Vàng Kim Hoàng Gia).
 */
export const BRAND = {
  /** Vàng kim hoàng gia chủ đạo */
  primary: '#D4AF37',
  /** Vàng kim sáng khi hover */
  primaryHover: '#E5C158',
  /** Vàng đồng đậm khi active / click */
  primaryActive: '#B8860B',
  /** Vàng hổ phách đậm cho text trên nền trắng — đảm bảo độ tương phản WCAG AAA */
  primaryDeep: '#8A6100',
  /** Kem champagne nhạt cho nền badge, thẻ highlight, dòng được chọn */
  primarySoft: '#FFF9E6',
  /** Nền ô nhập liệu đang focus hoặc hover nhẹ */
  primarySurface: '#FFFDF0',
  /** Trắng ngọc trai tinh khiết cho Card, Modal, bề mặt thao tác */
  bg: '#FFFFFF',
  /** Nền tổng thể toàn trang (Canvas), sắc ấm dịu mắt */
  bgLayout: '#FBFBFA',
  /** Đường viền thanh lịch */
  border: '#EFE9D9',
  /** Đường viền ánh kim mạ vàng */
  borderGold: 'rgba(212, 175, 55, 0.28)',
} as const;

export const antdThemeConfig = {
  token: {
    colorPrimary: BRAND.primary,
    colorLink: BRAND.primary,
    colorLinkHover: BRAND.primaryHover,
    colorInfo: BRAND.primary,
    colorBgLayout: BRAND.bgLayout,
    colorBgContainer: BRAND.bg,
    borderRadius: 8,
    fontSize: 14,
    fontFamily: "'Plus Jakarta Sans', 'Be Vietnam Pro', system-ui, -apple-system, sans-serif",
    colorText: '#403825',
    colorTextHeading: '#1C180E',
    colorBorder: BRAND.border,
  },
  components: {
    Layout: {
      siderBg: '#FFFFFF',
      headerBg: 'rgba(255, 255, 255, 0.85)',
      bodyBg: BRAND.bgLayout,
    },
    Menu: {
      itemBg: 'transparent',
      itemSelectedBg: BRAND.primarySoft,
      itemSelectedColor: BRAND.primaryDeep,
      itemHoverBg: 'rgba(212, 175, 55, 0.08)',
      itemHoverColor: BRAND.primary,
      darkItemBg: '#16130B',
      darkItemSelectedBg: BRAND.primary,
      darkItemSelectedColor: '#1A1403',
      darkItemHoverBg: 'rgba(212, 175, 55, 0.2)',
    },
    Card: {
      headerBg: '#FFFFFF',
      colorBorderSecondary: 'rgba(212, 175, 55, 0.2)',
    },
    Table: {
      headerBg: '#FAF8F2',
      headerColor: BRAND.primaryDeep,
      rowHoverBg: '#FFFDF2',
      borderColor: '#F4EEDC',
    },
    Button: {
      primaryShadow: '0 4px 14px 0 rgba(212, 175, 55, 0.35)',
      borderRadius: 8,
    },
    Input: {
      activeBorderColor: BRAND.primary,
      hoverBorderColor: BRAND.primaryHover,
      borderRadius: 8,
    },
    Tabs: {
      itemSelectedColor: BRAND.primaryDeep,
      itemHoverColor: BRAND.primary,
      inkBarColor: BRAND.primary,
    },
    Badge: {
      colorPrimary: BRAND.primary,
    },
  },
  algorithm: theme.defaultAlgorithm,
};
