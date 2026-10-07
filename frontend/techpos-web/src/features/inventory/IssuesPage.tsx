import React, { useEffect, useMemo, useState } from 'react';
import { Button, Card, Col, Descriptions, Empty, Input, InputNumber, Row, Select, Space, Table, message } from 'antd';
import { DeleteOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { createStockIssue, type StockIssueResult } from '../../api/inventory';
import { printStockVoucherPdf } from '../../api/reports';
import { getProducts, type Product } from '../../api/products';
import { getWarehouses, type Warehouse } from '../../api/warehouses';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

interface IssueLine {
  matHangId: string;
  tenMatHang: string;
  donViTinh: string | null;
  soLuong: number;
  donGia: number;
}

/** Xuất kho (mục 4.5) — xuất nội bộ, không qua bán hàng. */
const IssuesPage: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [search, setSearch] = useState('');
  const [khoXuatId, setKhoXuatId] = useState<string | undefined>(undefined);
  const [dienGiai, setDienGiai] = useState('Xuất kho');
  const [lines, setLines] = useState<IssueLine[]>([]);
  const [ghiChu, setGhiChu] = useState('');
  const [saving, setSaving] = useState(false);
  const [last, setLast] = useState<StockIssueResult | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    getWarehouses({ activeOnly: true, pageSize: 50 }).then((r) => {
      setWarehouses(r.items);
      if (r.items.length > 0) setKhoXuatId((k) => k ?? r.items[0].id);
    }).catch(() => { });
    getProducts({ pageSize: 50, activeOnly: true }).then((r) => setProducts(r.items)).catch(() => { });
  }, []);

  const tienHang = useMemo(() => lines.reduce((s, l) => s + l.soLuong * l.donGia, 0), [lines]);

  const addLine = (p: Product) => {
    setLines((prev) => {
      const found = prev.find((l) => l.matHangId === p.id);
      if (found) return prev.map((l) => l.matHangId === p.id ? { ...l, soLuong: l.soLuong + 1 } : l);
      return [...prev, {
        matHangId: p.id, tenMatHang: p.name, donViTinh: p.donViTinh,
        soLuong: 1, donGia: p.giaVon,
      }];
    });
  };
  const updateLine = (matHangId: string, patch: Partial<IssueLine>) =>
    setLines((prev) => prev.map((l) => l.matHangId === matHangId ? { ...l, ...patch } : l));
  const removeLine = (matHangId: string) =>
    setLines((prev) => prev.filter((l) => l.matHangId !== matHangId));

  const save = async () => {
    if (!khoXuatId) { void message.warning('Chưa chọn kho xuất'); return; }
    if (lines.length === 0) { void message.warning('Chưa có mặt hàng nào'); return; }
    setSaving(true);
    setErr(null);
    try {
      const result = await createStockIssue({
        khoXuatId,
        nhanVienId: null,
        dienGiai: dienGiai.trim() || null,
        lines: lines.map((l) => ({ matHangId: l.matHangId, soLuong: l.soLuong, donGia: l.donGia, note: null })),
        ghiChu: ghiChu.trim() || null,
      });
      setLast(result);
      void message.success(`Đã tạo phiếu xuất kho ${result.soPhieu}`);
      setLines([]);
      setGhiChu('');
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tạo phiếu xuất kho');
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
        <Card size="small" title="Chọn mặt hàng xuất" extra={
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
                      <span>Giá vốn: {fmtVnd(p.giaVon)}</span>
                      <PlusOutlined style={{ color: '#d4a017' }} />
                    </div>
                  </Card>
                </Col>
              ))}
            </Row>
            {filteredProducts.length === 0 && <Empty description="Không có mặt hàng" />}
          </div>
        </Card>

        <Card size="small" title={`Dòng xuất (${lines.length})`} style={{ marginTop: 12 }} styles={{ body: { padding: 0 } }}>
          <Table
            size="small" rowKey="matHangId" pagination={false}
            dataSource={lines}
            columns={[
              { title: 'Mặt hàng', dataIndex: 'tenMatHang' },
              { title: 'SL xuất', width: 100, render: (_, l: IssueLine) => (
                <InputNumber size="small" min={0.001} value={l.soLuong}
                  onChange={(v) => updateLine(l.matHangId, { soLuong: v ?? 1 })} />
              ) },
              { title: 'Đơn giá', width: 140, render: (_, l: IssueLine) => (
                <InputNumber size="small" min={0} step={1000} style={{ width: '100%' }} value={l.donGia}
                  onChange={(v) => updateLine(l.matHangId, { donGia: v ?? 0 })} />
              ) },
              { title: 'Thành tiền', width: 130, align: 'right',
                render: (_, l: IssueLine) => <b>{fmtVnd(l.soLuong * l.donGia)}</b> },
              { title: '', key: 'del', width: 40,
                render: (_, l: IssueLine) => <Button type="text" danger size="small" icon={<DeleteOutlined />}
                  onClick={() => removeLine(l.matHangId)} /> },
            ]}
            locale={{ emptyText: <Empty description="Chưa có hàng — click thẻ mặt hàng để thêm" /> }}
          />
        </Card>
      </Col>

      <Col xs={24} lg={10}>
        <Card size="small" title="Phiếu xuất kho">
          <Space direction="vertical" style={{ width: '100%' }} size={8}>
            <Select placeholder="Kho xuất" style={{ width: '100%' }} value={khoXuatId ?? undefined}
              onChange={(v) => setKhoXuatId(v)}
              options={warehouses.map((w) => ({ value: w.id, label: w.name }))} />
            <Input allowClear placeholder="Diễn giải / Lý do xuất" value={dienGiai}
              onChange={(e) => setDienGiai(e.target.value)} />
            <Input allowClear placeholder="Ghi chú" value={ghiChu}
              onChange={(e) => setGhiChu(e.target.value)} />

            <Descriptions column={1} size="small" bordered>
              <Descriptions.Item label="Tiền hàng"><b>{fmtVnd(tienHang)}</b></Descriptions.Item>
              <Descriptions.Item label="Tổng cộng">
                <span style={{ color: '#8a6100', fontSize: 18, fontWeight: 700 }}>{fmtVnd(tienHang)}</span>
              </Descriptions.Item>
            </Descriptions>

            <Button type="primary" size="large" block loading={saving} onClick={save}
              disabled={lines.length === 0 || !khoXuatId}>
              TẠO PHIẾU XUẤT KHO
            </Button>
            <Button block onClick={() => { setLines([]); setGhiChu(''); }}>Xoá form</Button>
            {err && <Card size="small" style={{ background: '#fff2f0', borderColor: '#ffccc7' }}>{err}</Card>}
            {last && (
              <Card size="small" style={{ background: '#f6ffed', borderColor: '#b7eb8f' }}>
                <b>Phiếu {last.soPhieu}</b> — {last.soDong} dòng · Tổng {fmtVnd(last.tienHang)}
                <Button type="link" size="small" style={{ padding: 0, marginLeft: 8 }}
                  onClick={() => printStockVoucherPdf(last.donHangId)}>
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

export default IssuesPage;