import React, { useEffect, useState } from 'react';
import {
  Alert, Button, Card, Col, DatePicker, Empty, Input, Row, Select, Space,
  Table, Tabs, Tag
} from 'antd';
import { AppstoreOutlined, ReloadOutlined, SearchOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { getInventory, getLedger, type InventoryItem, type LedgerEntry } from '../../api/inventory';
import { getWarehouses } from '../../api/warehouses';

const fmtVnd = (n: number | null | undefined) =>
  n === null || n === undefined ? '—' : n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';
const dash = (v: string | null | undefined) => v ?? '—';

const InventoryTab: React.FC = () => {
  const [items, setItems] = useState<InventoryItem[]>([]);
  const [warehouses, setWarehouses] = useState<{ id: string; name: string }[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [khoId, setKhoId] = useState<string | undefined>(undefined);
  const [nonZeroOnly, setNonZeroOnly] = useState(true);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const load = async () => {
    try {
      setErr(null);
      setLoading(true);
      const res = await getInventory({ search, khoId, nonZeroOnly, page, pageSize });
      setItems(res.items);
      setTotal(res.total);
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tải tồn kho');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); /* eslint-disable-line react-hooks/exhaustive-deps */ }, [search, khoId, nonZeroOnly, page, pageSize]);
  useEffect(() => { getWarehouses({ activeOnly: true, pageSize: 50 }).then((r) => setWarehouses(r.items.map((w) => ({ id: w.id, name: w.name })))).catch(() => { }); }, []);

  const columns: ColumnsType<InventoryItem> = [
    {
      title: 'Mã hàng',
      dataIndex: 'code',
      width: 120,
      render: (v) => <span className="code-badge">{dash(v)}</span>,
    },
    {
      title: 'Tên mặt hàng',
      dataIndex: 'name',
      render: (v) => <b style={{ color: '#1C180E' }}>{v}</b>,
    },
    {
      title: 'Kho hàng',
      dataIndex: 'kho',
      width: 150,
      render: (v) => <Tag color="gold">{dash(v)}</Tag>,
    },
    {
      title: 'Số lượng tồn',
      dataIndex: 'ton',
      width: 120,
      align: 'right',
      render: (v: number) => {
        const color = v < 0 ? '#E11D48' : v > 0 ? '#10B981' : '#888';
        return <b style={{ color, fontFamily: 'Outfit', fontSize: 14 }}>{v.toLocaleString('vi-VN')}</b>;
      },
    },
    {
      title: 'Giá vốn',
      dataIndex: 'giaVon',
      width: 130,
      align: 'right',
      render: (v) => <span style={{ fontFamily: 'Outfit' }}>{fmtVnd(v)}</span>,
    },
    {
      title: 'Giá bán lẻ',
      dataIndex: 'giaBan',
      width: 130,
      align: 'right',
      render: (v) => <b style={{ color: '#8A6100', fontFamily: 'Outfit' }}>{fmtVnd(v)}</b>,
    },
    {
      title: 'Giá trị tồn kho',
      dataIndex: 'giaTriTon',
      width: 150,
      align: 'right',
      render: (v) => <b style={{ fontFamily: 'Outfit', color: '#1C180E' }}>{fmtVnd(v)}</b>,
    },
  ];

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {/* THANH CÔNG CỤ TÌM KIẾM & BỘ LỌC KHO */}
      <Card className="luxury-card">
        <Space wrap size={12}>
          <Input
            allowClear
            placeholder="Tìm mã hoặc tên mặt hàng..."
            prefix={<SearchOutlined style={{ color: '#D4AF37' }} />}
            style={{ width: 280, borderRadius: 8 }}
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          />
          <Select
            allowClear
            placeholder="Tất cả kho hàng"
            style={{ width: 200 }}
            value={khoId}
            onChange={(v) => { setKhoId(v); setPage(1); }}
            options={warehouses.map((w) => ({ value: w.id, label: w.name }))}
          />
          <Select
            style={{ width: 170 }}
            value={nonZeroOnly ? 'nonzero' : 'all'}
            onChange={(v) => { setNonZeroOnly(v === 'nonzero'); setPage(1); }}
            options={[
              { value: 'nonzero', label: 'Tồn khác 0' },
              { value: 'all', label: 'Hiển thị tất cả' },
            ]}
          />
          <Button
            icon={<ReloadOutlined />}
            onClick={load}
            loading={loading}
            className="gold-gradient-btn"
          >
            Tải lại
          </Button>
        </Space>
      </Card>

      {err && <Alert type="error" showIcon message={err} closable style={{ borderRadius: 8 }} />}

      <Card
        className="luxury-card"
        title={
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <AppstoreOutlined style={{ color: '#D4AF37' }} />
            <span style={{ color: '#1C180E', fontWeight: 800 }}>
              Danh Sách Tồn Kho ({total.toLocaleString('vi-VN')} dòng)
            </span>
          </div>
        }
        styles={{ body: { padding: 0 } }}
      >
        <Table
          className="luxury-table"
          size="middle"
          rowKey={(r) => `${r.matHangId}-${r.khoId}`}
          loading={loading}
          columns={columns}
          dataSource={items}
          pagination={{
            current: page,
            pageSize,
            total,
            showSizeChanger: true,
            showTotal: (t) => `Tổng ${t.toLocaleString('vi-VN')} mặt hàng tồn kho`,
          }}
          onChange={(p) => { setPage(p.current ?? 1); setPageSize(p.pageSize ?? 20); }}
        />
      </Card>
    </Space>
  );
};

const LedgerTab: React.FC = () => {
  const [warehouses, setWarehouses] = useState<{ id: string; name: string }[]>([]);
  const [khoId, setKhoId] = useState<string | undefined>(undefined);
  const [search, setSearch] = useState('');
  const [products, setProducts] = useState<InventoryItem[]>([]);
  const [selected, setSelected] = useState<InventoryItem | null>(null);
  const [rows, setRows] = useState<LedgerEntry[]>([]);
  const [loading, setLoading] = useState(false);
  const [fromDate, setFromDate] = useState<dayjs.Dayjs | null>(null);
  const [toDate, setToDate] = useState<dayjs.Dayjs | null>(null);

  useEffect(() => { getWarehouses({ activeOnly: true, pageSize: 50 }).then((r) => setWarehouses(r.items.map((w) => ({ id: w.id, name: w.name })))).catch(() => { }); }, []);

  useEffect(() => {
    getInventory({ search, khoId, nonZeroOnly: false, pageSize: 50 })
      .then((r) => setProducts(r.items))
      .catch(() => { });
  }, [search, khoId]);

  const openLedger = async (item: InventoryItem) => {
    setSelected(item);
    setLoading(true);
    try {
      setRows(await getLedger(item.matHangId, {
        khoId: item.khoId ?? undefined,
        from: fromDate?.format('YYYY-MM-DD'),
        to: toDate?.format('YYYY-MM-DD'),
      }));
    } catch {
      setRows([]);
    } finally {
      setLoading(false);
    }
  };

  const ledgerColumns: ColumnsType<LedgerEntry> = [
    { title: 'Ngày phát sinh', dataIndex: 'entryDate', width: 120, render: (v) => dayjs(v).format('DD/MM/YYYY') },
    { title: 'Số chứng từ', dataIndex: 'soPhieu', width: 140, render: (v) => <span className="code-badge">{dash(v)}</span> },
    { title: 'Loại nghiệp vụ', dataIndex: 'docType', width: 120, render: (v) => <Tag color="gold">{dash(v)}</Tag> },
    {
      title: 'SL nhập',
      dataIndex: 'slNhap',
      width: 100,
      align: 'right',
      render: (v: number) => v > 0 ? <b style={{ color: '#10B981', fontFamily: 'Outfit' }}>+{v}</b> : '—',
    },
    {
      title: 'SL xuất',
      dataIndex: 'slXuat',
      width: 100,
      align: 'right',
      render: (v: number) => v > 0 ? <b style={{ color: '#E11D48', fontFamily: 'Outfit' }}>-{v}</b> : '—',
    },
    {
      title: 'Tồn lũy kế',
      dataIndex: 'tonLuyKe',
      width: 120,
      align: 'right',
      render: (v: number) => <b style={{ color: '#8A6100', fontFamily: 'Outfit', fontSize: 14 }}>{v}</b>,
    },
    {
      title: 'Giá vốn',
      dataIndex: 'giaVon',
      width: 120,
      align: 'right',
      render: (v) => <span style={{ fontFamily: 'Outfit' }}>{fmtVnd(v)}</span>,
    },
  ];

  const tonHienTai = selected ? rows.at(-1)?.tonLuyKe ?? 0 : 0;

  return (
    <Row gutter={[16, 16]}>
      <Col xs={24} lg={9}>
        <Card
          className="luxury-card"
          title={<span style={{ fontWeight: 700, color: '#8A6100' }}>Chọn Mặt Hàng Xem Thẻ Kho</span>}
        >
          <Space direction="vertical" style={{ width: '100%' }} size={10}>
            <Input
              allowClear
              placeholder="Tìm mã / tên hàng..."
              prefix={<SearchOutlined style={{ color: '#D4AF37' }} />}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <Select
              allowClear
              placeholder="Chọn kho hàng"
              style={{ width: '100%' }}
              value={khoId}
              onChange={setKhoId}
              options={warehouses.map((w) => ({ value: w.id, label: w.name }))}
            />
            <Space wrap>
              <DatePicker placeholder="Từ ngày" value={fromDate} onChange={setFromDate} format="DD/MM/YYYY" />
              <DatePicker placeholder="Đến ngày" value={toDate} onChange={setToDate} format="DD/MM/YYYY" />
            </Space>
          </Space>

          <div style={{ maxHeight: 380, overflowY: 'auto', marginTop: 12, borderTop: '1px solid #F4EEDC', paddingTop: 8 }}>
            {products.map((p) => {
              const isPicked = selected?.matHangId === p.matHangId && selected?.khoId === p.khoId;
              return (
                <div
                  key={`${p.matHangId}-${p.khoId}`}
                  onClick={() => void openLedger(p)}
                  style={{
                    padding: '8px 10px',
                    cursor: 'pointer',
                    borderRadius: 6,
                    marginBottom: 4,
                    background: isPicked ? '#FFF9E6' : 'transparent',
                    border: isPicked ? '1px solid rgba(212, 175, 55, 0.4)' : '1px solid transparent',
                    transition: 'all 0.2s ease',
                  }}
                >
                  <b style={{ color: '#8A6100' }}>{p.code}</b> — {p.name}
                  <div style={{ color: '#888', fontSize: 11, marginTop: 2 }}>
                    Kho: <b>{p.kho ?? 'Tất cả'}</b> · Tồn: <b style={{ color: p.ton > 0 ? '#10B981' : '#E11D48' }}>{p.ton}</b>
                  </div>
                </div>
              );
            })}
            {products.length === 0 && <Empty description="Không có mặt hàng" />}
          </div>
        </Card>
      </Col>

      <Col xs={24} lg={15}>
        <Card
          className="luxury-card"
          title={
            <span style={{ fontWeight: 700, color: '#1C180E' }}>
              {selected ? `Lịch Sử Thẻ Kho — ${selected.name} (${selected.kho ?? 'Tất cả kho'})` : 'Chi Tiết Thẻ Kho'}
            </span>
          }
        >
          {selected && (
            <div style={{
              background: '#FFFDF5',
              padding: '12px 18px',
              borderRadius: 8,
              border: '1px solid rgba(212, 175, 55, 0.3)',
              marginBottom: 14,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
            }}>
              <span style={{ fontSize: 13, fontWeight: 700, color: '#8A6100' }}>
                TỒN CUỐI KỲ TRÊN THẺ KHO
              </span>
              <span style={{ fontSize: 22, fontWeight: 800, fontFamily: 'Outfit', color: '#10B981' }}>
                {tonHienTai.toLocaleString('vi-VN')}
              </span>
            </div>
          )}
          <Table
            className="luxury-table"
            size="small"
            rowKey={(r) => `${r.soPhieu}-${r.entryDate}`}
            loading={loading}
            columns={ledgerColumns}
            dataSource={rows}
            pagination={false}
            locale={{ emptyText: <Empty description="Chọn một mặt hàng bên trái để xem biến động thẻ kho" /> }}
          />
        </Card>
      </Col>
    </Row>
  );
};

const InventoryPage: React.FC = () => (
  <Tabs
    className="luxury-tabs"
    defaultActiveKey="ton"
    items={[
      { key: 'ton', label: '📦 DANH MỤC TỒN KHO', children: <InventoryTab /> },
      { key: 'ledger', label: '📋 TRA CỨU THẺ KHO', children: <LedgerTab /> },
    ]}
  />
);

export default InventoryPage;
