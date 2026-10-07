import React, { useEffect, useState } from 'react';
import { Alert, Button, Card, Col, DatePicker, Empty, Input, Row, Select, Space, Statistic, Table, Tabs, Tag } from 'antd';
import { ReloadOutlined, SearchOutlined } from '@ant-design/icons';
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
    { title: 'Mã', dataIndex: 'code', width: 110, render: (v) => <b>{dash(v)}</b> },
    { title: 'Mặt hàng', dataIndex: 'name' },
    { title: 'Kho', dataIndex: 'kho', width: 140, render: dash },
    { title: 'Tồn', dataIndex: 'ton', width: 100, align: 'right',
      render: (v: number) => <b style={{ color: v < 0 ? '#cf1322' : undefined }}>{v.toLocaleString('vi-VN')}</b> },
    { title: 'Giá vốn', dataIndex: 'giaVon', width: 120, align: 'right', render: fmtVnd },
    { title: 'Giá bán', dataIndex: 'giaBan', width: 120, align: 'right', render: fmtVnd },
    { title: 'Giá trị tồn', dataIndex: 'giaTriTon', width: 140, align: 'right', render: fmtVnd },
  ];

  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      <Card size="small">
        <Space wrap>
          <Input allowClear placeholder="Tìm mã / tên mặt hàng..." prefix={<SearchOutlined />}
            style={{ width: 260 }} value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
          <Select allowClear placeholder="Kho hàng" style={{ width: 180 }} value={khoId}
            onChange={(v) => { setKhoId(v); setPage(1); }}
            options={warehouses.map((w) => ({ value: w.id, label: w.name }))} />
          <Select style={{ width: 190 }} value={nonZeroOnly ? 'nonzero' : 'all'}
            onChange={(v) => { setNonZeroOnly(v === 'nonzero'); setPage(1); }}
            options={[{ value: 'nonzero', label: 'Tồn <> 0' }, { value: 'all', label: 'Tất cả' }]} />
          <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Tải lại</Button>
        </Space>
      </Card>
      {err && <Alert type="error" showIcon message={err} closable />}
      <Card size="small" title={`Tồn kho (${total})`} styles={{ body: { padding: 0 } }}>
        <Table size="middle" rowKey={(r) => `${r.matHangId}-${r.khoId}`} loading={loading}
          columns={columns} dataSource={items}
          pagination={{
            current: page, pageSize, total, showSizeChanger: true, showTotal: (t) => `Tổng ${t} dòng`
          }}
          onChange={(p) => { setPage(p.current ?? 1); setPageSize(p.pageSize ?? 20); }} />
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
    } catch { setRows([]); } finally { setLoading(false); }
  };

  const ledgerColumns: ColumnsType<LedgerEntry> = [
    { title: 'Ngày', dataIndex: 'entryDate', width: 100, render: (v) => dayjs(v).format('DD/MM/YYYY') },
    { title: 'Số phiếu', dataIndex: 'soPhieu', width: 130, render: dash },
    { title: 'Loại', dataIndex: 'docType', width: 100, render: (v) => <Tag>{dash(v)}</Tag> },
    { title: 'SL nhập', dataIndex: 'slNhap', width: 90, align: 'right',
      render: (v: number) => v > 0 ? <span style={{ color: '#389e0d' }}>{v}</span> : '—' },
    { title: 'SL xuất', dataIndex: 'slXuat', width: 90, align: 'right',
      render: (v: number) => v > 0 ? <span style={{ color: '#cf1322' }}>{v}</span> : '—' },
    { title: 'Tồn lũy kế', dataIndex: 'tonLuyKe', width: 110, align: 'right', render: (v: number) => <b>{v}</b> },
    { title: 'Giá vốn', dataIndex: 'giaVon', width: 110, align: 'right', render: fmtVnd },
  ];

  const tonHienTai = selected ? rows.at(-1)?.tonLuyKe ?? 0 : 0;

  return (
    <Row gutter={12}>
      <Col xs={24} lg={10}>
        <Card size="small" title="Chọn mặt hàng">
          <Space direction="vertical" style={{ width: '100%' }} size={8}>
            <Input allowClear placeholder="Tìm mặt hàng..." prefix={<SearchOutlined />}
              value={search} onChange={(e) => setSearch(e.target.value)} />
            <Select allowClear placeholder="Kho" style={{ width: '100%' }} value={khoId}
              onChange={setKhoId}
              options={warehouses.map((w) => ({ value: w.id, label: w.name }))} />
            <Space>
              <DatePicker placeholder="Từ ngày" value={fromDate} onChange={setFromDate} />
              <DatePicker placeholder="Đến ngày" value={toDate} onChange={setToDate} />
            </Space>
          </Space>
          <div style={{ maxHeight: 320, overflowY: 'auto', marginTop: 8 }}>
            {products.map((p) => (
              <div key={`${p.matHangId}-${p.khoId}`}
                onClick={() => void openLedger(p)}
                style={{
                  padding: '6px 8px', cursor: 'pointer', borderRadius: 4,
                  background: selected?.matHangId === p.matHangId ? '#fff1f0' : undefined,
                }}>
                <b>{p.code}</b> — {p.name} <span style={{ color: '#888' }}>({p.kho})</span>
              </div>
            ))}
            {products.length === 0 && <Empty description="Không có mặt hàng" />}
          </div>
        </Card>
      </Col>
      <Col xs={24} lg={14}>
        <Card size="small" title={selected ? `Thẻ kho — ${selected.name} (${selected.kho ?? 'All kho'})` : 'Thẻ kho'}>
          {selected && (
            <Statistic title="Tồn cuối kỳ" value={tonHienTai} style={{ marginBottom: 12 }} />
          )}
          <Table size="small" rowKey={(r) => `${r.soPhieu}-${r.entryDate}`}
            loading={loading} columns={ledgerColumns} dataSource={rows} pagination={false}
            locale={{ emptyText: <Empty description="Chọn mặt hàng để xem thẻ kho" /> }} />
        </Card>
      </Col>
    </Row>
  );
};

const InventoryPage: React.FC = () => (
  <Tabs
    defaultActiveKey="ton"
    items={[
      { key: 'ton', label: 'Tồn kho', children: <InventoryTab /> },
      { key: 'ledger', label: 'Thẻ kho', children: <LedgerTab /> },
    ]}
  />
);

export default InventoryPage;
