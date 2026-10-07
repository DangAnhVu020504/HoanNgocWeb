import React, { useEffect, useMemo, useState } from 'react';
import { Alert, Button, Card, Col, Descriptions, Empty, Input, InputNumber, Row, Select, Space, Table, message } from 'antd';
import { DeleteOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { createReceipt, getInventory, type ReceiptResult, type InventoryItem } from '../../api/inventory';
import { printStockVoucherPdf } from '../../api/reports';
import { getProducts, type Product } from '../../api/products';
import { getSuppliers, type Supplier } from '../../api/suppliers';
import { getWarehouses, type Warehouse } from '../../api/warehouses';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

interface CartLine {
  matHangId: string;
  tenMatHang: string;
  donViTinh: string | null;
  slNhap: number;
  donGia: number;
}

const ReceiptsPage: React.FC = () => {
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [search, setSearch] = useState('');
  const [khoNhapId, setKhoNhapId] = useState<string | undefined>(undefined);
  const [nhaCungCapId, setNhaCungCapId] = useState<string | null>(null);
  const [lines, setLines] = useState<CartLine[]>([]);
  const [tienGiamGia, setTienGiamGia] = useState(0);
  const [tienThue, setTienThue] = useState(0);
  const [phiVanChuyen, setPhiVanChuyen] = useState(0);
  const [daThanhToan, setDaThanhToan] = useState(0);
  const [ghiChu, setGhiChu] = useState('');
  const [saving, setSaving] = useState(false);
  const [lastReceipt, setLastReceipt] = useState<ReceiptResult | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [stockByProduct, setStockByProduct] = useState<Record<string, number>>({});

  useEffect(() => {
    getWarehouses({ activeOnly: true, pageSize: 50 }).then((r) => {
      setWarehouses(r.items);
      if (r.items.length > 0) setKhoNhapId((k) => k ?? r.items[0].id);
    }).catch(() => { });
    getSuppliers({ pageSize: 200, activeOnly: true }).then((r) => setSuppliers(r.items)).catch(() => { });
    getProducts({ pageSize: 50, activeOnly: true }).then((r) => setProducts(r.items)).catch(() => { });
  }, []);

  // Tồn hiện tại theo kho nhập — reload khi đổi kho / sau khi nhập
  useEffect(() => {
    if (!khoNhapId) return;
    getInventory({ khoId: khoNhapId, nonZeroOnly: false, pageSize: 500 }).then((r) => {
      const map: Record<string, number> = {};
      for (const it of r.items as InventoryItem[]) map[it.matHangId] = it.ton;
      setStockByProduct(map);
    }).catch(() => { });
  }, [khoNhapId, lastReceipt]);

  const tienHang = useMemo(
    () => lines.reduce((sum, l) => sum + l.slNhap * l.donGia, 0),
    [lines]
  );
  const giam = Math.min(Math.max(0, tienGiamGia), tienHang);
  const tongCong = tienHang - giam + Math.max(0, tienThue) + Math.max(0, phiVanChuyen);
  const conLai = Math.max(0, tongCong - Math.min(daThanhToan, tongCong));

  const addLine = (p: Product) => {
    setLines((prev) => {
      const found = prev.find((l) => l.matHangId === p.id);
      if (found) {
        return prev.map((l) => l.matHangId === p.id ? { ...l, slNhap: l.slNhap + 1 } : l);
      }
      return [...prev, {
        matHangId: p.id, tenMatHang: p.name, donViTinh: p.donViTinh,
        slNhap: 1, donGia: p.giaNhap > 0 ? p.giaNhap : p.giaVon,
      }];
    });
  };

  const updateLine = (matHangId: string, patch: Partial<CartLine>) =>
    setLines((prev) => prev.map((l) => l.matHangId === matHangId ? { ...l, ...patch } : l));

  const removeLine = (matHangId: string) =>
    setLines((prev) => prev.filter((l) => l.matHangId !== matHangId));

  const resetForm = () => {
    setLines([]);
    setTienGiamGia(0);
    setTienThue(0);
    setPhiVanChuyen(0);
    setDaThanhToan(0);
    setGhiChu('');
  };

  const save = async () => {
    if (!khoNhapId) { void message.warning('Chưa chọn kho nhập'); return; }
    if (lines.length === 0) { void message.warning('Chưa có mặt hàng nào'); return; }
    setSaving(true);
    setErr(null);
    try {
      const result = await createReceipt({
        khoNhapId,
        nhaCungCapId,
        nhanVienId: null,
        lines: lines.map((l) => ({ matHangId: l.matHangId, slNhap: l.slNhap, donGia: l.donGia, note: null })),
        tienGiamGia: giam,
        tienThue,
        phiVanChuyen,
        daThanhToan: Math.min(daThanhToan, tongCong),
        ghiChu: ghiChu.trim() || null,
      });
      setLastReceipt(result);
      void message.success(`Đã tạo phiếu nhập ${result.soPhieu}`);
      resetForm();
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tạo phiếu nhập kho');
    } finally {
      setSaving(false);
    }
  };

  const filteredProducts = useMemo(() => {
    const s = search.trim().toLowerCase();
    if (!s) return products;
    return products.filter(
      (p) => (p.name ?? '').toLowerCase().includes(s) || (p.code ?? '').toLowerCase().includes(s)
    );
  }, [products, search]);

  const tonCua = (id: string) => stockByProduct[id] ?? 0;

  return (
    <Row gutter={12}>
      <Col xs={24} lg={14}>
        <Card size="small" title="Chọn mặt hàng nhập" extra={
          <Input allowClear prefix={<SearchOutlined />} placeholder="Tìm mã / tên hàng..."
            style={{ width: 220 }} value={search} onChange={(e) => setSearch(e.target.value)} />
        }>
          <div style={{ maxHeight: 340, overflowY: 'auto' }}>
            <Row gutter={[8, 8]}>
              {filteredProducts.map((p) => (
                <Col key={p.id} xs={12} md={8} lg={6}>
                  <Card size="small" hoverable onClick={() => addLine(p)}>
                    <div style={{ fontWeight: 600, fontSize: 13 }}>{p.name}</div>
                    <div style={{ color: '#888', fontSize: 12 }}>{p.code ?? ''} · {p.donViTinh ?? ''}</div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 4 }}>
                      <span>Giá nhập: {fmtVnd(p.giaNhap)}</span>
                      <span style={{ fontSize: 12, color: '#389e0d' }}>
                        Tồn: {tonCua(p.id).toLocaleString('vi-VN')}
                      </span>
                      <PlusOutlined style={{ color: '#d4a017' }} />
                    </div>
                  </Card>
                </Col>
              ))}
            </Row>
            {filteredProducts.length === 0 && <Empty description="Không có mặt hàng" />}
          </div>
        </Card>

        <Card size="small" title={`Dòng nhập (${lines.length})`} style={{ marginTop: 12 }} styles={{ body: { padding: 0 } }}>
          <Table
            size="small" rowKey="matHangId" pagination={false}
            dataSource={lines}
            columns={[
              { title: 'Mặt hàng', dataIndex: 'tenMatHang' },
              { title: 'SL nhập', width: 110, render: (_, l: CartLine) => (
                <InputNumber size="small" min={0.001} value={l.slNhap}
                  onChange={(v) => updateLine(l.matHangId, { slNhap: v ?? 1 })} />
              ) },
              { title: 'Đơn giá', width: 130, render: (_, l: CartLine) => (
                <InputNumber size="small" min={0} step={1000} style={{ width: '100%' }} value={l.donGia}
                  onChange={(v) => updateLine(l.matHangId, { donGia: v ?? 0 })} />
              ) },
              { title: 'Thành tiền', width: 130, align: 'right',
                render: (_, l: CartLine) => <b>{fmtVnd(l.slNhap * l.donGia)}</b> },
              { title: '', key: 'del', width: 40,
                render: (_, l: CartLine) => <Button type="text" danger size="small" icon={<DeleteOutlined />}
                  onClick={() => removeLine(l.matHangId)} /> },
            ]}
            locale={{ emptyText: <Empty description="Chưa có hàng — click thẻ mặt hàng để thêm" /> }}
          />
        </Card>
      </Col>

      <Col xs={24} lg={10}>
        <Card size="small" title="Phiếu nhập kho">
          <Space direction="vertical" style={{ width: '100%' }} size={8}>
            <Select placeholder="Kho nhập" style={{ width: '100%' }} value={khoNhapId ?? undefined}
              onChange={(v) => setKhoNhapId(v)}
              options={warehouses.map((w) => ({ value: w.id, label: w.name }))} />
            <Select allowClear showSearch optionFilterProp="label" placeholder="Nhà cung cấp"
              style={{ width: '100%' }} value={nhaCungCapId ?? undefined}
              onChange={(v) => setNhaCungCapId(v ?? null)}
              options={suppliers.map((s) => ({ value: s.id, label: `${s.maNhaCungCap ?? ''} — ${s.name}` }))} />
            <Input allowClear placeholder="Ghi chú / Diễn giải" value={ghiChu}
              onChange={(e) => setGhiChu(e.target.value)} />

            <Descriptions column={1} size="small" bordered>
              <Descriptions.Item label="Tiền hàng"><b>{fmtVnd(tienHang)}</b></Descriptions.Item>
              <Descriptions.Item label="Giảm giá">
                <InputNumber min={0} max={tienHang} value={tienGiamGia} style={{ width: 140 }}
                  onChange={(v) => setTienGiamGia(v ?? 0)} />
              </Descriptions.Item>
              <Descriptions.Item label="Tiền thuế">
                <InputNumber min={0} value={tienThue} style={{ width: 140 }}
                  onChange={(v) => setTienThue(v ?? 0)} />
              </Descriptions.Item>
              <Descriptions.Item label="Phí vận chuyển">
                <InputNumber min={0} value={phiVanChuyen} style={{ width: 140 }}
                  onChange={(v) => setPhiVanChuyen(v ?? 0)} />
              </Descriptions.Item>
              <Descriptions.Item label="Tổng cộng">
                <span style={{ color: '#8a6100', fontSize: 18, fontWeight: 700 }}>{fmtVnd(tongCong)}</span>
              </Descriptions.Item>
              <Descriptions.Item label="Đã thanh toán">
                <InputNumber min={0} step={10000} value={daThanhToan} style={{ width: 140 }}
                  onChange={(v) => setDaThanhToan(v ?? 0)} />
              </Descriptions.Item>
              <Descriptions.Item label="Còn lại">
                {conLai > 0 ? <span style={{ color: '#cf1322', fontWeight: 700 }}>{fmtVnd(conLai)}</span> : '—'}
              </Descriptions.Item>
            </Descriptions>

            <Button type="primary" size="large" block loading={saving} onClick={save}
              disabled={lines.length === 0 || !khoNhapId}>
              TẠO PHIẾU NHẬP
            </Button>
            <Button block onClick={resetForm}>Xoá form</Button>
            {err && <Alert type="error" showIcon message={err} closable />}
            {lastReceipt && (
              <Card size="small" style={{ background: '#f6ffed', borderColor: '#b7eb8f' }}>
                <b>Phiếu {lastReceipt.soPhieu}</b> — Tổng {fmtVnd(lastReceipt.tongCong)}
                {lastReceipt.daThanhToan > 0 && ` · Đã trả ${fmtVnd(lastReceipt.daThanhToan)}`}
                {lastReceipt.conLai > 0 && ` · Còn lại ${fmtVnd(lastReceipt.conLai)}`}
                <Button type="link" size="small" style={{ padding: 0, marginLeft: 8 }}
                  onClick={() => printStockVoucherPdf(lastReceipt.donHangId)}>
                  In phiếu
                </Button>
              </Card>
            )}
          </Space>
        </Card>
      </Col>
    </Row>
  );
};

export default ReceiptsPage;
