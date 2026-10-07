import React, { useEffect, useMemo, useState } from 'react';
import { Alert, Button, Card, Col, Descriptions, Empty, Input, InputNumber, Row, Select, Space, Table, Tag, message } from 'antd';
import { DeleteOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { createWholesale, type PosSaleResult } from '../../api/pos';
import { getProducts, type Product } from '../../api/products';
import { getCustomers, type Customer } from '../../api/customers';
import { getWarehouses, type Warehouse } from '../../api/warehouses';
import { getInventory, type InventoryItem } from '../../api/inventory';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

interface CartLine {
  matHangId: string;
  tenMatHang: string;
  donViTinh: string | null;
  soLuong: number;
  donGia: number;
  giamTheoTien: number;
}

/** Bán sỉ (mục 4.2) — hoá đơn BS, giá sỉ (GIABAN2), chiết khấu, công nợ. */
const WholesalePage: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [search, setSearch] = useState('');
  const [khoId, setKhoId] = useState<string | undefined>(undefined);
  const [khachHangId, setKhachHangId] = useState<string | null>(null);
  const [lines, setLines] = useState<CartLine[]>([]);
  const [tienGiamGia, setTienGiamGia] = useState(0);
  const [khachDua, setKhachDua] = useState(0);
  const [saving, setSaving] = useState(false);
  const [lastBill, setLastBill] = useState<PosSaleResult | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [stockByProduct, setStockByProduct] = useState<Record<string, number>>({});

  useEffect(() => {
    getWarehouses({ activeOnly: true, pageSize: 50 }).then((r) => {
      setWarehouses(r.items);
      if (r.items.length > 0) setKhoId((k) => k ?? r.items[0].id);
    }).catch(() => { });
    getCustomers({ pageSize: 200 }).then((r) => setCustomers(r.items)).catch(() => { });
    getProducts({ pageSize: 50, activeOnly: true }).then((r) => setProducts(r.items)).catch(() => { });
  }, []);

  // Tồn kho theo kho đang chọn — reload khi đổi kho
  useEffect(() => {
    if (!khoId) return;
    getInventory({ khoId, nonZeroOnly: false, pageSize: 500 }).then((r) => {
      const map: Record<string, number> = {};
      for (const it of r.items as InventoryItem[]) map[it.matHangId] = it.ton;
      setStockByProduct(map);
    }).catch(() => { });
  }, [khoId]);

  const tienHang = useMemo(
    () => lines.reduce((sum, l) => sum + Math.max(0, l.soLuong * l.donGia - l.giamTheoTien), 0),
    [lines]
  );
  const tongCong = Math.max(0, tienHang - Math.min(tienGiamGia, tienHang));
  const traLai = Math.max(0, khachDua - tongCong);
  const conNo = Math.max(0, tongCong - khachDua);

  const giaSi = (p: Product) => (p.giaBan2 && p.giaBan2 > 0 ? p.giaBan2 : p.giaBan);
  const tonCua = (id: string) => stockByProduct[id] ?? 0;
  // Tổng SL trong giỏ theo mặt hàng — để cảnh báo vượt tồn
  const soLuongTrongGio = useMemo(() => {
    const map: Record<string, number> = {};
    for (const l of lines) map[l.matHangId] = (map[l.matHangId] ?? 0) + l.soLuong;
    return map;
  }, [lines]);
  const vuotTonIds = useMemo(() =>
    Object.keys(soLuongTrongGio).filter((id) => soLuongTrongGio[id] > tonCua(id)),
    [soLuongTrongGio, stockByProduct]);

  const addLine = (p: Product) => {
    setLines((prev) => {
      const found = prev.find((l) => l.matHangId === p.id);
      if (found) return prev.map((l) => l.matHangId === p.id ? { ...l, soLuong: l.soLuong + 1 } : l);
      return [...prev, {
        matHangId: p.id, tenMatHang: p.name, donViTinh: p.donViTinh,
        soLuong: 1, donGia: giaSi(p), giamTheoTien: 0,
      }];
    });
  };
  const updateLine = (matHangId: string, patch: Partial<CartLine>) =>
    setLines((prev) => prev.map((l) => l.matHangId === matHangId ? { ...l, ...patch } : l));
  const removeLine = (matHangId: string) =>
    setLines((prev) => prev.filter((l) => l.matHangId !== matHangId));

  const save = async () => {
    if (!khoId) { void message.warning('Chưa chọn kho xuất'); return; }
    if (lines.length === 0) { void message.warning('Chưa có mặt hàng nào'); return; }
    if (vuotTonIds.length > 0) {
      const names = lines.filter((l) => vuotTonIds.includes(l.matHangId))
        .map((l) => `${l.tenMatHang} (tồn ${tonCua(l.matHangId).toLocaleString('vi-VN')}, cần ${soLuongTrongGio[l.matHangId]})`);
      void message.error(`Không đủ tồn kho: ${names.join('; ')}`);
      return;
    }
    setSaving(true);
    setErr(null);
    try {
      const result = await createWholesale({
        khoId,
        khachHangId,
        nhanVienId: null,
        lines: lines.map((l) => ({
          matHangId: l.matHangId, khoHangId: null, soLuong: l.soLuong, donGia: l.donGia,
          tileGiamGia: 0, giamTheoTien: l.giamTheoTien,
        })),
        tienGiamGia,
        tileGiamGia: 0,
        ghiChu: null,
        khachDua: khachDua > 0 ? khachDua : tongCong,
      });
      setLastBill(result);
      void message.success(`Đã tạo hoá đơn bán sỉ ${result.soPhieu}`);
      setLines([]);
      setTienGiamGia(0);
      setKhachDua(0);
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tạo hoá đơn bán sỉ');
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
        <Card size="small" title="Chọn mặt hàng (giá sỉ)" extra={
          <Input allowClear prefix={<SearchOutlined />} placeholder="Tìm mã / tên hàng..."
            style={{ width: 220 }} value={search} onChange={(e) => setSearch(e.target.value)} />
        }>
          <div style={{ maxHeight: 340, overflowY: 'auto' }}>
            <Row gutter={[8, 8]}>
              {filteredProducts.map((p) => {
                return (
                  <Col key={p.id} xs={12} md={8} lg={6}>
                    <Card size="small" hoverable onClick={() => addLine(p)}>
                      <div style={{ fontWeight: 600, fontSize: 13 }}>{p.name}</div>
                      <div style={{ color: '#888', fontSize: 12 }}>{p.code ?? ''} · {p.donViTinh ?? ''}</div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 4 }}>
                        <span>{fmtVnd(giaSi(p))}</span>
                        <span style={{ fontSize: 12, color: tonCua(p.id) > 0 ? '#389e0d' : '#ff4d4f' }}>
                          Tồn: {tonCua(p.id).toLocaleString('vi-VN')}
                        </span>
                        <PlusOutlined style={{ color: '#d4a017' }} />
                      </div>
                    </Card>
                  </Col>
                );
              })}
            </Row>
            {filteredProducts.length === 0 && <Empty description="Không có mặt hàng" />}
          </div>
        </Card>

        <Card size="small" title={`Giỏ bán sỉ (${lines.length})`} style={{ marginTop: 12 }} styles={{ body: { padding: 0 } }}>
          <Table
            size="small" rowKey="matHangId" pagination={false}
            dataSource={lines}
            columns={[
              { title: 'Mặt hàng', dataIndex: 'tenMatHang' },
              { title: 'SL', width: 100, render: (_, l: CartLine) => (
                <InputNumber size="small" min={0.001} value={l.soLuong}
                  onChange={(v) => updateLine(l.matHangId, { soLuong: v ?? 1 })} />
              ) },
              { title: 'Đơn giá sỉ', width: 140, render: (_, l: CartLine) => (
                <InputNumber size="small" min={0} step={1000} style={{ width: '100%' }} value={l.donGia}
                  onChange={(v) => updateLine(l.matHangId, { donGia: v ?? 0 })} />
              ) },
              { title: 'Chiết khấu', width: 130, render: (_, l: CartLine) => (
                <InputNumber size="small" min={0} style={{ width: '100%' }} value={l.giamTheoTien}
                  onChange={(v) => updateLine(l.matHangId, { giamTheoTien: v ?? 0 })} />
              ) },
              { title: 'Thành tiền', width: 130, align: 'right',
                render: (_, l: CartLine) => <b>{fmtVnd(l.soLuong * l.donGia - l.giamTheoTien)}</b> },
              { title: '', key: 'del', width: 40,
                render: (_, l: CartLine) => <Button type="text" danger size="small" icon={<DeleteOutlined />}
                  onClick={() => removeLine(l.matHangId)} /> },
            ]}
            locale={{ emptyText: <Empty description="Chưa có hàng — click thẻ mặt hàng để thêm" /> }}
          />
        </Card>
      </Col>

      <Col xs={24} lg={10}>
        <Card size="small" title="Hoá đơn bán sỉ">
          <Space direction="vertical" style={{ width: '100%' }} size={8}>
            <Select placeholder="Kho xuất" style={{ width: '100%' }} value={khoId ?? undefined}
              onChange={(v) => setKhoId(v)}
              options={warehouses.map((w) => ({ value: w.id, label: w.name }))} />
            <Select allowClear showSearch optionFilterProp="label" placeholder="Khách hàng"
              style={{ width: '100%' }} value={khachHangId ?? undefined}
              onChange={(v) => setKhachHangId(v ?? null)}
              options={customers.map((c) => ({ value: c.id, label: `${c.maKhach ?? ''} — ${c.name}` }))} />

            <Descriptions column={1} size="small" bordered>
              <Descriptions.Item label="Tiền hàng"><b>{fmtVnd(tienHang)}</b></Descriptions.Item>
              <Descriptions.Item label="Giảm giá">
                <InputNumber min={0} max={tienHang} value={tienGiamGia} style={{ width: 140 }}
                  onChange={(v) => setTienGiamGia(v ?? 0)} />
              </Descriptions.Item>
              <Descriptions.Item label="Tổng cộng">
                <span style={{ color: '#8a6100', fontSize: 18, fontWeight: 700 }}>{fmtVnd(tongCong)}</span>
              </Descriptions.Item>
              <Descriptions.Item label="Khách đưa">
                <InputNumber style={{ width: 140 }} min={0} step={10000} value={khachDua}
                  onChange={(v) => setKhachDua(v ?? 0)} />
              </Descriptions.Item>
              <Descriptions.Item label="Tiền thừa"><span style={{ color: '#389e0d' }}>{fmtVnd(traLai)}</span></Descriptions.Item>
              {conNo > 0 && <Descriptions.Item label="Còn nợ"><Tag color="red">{fmtVnd(conNo)}</Tag></Descriptions.Item>}
            </Descriptions>

            <Button type="primary" size="large" block loading={saving} onClick={save}
              disabled={lines.length === 0 || !khoId}>
              TẠO HOÁ ĐƠN BÁN SỈ
            </Button>
            <Button block onClick={() => { setLines([]); setTienGiamGia(0); setKhachDua(0); }}>Xoá giỏ</Button>
            {err && <Tag color="error">{err}</Tag>}
            {vuotTonIds.length > 0 && (
              <Alert type="warning" showIcon
                message="Vượt tồn kho tại kho đang chọn"
                description="Vui lòng giảm số lượng hoặc chọn kho khác. Hệ thống không cho tạo hoá đơn bán sỉ vượt tồn." />
            )}
            {lastBill && (
              <Card size="small" style={{ background: '#f6ffed', borderColor: '#b7eb8f' }}>
                <b>Hoá đơn {lastBill.soPhieu}</b> — Tổng {fmtVnd(lastBill.tongCong)}
                {lastBill.conNo > 0 && ` · Còn nợ ${fmtVnd(lastBill.conNo)}`}
              </Card>
            )}
          </Space>
        </Card>
      </Col>
    </Row>
  );
};

export default WholesalePage;