import React, { useEffect, useMemo, useState } from 'react';
import { Alert, Button, Card, Empty, Input, InputNumber, Select, Space, Table, message } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';
import { createStocktake, getInventory, type InventoryItem, type StocktakeResult } from '../../api/inventory';
import { printStockVoucherPdf } from '../../api/reports';
import { getWarehouses, type Warehouse } from '../../api/warehouses';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

interface StocktakeRow extends InventoryItem {
  slThucTe: number;
  ghiChep: boolean;
}

const StocktakesPage: React.FC = () => {
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [khoId, setKhoId] = useState<string | undefined>(undefined);
  const [search, setSearch] = useState('');
  const [rows, setRows] = useState<StocktakeRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [lastResult, setLastResult] = useState<StocktakeResult | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    getWarehouses({ activeOnly: true, pageSize: 50 }).then((r) => {
      setWarehouses(r.items);
      if (r.items.length > 0) setKhoId((k) => k ?? r.items[0].id);
    }).catch(() => { });
  }, []);

  const loadStock = async () => {
    if (!khoId) return;
    setLoading(true);
    setErr(null);
    try {
      const res = await getInventory({ khoId, nonZeroOnly: false, pageSize: 200 });
      setRows(res.items.map((it) => ({ ...it, slThucTe: it.ton, ghiChep: true })));
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tải tồn kho');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void loadStock(); /* eslint-disable-line react-hooks/exhaustive-deps */ }, [khoId]);

  const setSlThucTe = (matHangId: string, v: number) =>
    setRows((prev) => prev.map((r) => r.matHangId === matHangId ? { ...r, slThucTe: v, ghiChep: false } : r));

  const chenhLechRows = useMemo(
    () => rows.filter((r) => r.slThucTe !== r.ton),
    [rows]
  );

  const save = async () => {
    if (!khoId) { void message.warning('Chưa chọn kho kiểm kê'); return; }
    if (chenhLechRows.length === 0) { void message.warning('Không có chênh lệch nào để lập phiếu'); return; }
    setSaving(true);
    setErr(null);
    try {
      const result = await createStocktake({
        khoId,
        lines: chenhLechRows.map((r) => ({ matHangId: r.matHangId, slThucTe: r.slThucTe })),
        ghiChu: null,
      });
      setLastResult(result);
      void message.success(`Đã tạo phiếu kiểm kê ${result.soPhieu} (${result.linesAdjusted} dòng hiệu chỉnh)`);
      await loadStock();
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tạo phiếu kiểm kê');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      <Card size="small">
        <Space wrap>
          <Select placeholder="Kho kiểm kê" style={{ width: 220 }} value={khoId ?? undefined}
            onChange={(v) => { setKhoId(v); setLastResult(null); }}
            options={warehouses.map((w) => ({ value: w.id, label: w.name }))} />
          <Input allowClear placeholder="Tìm mặt hàng..." style={{ width: 220 }} value={search}
            onChange={(e) => setSearch(e.target.value)} />
          <Button icon={<ReloadOutlined />} onClick={loadStock} loading={loading}>Tải tồn kho</Button>
          <Button type="primary" onClick={save} loading={saving}
            disabled={chenhLechRows.length === 0 || !khoId}>
            LẬP PHIẾU KIỂM KÊ ({chenhLechRows.length} chênh lệch)
          </Button>
        </Space>
      </Card>
      {err && <Alert type="error" showIcon message={err} closable />}
      {lastResult && (
        <Card size="small" style={{ background: '#f6ffed', borderColor: '#b7eb8f' }}>
          <b>Phiếu {lastResult.soPhieu}</b> — {lastResult.linesAdjusted} dòng hiệu chỉnh
          {lastResult.tienTang > 0 && ` · Tăng ${fmtVnd(lastResult.tienTang)}`}
          {lastResult.tienGiam > 0 && ` · Giảm ${fmtVnd(lastResult.tienGiam)}`}
          <Button type="link" size="small" style={{ padding: 0, marginLeft: 8 }}
            onClick={() => printStockVoucherPdf(lastResult.donHangId)}>
            In phiếu
          </Button>
        </Card>
      )}
      <Card size="small" title={`Bảng kiểm kê — SL hệ thống so với SL thực tế (${rows.length})`} styles={{ body: { padding: 0 } }}>
        <Table
          size="small" rowKey="matHangId" loading={loading}
          dataSource={rows.filter((r) => {
            const s = search.trim().toLowerCase();
            if (!s) return true;
            return (r.name ?? '').toLowerCase().includes(s) || (r.code ?? '').toLowerCase().includes(s);
          })}
          pagination={{ pageSize: 15, showTotal: (t) => `Tổng ${t} dòng` }}
          columns={[
            { title: 'Mã', dataIndex: 'code', width: 110, render: (v) => <b>{v ?? '—'}</b> },
            { title: 'Mặt hàng', dataIndex: 'name' },
            { title: 'SL hệ thống', dataIndex: 'ton', width: 110, align: 'right',
              render: (v: number) => v.toLocaleString('vi-VN') },
            { title: 'SL thực tế', width: 120, render: (_, r: StocktakeRow) => (
              <InputNumber size="small" min={0} value={r.slThucTe}
                status={r.slThucTe !== r.ton ? 'warning' : undefined}
                onChange={(v) => setSlThucTe(r.matHangId, v ?? 0)} />
            ) },
            { title: 'Chênh lệch', key: 'cl', width: 100, align: 'right', render: (_, r: StocktakeRow) => {
              const d = r.slThucTe - r.ton;
              if (d === 0) return '—';
              return <b style={{ color: d > 0 ? '#389e0d' : '#cf1322' }}>{d > 0 ? '+' : ''}{d}</b>;
            } },
            { title: 'Giá vốn', dataIndex: 'giaVon', width: 110, align: 'right', render: fmtVnd },
            { title: 'Giá trị lệch', key: 'gt', width: 120, align: 'right', render: (_, r: StocktakeRow) => {
              const d = (r.slThucTe - r.ton) * r.giaVon;
              if (d === 0) return '—';
              return <span style={{ color: d > 0 ? '#389e0d' : '#cf1322' }}>{fmtVnd(Math.abs(d))}</span>;
            } },
          ]}
          locale={{ emptyText: <Empty description="Kho chưa có tồn kho" /> }}
        />
      </Card>
    </Space>
  );
};

export default StocktakesPage;
