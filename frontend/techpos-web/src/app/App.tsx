import React from 'react';
import { BrowserRouter, Navigate, Outlet, Route, Routes } from 'react-router-dom';
import { App as AntdApp, ConfigProvider } from 'antd';
import { antdThemeConfig } from './brand';
import viVN from 'antd/locale/vi_VN';
import dayjs from 'dayjs';
import 'dayjs/locale/vi';
import { DesktopLayout, MobileLayout, PageHeader, useIsMobile } from '../layouts/ResponsiveLayout';
import LoginPage from '../features/auth/LoginPage';
import DashboardPage from '../features/dashboard/DashboardPage';
import SyncAdminPage from '../features/sync-admin/SyncAdminPage';
import ProductsPage from '../features/catalog/ProductsPage';
import CatalogPage from '../features/catalog/CatalogPage';
import RetailSalePage from '../features/pos/RetailSalePage';
import WholesalePage from '../features/pos/WholesalePage';
import ReturnsPage from '../features/pos/ReturnsPage';
import QuotationPage from '../features/sales/QuotationPage';
import SalesOrderPage from '../features/sales/SalesOrderPage';
import ContractsPage from '../features/sales/ContractsPage';
import PurchaseOrdersPage from '../features/sales/PurchaseOrdersPage';
import CodPage from '../features/sales/CodPage';
import VehicleDeliveriesPage from '../features/sales/VehicleDeliveriesPage';
import InventoryPage from '../features/inventory/InventoryPage';
import ReceiptsPage from '../features/inventory/ReceiptsPage';
import IssuesPage from '../features/inventory/IssuesPage';
import TransfersPage from '../features/inventory/TransfersPage';
import SupplierReturnsPage from '../features/inventory/SupplierReturnsPage';
import StocktakesPage from '../features/inventory/StocktakesPage';
import AllWarehousesPage from '../features/inventory/AllWarehousesPage';
import CostRecalcPage from '../features/inventory/CostRecalcPage';
import CashbookPage from '../features/cashbook/CashbookPage';
import DebtsPage from '../features/debt/DebtsPage';
import EmployeesPage from '../features/hr/EmployeesPage';
import AttendancePage from '../features/hr/AttendancePage';
import PayrollPage from '../features/hr/PayrollPage';
import BonusPenaltyPage from '../features/hr/BonusPenaltyPage';
import IntroLettersPage from '../features/hr/IntroLettersPage';
import ReportsPage from '../features/reports/ReportsPage';
import NotificationsPage from '../features/notifications/NotificationsPage';
import EmailsPage from '../features/notifications/EmailsPage';
import HistoryPage from '../features/history/HistoryPage';
import { useAuthStore } from '../stores/authStore';

dayjs.locale('vi');

const ProtectedRoute: React.FC = () => {
  const token = useAuthStore((s) => s.token);
  return token ? <Outlet /> : <Navigate to="/login" replace />;
};

const ShellRoute: React.FC = () => {
  const isMobile = useIsMobile();
  const Layout = isMobile ? MobileLayout : DesktopLayout;
  return (
    <Layout>
      <PageHeader />
      <Outlet />
    </Layout>
  );
};

const App: React.FC = () => (
  <ConfigProvider locale={viVN} theme={antdThemeConfig}>
    <AntdApp>
      <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<ProtectedRoute />}>
          <Route element={<ShellRoute />}>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/catalog" element={<CatalogPage />} />
            <Route path="/catalog/products" element={<ProductsPage />} />
            <Route path="/pos/retail" element={<RetailSalePage />} />
            <Route path="/pos/wholesale" element={<WholesalePage />} />
            <Route path="/pos/returns" element={<ReturnsPage />} />
            <Route path="/sales/quotation" element={<QuotationPage />} />
            <Route path="/sales/order" element={<SalesOrderPage />} />
            <Route path="/sales/contract" element={<ContractsPage />} />
            <Route path="/sales/cod" element={<CodPage />} />
            <Route path="/sales/vehicle-delivery" element={<VehicleDeliveriesPage />} />
            <Route path="/purchases/orders" element={<PurchaseOrdersPage />} />
            <Route path="/inventory" element={<InventoryPage />} />
            <Route path="/inventory/all-warehouses" element={<AllWarehousesPage />} />
            <Route path="/inventory/cost-recalc" element={<CostRecalcPage />} />
            <Route path="/cashbook" element={<CashbookPage />} />
            <Route path="/debt" element={<DebtsPage />} />
            <Route path="/hr/employees" element={<EmployeesPage />} />
            <Route path="/hr/attendance" element={<AttendancePage />} />
            <Route path="/hr/payroll" element={<PayrollPage />} />
            <Route path="/hr/bonus" element={<BonusPenaltyPage />} />
            <Route path="/hr/intro-letters" element={<IntroLettersPage />} />
            <Route path="/reports" element={<ReportsPage />} />
            <Route path="/notifications" element={<NotificationsPage />} />
            <Route path="/history" element={<HistoryPage />} />
            <Route path="/emails" element={<EmailsPage />} />
            <Route path="/inventory/receipts" element={<ReceiptsPage />} />
            <Route path="/inventory/issues" element={<IssuesPage />} />
            <Route path="/inventory/transfers" element={<TransfersPage />} />
            <Route path="/inventory/supplier-returns" element={<SupplierReturnsPage />} />
            <Route path="/inventory/stocktakes" element={<StocktakesPage />} />
            <Route path="/sync-admin" element={<SyncAdminPage />} />
            {/* Phase 4–5: bán hàng / kho / quỹ / công nợ / nhân sự / báo cáo — docs/architecture/FRONTEND-STRUCTURE.md */}
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Route>
      </Routes>
      </BrowserRouter>
    </AntdApp>
  </ConfigProvider>
);

export default App;