import React, { useCallback, useEffect, useState } from 'react';
import { Alert, Button, Card, Col, DatePicker, Row, Space, Table, Tabs, message } from 'antd';
import { DownloadOutlined, SearchOutlined } from '@ant-design/icons';
import dayjs, { Dayjs } from 'dayjs';
import {
  exportExcel, fmtVnd,
  getCashByDay, getCashByReason, getSalesByDay, getSalesByItem, getXnt,
  getCustomerDebts, getSupplierDebts, getCashBalance, getKqkd, getRevenueDetail,
  type CashByDayRowDto, type CashByReasonRowDto, type SalesByDayRowDto,
  type SalesByItemRowDto, type XntRowDto, type RevenueDetailRowDto,
  type CustomerDebtRowDto, type SupplierDebtRowDto, type CashBalanceRowDto, type KqkdDto,
} from '../../api/reports';
import { getWarehouses, type Warehouse } from '../../api/warehouses';

/** Bộ lọc kỳ chung: 2 DatePicker + nút Xem + nút Xuất Excel (White & Gold Luxury). */
const RangeFilter: React.FC<{
  from: Dayjs | null; to: Dayjs | null;
  onChange: (f: Dayjs | null, t: Dayjs | null) => void;
  onView: () => void; onExport?: () => void; exporting?: boolean;
  onQuick?: (f: Dayjs, t: Dayjs) => void;
  extra?: React.ReactNode;
}> = ({ from, to, onChange, onView, onExport, exporting, onQuick, extra }) => (
  <div className="luxury-card" style={{ padding: '12px 18px', marginBottom: 12 }}>
    <Space wrap size={12}>
      <DatePicker placeholder="Từ ngày" value={from} format="DD/MM/YYYY"
        onChange={(v) => onChange(v, to)} style={{ borderRadius: 8 }} />
      <DatePicker placeholder="Đến ngày" value={to} format="DD/MM/YYYY"
        onChange={(v) => onChange(from, v)} style={{ borderRadius: 8 }} />
      <Button type="primary" onClick={onView} className="gold-gradient-btn">
        Xem Dữ Liệu
      </Button>
      {onExport && (
        <Button icon={<DownloadOutlined />} loading={exporting} onClick={onExport} style={{ borderRadius: 8, fontWeight: 600 }}>
          Xuất File Excel
        </Button>
      )}
      {onQuick && (
        <Space size={4}>
          <Button size="small" onClick={() => onQuick(dayjs(), dayjs())}>Hôm nay</Button>
          <Button size="small" onClick={() => onQuick(dayjs().startOf('week'), dayjs())}>Tuần này</Button>
          <Button size="small" onClick={() => onQuick(dayjs().startOf('month'), dayjs())}>Tháng này</Button>
          <Button size="small" onClick={() => onQuick(dayjs().startOf('year'), dayjs())}>Năm nay</Button>
        </Space>
      )}
      {extra}
    </Space>
  </div>
);

const money = (v: number, cls = '') => <b className={`money ${cls}`}>{fmtVnd(v)}</b>;

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

  const quick = (f: Dayjs, t: Dayjs) => { setFrom(f); setTo(t); void load(f.format('YYYY-MM-DD'), t.format('YYYY-MM-DD')); };

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <RangeFilter from={from} to={to}
        onChange={(f, t) => { setFrom(f); setTo(t); }}
        onQuick={quick}
        onView={() => void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'))}
        onExport={() => void exportExcel('sales-by-day/export',
          { from: from?.format('YYYY-MM-DD'), to: to?.format('YYYY-MM-DD') }, 'ban-hang-theo-ngay.xlsx')}
        exporting={false} />
      {err && <Alert type="error" showIcon message={err} />}
      <Table className="luxury-table" size="middle" rowKey="ngay" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Ngày', dataIndex: 'ngay', width: 120, render: (v: string) => dayjs(v).format('DD/MM/YYYY') },
          { title: 'Số hoá đơn', dataIndex: 'soHoaDon', width: 110, align: 'right' },
          { title: 'Tiền hàng', dataIndex: 'tienHang', width: 140, align: 'right', render: (v: number) => money(v) },
          { title: 'Giảm giá', dataIndex: 'tienGiamGia', width: 130, align: 'right', render: (v: number) => money(v) },
          { title: 'Tổng cộng', dataIndex: 'tongCong', width: 150, align: 'right',
            render: (v: number) => money(v, 'money-neg') },
          { title: 'Còn nợ', dataIndex: 'conNo', width: 140, align: 'right',
            render: (v: number) => v > 0 ? money(v, 'money-neg') : money(v) },
        ]}
        summary={(data) => data.length > 0 ? (
          <Table.Summary.Row className="luxury-summary-row">
            <Table.Summary.Cell index={0} colSpan={2}><b style={{ color: '#8A6100' }}>TỔNG CỘNG</b></Table.Summary.Cell>
            <Table.Summary.Cell index={1} align="right"><b className="money">{fmtVnd(data.reduce((s, r) => s + r.tienHang, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={2} align="right"><b className="money">{fmtVnd(data.reduce((s, r) => s + r.tienGiamGia, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={3} align="right"><b className="money money-neg">{fmtVnd(data.reduce((s, r) => s + r.tongCong, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={4} align="right"><b className="money money-neg">{fmtVnd(data.reduce((s, r) => s + r.conNo, 0))}</b></Table.Summary.Cell>
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

  const quick = (f: Dayjs, t: Dayjs) => { setFrom(f); setTo(t); void load(f.format('YYYY-MM-DD'), t.format('YYYY-MM-DD')); };

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <RangeFilter from={from} to={to}
        onChange={(f, t) => { setFrom(f); setTo(t); }}
        onQuick={quick}
        onView={() => void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'))}
        onExport={() => void exportExcel('sales-by-item/export',
          { from: from?.format('YYYY-MM-DD'), to: to?.format('YYYY-MM-DD') }, 'mat-hang-ban-chay.xlsx')} />
      <Table className="luxury-table" size="middle" rowKey="matHangId" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Mã', dataIndex: 'code', width: 120, render: (v: string | null) => v ? <span className="code-badge">{v}</span> : '—' },
          { title: 'Tên mặt hàng', dataIndex: 'name', render: (v: string) => <b style={{ color: '#1C180E' }}>{v}</b> },
          { title: 'ĐVT', dataIndex: 'donViTinh', width: 90 },
          { title: 'Số lượng', dataIndex: 'soLuong', width: 110, align: 'right',
            render: (v: number) => v.toLocaleString('vi-VN') },
          { title: 'Thành tiền', dataIndex: 'thanhTien', width: 150, align: 'right',
            render: (v: number) => money(v) },
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

  const quick = (f: Dayjs, t: Dayjs) => { setFrom(f); setTo(t); void load(f.format('YYYY-MM-DD'), t.format('YYYY-MM-DD')); };

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <RangeFilter from={from} to={to}
        onChange={(f, t) => { setFrom(f); setTo(t); }}
        onQuick={quick}
        onView={() => void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'))}
        onExport={() => void exportExcel('cash-by-day/export',
          { from: from?.format('YYYY-MM-DD'), to: to?.format('YYYY-MM-DD') }, 'thu-chi-theo-ngay.xlsx')} />
      <Table className="luxury-table" size="middle" rowKey="ngay" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Ngày', dataIndex: 'ngay', width: 130, render: (v: string) => dayjs(v).format('DD/MM/YYYY') },
          { title: 'Thu', dataIndex: 'thu', width: 160, align: 'right',
            render: (v: number) => money(v, 'money-pos') },
          { title: 'Chi', dataIndex: 'chi', width: 160, align: 'right',
            render: (v: number) => money(v, 'money-neg') },
        ]}
        summary={(data) => data.length > 0 ? (
          <Table.Summary.Row className="luxury-summary-row">
            <Table.Summary.Cell index={0}><b style={{ color: '#8A6100' }}>TỔNG</b></Table.Summary.Cell>
            <Table.Summary.Cell index={1} align="right"><b className="money money-pos">{fmtVnd(data.reduce((s, r) => s + r.thu, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={2} align="right"><b className="money money-neg">{fmtVnd(data.reduce((s, r) => s + r.chi, 0))}</b></Table.Summary.Cell>
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

  const quick = (f: Dayjs, t: Dayjs) => { setFrom(f); setTo(t); void load(f.format('YYYY-MM-DD'), t.format('YYYY-MM-DD')); };

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <RangeFilter from={from} to={to}
        onChange={(f, t) => { setFrom(f); setTo(t); }}
        onQuick={quick}
        onView={() => void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'))}
        onExport={() => void exportExcel('cash-by-reason/export',
          { from: from?.format('YYYY-MM-DD'), to: to?.format('YYYY-MM-DD') }, 'thu-chi-theo-ly-do.xlsx')} />
      <Table className="luxury-table" size="middle" rowKey="lyDo" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Lý do', dataIndex: 'lyDo', render: (v: string) => <b style={{ color: '#1C180E' }}>{v}</b> },
          { title: 'Thu', dataIndex: 'thu', width: 160, align: 'right',
            render: (v: number) => v !== 0 ? <b className="money money-pos">{fmtVnd(v)}</b> : '—' },
          { title: 'Chi', dataIndex: 'chi', width: 160, align: 'right',
            render: (v: number) => v !== 0 ? <b className="money money-neg">{fmtVnd(v)}</b> : '—' },
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

  const quick = (f: Dayjs, t: Dayjs) => { setFrom(f); setTo(t); void load(f.format('YYYY-MM-DD'), t.format('YYYY-MM-DD'), khoId); };

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <RangeFilter from={from} to={to}
        onChange={(f, t) => { setFrom(f); setTo(t); }}
        onQuick={quick}
        onView={() => void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'), khoId)}
        onExport={() => void exportExcel('xnt/export',
          { from: from?.format('YYYY-MM-DD'), to: to?.format('YYYY-MM-DD'), khoId }, 'xuat-nhap-ton.xlsx')}
        extra={
          <select value={khoId ?? ''} onChange={(e) => setKhoId(e.target.value || undefined)}
            style={{ padding: '5px 10px', border: '1px solid rgba(212, 175, 55, 0.35)', borderRadius: 8, fontWeight: 600 }}>
            <option value="">Tất cả kho</option>
            {warehouses.map((w) => <option key={w.id} value={w.id}>{w.name}</option>)}
          </select>
        } />
      <Table className="luxury-table" size="middle" rowKey="matHangId" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }} scroll={{ x: 'max-content' }}
        columns={[
          { title: 'Mã', dataIndex: 'code', width: 110, render: (v: string | null) => v ? <span className="code-badge">{v}</span> : '—' },
          { title: 'Tên mặt hàng', dataIndex: 'name', render: (v: string) => <b style={{ color: '#1C180E' }}>{v}</b> },
          { title: 'ĐVT', dataIndex: 'donViTinh', width: 80 },
          { title: 'Tồn đầu kỳ', dataIndex: 'tonDauKy', width: 110, align: 'right',
            render: (v: number) => v.toLocaleString('vi-VN') },
          { title: 'Nhập', dataIndex: 'slNhap', width: 100, align: 'right',
            render: (v: number) => <span className="money money-pos">{v.toLocaleString('vi-VN')}</span> },
          { title: 'Xuất', dataIndex: 'slXuat', width: 100, align: 'right',
            render: (v: number) => <span className="money money-neg">{v.toLocaleString('vi-VN')}</span> },
          { title: 'Tồn cuối', dataIndex: 'tonCuoi', width: 110, align: 'right',
            render: (v: number) => <b className="money">{v.toLocaleString('vi-VN')}</b> },
          { title: 'Giá vốn', dataIndex: 'giaVon', width: 120, align: 'right', render: (v: number) => money(v) },
          { title: 'Giá trị tồn', dataIndex: 'giaTriTon', width: 150, align: 'right',
            render: (v: number) => money(v) },
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
    <Card className="luxury-card" title={<span className="card-title-gold">Xuất tồn kho ra Excel</span>}>
      <Space direction="vertical" size={12}>
        <div style={{ color: '#7E7359' }}>
          Xuất toàn bộ tồn kho hiện tại (OTONKHO join DMATHANG/DKHOHANG) ra file Excel.
        </div>
        <Button type="primary" icon={<DownloadOutlined />} loading={exporting} onClick={doExport} className="gold-gradient-btn">
          Xuất Excel tồn kho
        </Button>
      </Space>
    </Card>
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

  const totalDebt = rows.reduce((s, r) => s + r.congNo, 0);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={12}>
      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} md={8}>
          <div className="luxury-kpi-card">
            <div className="kpi-label">SỐ KHÁCH HÀNG ĐANG NỢ</div>
            <div className="kpi-value">{rows.length.toLocaleString('vi-VN')} <span style={{ fontSize: 16, fontWeight: 500, color: '#888' }}>khách</span></div>
          </div>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <div className="luxury-kpi-card">
            <div className="kpi-label">TỔNG TIỀN NỢ PHẢI THU</div>
            <div className="kpi-value" style={{ color: '#E11D48' }}>{fmtVnd(totalDebt)}</div>
          </div>
        </Col>
      </Row>
      <div className="luxury-card" style={{ padding: '12px 18px' }}>
        <Space wrap>
          <input placeholder="Tìm khách hàng..." value={search}
            onChange={(e) => setSearch(e.target.value)}
            style={{ padding: '6px 12px', border: '1px solid rgba(212, 175, 55, 0.35)', borderRadius: 8, minWidth: 240 }} />
          <Button type="primary" icon={<SearchOutlined />} onClick={() => void load(search)} className="gold-gradient-btn">Xem</Button>
          <Button icon={<DownloadOutlined />} onClick={() =>
            void exportExcel('customer-debts/export', { search: search || undefined }, 'cong-no-khach-hang.xlsx')}>
            Xuất Excel
          </Button>
        </Space>
      </div>
      <Table className="luxury-table" size="middle" rowKey="khachHangId" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Mã khách', dataIndex: 'maKhach', width: 110, render: (v: string | null) => v ? <span className="code-badge">{v}</span> : '—' },
          { title: 'Tên khách hàng', dataIndex: 'tenKhach', render: (v: string) => <b style={{ color: '#1C180E' }}>{v}</b> },
          { title: 'Điện thoại', dataIndex: 'dienThoai', width: 130 },
          { title: 'Số HĐ còn nợ', dataIndex: 'soHoaDon', width: 120, align: 'right' },
          { title: 'Số ngày cho nợ', dataIndex: 'songayChoNo', width: 130, align: 'right' },
          { title: 'Công nợ', dataIndex: 'congNo', width: 150, align: 'right',
            render: (v: number) => <b className="money money-neg">{fmtVnd(v)}</b> },
        ]}
        summary={(data) => data.length > 0 ? (
          <Table.Summary.Row className="luxury-summary-row">
            <Table.Summary.Cell index={0} colSpan={5}><b style={{ color: '#8A6100' }}>TỔNG CÔNG NỢ</b></Table.Summary.Cell>
            <Table.Summary.Cell index={1} align="right">
              <b className="money money-neg">{fmtVnd(data.reduce((s, r) => s + r.congNo, 0))}</b>
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

  const totalDebt = rows.reduce((s, r) => s + r.congNo, 0);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={12}>
      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} md={8}>
          <div className="luxury-kpi-card">
            <div className="kpi-label">SỐ NHÀ CUNG CẤP ĐANG NỢ</div>
            <div className="kpi-value">{rows.length.toLocaleString('vi-VN')} <span style={{ fontSize: 16, fontWeight: 500, color: '#888' }}>NCC</span></div>
          </div>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <div className="luxury-kpi-card">
            <div className="kpi-label">TỔNG TIỀN NỢ PHẢI TRẢ</div>
            <div className="kpi-value" style={{ color: '#E11D48' }}>{fmtVnd(totalDebt)}</div>
          </div>
        </Col>
      </Row>
      <div className="luxury-card" style={{ padding: '12px 18px' }}>
        <Space wrap>
          <input placeholder="Tìm nhà cung cấp..." value={search}
            onChange={(e) => setSearch(e.target.value)}
            style={{ padding: '6px 12px', border: '1px solid rgba(212, 175, 55, 0.35)', borderRadius: 8, minWidth: 240 }} />
          <Button type="primary" icon={<SearchOutlined />} onClick={() => void load(search)} className="gold-gradient-btn">Xem</Button>
          <Button icon={<DownloadOutlined />} onClick={() =>
            void exportExcel('supplier-debts/export', { search: search || undefined }, 'cong-no-nha-cung-cap.xlsx')}>
            Xuất Excel
          </Button>
        </Space>
      </div>
      <Table className="luxury-table" size="middle" rowKey="nhaCungCapId" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Mã NCC', dataIndex: 'maNhaCungCap', width: 110, render: (v: string | null) => v ? <span className="code-badge">{v}</span> : '—' },
          { title: 'Tên nhà cung cấp', dataIndex: 'tenNhaCungCap', render: (v: string) => <b style={{ color: '#1C180E' }}>{v}</b> },
          { title: 'Điện thoại', dataIndex: 'dienThoai', width: 130 },
          { title: 'Số phiếu còn nợ', dataIndex: 'soPhieu', width: 130, align: 'right' },
          { title: 'Công nợ', dataIndex: 'congNo', width: 150, align: 'right',
            render: (v: number) => <b className="money money-neg">{fmtVnd(v)}</b> },
        ]}
        summary={(data) => data.length > 0 ? (
          <Table.Summary.Row className="luxury-summary-row">
            <Table.Summary.Cell index={0} colSpan={4}><b style={{ color: '#8A6100' }}>TỔNG CÔNG NỢ</b></Table.Summary.Cell>
            <Table.Summary.Cell index={1} align="right">
              <b className="money money-neg">{fmtVnd(data.reduce((s, r) => s + r.congNo, 0))}</b>
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

  const quick = (f: Dayjs, t: Dayjs) => { setFrom(f); setTo(t); void load(f.format('YYYY-MM-DD'), t.format('YYYY-MM-DD')); };

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <RangeFilter from={from} to={to}
        onChange={(f, t) => { setFrom(f); setTo(t); }}
        onQuick={quick}
        onView={() => void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'))}
        onExport={() => void exportExcel('cash-balance/export',
          { from: from?.format('YYYY-MM-DD'), to: to?.format('YYYY-MM-DD') }, 'ton-quy-theo-ngay.xlsx')} />
      <Table className="luxury-table" size="middle" rowKey="ngay" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }}
        columns={[
          { title: 'Ngày', dataIndex: 'ngay', width: 130, render: (v: string) => dayjs(v).format('DD/MM/YYYY') },
          { title: 'Thu', dataIndex: 'thu', width: 160, align: 'right',
            render: (v: number) => money(v, 'money-pos') },
          { title: 'Chi', dataIndex: 'chi', width: 160, align: 'right',
            render: (v: number) => money(v, 'money-neg') },
          { title: 'Tồn cuối ngày', dataIndex: 'tonCuoiNgay', width: 180, align: 'right',
            render: (v: number) => money(v) },
        ]}
        summary={rows.length > 0 ? () => (
          <Table.Summary.Row className="luxury-summary-row">
            <Table.Summary.Cell index={0} colSpan={3}><b style={{ color: '#8A6100' }}>TỒN QUỸ CUỐI KỲ</b></Table.Summary.Cell>
            <Table.Summary.Cell index={1} align="right"><b className="money money-neg">{fmtVnd(last)}</b></Table.Summary.Cell>
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

  const quick = (f: Dayjs, t: Dayjs) => { setFrom(f); setTo(t); void load(f.format('YYYY-MM-DD'), t.format('YYYY-MM-DD')); };

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <RangeFilter from={from} to={to}
        onChange={(f, t) => { setFrom(f); setTo(t); }}
        onQuick={quick}
        onView={() => void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'))}
        exporting={loading} />
      {data && (
        <>
          <Row gutter={[16, 16]}>
            <Col xs={12} md={6}>
              <div className="luxury-kpi-card">
                <div className="kpi-label">DOANH THU</div>
                <div className="kpi-value">{fmtVnd(data.doanhThu)}</div>
              </div>
            </Col>
            <Col xs={12} md={6}>
              <div className="luxury-kpi-card">
                <div className="kpi-label">GIÁ VỐN HÀNG BÁN</div>
                <div className="kpi-value" style={{ color: '#E11D48' }}>{fmtVnd(data.giaVon)}</div>
              </div>
            </Col>
            <Col xs={12} md={6}>
              <div className="luxury-kpi-card">
                <div className="kpi-label">CHI PHÍ</div>
                <div className="kpi-value" style={{ color: '#E11D48' }}>{fmtVnd(data.chiPhi)}</div>
              </div>
            </Col>
            <Col xs={12} md={6}>
              <div className="luxury-kpi-card">
                <div className="kpi-label">LỢI NHUẬN</div>
                <div className="kpi-value" style={{ color: data.loiNhuan >= 0 ? '#10B981' : '#E11D48' }}>{fmtVnd(data.loiNhuan)}</div>
              </div>
            </Col>
          </Row>
          <Table className="luxury-table" size="middle" pagination={false} dataSource={[
            { k: 'Doanh thu', v: data.doanhThu },
            { k: 'Giảm giá', v: -data.giamGia },
            { k: 'Giá vốn hàng bán', v: -data.giaVon },
            { k: 'Chi phí (thu chi quỹ)', v: -data.chiPhi },
            { k: 'LỢI NHUẬN', v: data.loiNhuan },
          ]} rowKey="k"
            columns={[
              { title: 'Chỉ tiêu', dataIndex: 'k', render: (v: string) => <b style={{ color: '#1C180E' }}>{v}</b> },
              { title: 'Giá trị', dataIndex: 'v', width: 220, align: 'right',
                render: (v: number) => <b className={`money ${v < 0 ? 'money-neg' : 'money-pos'}`}>{fmtVnd(v)}</b> },
            ]} />
        </>
      )}
    </Space>
  );
};

// ---------------- Doanh thu chi tiết (Phase 8) ----------------

const RevenueDetailTab: React.FC = () => {
  const [rows, setRows] = useState<RevenueDetailRowDto[]>([]);
  const [from, setFrom] = useState<Dayjs | null>(null);
  const [to, setTo] = useState<Dayjs | null>(null);
  const [loading, setLoading] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const load = useCallback(async (f?: string, t?: string) => {
    setLoading(true); setErr(null);
    try { setRows(await getRevenueDetail({ from: f, to: t })); }
    catch (e: unknown) { setErr(e instanceof Error ? e.message : 'Lỗi tải báo cáo'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const quick = (f: Dayjs, t: Dayjs) => { setFrom(f); setTo(t); void load(f.format('YYYY-MM-DD'), t.format('YYYY-MM-DD')); };

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <RangeFilter from={from} to={to}
        onChange={(f, t) => { setFrom(f); setTo(t); }}
        onQuick={quick}
        onView={() => void load(from?.format('YYYY-MM-DD'), to?.format('YYYY-MM-DD'))} />
      {err && <Alert type="error" showIcon message={err} />}
      <Table className="luxury-table" size="middle" rowKey="donHangId" loading={loading} dataSource={rows}
        pagination={{ pageSize: 15, showSizeChanger: false }} scroll={{ x: 'max-content' }}
        columns={[
          { title: 'Số phiếu', dataIndex: 'soPhieu', width: 120, fixed: 'left',
            render: (v: string) => <span className="code-badge">{v}</span> },
          { title: 'Ngày', dataIndex: 'ngay', width: 110, render: (v: string | null) => v ? dayjs(v).format('DD/MM/YYYY') : '—' },
          { title: 'Khách hàng', dataIndex: 'khachHang', width: 200, render: (v: string | null) => v ?? 'Khách lẻ' },
          { title: 'Điện thoại', dataIndex: 'dienThoai', width: 120, render: (v: string | null) => v ?? '—' },
          { title: 'Nhân viên', dataIndex: 'nhanVien', width: 150, render: (v: string | null) => v ?? '—' },
          { title: 'Kho', dataIndex: 'kho', width: 140, render: (v: string | null) => v ?? '—' },
          { title: 'Loại', dataIndex: 'loai', width: 90, render: (v: number | null) => v === 3 ? 'Bán buôn' : 'Bán lẻ' },
          { title: 'Tiền hàng', dataIndex: 'tienHang', width: 140, align: 'right', render: (v: number) => money(v) },
          { title: 'Giảm giá', dataIndex: 'tienGiamGia', width: 130, align: 'right', render: (v: number) => money(v) },
          { title: 'Thuế', dataIndex: 'tienThue', width: 120, align: 'right', render: (v: number) => money(v) },
          { title: 'Phí vận chuyển', dataIndex: 'phiVanChuyen', width: 140, align: 'right', render: (v: number) => money(v) },
          { title: 'Tổng cộng', dataIndex: 'tongCong', width: 150, align: 'right',
            render: (v: number) => money(v, 'money-neg') },
          { title: 'Đã thanh toán', dataIndex: 'thanhToan', width: 150, align: 'right',
            render: (v: number) => money(v, 'money-pos') },
          { title: 'Còn nợ', dataIndex: 'conNo', width: 140, align: 'right',
            render: (v: number) => v > 0 ? money(v, 'money-neg') : money(v) },
        ]}
        summary={(data) => data.length > 0 ? (
          <Table.Summary.Row className="luxury-summary-row">
            <Table.Summary.Cell index={0} colSpan={7}><b style={{ color: '#8A6100' }}>TỔNG CỘNG ({data.length} phiếu)</b></Table.Summary.Cell>
            <Table.Summary.Cell index={1} align="right"><b className="money">{fmtVnd(data.reduce((s, r) => s + r.tienHang, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={2} align="right"><b className="money">{fmtVnd(data.reduce((s, r) => s + r.tienGiamGia, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={3} align="right"><b className="money">{fmtVnd(data.reduce((s, r) => s + r.tienThue, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={4} align="right"><b className="money">{fmtVnd(data.reduce((s, r) => s + r.phiVanChuyen, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={5} align="right"><b className="money money-neg">{fmtVnd(data.reduce((s, r) => s + r.tongCong, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={6} align="right"><b className="money money-pos">{fmtVnd(data.reduce((s, r) => s + r.thanhToan, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={7} align="right"><b className="money money-neg">{fmtVnd(data.reduce((s, r) => s + r.conNo, 0))}</b></Table.Summary.Cell>
          </Table.Summary.Row>
        ) : null} />
    </Space>
  );
};

/** Report Center (Phase 6 — mục 6.5): 6 báo cáo vận hành + xuất Excel. */
const ReportsPage: React.FC = () => (
  <Tabs className="luxury-tabs" items={[
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
    { key: 'revenue-detail', label: 'Doanh thu chi tiết', children: <RevenueDetailTab /> },
  ]} />
);

export default ReportsPage;
