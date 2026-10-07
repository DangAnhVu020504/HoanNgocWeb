import React, { useEffect, useMemo, useState } from 'react';
import { Alert, Button, Card, Col, Descriptions, Empty, Input, InputNumber, Row, Select, Space, Table, Tag, message } from 'antd';
import { DeleteOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { createSale, type PosSaleResult } from '../../api/pos';
import { printInvoicePdf } from '../../api/reports';
import { getProducts, type Product } from '../../api/products';
import { getCustomers, type Customer } from '../../api/customers';
import { getWarehouses, type Warehouse } from '../../api/warehouses';
import { getInventory, type InventoryItem } from '../../api/inventory';
import { usePosCartStore } from '../../stores/posCartStore';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

const RetailSalePage: React.FC = () => {
  const khoId = usePosCartStore((s) => s.khoId);
  const khachHangId = usePosCartStore((s) => s.khachHangId);
  const lines = usePosCartStore((s) => s.lines);
  const tienGiamGia = usePosCartStore((s) => s.tienGiamGia);
  const khachDua = usePosCartStore((s) => s.khachDua);
  const setKho = usePosCartStore((s) => s.setKho);
  const setKhachHang = usePosCartStore((s) => s.setKhachHang);
  const addLine = usePosCartStore((s) => s.addLine);
  const removeLine = usePosCartStore((s) => s.removeLine);
  const updateLine = usePosCartStore((s) => s.updateLine);
  const setTienGiamGia = usePosCartStore((s) => s.setTienGiamGia);
  const setKhachDua = usePosCartStore((s) => s.setKhachDua);
  const resetCart = usePosCartStore((s) => s.resetCart);

  const [products, setProducts] = useState<Product[]>([]);
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [search, setSearch] = useState('');
  const [paying, setPaying] = useState(false);
  const [lastBill, setLastBill] = useState<PosSaleResult | null>(null);
  const [stockByProduct, setStockByProduct] = useState<Record<string, number>>({});

  const loadProducts = async () => {
    try {
      const res = await getProducts({ pageSize: 50, activeOnly: true });
      setProducts(res.items);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải mặt hàng');
    }
  };

  useEffect(() => { void loadProducts(); /* eslint-disable-line react-hooks/exhaustive-deps */ }, []);

  useEffect(() => {
    getWarehouses({ activeOnly: true, pageSize: 50 }).then((r) => {
      setWarehouses(r.items);
      if (r.items.length > 0) usePosCartStore.setState({ khoId: r.items[0].id });
    }).catch(() => { });
    getCustomers({ pageSize: 200 }).then((r) => setCustomers(r.items)).catch(() => { });
  }, []);

  // Tồn kho theo kho đang chọn — reload khi đổi kho
  useEffect(() => {
    if (!khoId) return;
    getInventory({ khoId, nonZeroOnly: false, pageSize: 500 }).then((r) => {
      const map: Record<string, number> = {};
      for (const it of r.items as InventoryItem[]) map[it.matHangId] = it.ton;
      setStockByProduct(map);
    }).catch(() => { });
  }, [khoId, lastBill]);

  const tienHang = useMemo(
    () => lines.reduce((sum, l) => sum + Math.max(0, l.soLuong * l.donGia - l.tienGiamGia), 0),
    [lines]
  );
  const tongCong = Math.max(0, tienHang - Math.min(tienGiamGia, tienHang));
  const traLai = Math.max(0, khachDua - tongCong);
  const conNo = Math.max(0, tongCong - khachDua);

  const thanhToan = async () => {
    if (!khoId) { void message.warning('Chưa chọn kho xuất'); return; }
    if (lines.length === 0) { void message.warning('Chưa có mặt hàng nào'); return; }
    if (vuotTonIds.length > 0) {
      const names = lines.filter((l) => vuotTonIds.includes(l.matHangId))
        .map((l) => `${l.tenMatHang} (tồn ${tonCua(l.matHangId).toLocaleString('vi-VN')}, cần ${soLuongTrongGio[l.matHangId]})`);
      void message.error(`Không đủ tồn kho: ${names.join('; ')}`);
      return;
    }
    setPaying(true);
    try {
      const result = await createSale({
        khoId,
        khachHangId,
        nhanVienId: null,
        lines: lines.map((l) => ({
          matHangId: l.matHangId, khoHangId: null, soLuong: l.soLuong, donGia: l.donGia,
          tileGiamGia: l.tileGiamGia, giamTheoTien: l.tienGiamGia,
        })),
        tienGiamGia,
        tileGiamGia: 0,
        ghiChu: null,
        khachDua: khachDua > 0 ? khachDua : tongCong,
      });
      setLastBill(result);
      void message.success(`Đã tạo hoá đơn ${result.soPhieu}`);
      resetCart();
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi thanh toán');
    } finally {
      setPaying(false);
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
  const soLuongTrongGio = useMemo(() => {
    const map: Record<string, number> = {};
    for (const l of lines) map[l.matHangId] = (map[l.matHangId] ?? 0) + l.soLuong;
    return map;
  }, [lines]);
  const vuotTonIds = useMemo(() =>
    Object.keys(soLuongTrongGio).filter((id) => soLuongTrongGio[id] > tonCua(id)),
    [soLuongTrongGio, stockByProduct]);

  return (
    <Row gutter={12}>
      {/* Trái: chọn hàng + giỏ */}
      <Col xs={24} lg={14}>
        <Card size="small" title="Chọn mặt hàng" extra={
          <Input allowClear prefix={<SearchOutlined />} placeholder="F3 — Tìm mã / tên hàng..."
            style={{ width: 220 }} value={search} onChange={(e) => setSearch(e.target.value)} />
        }>
          <div style={{ maxHeight: 360, overflowY: 'auto' }}>
            <Row gutter={[8, 8]}>
              {filteredProducts.map((p) => (
                <Col key={p.id} xs={12} md={8} lg={6}>
                  <Card size="small" hoverable onClick={() =>
                    addLine({
                      matHangId: p.id, tenMatHang: p.name, donViTinh: p.donViTinh,
                      donGia: p.giaBan, soLuong: 1, tileGiamGia: 0, tienGiamGia: 0,
                    })
                  }>
                    <div style={{ fontWeight: 600, fontSize: 13 }}>{p.name}</div>
                    <div style={{ color: '#888', fontSize: 12 }}>{p.code ?? ''} · {p.donViTinh ?? ''}</div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 4 }}>
                      <span>{fmtVnd(p.giaBan)}</span>
                      <span style={{ fontSize: 12, color: tonCua(p.id) > 0 ? '#389e0d' : '#ff4d4f' }}>
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

        <Card size="small" title={`Giỏ hàng (${lines.length})`} style={{ marginTop: 12 }} styles={{ body: { padding: 0 } }}>
          <Table
            size="small" rowKey="matHangId" pagination={false}
            dataSource={lines}
            columns={[
              { title: 'Mặt hàng', dataIndex: 'tenMatHang' },
              { title: 'SL', width: 100, render: (_, l) => (
                <InputNumber size="small" min={0.001} value={l.soLuong}
                  onChange={(v) => updateLine(l.matHangId, { soLuong: v ?? 1 })} />
              ) },
              { title: 'Đơn giá', dataIndex: 'donGia', width: 110, align: 'right', render: fmtVnd },
              { title: 'Thành tiền', width: 130, align: 'right',
                render: (_, l) => <b>{fmtVnd(l.soLuong * l.donGia - l.tienGiamGia)}</b> },
              { title: '', key: 'del', width: 40,
                render: (_, l) => <Button type="text" danger size="small" icon={<DeleteOutlined />}
                  onClick={() => removeLine(l.matHangId)} /> },
            ]}
            locale={{ emptyText: <Empty description="Chưa có hàng — click thẻ mặt hàng để thêm" /> }}
          />
        </Card>
      </Col>

      {/* Phải: thanh toán */}
      <Col xs={24} lg={10}>
        <Card size="small" title="Thanh toán (F12)">
          <Space direction="vertical" style={{ width: '100%' }} size={8}>
            <Select placeholder="Kho xuất" style={{ width: '100%' }} value={khoId ?? undefined}
              onChange={(v) => setKho(v)}
              options={warehouses.map((w) => ({ value: w.id, label: w.name }))} />
            <Select allowClear showSearch optionFilterProp="label" placeholder="Khách hàng (mặc định khách lẻ)"
              style={{ width: '100%' }} value={khachHangId ?? undefined}
              onChange={(v) => setKhachHang(v ?? null)}
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

            <Button type="primary" size="large" block loading={paying} onClick={thanhToan}
              disabled={lines.length === 0 || !khoId}>
              THANH TOÁN (F12)
            </Button>
            <Button block onClick={resetCart}>Xoá giỏ</Button>
            {vuotTonIds.length > 0 && (
              <Alert type="warning" showIcon
                message="Vượt tồn kho tại kho đang chọn"
                description="Vui lòng giảm số lượng hoặc chọn kho khác." />
            )}
            {lastBill && (
              <Card size="small" style={{ background: '#f6ffed', borderColor: '#b7eb8f' }}>
                <b>Hoá đơn {lastBill.soPhieu}</b> — Tổng {fmtVnd(lastBill.tongCong)}
                {lastBill.traLai > 0 && ` · Trả lại ${fmtVnd(lastBill.traLai)}`}
                {lastBill.conNo > 0 && ` · Còn nợ ${fmtVnd(lastBill.conNo)}`}
                <div style={{ marginTop: 6 }}>
                  <Button size="small" type="primary" ghost onClick={() => printInvoicePdf(lastBill.donHangId)}>
                    In hoá đơn (PDF)
                  </Button>
                </div>
              </Card>
            )}
          </Space>
        </Card>
      </Col>
    </Row>
  );
};

export default RetailSalePage;
