import React, { useCallback, useEffect, useState } from 'react';
import { Alert, Button, DatePicker, Space, Table, Tabs, message } from 'antd';
import { DownloadOutlined } from '@ant-design/icons';
import dayjs, { Dayjs } from 'dayjs';
import {
  exportExcel, fmtVnd,
  getCashByDay, getCashByReason, getSalesByDay, getSalesByItem, getXnt,
  getCustomerDebts, getSupplierDebts, getCashBalance, getKqkd,
  type CashByDayRowDto, type CashByReasonRowDto, type SalesByDayRowDto,
  type SalesByItemRowDto, type XntRowDto,
  type CustomerDebtRowDto, type SupplierDebtRowDto, type CashBalanceRowDto, type KqkdDto,
} from '../../api/reports';
import { getWarehouses, type Warehouse } from '../../api/warehouses';

/** Bộ lọc kỳ chung: 2 DatePicker + nút Xem + nút Xuất Excel. */
const RangeFilter: React.FC<{
  from: Dayjs | null; to: Dayjs | null;
  onChange: (f: Dayjs | null, t: Dayjs | null) => void;
  onView: () => void; onExport?: () => void; exporting?: boolean;
  extra?: React.ReactNode;
}> = ({ from, to, onChange, onView, onExport, exporting, extra }) => (
  <Space wrap>
    <DatePicker placeholder="Từ ngày" value={from} format="DD/MM/YYYY"
      onChange={(v) => onChange(v, to)} />
    <DatePicker placeholder="Đến ngày" value={to} format="DD/MM/YYYY"
      onChange={(v) => onChange(from, v)} />
    <Button type="primary" onClick={onView}>Xem</Button>
    {onExport && <Button icon={<DownloadOutlined />} loading={exporting} onClick={onExport}>Xuất Excel</Button>}
    {extra}
  </Space>
);

// ---------------- Bán hàng theo ngày ----------------

const SalesByDayTab: React.FC = () => {
  const [rows, setRows] = useState<SalesByDayRowDto[]>([]);
  const [from, setFrom] = useState<Dayjs | null>(null);
  const [to, setTo] = useState<Dayjs | null>(null);
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const load = useCallback(async (f?: string, t?: string) => {
    setLoading(true); setErr(null);
    try { setRows(await getSalesByDay({ from: f, to: t })); }
    catch (e: unknown) { setErr(e instanceof Error ? e.message : 'Lỗi tải báo cáo'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(); }, [load]);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <RangeFilter from={from} to={to}
        onChange={(f, t) => { setFrom(f); setTo(t); }}
        onView={() => void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'))}
        onExport={() => void exportExcel('sales-by-day/export',
          { from: from?.format('YYYY-MM-DD'), to: to?.format('YYYY-MM-DD') }, 'ban-hang-theo-ngay.xlsx')}
        exporting={false} />
      {err && <Alert type="error" showIcon message={err} />}
      <Table size="small" rowKey="ngay" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Ngày', dataIndex: 'ngay', width: 120, render: (v: string) => dayjs(v).format('DD/MM/YYYY') },
          { title: 'Số hoá đơn', dataIndex: 'soHoaDon', width: 110, align: 'right' },
          { title: 'Tiền hàng', dataIndex: 'tienHang', width: 140, align: 'right', render: fmtVnd },
          { title: 'Giảm giá', dataIndex: 'tienGiamGia', width: 130, align: 'right', render: fmtVnd },
          { title: 'Tổng cộng', dataIndex: 'tongCong', width: 150, align: 'right',
            render: (v: number) => <b style={{ color: '#cf1322' }}>{fmtVnd(v)}</b> },
          { title: 'Còn nợ', dataIndex: 'conNo', width: 140, align: 'right',
            render: (v: number) => v > 0 ? <b style={{ color: '#cf1322' }}>{fmtVnd(v)}</b> : fmtVnd(v) },
        ]}
        summary={(data) => data.length > 0 ? (
          <Table.Summary.Row>
            <Table.Summary.Cell index={0} colSpan={2}><b>TỔNG CỘNG</b></Table.Summary.Cell>
            <Table.Summary.Cell index={1} align="right"><b>{fmtVnd(data.reduce((s, r) => s + r.tienHang, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={2} align="right"><b>{fmtVnd(data.reduce((s, r) => s + r.tienGiamGia, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={3} align="right"><b style={{ color: '#cf1322' }}>{fmtVnd(data.reduce((s, r) => s + r.tongCong, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={4} align="right"><b style={{ color: '#cf1322' }}>{fmtVnd(data.reduce((s, r) => s + r.conNo, 0))}</b></Table.Summary.Cell>
          </Table.Summary.Row>
        ) : null} />
    </Space>
  );
};

// ---------------- Mặt hàng bán chạy ----------------

const SalesByItemTab: React.FC = () => {
  const [rows, setRows] = useState<SalesByItemRowDto[]>([]);
  const [from, setFrom] = useState<Dayjs | null>(null);
  const [to, setTo] = useState<Dayjs | null>(null);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (f?: string, t?: string) => {
    setLoading(true);
    try { setRows(await getSalesByItem({ from: f, to: t, top: 200 })); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi tải báo cáo'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(); }, [load]);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <RangeFilter from={from} to={to}
        onChange={(f, t) => { setFrom(f); setTo(t); }}
        onView={() => void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'))}
        onExport={() => void exportExcel('sales-by-item/export',
          { from: from?.format('YYYY-MM-DD'), to: to?.format('YYYY-MM-DD') }, 'mat-hang-ban-chay.xlsx')} />
      <Table size="small" rowKey="matHangId" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Mã', dataIndex: 'code', width: 120, render: (v: string | null) => v ?? '—' },
          { title: 'Tên mặt hàng', dataIndex: 'name' },
          { title: 'ĐVT', dataIndex: 'donViTinh', width: 90 },
          { title: 'Số lượng', dataIndex: 'soLuong', width: 110, align: 'right',
            render: (v: number) => v.toLocaleString('vi-VN') },
          { title: 'Thành tiền', dataIndex: 'thanhTien', width: 150, align: 'right',
            render: (v: number) => <b>{fmtVnd(v)}</b> },
        ]} />
    </Space>
  );
};

// ---------------- Thu chi theo ngày / lý do ----------------

const CashByDayTab: React.FC = () => {
  const [rows, setRows] = useState<CashByDayRowDto[]>([]);
  const [from, setFrom] = useState<Dayjs | null>(null);
  const [to, setTo] = useState<Dayjs | null>(null);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (f?: string, t?: string) => {
    setLoading(true);
    try { setRows(await getCashByDay({ from: f, to: t })); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi tải báo cáo'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(); }, [load]);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <RangeFilter from={from} to={to}
        onChange={(f, t) => { setFrom(f); setTo(t); }}
        onView={() => void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'))}
        onExport={() => void exportExcel('cash-by-day/export',
          { from: from?.format('YYYY-MM-DD'), to: to?.format('YYYY-MM-DD') }, 'thu-chi-theo-ngay.xlsx')} />
      <Table size="small" rowKey="ngay" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Ngày', dataIndex: 'ngay', width: 130, render: (v: string) => dayjs(v).format('DD/MM/YYYY') },
          { title: 'Thu', dataIndex: 'thu', width: 160, align: 'right',
            render: (v: number) => <b style={{ color: '#389e0d' }}>{fmtVnd(v)}</b> },
          { title: 'Chi', dataIndex: 'chi', width: 160, align: 'right',
            render: (v: number) => <b style={{ color: '#cf1322' }}>{fmtVnd(v)}</b> },
        ]}
        summary={(data) => data.length > 0 ? (
          <Table.Summary.Row>
            <Table.Summary.Cell index={0}><b>TỔNG</b></Table.Summary.Cell>
            <Table.Summary.Cell index={1} align="right"><b style={{ color: '#389e0d' }}>{fmtVnd(data.reduce((s, r) => s + r.thu, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={2} align="right"><b style={{ color: '#cf1322' }}>{fmtVnd(data.reduce((s, r) => s + r.chi, 0))}</b></Table.Summary.Cell>
          </Table.Summary.Row>
        ) : null} />
    </Space>
  );
};

const CashByReasonTab: React.FC = () => {
  const [rows, setRows] = useState<CashByReasonRowDto[]>([]);
  const [from, setFrom] = useState<Dayjs | null>(null);
  const [to, setTo] = useState<Dayjs | null>(null);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (f?: string, t?: string) => {
    setLoading(true);
    try { setRows(await getCashByReason({ from: f, to: t })); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi tải báo cáo'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(); }, [load]);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <RangeFilter from={from} to={to}
        onChange={(f, t) => { setFrom(f); setTo(t); }}
        onView={() => void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'))}
        onExport={() => void exportExcel('cash-by-reason/export',
          { from: from?.format('YYYY-MM-DD'), to: to?.format('YYYY-MM-DD') }, 'thu-chi-theo-ly-do.xlsx')} />
      <Table size="small" rowKey="lyDo" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Lý do', dataIndex: 'lyDo' },
          { title: 'Thu', dataIndex: 'thu', width: 160, align: 'right',
            render: (v: number) => v !== 0 ? <b style={{ color: '#389e0d' }}>{fmtVnd(v)}</b> : '—' },
          { title: 'Chi', dataIndex: 'chi', width: 160, align: 'right',
            render: (v: number) => v !== 0 ? <b style={{ color: '#cf1322' }}>{fmtVnd(v)}</b> : '—' },
        ]} />
    </Space>
  );
};

// ---------------- Xuất – nhập – tồn ----------------

const XntTab: React.FC = () => {
  const [rows, setRows] = useState<XntRowDto[]>([]);
  const [from, setFrom] = useState<Dayjs | null>(null);
  const [to, setTo] = useState<Dayjs | null>(null);
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [khoId, setKhoId] = useState<string | undefined>(undefined);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (f?: string, t?: string, k?: string) => {
    setLoading(true);
    try { setRows(await getXnt({ from: f, to: t, khoId: k })); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi tải báo cáo'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => { getWarehouses({ activeOnly: true, pageSize: 50 }).then((r) => setWarehouses(r.items)).catch(() => { }); }, []);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <Space wrap>
        <DatePicker placeholder="Từ ngày" value={from} format="DD/MM/YYYY" onChange={(v) => setFrom(v)} />
        <DatePicker placeholder="Đến ngày" value={to} format="DD/MM/YYYY" onChange={(v) => setTo(v)} />
        <select value={khoId ?? ''} onChange={(e) => setKhoId(e.target.value || undefined)}
          style={{ padding: '4px 8px', border: '1px solid #d9d9d9', borderRadius: 6 }}>
          <option value="">Tất cả kho</option>
          {warehouses.map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
        </select>
        <Button type="primary" onClick={() =>
          void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'), khoId)}>Xem</Button>
        <Button icon={<DownloadOutlined />} onClick={() =>
          void exportExcel('xnt/export',
            {
              from: from?.format('YYYY-MM-DD'), to: to?.format('YYYY-MM-DD'), khoId,
            }, 'xuat-nhap-ton.xlsx')}>Xuất Excel</Button>
      </Space>
      <Table size="small" rowKey="matHangId" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Mã', dataIndex: 'code', width: 110, render: (v: string | null) => v ?? '—' },
          { title: 'Tên mặt hàng', dataIndex: 'name' },
          { title: 'ĐVT', dataIndex: 'donViTinh', width: 80 },
          { title: 'Tồn đầu kỳ', dataIndex: 'tonDauKy', width: 110, align: 'right',
            render: (v: number) => v.toLocaleString('vi-VN') },
          { title: 'Nhập', dataIndex: 'slNhap', width: 100, align: 'right',
            render: (v: number) => v.toLocaleString('vi-VN') },
          { title: 'Xuất', dataIndex: 'slXuat', width: 100, align: 'right',
            render: (v: number) => v.toLocaleString('vi-VN') },
          { title: 'Tồn cuối', dataIndex: 'tonCuoi', width: 110, align: 'right',
            render: (v: number) => <b>{v.toLocaleString('vi-VN')}</b> },
          { title: 'Giá vốn', dataIndex: 'giaVon', width: 120, align: 'right', render: fmtVnd },
          { title: 'Giá trị tồn', dataIndex: 'giaTriTon', width: 150, align: 'right',
            render: (v: number) => <b>{fmtVnd(v)}</b> },
        ]} />
    </Space>
  );
};

// ---------------- Tồn kho (export từ OTONKHO) ----------------

const StockExportTab: React.FC = () => {
  const [exporting, setExporting] = useState(false);
  const doExport = async () => {
    setExporting(true);
    try { await exportExcel('stock/export', {}, 'ton-kho.xlsx'); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi xuất Excel'); }
    finally { setExporting(false); }
  };
  return (
    <Space direction="vertical" size={10}>
      <div style={{ color: '#666' }}>
        Xuất toàn bộ tồn kho hiện tại (OTONKHO join DMATHANG/DKHOHANG) ra file Excel.
      </div>
      <Button type="primary" icon={<DownloadOutlined />} loading={exporting} onClick={doExport}>
        Xuất Excel tồn kho
      </Button>
    </Space>
  );
};

// ---------------- Công nợ khách hàng / NCC (6.1) ----------------

const CustomerDebtsTab: React.FC = () => {
  const [rows, setRows] = useState<CustomerDebtRowDto[]>([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (s?: string) => {
    setLoading(true);
    try { setRows(await getCustomerDebts({ search: s })); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi tải báo cáo'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(); }, [load]);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <Space wrap>
        <input placeholder="Tìm khách hàng..." value={search}
          onChange={(e) => setSearch(e.target.value)}
          style={{ padding: '4px 8px', border: '1px solid #d9d9d9', borderRadius: 6 }} />
        <Button type="primary" onClick={() => void load(search)}>Xem</Button>
        <Button icon={<DownloadOutlined />} onClick={() =>
          void exportExcel('customer-debts/export', { search: search || undefined }, 'cong-no-khach-hang.xlsx')}>
          Xuất Excel
        </Button>
      </Space>
      <Table size="small" rowKey="khachHangId" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Mã khách', dataIndex: 'maKhach', width: 110, render: (v: string | null) => v ?? '—' },
          { title: 'Tên khách hàng', dataIndex: 'tenKhach' },
          { title: 'Điện thoại', dataIndex: 'dienThoai', width: 130 },
          { title: 'Số HĐ còn nợ', dataIndex: 'soHoaDon', width: 120, align: 'right' },
          { title: 'Số ngày cho nợ', dataIndex: 'songayChoNo', width: 130, align: 'right' },
          { title: 'Công nợ', dataIndex: 'congNo', width: 150, align: 'right',
            render: (v: number) => <b style={{ color: '#cf1322' }}>{fmtVnd(v)}</b> },
        ]}
        summary={(data) => data.length > 0 ? (
          <Table.Summary.Row>
            <Table.Summary.Cell index={0} colSpan={5}><b>TỔNG CÔNG NỢ</b></Table.Summary.Cell>
            <Table.Summary.Cell index={1} align="right">
              <b style={{ color: '#cf1322' }}>{fmtVnd(data.reduce((s, r) => s + r.congNo, 0))}</b>
            </Table.Summary.Cell>
          </Table.Summary.Row>
        ) : null} />
    </Space>
  );
};

const SupplierDebtsTab: React.FC = () => {
  const [rows, setRows] = useState<SupplierDebtRowDto[]>([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (s?: string) => {
    setLoading(true);
    try { setRows(await getSupplierDebts({ search: s })); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi tải báo cáo'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(); }, [load]);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <Space wrap>
        <input placeholder="Tìm nhà cung cấp..." value={search}
          onChange={(e) => setSearch(e.target.value)}
          style={{ padding: '4px 8px', border: '1px solid #d9d9d9', borderRadius: 6 }} />
        <Button type="primary" onClick={() => void load(search)}>Xem</Button>
        <Button icon={<DownloadOutlined />} onClick={() =>
          void exportExcel('supplier-debts/export', { search: search || undefined }, 'cong-no-nha-cung-cap.xlsx')}>
          Xuất Excel
        </Button>
      </Space>
      <Table size="small" rowKey="nhaCungCapId" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Mã NCC', dataIndex: 'maNhaCungCap', width: 110, render: (v: string | null) => v ?? '—' },
          { title: 'Tên nhà cung cấp', dataIndex: 'tenNhaCungCap' },
          { title: 'Điện thoại', dataIndex: 'dienThoai', width: 130 },
          { title: 'Số phiếu còn nợ', dataIndex: 'soPhieu', width: 130, align: 'right' },
          { title: 'Công nợ', dataIndex: 'congNo', width: 150, align: 'right',
            render: (v: number) => <b style={{ color: '#cf1322' }}>{fmtVnd(v)}</b> },
        ]}
        summary={(data) => data.length > 0 ? (
          <Table.Summary.Row>
            <Table.Summary.Cell index={0} colSpan={4}><b>TỔNG CÔNG NỢ</b></Table.Summary.Cell>
            <Table.Summary.Cell index={1} align="right">
              <b style={{ color: '#cf1322' }}>{fmtVnd(data.reduce((s, r) => s + r.congNo, 0))}</b>
            </Table.Summary.Cell>
          </Table.Summary.Row>
        ) : null} />
    </Space>
  );
};

// ---------------- Tồn quỹ theo ngày (6.1) ----------------

const CashBalanceTab: React.FC = () => {
  const [rows, setRows] = useState<CashBalanceRowDto[]>([]);
  const [from, setFrom] = useState<Dayjs | null>(null);
  const [to, setTo] = useState<Dayjs | null>(null);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (f?: string, t?: string) => {
    setLoading(true);
    try { setRows(await getCashBalance({ from: f, to: t })); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi tải báo cáo'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(); }, [load]);
  const last = rows.length > 0 ? rows[rows.length - 1].tonCuoiNgay : 0;

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <RangeFilter from={from} to={to}
        onChange={(f, t) => { setFrom(f); setTo(t); }}
        onView={() => void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'))}
        onExport={() => void exportExcel('cash-balance/export',
          { from: from?.format('YYYY-MM-DD'), to: to?.format('YYYY-MM-DD') }, 'ton-quy-theo-ngay.xlsx')} />
      <Table size="small" rowKey="ngay" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Ngày', dataIndex: 'ngay', width: 130, render: (v: string) => dayjs(v).format('DD/MM/YYYY') },
          { title: 'Thu', dataIndex: 'thu', width: 160, align: 'right',
            render: (v: number) => <b style={{ color: '#389e0d' }}>{fmtVnd(v)}</b> },
          { title: 'Chi', dataIndex: 'chi', width: 160, align: 'right',
            render: (v: number) => <b style={{ color: '#cf1322' }}>{fmtVnd(v)}</b> },
          { title: 'Tồn cuối ngày', dataIndex: 'tonCuoiNgay', width: 180, align: 'right',
            render: (v: number) => <b>{fmtVnd(v)}</b> },
        ]}
        summary={rows.length > 0 ? () => (
          <Table.Summary.Row>
            <Table.Summary.Cell index={0} colSpan={3}><b>TỒN QUỸ CUỐI KỲ</b></Table.Summary.Cell>
            <Table.Summary.Cell index={1} align="right"><b style={{ color: '#cf1322' }}>{fmtVnd(last)}</b></Table.Summary.Cell>
          </Table.Summary.Row>
        ) : undefined} />
    </Space>
  );
};

// ---------------- Kết quả kinh doanh (6.1) ----------------

const KqkdTab: React.FC = () => {
  const [data, setData] = useState<KqkdDto | null>(null);
  const [from, setFrom] = useState<Dayjs | null>(null);
  const [to, setTo] = useState<Dayjs | null>(null);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (f?: string, t?: string) => {
    setLoading(true);
    try { setData(await getKqkd({ from: f, to: t })); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi tải báo cáo'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(); }, [load]);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <RangeFilter from={from} to={to}
        onChange={(f, t) => { setFrom(f); setTo(t); }}
        onView={() => void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'))}
        exporting={loading} />
      {data && (
        <Table size="small" pagination={false} dataSource={[
          { k: 'Doanh thu', v: data.doanhThu },
          { k: 'Giảm giá', v: -data.giamGia },
          { k: 'Giá vốn hàng bán', v: -data.giaVon },
          { k: 'Chi phí (thu chi quỹ)', v: -data.chiPhi },
          { k: 'LỢI NHUẬN', v: data.loiNhuan },
        ]} rowKey="k"
          columns={[
            { title: 'Chỉ tiêu', dataIndex: 'k' },
            { title: 'Giá trị', dataIndex: 'v', width: 200, align: 'right',
              render: (v: number) => <b style={{ color: '#cf1322' }}>{fmtVnd(v)}</b> },
          ]} />
      )}
    </Space>
  );
};

/** Report Center (Phase 6 — mục 6.5): 6 báo cáo vận hành + xuất Excel. */
const ReportsPage: React.FC = () => (
  <Tabs items={[
    { key: 'sales-day', label: 'Bán hàng theo ngày', children: <SalesByDayTab /> },
    { key: 'sales-item', label: 'Mặt hàng bán chạy', children: <SalesByItemTab /> },
    { key: 'cash-day', label: 'Thu chi theo ngày', children: <CashByDayTab /> },
    { key: 'cash-reason', label: 'Thu chi theo lý do', children: <CashByReasonTab /> },
    { key: 'xnt', label: 'Xuất – nhập – tồn', children: <XntTab /> },
    { key: 'stock', label: 'Tồn kho (Excel)', children: <StockExportTab /> },
    { key: 'cust-debt', label: 'Công nợ KH', children: <CustomerDebtsTab /> },
    { key: 'supp-debt', label: 'Công nợ NCC', children: <SupplierDebtsTab /> },
    { key: 'cash-balance', label: 'Tồn quỹ', children: <CashBalanceTab /> },
    { key: 'kqkd', label: 'Kết quả kinh doanh', children: <KqkdTab /> },
  ]} />
);

export default ReportsPage;
