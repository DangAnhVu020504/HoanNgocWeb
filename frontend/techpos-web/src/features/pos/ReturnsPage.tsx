import React, { useEffect, useMemo, useState } from 'react';
import { Button, Card, Col, Descriptions, Empty, Input, InputNumber, Row, Select, Space, Table, message } from 'antd';
import { DeleteOutlined, DownloadOutlined, EyeOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import { createCustomerReturn, type CustomerReturnResult } from '../../api/pos';
import { downloadStockVoucherPdf, printStockVoucherPdf } from '../../api/reports';
import { getProducts, type Product } from '../../api/products';
import { getCustomers, type Customer } from '../../api/customers';
import { getWarehouses, type Warehouse } from '../../api/warehouses';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

interface ReturnLine {
  matHangId: string;
  tenMatHang: string;
  donViTinh: string | null;
  soLuong: number;
  donGia: number;
}

/** Trả lại KH (mục 4.3) — khách trả hàng về kho, shop trả tiền lại khách. */
const ReturnsPage: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [search, setSearch] = useState('');
  const [khoNhanId, setKhoNhanId] = useState<string | undefined>(undefined);
  const [khachHangId, setKhachHangId] = useState<string | null>(null);
  const [lines, setLines] = useState<ReturnLine[]>([]);
  const [tienGiamGia, setTienGiamGia] = useState(0);
  const [tienTraLai, setTienTraLai] = useState(0);
  const [ghiChu, setGhiChu] = useState('');
  const [saving, setSaving] = useState(false);
  const [last, setLast] = useState<CustomerReturnResult | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    getWarehouses({ activeOnly: true, pageSize: 50 }).then((r) => {
      setWarehouses(r.items);
      if (r.items.length > 0) setKhoNhanId((k) => k ?? r.items[0].id);
    }).catch(() => { });
    getCustomers({ pageSize: 200 }).then((r) => setCustomers(r.items)).catch(() => { });
  }, []);

  // Tồn tại kho nhận đang chọn — reload khi đổi kho
  useEffect(() => {
    getProducts({ pageSize: 50, activeOnly: true, khoId: khoNhanId }).then((r) => setProducts(r.items)).catch(() => { });
  }, [khoNhanId]);

  const tienHang = useMemo(() => lines.reduce((s, l) => s + l.soLuong * l.donGia, 0), [lines]);
  const giam = Math.min(Math.max(0, tienGiamGia), tienHang);
  const tongCong = tienHang - giam;

  const addLine = (p: Product) => {
    setLines((prev) => {
      const found = prev.find((l) => l.matHangId === p.id);
      if (found) return prev.map((l) => l.matHangId === p.id ? { ...l, soLuong: l.soLuong + 1 } : l);
      return [...prev, {
        matHangId: p.id, tenMatHang: p.name, donViTinh: p.donViTinh,
        soLuong: 1, donGia: p.giaBan,
      }];
    });
  };
  const updateLine = (matHangId: string, patch: Partial<ReturnLine>) =>
    setLines((prev) => prev.map((l) => l.matHangId === matHangId ? { ...l, ...patch } : l));
  const removeLine = (matHangId: string) =>
    setLines((prev) => prev.filter((l) => l.matHangId !== matHangId));

  const resetForm = () => { setLines([]); setTienGiamGia(0); setTienTraLai(0); setGhiChu(''); };

  const save = async () => {
    if (!khoNhanId) { void message.warning('Chưa chọn kho nhận hàng về'); return; }
    if (lines.length === 0) { void message.warning('Chưa có mặt hàng nào'); return; }
    const tra = tienTraLai > 0 ? Math.min(tienTraLai, tongCong) : tongCong;
    setSaving(true);
    setErr(null);
    try {
      const result = await createCustomerReturn({
        khoNhanId,
        khachHangId,
        nhanVienId: null,
        lines: lines.map((l) => ({ matHangId: l.matHangId, soLuong: l.soLuong, donGia: l.donGia, note: null })),
        tienGiamGia: giam,
        tienTraLai: tra,
        ghiChu: ghiChu.trim() || null,
      });
      setLast(result);
      void message.success(`Đã tạo phiếu trả lại ${result.soPhieu}`);
      resetForm();
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tạo phiếu trả lại');
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
        <Card size="small" className="luxury-card" title={<span className="card-title-gold">Chọn mặt hàng trả lại</span>} extra={
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
                      <span className="money">{fmtVnd(p.giaBan)}</span>
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

        <Card size="small" className="luxury-card" title={<span className="card-title-gold">Dòng trả lại ({lines.length})</span>} style={{ marginTop: 12 }} styles={{ body: { padding: 0 } }}>
          <Table
            className="luxury-table"
            size="small" rowKey="matHangId" pagination={false}
            dataSource={lines}
            columns={[
              { title: 'Mặt hàng', dataIndex: 'tenMatHang' },
              { title: 'SL trả', width: 100, render: (_, l: ReturnLine) => (
                <InputNumber size="small" min={1} precision={0} value={l.soLuong}
                  onChange={(v) => updateLine(l.matHangId, { soLuong: v ?? 1 })} />
              ) },
              { title: 'Đơn giá', width: 140, render: (_, l: ReturnLine) => (
                <InputNumber size="small" min={0} step={1000} style={{ width: '100%' }} value={l.donGia}
                  onChange={(v) => updateLine(l.matHangId, { donGia: v ?? 0 })} />
              ) },
              { title: 'Thành tiền', width: 130, align: 'right',
                render: (_, l: ReturnLine) => <b>{fmtVnd(l.soLuong * l.donGia)}</b> },
              { title: '', key: 'del', width: 40,
                render: (_, l: ReturnLine) => <Button type="text" danger size="small" icon={<DeleteOutlined />}
                  onClick={() => removeLine(l.matHangId)} /> },
            ]}
            locale={{ emptyText: <Empty description="Chưa có hàng — click thẻ mặt hàng để thêm" /> }}
          />
        </Card>
      </Col>

      <Col xs={24} lg={10}>
        <Card size="small" className="luxury-card" title={<span className="card-title-gold">Phiếu khách trả lại</span>}>
          <Space direction="vertical" style={{ width: '100%' }} size={8}>
            <Select placeholder="Kho nhận hàng về" style={{ width: '100%' }} value={khoNhanId ?? undefined}
              onChange={(v) => setKhoNhanId(v)}
              options={warehouses.map((w) => ({ value: w.id, label: w.name }))} />
            <Select allowClear showSearch optionFilterProp="label" placeholder="Khách hàng trả"
              style={{ width: '100%' }} value={khachHangId ?? undefined}
              onChange={(v) => setKhachHangId(v ?? null)}
              options={customers.map((c) => ({ value: c.id, label: `${c.maKhach ?? ''} — ${c.name}` }))} />
            <Input allowClear placeholder="Ghi chú" value={ghiChu}
              onChange={(e) => setGhiChu(e.target.value)} />

            <Descriptions column={1} size="small" bordered>
              <Descriptions.Item label="Tiền hàng"><b>{fmtVnd(tienHang)}</b></Descriptions.Item>
              <Descriptions.Item label="Giảm giá">
                <InputNumber min={0} max={tienHang} value={tienGiamGia} style={{ width: 140 }}
                  onChange={(v) => setTienGiamGia(v ?? 0)} />
              </Descriptions.Item>
              <Descriptions.Item label="Tổng cộng">
                <span className="money" style={{ color: '#8A6100', fontSize: 18 }}>{fmtVnd(tongCong)}</span>
              </Descriptions.Item>
              <Descriptions.Item label="Tiền trả khách">
                <InputNumber min={0} max={tongCong} step={10000} value={tienTraLai} style={{ width: 140 }}
                  onChange={(v) => setTienTraLai(v ?? 0)} />
              </Descriptions.Item>
            </Descriptions>

            <Button type="primary" size="large" block loading={saving} onClick={save}
              className="gold-gradient-btn"
              disabled={lines.length === 0 || !khoNhanId}>
              TẠO PHIẾU TRẢ LẠI
            </Button>
            <Button block onClick={resetForm}>Xoá form</Button>
            {err && <Card size="small" style={{ background: '#fff2f0', borderColor: '#ffccc7' }}>{err}</Card>}
            {last && (
              <Card size="small" style={{ background: '#f6ffed', borderColor: '#b7eb8f' }}>
                <b>Phiếu {last.soPhieu}</b> — Tổng {fmtVnd(last.tongCong)} · Trả khách {fmtVnd(last.tienTraLai)}
                <div style={{ marginTop: 6 }}>
                  <Space>
                    <Button size="small" icon={<EyeOutlined />} onClick={() => printStockVoucherPdf(last.donHangId)}>
                      Xem PDF
                    </Button>
                    <Button size="small" icon={<DownloadOutlined />} onClick={() => downloadStockVoucherPdf(last.donHangId)}>
                      Tải về máy
                    </Button>
                  </Space>
                </div>
              </Card>
            )}
          </Space>
        </Card>
      </Col>
    </Row>
  );
};

export default ReturnsPage;