import React, { useEffect, useMemo, useState } from 'react';
import { Button, Card, Col, Descriptions, Empty, Input, InputNumber, Row, Select, Space, Table, message } from 'antd';
import { DeleteOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { createPurchaseOrder, type QuoteLineInput } from '../../api/salesdocs';
import { getProducts, type Product } from '../../api/products';
import { getSuppliers, type Supplier } from '../../api/suppliers';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

interface PoLine {
  matHangId: string;
  tenMatHang: string;
  donViTinh: string | null;
  soLuong: number;
  donGia: number;
  tileGiamGia: number;
}

/** Đặt mua hàng nhà cung cấp (mục 4.9). */
const PurchaseOrdersPage: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [search, setSearch] = useState('');
  const [nhaCungCapId, setNhaCungCapId] = useState<string | undefined>(undefined);
  const [lines, setLines] = useState<PoLine[]>([]);
  const [tienThue, setTienThue] = useState(0);
  const [tienGiamGia, setTienGiamGia] = useState(0);
  const [phiVanChuyen, setPhiVanChuyen] = useState(0);
  const [ghiChu, setGhiChu] = useState('');
  const [saving, setSaving] = useState(false);
  const [last, setLast] = useState<{ soPhieu: string; tongCong: number } | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    getSuppliers({ pageSize: 200, activeOnly: true }).then((r) => setSuppliers(r.items)).catch(() => { });
    getProducts({ pageSize: 50, activeOnly: true }).then((r) => setProducts(r.items)).catch(() => { });
  }, []);

  const tienHang = useMemo(
    () => lines.reduce((s, l) => s + l.soLuong * l.donGia * (1 - l.tileGiamGia / 100), 0),
    [lines]
  );
  const giam = Math.min(Math.max(0, tienGiamGia), tienHang);
  const tongCong = tienHang - giam + Math.max(0, tienThue) + Math.max(0, phiVanChuyen);

  const addLine = (p: Product) => {
    setLines((prev) => {
      const found = prev.find((l) => l.matHangId === p.id);
      if (found) return prev.map((l) => l.matHangId === p.id ? { ...l, soLuong: l.soLuong + 1 } : l);
      return [...prev, {
        matHangId: p.id, tenMatHang: p.name, donViTinh: p.donViTinh,
        soLuong: 1, donGia: p.giaNhap > 0 ? p.giaNhap : p.giaVon, tileGiamGia: 0,
      }];
    });
  };
  const updateLine = (matHangId: string, patch: Partial<PoLine>) =>
    setLines((prev) => prev.map((l) => l.matHangId === matHangId ? { ...l, ...patch } : l));
  const removeLine = (matHangId: string) =>
    setLines((prev) => prev.filter((l) => l.matHangId !== matHangId));
  const resetForm = () => { setLines([]); setTienThue(0); setTienGiamGia(0); setPhiVanChuyen(0); setGhiChu(''); };

  const save = async () => {
    if (!nhaCungCapId) { void message.warning('Chưa chọn nhà cung cấp'); return; }
    if (lines.length === 0) { void message.warning('Chưa có mặt hàng nào'); return; }
    setSaving(true);
    setErr(null);
    try {
      const result = await createPurchaseOrder({
        nhaCungCapId,
        lines: lines.map<QuoteLineInput>((l) => ({
          matHangId: l.matHangId, soLuong: l.soLuong, donGia: l.donGia, tileGiamGia: l.tileGiamGia, note: null,
        })),
        tienThue, tienGiamGia: giam, phiVanChuyen,
        ghiChu: ghiChu.trim() || null,
      });
      setLast(result);
      void message.success(`Đã tạo đơn đặt mua NCC ${result.soPhieu}`);
      resetForm();
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tạo đơn đặt mua');
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
        <Card size="small" title="Chọn mặt hàng đặt mua" extra={
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
                      <span>{fmtVnd(p.giaNhap)}</span>
                      <PlusOutlined style={{ color: '#d4a017' }} />
                    </div>
                  </Card>
                </Col>
              ))}
            </Row>
            {filteredProducts.length === 0 && <Empty description="Không có mặt hàng" />}
          </div>
        </Card>

        <Card size="small" title={`Dòng đặt mua (${lines.length})`} style={{ marginTop: 12 }} styles={{ body: { padding: 0 } }}>
          <Table
            size="small" rowKey="matHangId" pagination={false}
            dataSource={lines}
            columns={[
              { title: 'Mặt hàng', dataIndex: 'tenMatHang' },
              { title: 'SL', width: 90, render: (_, l: PoLine) => (
                <InputNumber size="small" min={0.001} value={l.soLuong}
                  onChange={(v) => updateLine(l.matHangId, { soLuong: v ?? 1 })} />
              ) },
              { title: 'Đơn giá', width: 130, render: (_, l: PoLine) => (
                <InputNumber size="small" min={0} step={1000} style={{ width: '100%' }} value={l.donGia}
                  onChange={(v) => updateLine(l.matHangId, { donGia: v ?? 0 })} />
              ) },
              { title: 'Giảm %', width: 90, render: (_, l: PoLine) => (
                <InputNumber size="small" min={0} max={100} value={l.tileGiamGia}
                  onChange={(v) => updateLine(l.matHangId, { tileGiamGia: v ?? 0 })} />
              ) },
              { title: 'Thành tiền', width: 130, align: 'right',
                render: (_, l: PoLine) => <b>{fmtVnd(l.soLuong * l.donGia * (1 - l.tileGiamGia / 100))}</b> },
              { title: '', key: 'del', width: 40,
                render: (_, l: PoLine) => <Button type="text" danger size="small" icon={<DeleteOutlined />}
                  onClick={() => removeLine(l.matHangId)} /> },
            ]}
            locale={{ emptyText: <Empty description="Chưa có hàng — click thẻ mặt hàng để thêm" /> }}
          />
        </Card>
      </Col>

      <Col xs={24} lg={10}>
        <Card size="small" title="Đơn đặt mua nhà cung cấp">
          <Space direction="vertical" style={{ width: '100%' }} size={8}>
            <Select showSearch optionFilterProp="label" placeholder="Nhà cung cấp"
              style={{ width: '100%' }} value={nhaCungCapId} onChange={(v) => setNhaCungCapId(v)}
              options={suppliers.map((s) => ({ value: s.id, label: `${s.maNhaCungCap ?? ''} — ${s.name}` }))} />
            <Input allowClear placeholder="Ghi chú" value={ghiChu} onChange={(e) => setGhiChu(e.target.value)} />

            <Descriptions column={1} size="small" bordered>
              <Descriptions.Item label="Tiền hàng"><b>{fmtVnd(tienHang)}</b></Descriptions.Item>
              <Descriptions.Item label="Thuế">
                <InputNumber min={0} value={tienThue} style={{ width: 140 }} onChange={(v) => setTienThue(v ?? 0)} />
              </Descriptions.Item>
              <Descriptions.Item label="Giảm giá">
                <InputNumber min={0} max={tienHang} value={tienGiamGia} style={{ width: 140 }} onChange={(v) => setTienGiamGia(v ?? 0)} />
              </Descriptions.Item>
              <Descriptions.Item label="Phí vận chuyển">
                <InputNumber min={0} value={phiVanChuyen} style={{ width: 140 }} onChange={(v) => setPhiVanChuyen(v ?? 0)} />
              </Descriptions.Item>
              <Descriptions.Item label="Tổng cộng">
                <span style={{ color: '#8a6100', fontSize: 18, fontWeight: 700 }}>{fmtVnd(tongCong)}</span>
              </Descriptions.Item>
            </Descriptions>

            <Button type="primary" size="large" block loading={saving} onClick={save}
              disabled={lines.length === 0 || !nhaCungCapId}>
              TẠO ĐƠN ĐẶT MUA
            </Button>
            <Button block onClick={resetForm}>Xoá form</Button>
            {err && <Card size="small" style={{ background: '#fff2f0', borderColor: '#ffccc7' }}>{err}</Card>}
            {last && (
              <Card size="small" style={{ background: '#f6ffed', borderColor: '#b7eb8f' }}>
                <b>Đơn {last.soPhieu}</b> — Tổng {fmtVnd(last.tongCong)}
              </Card>
            )}
          </Space>
        </Card>
      </Col>
    </Row>
  );
};

export default PurchaseOrdersPage;