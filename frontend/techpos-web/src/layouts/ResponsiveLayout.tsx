import React, { useEffect, useState } from 'react';
import { Avatar, Badge, Button, Drawer, Layout, Menu, Dropdown } from 'antd';
import { useLocation, useNavigate } from 'react-router-dom';
import {
  AppstoreOutlined, AuditOutlined, BarChartOutlined, BellOutlined, DatabaseOutlined, HistoryOutlined, HomeOutlined,
  LogoutOutlined, MailOutlined, MenuOutlined, SettingOutlined, ShoppingOutlined, SyncOutlined,
  TeamOutlined, UserOutlined, WalletOutlined
} from '@ant-design/icons';
import { useAuthStore } from '../stores/authStore';
import { useNotificationStore } from '../stores/notificationStore';
import { BRAND } from '../app/brand';

const { Sider, Header, Content } = Layout;

export const useIsMobile = () => {
  const [isMobile, setIsMobile] = useState(() => window.innerWidth < 768);
  useEffect(() => {
    const onResize = () => setIsMobile(window.innerWidth < 768);
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);
  return isMobile;
};

/** Menu 10 nhóm — đúng cây menu desktop (DANH-MUC-CHUC-NANG.md) */
export const MENU_ITEMS = [
  { key: '/', icon: <HomeOutlined />, label: 'Trang chủ' },
  {
    key: '/catalog', icon: <DatabaseOutlined />, label: 'DANH MỤC', children: [
      { key: '/catalog/products', label: 'Mặt hàng' },
      { key: '/catalog', label: 'Khách hàng / NCC / Kho' },
    ]
  },
  {
    key: '/sales', icon: <ShoppingOutlined />, label: 'BÁN HÀNG', children: [
      { key: '/pos/retail', label: 'Bán lẻ (POS)' },
      { key: '/pos/wholesale', label: 'Bán sỉ' },
      { key: '/pos/returns', label: 'Khách trả lại' },
      { key: '/sales/quotation', label: 'Báo giá' },
      { key: '/sales/order', label: 'Khách đặt hàng' },
      { key: '/sales/contract', label: 'Hợp đồng' },
      { key: '/sales/cod', label: 'Giao COD' },
      { key: '/sales/vehicle-delivery', label: 'Giao hàng theo xe' },
    ]
  },
  {
    key: '/cashbook', icon: <WalletOutlined />, label: 'QUỸ', children: [
      { key: '/cashbook', label: 'Phiếu thu / chi · Tồn quỹ' },
    ]
  },
  {
    key: '/debt', icon: <AuditOutlined />, label: 'CÔNG NỢ', children: [
      { key: '/debt', label: 'Công nợ KH / NCC' },
    ]
  },
  {
    key: '/hr', icon: <TeamOutlined />, label: 'NHÂN SỰ', children: [
      { key: '/hr/employees', label: 'Danh mục nhân viên' },
      { key: '/hr/intro-letters', label: 'Giấy giới thiệu' },
      { key: '/hr/payroll', label: 'Tính lương · Tạm ứng' },
      { key: '/hr/attendance', label: 'Chấm công' },
      { key: '/hr/bonus', label: 'Thưởng phạt' },
    ]
  },
  {
    key: '/inventory', icon: <AppstoreOutlined />, label: 'KHO HÀNG', children: [
      { key: '/inventory', label: 'Tồn kho / Thẻ kho' },
      { key: '/inventory/all-warehouses', label: 'Tồn tất cả các kho' },
      { key: '/purchases/orders', label: 'Đặt mua NCC' },
      { key: '/inventory/receipts', label: 'Nhập kho' },
      { key: '/inventory/issues', label: 'Xuất kho' },
      { key: '/inventory/transfers', label: 'Chuyển kho' },
      { key: '/inventory/supplier-returns', label: 'Xuất trả NCC' },
      { key: '/inventory/stocktakes', label: 'Kiểm kê' },
      { key: '/inventory/cost-recalc', label: 'Tính lại giá vốn' },
    ]
  },
  {
    key: '/reports', icon: <BarChartOutlined />, label: 'BÁO CÁO', children: [
      { key: '/reports', label: 'Report Center (6 báo cáo + Excel)' },
    ]
  }
  // HỆ THỐNG / CHĂM SÓC KH / QUẢN TRỊ / TRỢ GIÚP — bổ sung theo phase 7–8
];

/** Menu Lịch sử — chỉ role có quyền xem audit mới thấy (ADMIN.AUDIT.VIEW). */
export const HISTORY_MENU_ITEMS = [
  {
    key: '/history', icon: <HistoryOutlined />, label: 'LỊCH SỬ', children: [
      { key: '/history', label: 'Lịch sử chứng từ' },
    ]
  }
];

/** Bottom nav mobile cố định 5 mục (mục 46). */
export const MOBILE_BOTTOM_NAV = [
  { key: '/', icon: <HomeOutlined />, label: 'Trang chủ' },
  { key: '/catalog', icon: <DatabaseOutlined />, label: 'Danh mục' },
  { key: '/sales', icon: <ShoppingOutlined />, label: 'Bán' },
  { key: '/inventory', icon: <AppstoreOutlined />, label: 'Kho' },
  { key: '/notifications', icon: <BellOutlined />, label: '🔔' }
];

/** Menu mở rộng — chỉ SUPER_ADMIN/ADMIN/IT mới thấy (mục 37, FRONTEND-STRUCTURE §2). */
export const ADMIN_MENU_ITEMS = [
  { key: '/sync-admin', icon: <SyncOutlined />, label: 'SYNC ADMIN' },
  { key: '/emails', icon: <MailOutlined />, label: 'EMAIL / THÔNG BÁO' },
  { key: '/admin', icon: <SettingOutlined />, label: 'QUẢN TRỊ' }
];

const hasAdminRole = (roles: string[]) =>
  roles.includes('SUPER_ADMIN') || roles.includes('ADMIN') || roles.includes('IT');

const EMPTY_ROLES: string[] = [];

/** Nhóm menu chứa route hiện tại — dùng để mở sẵn submenu đúng trang đang mở. */
const findGroupKey = (pathname: string): string | undefined => {
  for (const item of MENU_ITEMS) {
    if (item.children?.some((c) => c.key === pathname)) return item.key;
  }
  if (ADMIN_MENU_ITEMS.some((i) => i.key === pathname)) return pathname;
  return undefined;
};

/** Tên nhóm mục (vd "BÁN HÀNG") + tên trang (vd "Bán sỉ") theo route hiện tại. */
type MenuLike = { key: string; label: string; children?: { key: string; label: string }[] };
export const findGroupAndPage = (pathname: string): { group: string | undefined; page: string | undefined } => {
  for (const item of [...MENU_ITEMS, ...ADMIN_MENU_ITEMS] as MenuLike[]) {
    if (item.key === pathname) return { group: undefined, page: item.label };
    if (item.children) {
      const child = item.children.find((c) => c.key === pathname);
      if (child) return { group: item.label, page: child.label };
    }
  }
  return { group: undefined, page: undefined };
};

/** Tiêu đề trang dùng chung — hiện "tên mục › tên trang" ở đầu mọi trang. */
export const PageHeader: React.FC = () => {
  const location = useLocation();
  const { group, page } = findGroupAndPage(location.pathname);
  if (!page) return null;
  return (
    <div style={{ marginBottom: 16 }}>
      {group && <div style={{ color: '#8a6100', fontSize: 12, fontWeight: 600, letterSpacing: 1 }}>{group}</div>}
      <div style={{ fontSize: 20, fontWeight: 700, color: '#333' }}>{page}</div>
    </div>
  );
};

const UserMenu: React.FC = () => {
  const logout = useAuthStore((s) => s.logout);
  return (
    <Dropdown menu={{ items: [{ key: 'logout', icon: <LogoutOutlined />, label: 'Đăng xuất', onClick: logout }] }}>
      <Avatar style={{ backgroundColor: BRAND.primary }} icon={<UserOutlined />} />
    </Dropdown>
  );
};

/** Chuông thông báo — badge unread (poll 30s trong notificationStore) + link /notifications. */
const NotificationBell: React.FC = () => {
  const unread = useNotificationStore((s) => s.unread);
  const startPolling = useNotificationStore((s) => s.startPolling);
  const navigate = useNavigate();

  useEffect(() => {
    startPolling();
  }, [startPolling]);

  return (
    <Badge count={unread} size="small" overflowCount={99}>
      <BellOutlined data-testid="notification-bell" style={{ fontSize: 18, cursor: 'pointer' }}
        onClick={() => navigate('/notifications')} />
    </Badge>
  );
};

export const DesktopLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const roles = useAuthStore(s => s.user?.roles ?? EMPTY_ROLES);
  const isAdmin = hasAdminRole(roles);
  const navigate = useNavigate();
  const location = useLocation();
  const items = isAdmin ? [...MENU_ITEMS, ...HISTORY_MENU_ITEMS, ...ADMIN_MENU_ITEMS] : MENU_ITEMS;
  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider collapsible theme="dark" width={220}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, padding: 16 }}>
          <img src="/logo.png" alt="HN logo" style={{ width: 32, height: 32, borderRadius: 6, objectFit: 'contain', background: '#fff' }} />
          <span style={{ color: '#fff', fontWeight: 700 }}>Hoàn Ngọc Group</span>
        </div>
        <Menu
          theme="dark"
          mode="inline"
          items={items}
          selectedKeys={[location.pathname]}
          defaultOpenKeys={[findGroupKey(location.pathname) ?? '']}
          onClick={(e) => { void navigate(e.key); }}
        />
      </Sider>
      <Layout>
        <Header style={{ background: '#fff', display: 'flex', justifyContent: 'flex-end', alignItems: 'center', gap: 16, paddingInline: 24 }}>
          <NotificationBell />
          <UserMenu />
        </Header>
        <Content style={{ padding: 24 }}>{children}</Content>
      </Layout>
    </Layout>
  );
};

export const MobileLayout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [open, setOpen] = useState(false);
  const roles = useAuthStore(s => s.user?.roles ?? EMPTY_ROLES);
  const isAdmin = hasAdminRole(roles);
  const navigate = useNavigate();
  const location = useLocation();
  const drawerItems = isAdmin ? [...MENU_ITEMS, ...HISTORY_MENU_ITEMS, ...ADMIN_MENU_ITEMS] : MENU_ITEMS;
  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ background: '#fff', display: 'flex', alignItems: 'center', justifyContent: 'space-between', paddingInline: 12, position: 'sticky', top: 0, zIndex: 10 }}>
        <Button type="text" icon={<MenuOutlined />} onClick={() => setOpen(true)} />
        <b style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <img src="/logo.png" alt="HN logo" style={{ width: 24, height: 24, borderRadius: 4, objectFit: 'contain' }} />
          Hoàn Ngọc
        </b>
        <NotificationBell />
      </Header>
      <Content style={{ padding: 12, paddingBottom: 64 }}>{children}</Content>
      <nav style={{ position: 'fixed', bottom: 0, left: 0, right: 0, background: '#fff', borderTop: '1px solid #eee', display: 'flex', justifyContent: 'space-around', padding: '6px 0' }}>
        {MOBILE_BOTTOM_NAV.map((m) => <div key={m.key} style={{ textAlign: 'center', fontSize: 11 }}>{m.icon}<div>{m.label}</div></div>)}
      </nav>
      <Drawer title="Hoàn Ngọc Group" placement="left" onClose={() => setOpen(false)} open={open}>
        <Menu
          mode="inline"
          items={drawerItems}
          selectedKeys={[location.pathname]}
          defaultOpenKeys={[findGroupKey(location.pathname) ?? '']}
          onClick={(e) => { setOpen(false); void navigate(e.key); }}
        />
      </Drawer>
    </Layout>
  );
};