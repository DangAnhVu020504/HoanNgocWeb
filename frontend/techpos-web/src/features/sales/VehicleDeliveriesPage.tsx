import React, { useEffect, useMemo, useState } from 'react';
import { Alert, Button, Empty, Input, Select, Space, Table, Tag, message } from 'antd';
import {
  createVehicleDelivery, getVehicles, getWebOrders,
  type LookupItem, type WebOrderItem,
} from '../../api/salesdocs';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

/** Giao hàng theo xe (mục 4.10) — chọn xe + các đơn giao trong chuyến. */
const VehicleDeliveriesPage: React.FC = () => {
  const [vehicles, setVehicles] = useState<LookupItem[]>([]);
  const [orders, setOrders] = useState<WebOrderItem[]>([]);
  const [xeGiaoHangId, setXeGiaoHangId] = useState<string | undefined>(undefined);
  const [ghiChu, setGhiChu] = useState('');
  const [selected, setSelected] = useState<string[]>([]);
  const [saving, setSaving] = useState(false);
  const [last, setLast] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    getVehicles().then(setVehicles).catch(() => { });
    getWebOrders().then(setOrders).catch(() => { });
  }, []);

  const columns = useMemo(() => [
    { title: 'Số phiếu', dataIndex: 'soPhieu' },
    { title: 'Ngày', dataIndex: 'ngay', width: 130, render: (v: string | null) => v ? new Date(v).toLocaleDateString('vi-VN') : '—' },
    { title: 'Khách hàng', dataIndex: 'khachHang' },
    { title: 'Tổng cộng', dataIndex: 'tongCong', width: 140, align: 'right' as const, render: fmtVnd },
  ], []);

  const save = async () => {
    if (!xeGiaoHangId) { void message.warning('Chưa chọn xe giao hàng'); return; }
    if (selected.length === 0) { void message.warning('Chưa chọn đơn hàng nào'); return; }
    setSaving(true); setErr(null);
    try {
      const r = await createVehicleDelivery({ xeGiaoHangId, nhanVienId: null, donHangIds: selected, ghiChu: ghiChu || null });
      setLast(`Đã tạo phiếu giao xe ${r.soPhieu} (${r.soDonHang} đơn)`);
      void message.success(`Đã tạo phiếu giao xe ${r.soPhieu}`);
      setSelected([]); setGhiChu('');
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tạo phiếu giao xe');
    } finally { setSaving(false); }
  };

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <Select placeholder="Xe giao hàng" style={{ width: 360 }} value={xeGiaoHangId}
        onChange={setXeGiaoHangId} options={vehicles.map((v) => ({ value: v.id, label: v.name }))} />
      <Input allowClear placeholder="Ghi chú / Tuyến đường" style={{ maxWidth: 420 }} value={ghiChu} onChange={(e) => setGhiChu(e.target.value)} />
      <Tag color="blue">Chọn đơn web để gắn vào chuyến ({selected.length} đã chọn)</Tag>
      <Table
        size="small" rowKey="id" dataSource={orders} columns={columns} pagination={{ pageSize: 8 }}
        rowSelection={{ selectedRowKeys: selected, onChange: (keys) => setSelected(keys as string[]) }}
        locale={{ emptyText: <Empty description="Chưa có đơn hàng web nào" /> }}
      />
      <Button type="primary" loading={saving} onClick={save} disabled={selected.length === 0 || !xeGiaoHangId}>
        TẠO PHIẾU GIAO XE ({selected.length})
      </Button>
      {err && <Alert type="error" showIcon message={err} />}
      {last && <Alert type="success" showIcon message={last} />}
    </Space>
  );
};

export default VehicleDeliveriesPage;