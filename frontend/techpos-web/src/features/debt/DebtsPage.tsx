import React, { useCallback, useEffect, useState } from 'react';
import { Card, Col, Input, Row, Space, Table, Tabs, message } from 'antd';
import { AuditOutlined, SearchOutlined, UserOutlined } from '@ant-design/icons';
import {
  getCustomerDebts, getSupplierDebts, type CustomerDebt, type SupplierDebt,
} from '../../api/debts';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

const CustomerDebts: React.FC = () => {
  const [rows, setRows] = useState<CustomerDebt[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (p: number, s: string) => {
    setLoading(true);
    try {
      const r = await getCustomerDebts({ search: s || undefined, page: p, pageSize: 50 });
      setRows(r.items); setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải công nợ khách hàng');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(1, ''); }, [load]);

  const totalDebt = rows.reduce((s, r) => s + r.congNo, 0);

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {/* THẺ TỔNG HỢP KPI MẠ VÀNG */}
      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} md={8}>
          <div className="luxury-kpi-card">
            <div className="kpi-label">
              <UserOutlined style={{ color: '#D4AF37' }} /> TỔNG KHÁCH HÀNG NỢ
            </div>
            <div className="kpi-value">{total.toLocaleString('vi-VN')} <span style={{ fontSize: 16, fontWeight: 500, color: '#888' }}>khách</span></div>
            <div className="kpi-sub">Theo dõi từ các đơn hàng có nợ lại</div>
          </div>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <div className="luxury-kpi-card">
            <div className="kpi-label">
              <AuditOutlined style={{ color: '#E11D48' }} /> TỔNG TIỀN NỢ PHẢI THU
            </div>
            <div className="kpi-value" style={{ color: '#E11D48' }}>{fmtVnd(totalDebt)}</div>
            <div className="kpi-sub">Tổng giá trị nợ trên trang hiện tại</div>
          </div>
        </Col>
      </Row>

      {/* BẢNG DỮ LIỆU CÔNG NỢ KHÁCH HÀNG */}
      <Card
        className="luxury-card"
        title={<span style={{ color: '#8A6100', fontWeight: 800, fontSize: 15 }}>Danh Sách Công Nợ Khách Hàng</span>}
        extra={
          <Input.Search
            allowClear
            prefix={<SearchOutlined style={{ color: '#D4AF37' }} />}
            placeholder="Tìm mã / tên khách..."
            style={{ width: 280, borderRadius: 8 }}
            onSearch={(v) => { setSearch(v); setPage(1); void load(1, v); }}
          />
        }
      >
        <Table
          className="luxury-table"
          size="middle"
          rowKey="khachHangId"
          loading={loading}
          dataSource={rows}
          pagination={{
            current: page, pageSize: 50, total, showSizeChanger: false,
            onChange: (p) => { setPage(p); void load(p, search); }
          }}
          columns={[
            {
              title: 'Mã khách',
              dataIndex: 'maKhach',
              width: 130,
              render: (v) => <span className="code-badge">{v ?? '—'}</span>
            },
            {
              title: 'Tên khách hàng',
              dataIndex: 'tenKhach',
              render: (v) => <b style={{ color: '#1C180E' }}>{v}</b>
            },
            { title: 'Điện thoại', dataIndex: 'dienThoai', width: 140 },
            {
              title: 'Số HĐ nợ',
              dataIndex: 'soHoaDon',
              width: 120,
              align: 'right',
              render: (v) => <b style={{ fontFamily: 'Outfit' }}>{v}</b>
            },
            {
              title: 'Công nợ',
              dataIndex: 'congNo',
              width: 180,
              align: 'right',
              render: (v: number) => (
                <span className={v > 0 ? "debt-amount-badge" : ""}>
                  {fmtVnd(v)}
                </span>
              )
            },
          ]}
          summary={() => rows.length > 0 ? (
            <Table.Summary.Row className="luxury-summary-row">
              <Table.Summary.Cell index={0} colSpan={4}>
                <b style={{ color: '#8A6100', letterSpacing: '0.5px' }}>TỔNG {total} KHÁCH HÀNG ĐANG NỢ</b>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={1} align="right">
                <b className="debt-summary-val">{fmtVnd(totalDebt)}</b>
              </Table.Summary.Cell>
            </Table.Summary.Row>
          ) : null}
        />
      </Card>
    </Space>
  );
};

const SupplierDebts: React.FC = () => {
  const [rows, setRows] = useState<SupplierDebt[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (p: number, s: string) => {
    setLoading(true);
    try {
      const r = await getSupplierDebts({ search: s || undefined, page: p, pageSize: 50 });
      setRows(r.items); setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải công nợ NCC');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(1, ''); }, [load]);

  const totalDebt = rows.reduce((s, r) => s + r.congNo, 0);

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {/* THẺ TỔNG HỢP KPI MẠ VÀNG CHO NCC */}
      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} md={8}>
          <div className="luxury-kpi-card">
            <div className="kpi-label">
              <UserOutlined style={{ color: '#D4AF37' }} /> TỔNG NHÀ CUNG CẤP NỢ
            </div>
            <div className="kpi-value">{total.toLocaleString('vi-VN')} <span style={{ fontSize: 16, fontWeight: 500, color: '#888' }}>NCC</span></div>
            <div className="kpi-sub">Theo dõi từ các phiếu nhập kho chưa thanh toán đủ</div>
          </div>
        </Col>
        <Col xs={24} sm={12} md={8}>
          <div className="luxury-kpi-card">
            <div className="kpi-label">
              <AuditOutlined style={{ color: '#E11D48' }} /> TỔNG TIỀN NỢ PHẢI TRẢ NCC
            </div>
            <div className="kpi-value" style={{ color: '#E11D48' }}>{fmtVnd(totalDebt)}</div>
            <div className="kpi-sub">Tổng công nợ nhà cung cấp trên trang hiện tại</div>
          </div>
        </Col>
      </Row>

      {/* BẢNG DỮ LIỆU CÔNG NỢ NCC */}
      <Card
        className="luxury-card"
        title={<span style={{ color: '#8A6100', fontWeight: 800, fontSize: 15 }}>Danh Sách Công Nợ Nhà Cung Cấp</span>}
        extra={
          <Input.Search
            allowClear
            prefix={<SearchOutlined style={{ color: '#D4AF37' }} />}
            placeholder="Tìm mã / tên NCC..."
            style={{ width: 280, borderRadius: 8 }}
            onSearch={(v) => { setSearch(v); setPage(1); void load(1, v); }}
          />
        }
      >
        <Table
          className="luxury-table"
          size="middle"
          rowKey="nhaCungCapId"
          loading={loading}
          dataSource={rows}
          pagination={{
            current: page, pageSize: 50, total, showSizeChanger: false,
            onChange: (p) => { setPage(p); void load(p, search); }
          }}
          columns={[
            {
              title: 'Mã NCC',
              dataIndex: 'maNhaCungCap',
              width: 130,
              render: (v) => <span className="code-badge">{v ?? '—'}</span>
            },
            {
              title: 'Tên nhà cung cấp',
              dataIndex: 'tenNhaCungCap',
              render: (v) => <b style={{ color: '#1C180E' }}>{v}</b>
            },
            { title: 'Điện thoại', dataIndex: 'dienThoai', width: 140 },
            {
              title: 'Số phiếu nợ',
              dataIndex: 'soPhieu',
              width: 120,
              align: 'right',
              render: (v) => <b style={{ fontFamily: 'Outfit' }}>{v}</b>
            },
            {
              title: 'Công nợ',
              dataIndex: 'congNo',
              width: 180,
              align: 'right',
              render: (v: number) => (
                <span className={v > 0 ? "debt-amount-badge" : ""}>
                  {fmtVnd(v)}
                </span>
              )
            },
          ]}
          summary={() => rows.length > 0 ? (
            <Table.Summary.Row className="luxury-summary-row">
              <Table.Summary.Cell index={0} colSpan={4}>
                <b style={{ color: '#8A6100', letterSpacing: '0.5px' }}>TỔNG {total} NCC ĐANG NỢ</b>
              </Table.Summary.Cell>
              <Table.Summary.Cell index={1} align="right">
                <b className="debt-summary-val">{fmtVnd(totalDebt)}</b>
              </Table.Summary.Cell>
            </Table.Summary.Row>
          ) : null}
        />
      </Card>
    </Space>
  );
};

/** Công nợ (mục 5.2) — công nợ khách hàng / nhà cung cấp từ TDONHANG.CONNO/CONLAI. */
const DebtsPage: React.FC = () => (
  <Tabs
    className="luxury-tabs"
    items={[
      { key: 'kh', label: 'CÔNG NỢ KHÁCH HÀNG', children: <CustomerDebts /> },
      { key: 'ncc', label: 'CÔNG NỢ NHÀ CUNG CẤP', children: <SupplierDebts /> },
    ]}
  />
);

export default DebtsPage;