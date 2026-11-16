import React, { useEffect, useMemo, useState } from 'react';
import { Button, Card, Col, Descriptions, Empty, Input, InputNumber, Row, Select, Space, Table, message } from 'antd';
import { DeleteOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { createStockTransfer, type StockTransferResult } from '../../api/inventory';
import { printStockVoucherPdf, downloadStockVoucherPdf } from '../../api/reports';
import { getProducts, type Product } from '../../api/products';
import { getWarehouses, type Warehouse } from '../../api/warehouses';

const fmtQty = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 3 });

interface TransferLine {
  matHangId: string;
  tenMatHang: string;
  donViTinh: string | null;
  soLuong: number;
}

/** Chuyển kho (mục 4.6) — kho xuất → kho nhập, cùng một phiếu. */
const TransfersPage: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [search, setSearch] = useState('');
  const [khoXuatId, setKhoXuatId] = useState<string | undefined>(undefined);
  const [khoNhapId, setKhoNhapId] = useState<string | undefined>(undefined);
  const [lines, setLines] = useState<TransferLine[]>([]);
  const [ghiChu, setGhiChu] = useState('');
  const [saving, setSaving] = useState(false);
  const [last, setLast] = useState<StockTransferResult | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    getWarehouses({ activeOnly: true, pageSize: 50 }).then((r) => {
      setWarehouses(r.items);
      if (r.items.length > 0) setKhoXuatId((k) => k ?? r.items[0].id);
      if (r.items.length > 1) setKhoNhapId((k) => k ?? r.items[1].id);
    }).catch(() => { });
  }, []);

  // Tồn tại kho xuất đang chọn — reload khi đổi kho
  useEffect(() => {
    getProducts({ pageSize: 50, activeOnly: true, khoId: khoXuatId }).then((r) => setProducts(r.items)).catch(() => { });
  }, [khoXuatId]);

  const tongSoLuong = useMemo(() => lines.reduce((s, l) => s + l.soLuong, 0), [lines]);

  const addLine = (p: Product) => {
    setLines((prev) => {
      const found = prev.find((l) => l.matHangId === p.id);
      if (found) return prev.map((l) => l.matHangId === p.id ? { ...l, soLuong: l.soLuong + 1 } : l);
      return [...prev, { matHangId: p.id, tenMatHang: p.name, donViTinh: p.donViTinh, soLuong: 1 }];
    });
  };
  const updateLine = (matHangId: string, patch: Partial<TransferLine>) =>
    setLines((prev) => prev.map((l) => l.matHangId === matHangId ? { ...l, ...patch } : l));
  const removeLine = (matHangId: string) =>
    setLines((prev) => prev.filter((l) => l.matHangId !== matHangId));

  const save = async () => {
    if (!khoXuatId) { void message.warning('Chưa chọn kho xuất'); return; }
    if (!khoNhapId) { void message.warning('Chưa chọn kho nhập'); return; }
    if (khoXuatId === khoNhapId) { void message.warning('Kho nhập phải khác kho xuất'); return; }
    if (lines.length === 0) { void message.warning('Chưa có mặt hàng nào'); return; }
    setSaving(true);
    setErr(null);
    try {
      const result = await createStockTransfer({
        khoXuatId,
        khoNhapId,
        nhanVienXuatId: null,
        nhanVienNhapId: null,
        lines: lines.map((l) => ({ matHangId: l.matHangId, soLuong: l.soLuong, note: null })),
        ghiChu: ghiChu.trim() || null,
      });
      setLast(result);
      void message.success(`Đã tạo phiếu chuyển kho ${result.soPhieu}`);
      setLines([]);
      setGhiChu('');
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tạo phiếu chuyển kho');
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

  return (
    <Row gutter={12}>
      <Col xs={24} lg={14}>
        <Card size="small" className="luxury-card" title={
          <span className="card-title-gold">Chuyển kho · Kho xuất <span style={{ color: '#D4AF37' }}>➔</span> Kho nhập</span>
        } extra={
          <Input allowClear prefix={<SearchOutlined style={{ color: '#D4AF37' }} />} placeholder="Tìm mã / tên hàng..."
            style={{ width: 220 }} value={search} onChange={(e) => setSearch(e.target.value)} />
        }>
          <div style={{ maxHeight: 340, overflowY: 'auto' }}>
            <Row gutter={[8, 8]}>
              {filteredProducts.map((p) => (
                <Col key={p.id} xs={12} md={8} lg={6}>
                  <Card size="small" hoverable className="pos-product-card" onClick={() => addLine(p)}>
                    <div style={{ fontWeight: 600, fontSize: 13, color: '#1C180E' }}>{p.name}</div>
                    <div style={{ color: '#7E7359', fontSize: 12 }}>{p.code ?? ''} · {p.donViTinh ?? ''}</div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 4 }}>
                      <span style={{ fontSize: 12, color: p.tonKho > 0 ? '#10B981' : '#E11D48', fontWeight: 600 }}>
                        Tồn: {p.tonKho.toLocaleString('vi-VN')}
                      </span>
                      <PlusOutlined style={{ color: '#D4AF37' }} />
                    </div>
                  </Card>
                </Col>
              ))}
            </Row>
            {filteredProducts.length === 0 && <Empty description="Không có mặt hàng" />}
          </div>
        </Card>

        <Card size="small" className="luxury-card" title={<span className="card-title-gold">Dòng chuyển ({lines.length})</span>} style={{ marginTop: 12 }} styles={{ body: { padding: 0 } }}>
          <Table
            className="luxury-table"
            size="small" rowKey="matHangId" pagination={false}
            dataSource={lines}
            columns={[
              { title: 'Mặt hàng', dataIndex: 'tenMatHang' },
              { title: 'ĐVT', dataIndex: 'donViTinh', width: 80 },
              { title: 'SL chuyển', width: 110, render: (_, l: TransferLine) => (
                <InputNumber size="small" min={1} precision={0} value={l.soLuong}
                  onChange={(v) => updateLine(l.matHangId, { soLuong: v ?? 1 })} />
              ) },
              { title: '', key: 'del', width: 40,
                render: (_, l: TransferLine) => <Button type="text" danger size="small" icon={<DeleteOutlined />}
                  onClick={() => removeLine(l.matHangId)} /> },
            ]}
            locale={{ emptyText: <Empty description="Chưa có hàng — click thẻ mặt hàng để thêm" /> }}
          />
        </Card>
      </Col>

      <Col xs={24} lg={10}>
        <Card size="small" className="luxury-card" title={<span className="card-title-gold">Phiếu chuyển kho</span>}>
          <Space direction="vertical" style={{ width: '100%' }} size={8}>
            <Select placeholder="Kho xuất" style={{ width: '100%' }} value={khoXuatId ?? undefined}
              onChange={(v) => setKhoXuatId(v)}
              options={warehouses.map((w) => ({ value: w.id, label: w.name }))} />
            <Select placeholder="Kho nhập" style={{ width: '100%' }} value={khoNhapId ?? undefined}
              onChange={(v) => setKhoNhapId(v)}
              options={warehouses.map((w) => ({ value: w.id, label: w.name, disabled: w.id === khoXuatId }))} />
            <Input allowClear placeholder="Ghi chú / Diễn giải" value={ghiChu}
              onChange={(e) => setGhiChu(e.target.value)} />

            <Descriptions column={1} size="small" bordered>
              <Descriptions.Item label="Số mặt hàng"><b>{lines.length}</b></Descriptions.Item>
              <Descriptions.Item label="Tổng số lượng"><b>{fmtQty(tongSoLuong)}</b></Descriptions.Item>
            </Descriptions>

            <Button type="primary" size="large" block loading={saving} onClick={save}
              className="gold-gradient-btn"
              disabled={lines.length === 0 || !khoXuatId || !khoNhapId || khoXuatId === khoNhapId}>
              TẠO PHIẾU CHUYỂN KHO
            </Button>
            <Button block onClick={() => { setLines([]); setGhiChu(''); }}>Xoá form</Button>
            {err && <Card size="small" style={{ background: '#fff2f0', borderColor: '#ffccc7' }}>{err}</Card>}
            {last && (
              <Card size="small" style={{ background: '#f6ffed', borderColor: '#b7eb8f' }}>
                <b>Phiếu {last.soPhieu}</b> — {last.soDong} dòng · {fmtQty(last.tongSoLuong)} đơn vị
                <Button type="link" size="small" style={{ padding: 0, marginLeft: 8 }}
                  onClick={() => printStockVoucherPdf(last.donHangId)}>
                  In phiếu
                </Button>
                <Button type="link" size="small" style={{ padding: 0, marginLeft: 8 }}
                  onClick={() => downloadStockVoucherPdf(last.donHangId)}>
                  Tải về
                </Button>
              </Card>
            )}
          </Space>
        </Card>
      </Col>
    </Row>
  );
};

export default TransfersPage;