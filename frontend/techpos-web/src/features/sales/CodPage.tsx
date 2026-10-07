import React, { useEffect, useMemo, useState } from 'react';
import { Alert, Button, Card, Empty, Input, Select, Space, Table, Tabs, Tag, message } from 'antd';
import {
  createCodShipment, createCodResult, getDeliveryPartners, getWebOrders,
  type LookupItem, type WebOrderItem,
} from '../../api/salesdocs';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

/** Giao hàng COD (mục 4.10) — 2 tab: gửi hàng COD + nhập kết quả. */
const CodPage: React.FC = () => {
  const [partners, setPartners] = useState<LookupItem[]>([]);
  const [orders, setOrders] = useState<WebOrderItem[]>([]);

  useEffect(() => {
    getDeliveryPartners().then(setPartners).catch(() => { });
    getWebOrders().then(setOrders).catch(() => { });
  }, []);

  return (
    <Card size="small">
      <Tabs
        items={[
          { key: 'ship', label: 'Gửi hàng COD', children: <CodShip partners={partners} orders={orders} /> },
          { key: 'result', label: 'Kết quả gửi COD', children: <CodResult partners={partners} orders={orders} /> },
        ]}
      />
    </Card>
  );
};

const CodShip: React.FC<{ partners: LookupItem[]; orders: WebOrderItem[] }> = ({ partners, orders }) => {
  const [donViChuyenPhatId, setDonViChuyenPhatId] = useState<string | undefined>(undefined);
  const [ghiChu, setGhiChu] = useState('');
  const [selected, setSelected] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [last, setLast] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  const save = async () => {
    if (!donViChuyenPhatId) { void message.warning('Chưa chọn đơn vị chuyển phát'); return; }
    if (selected.length === 0) { void message.warning('Chưa chọn đơn hàng nào'); return; }
    setSaving(true); setErr(null);
    try {
      const r = await createCodShipment({ donViChuyenPhatId, nhanVienId: null, donHangIds: selected, ghiChu: ghiChu || null });
      setLast(`Đã tạo phiếu gửi COD ${r.soPhieu} (${r.soDonHang} đơn)`);
      void message.success(`Đã tạo phiếu gửi COD ${r.soPhieu}`);
      setSelected([]); setGhiChu('');
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tạo phiếu gửi COD');
    } finally { setSaving(false); }
  };

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <Select placeholder="Đơn vị chuyển phát" style={{ width: 320 }} value={donViChuyenPhatId}
        onChange={setDonViChuyenPhatId} options={partners.map((p) => ({ value: p.id, label: p.name }))} />
      <Input allowClear placeholder="Ghi chú" style={{ maxWidth: 420 }} value={ghiChu} onChange={(e) => setGhiChu(e.target.value)} />
      <OrderPicker orders={orders} selected={selected} onChange={setSelected} />
      <Button type="primary" loading={saving} onClick={save} disabled={selected.length === 0 || !donViChuyenPhatId}>
        TẠO PHIẾU GỬI COD ({selected.length})
      </Button>
      {err && <Alert type="error" showIcon message={err} />}
      {last && <Alert type="success" showIcon message={last} />}
    </Space>
  );
};

const CodResult: React.FC<{ partners: LookupItem[]; orders: WebOrderItem[] }> = ({ partners, orders }) => {
  const [donViChuyenPhatId, setDonViChuyenPhatId] = useState<string | undefined>(undefined);
  const [ghiChu, setGhiChu] = useState('');
  const [selected, setSelected] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [last, setLast] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  const save = async () => {
    setSaving(true); setErr(null);
    try {
      const r = await createCodResult({
        donViChuyenPhatId: donViChuyenPhatId ?? null, nhanVienId: null, donHangIds: selected, ghiChu: ghiChu || null,
      });
      setLast(`Đã nhập kết quả gửi COD ${r.soPhieu}`);
      void message.success(`Đã nhập kết quả ${r.soPhieu}`);
      setSelected([]); setGhiChu('');
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi nhập kết quả COD');
    } finally { setSaving(false); }
  };

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <Select allowClear placeholder="Đơn vị chuyển phát" style={{ width: 320 }} value={donViChuyenPhatId}
        onChange={(v) => setDonViChuyenPhatId(v)} options={partners.map((p) => ({ value: p.id, label: p.name }))} />
      <Input allowClear placeholder="Ghi chú" style={{ maxWidth: 420 }} value={ghiChu} onChange={(e) => setGhiChu(e.target.value)} />
      <OrderPicker orders={orders} selected={selected} onChange={setSelected} />
      <Button type="primary" loading={saving} onClick={save}>
        NHẬP KẾT QUẢ ({selected.length})
      </Button>
      {err && <Alert type="error" showIcon message={err} />}
      {last && <Alert type="success" showIcon message={last} />}
    </Space>
  );
};

const OrderPicker: React.FC<{ orders: WebOrderItem[]; selected: string[]; onChange: (v: string[]) => void }> = ({
  orders, selected, onChange,
}) => {
  const columns = useMemo(() => [
    { title: 'Số phiếu', dataIndex: 'soPhieu' },
    { title: 'Ngày', dataIndex: 'ngay', width: 130, render: (v: string | null) => v ? new Date(v).toLocaleDateString('vi-VN') : '—' },
    { title: 'Khách hàng', dataIndex: 'khachHang' },
    { title: 'Tổng cộng', dataIndex: 'tongCong', width: 140, align: 'right' as const, render: fmtVnd },
  ], []);
  return (
    <div>
      <Tag color="blue">Chọn đơn web để gắn vào phiếu ({selected.length} đã chọn)</Tag>
      <Table
        size="small" rowKey="id" dataSource={orders} columns={columns} pagination={{ pageSize: 8 }}
        rowSelection={{ selectedRowKeys: selected, onChange: (keys) => onChange(keys as string[]) }}
        locale={{ emptyText: <Empty description="Chưa có đơn hàng web nào" /> }}
      />
    </div>
  );
};

export default CodPage;