import React, { useEffect, useState } from 'react';
import { Avatar, Badge, Button, Drawer, Layout, Menu, Dropdown } from 'antd';
import { useLocation, useNavigate } from 'react-router-dom';
import {
  AppstoreOutlined, AuditOutlined, BarChartOutlined, BellOutlined, CustomerServiceOutlined, DatabaseOutlined,
  HistoryOutlined, HomeOutlined, LogoutOutlined, MailOutlined, MenuOutlined,
  SafetyCertificateOutlined, SettingOutlined, ShoppingOutlined, SyncOutlined,
  TeamOutlined, UserOutlined, WalletOutlined
} from '@ant-design/icons';
import { useAuthStore } from '../stores/authStore';
import { useNotificationStore } from '../stores/notificationStore';

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
      { key: '/payments/momo', label: 'Thanh toán MoMo' },
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
  },
  {
    key: '/crm-group', icon: <CustomerServiceOutlined />, label: 'CHĂM SÓC KHÁCH HÀNG', children: [
      { key: '/crm', label: 'Chăm sóc · Phản hồi · Tin nhắn · Điểm' },
    ]
  }
  // HỆ THỐNG / QUẢN TRỊ / TRỢ GIÚP — xem ADMIN_MENU_ITEMS
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
  { key: '/admin/users', icon: <SafetyCertificateOutlined />, label: 'NGƯỜI DÙNG & PHÂN QUYỀN' },
  { key: '/settings', icon: <SettingOutlined />, label: 'CÀI ĐẶT HỆ THỐNG' }
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

/** Tiêu đề trang dùng chung — hiện "tên mục › tên trang" ở đầu mọi trang với huy hiệu vàng kim. */
export const PageHeader: React.FC = () => {
  const location = useLocation();
  const { group, page } = findGroupAndPage(location.pathname);
  if (!page) return null;
  return (
    <div style={{ marginBottom: 20, display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 12 }}>
      <div>
        {group && (
          <div style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: 6,
            background: '#FFF9E6',
            color: '#8A6100',
            fontSize: 11,
            fontWeight: 700,
            letterSpacing: 1,
            padding: '2px 8px',
            borderRadius: 4,
            marginBottom: 6,
            border: '1px solid rgba(212, 175, 55, 0.3)'
          }}>
            <span>●</span> {group}
          </div>
        )}
        <div style={{
          fontSize: 22,
          fontWeight: 800,
          color: '#1C180E',
          letterSpacing: '-0.3px',
          display: 'flex',
          alignItems: 'center',
          gap: 10
        }}>
          {page}
        </div>
      </div>
    </div>
  );
};

const UserMenu: React.FC = () => {
  const user = useAuthStore((s) => s.user);
  const logout = useAuthStore((s) => s.logout);
  return (
    <Dropdown menu={{ items: [{ key: 'logout', icon: <LogoutOutlined />, label: 'Đăng xuất', onClick: logout }] }}>
      <div style={{
        display: 'flex',
        alignItems: 'center',
        gap: 8,
        cursor: 'pointer',
        padding: '4px 10px',
        borderRadius: 20,
        background: '#FFFDF5',
        border: '1px solid rgba(212, 175, 55, 0.3)',
        transition: 'all 0.2s ease'
      }}>
        <Avatar
          style={{
            background: 'linear-gradient(135deg, #FFE28A, #D4AF37)',
            color: '#1A1403',
            fontWeight: 700,
            boxShadow: '0 2px 6px rgba(212, 175, 55, 0.3)'
          }}
          icon={<UserOutlined />}
        />
        <span style={{ fontSize: 13, fontWeight: 600, color: '#403825' }}>
          {user?.fullName || user?.username || 'Tài khoản'}
        </span>
      </div>
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
      <div
        data-testid="notification-bell"
        style={{
          width: 36,
          height: 36,
          borderRadius: 8,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          cursor: 'pointer',
          background: unread > 0 ? '#FFF9E6' : '#FAF8F2',
          border: '1px solid rgba(212, 175, 55, 0.3)',
          transition: 'all 0.2s ease'
        }}
        onClick={() => navigate('/notifications')}
      >
        <BellOutlined style={{ fontSize: 17, color: unread > 0 ? '#D4AF37' : '#7E7359' }} />
      </div>
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
    <Layout style={{ minHeight: '100vh', background: '#FBFBFA' }}>
      {/* Sticky sider: phong cách White & Gold sang trọng */}
      <Sider
        collapsible
        theme="light"
        width={230}
        style={{
          position: 'sticky',
          top: 0,
          height: '100vh',
          overflow: 'hidden',
          background: '#FFFFFF',
          borderRight: '1px solid rgba(212, 175, 55, 0.22)',
          boxShadow: '2px 0 12px rgba(212, 175, 55, 0.05)'
        }}
      >
        <div style={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
          <div style={{
            display: 'flex',
            alignItems: 'center',
            gap: 10,
            padding: '16px 18px',
            flexShrink: 0,
            borderBottom: '1px solid #F4EEDC',
            background: 'linear-gradient(180deg, #FFFDF8 0%, #FFFFFF 100%)'
          }}>
            <img
              src="/logo.png"
              alt="HN logo"
              style={{
                width: 34,
                height: 34,
                borderRadius: 8,
                objectFit: 'contain',
                background: '#fff',
                padding: 2,
                border: '1px solid rgba(212, 175, 55, 0.4)',
                boxShadow: '0 2px 8px rgba(212, 175, 55, 0.25)',
                flexShrink: 0
              }}
            />
            <div style={{ overflow: 'hidden' }}>
              <div style={{ color: '#8A6100', fontWeight: 800, fontSize: 14, letterSpacing: '0.2px', whiteSpace: 'nowrap' }}>
                HOÀN NGỌC GROUP
              </div>
              <div style={{ fontSize: 11, color: '#A08D6A', whiteSpace: 'nowrap' }}>
                Quản lý bán hàng v2.0
              </div>
            </div>
          </div>
          <div style={{ flex: 1, minHeight: 0, overflowY: 'auto', overflowX: 'hidden', padding: '8px 4px' }}>
            <Menu
              theme="light"
              mode="inline"
              items={items}
              selectedKeys={[location.pathname]}
              defaultOpenKeys={[findGroupKey(location.pathname) ?? '']}
              onClick={(e) => { void navigate(e.key); }}
              style={{ borderRight: 'none', background: 'transparent' }}
            />
          </div>
        </div>
      </Sider>
      <Layout style={{ background: '#FBFBFA' }}>
        <Header style={{
          background: 'rgba(255, 255, 255, 0.88)',
          backdropFilter: 'blur(16px)',
          WebkitBackdropFilter: 'blur(16px)',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          gap: 16,
          paddingInline: 24,
          position: 'sticky',
          top: 0,
          zIndex: 100,
          borderBottom: '1px solid rgba(212, 175, 55, 0.2)',
          boxShadow: '0 2px 10px rgba(0, 0, 0, 0.02)',
          height: 60
        }}>
          {/* Lối tắt bán lẻ POS nhanh */}
          <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
            <Button
              type="primary"
              size="small"
              className="gold-gradient-btn"
              onClick={() => navigate('/pos/retail')}
            >
              ⚡ BÁN LẺ
            </Button>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: 14 }}>
            <NotificationBell />
            <UserMenu />
          </div>
        </Header>
        <Content style={{ padding: '24px 28px', maxWidth: 1600, margin: '0 auto', width: '100%' }}>
          {children}
        </Content>
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
    <Layout style={{ minHeight: '100vh', background: '#FBFBFA' }}>
      <Header style={{
        background: 'rgba(255, 255, 255, 0.92)',
        backdropFilter: 'blur(16px)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        paddingInline: 12,
        position: 'sticky',
        top: 0,
        zIndex: 100,
        borderBottom: '1px solid rgba(212, 175, 55, 0.2)'
      }}>
        <Button type="text" icon={<MenuOutlined />} onClick={() => setOpen(true)} />
        <b style={{ display: 'flex', alignItems: 'center', gap: 8, color: '#8A6100' }}>
          <img src="/logo.png" alt="HN logo" style={{ width: 26, height: 26, borderRadius: 6, objectFit: 'contain' }} />
          Hoàn Ngọc
        </b>
        <NotificationBell />
      </Header>
      <Content style={{ padding: 12, paddingBottom: 68 }}>{children}</Content>
      <nav style={{
        position: 'fixed',
        bottom: 0,
        left: 0,
        right: 0,
        background: 'rgba(255, 255, 255, 0.95)',
        backdropFilter: 'blur(12px)',
        borderTop: '1px solid rgba(212, 175, 55, 0.25)',
        display: 'flex',
        justifyContent: 'space-around',
        padding: '6px 0',
        zIndex: 100
      }}>
        {MOBILE_BOTTOM_NAV.map((m) => {
          const active = location.pathname === m.key;
          return (
            <div
              key={m.key}
              style={{
                textAlign: 'center',
                fontSize: 11,
                cursor: 'pointer',
                color: active ? '#8A6100' : '#888',
                fontWeight: active ? 700 : 500
              }}
              onClick={() => navigate(m.key)}
            >
              <div style={{ fontSize: 18, color: active ? '#D4AF37' : '#888' }}>{m.icon}</div>
              <div>{m.label}</div>
            </div>
          );
        })}
      </nav>
      <Drawer
        title={<span style={{ color: '#8A6100', fontWeight: 800 }}>Hoàn Ngọc Group</span>}
        placement="left"
        onClose={() => setOpen(false)}
        open={open}
      >
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