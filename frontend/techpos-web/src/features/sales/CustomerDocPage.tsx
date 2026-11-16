import React, { useEffect, useMemo, useState } from 'react';
import {
  Alert, Button, Card, Col, Descriptions, Empty, Input, InputNumber, Row, Select,
  Space, Table, message
} from 'antd';
import {
  CheckCircleOutlined, DeleteOutlined, DownloadOutlined, EyeOutlined,
  PlusOutlined, SearchOutlined, ShoppingCartOutlined
} from '@ant-design/icons';
import {
  createQuotation, createOrder, type QuoteLineInput,
} from '../../api/salesdocs';
import {
  printQuotationPdf, downloadQuotationPdf, printOrderPdf, downloadOrderPdf,
} from '../../api/reports';
import { getProducts, type Product } from '../../api/products';
import { getCustomers, type Customer } from '../../api/customers';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

interface DocLine {
  matHangId: string;
  tenMatHang: string;
  donViTinh: string | null;
  soLuong: number;
  donGia: number;
  tileGiamGia: number;
}

interface Props { mode: 'quote' | 'order'; }

/**
 * Báo giá / Khách đặt hàng (White & Gold Luxury)
 * Giữ nguyên 100% logic tạo báo giá, tạo đơn, in PDF & tải file.
 */
const CustomerDocPage: React.FC<Props> = ({ mode }) => {
  const isQuote = mode === 'quote';
  const title = isQuote ? 'LẬP BÁO GIÁ BÁN HÀNG' : 'LẬP ĐƠN ĐẶT HÀNG';
  const submitLabel = isQuote ? 'TẠO BÁO GIÁ' : 'TẠO ĐƠN ĐẶT HÀNG';

  const [products, setProducts] = useState<Product[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [search, setSearch] = useState('');
  const [khachHangId, setKhachHangId] = useState<string | null>(null);
  const [tenKhach, setTenKhach] = useState('');
  const [diaChi, setDiaChi] = useState('');
  const [dienThoai, setDienThoai] = useState('');
  const [email, setEmail] = useState('');
  const [lines, setLines] = useState<DocLine[]>([]);
  const [tienThue, setTienThue] = useState(0);
  const [tienGiamGia, setTienGiamGia] = useState(0);
  const [phiVanChuyen, setPhiVanChuyen] = useState(0);
  const [ghiChu, setGhiChu] = useState('');
  const [saving, setSaving] = useState(false);
  const [last, setLast] = useState<{ id: string; soPhieu: string; tongCong: number } | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    getProducts({ pageSize: 50, activeOnly: true }).then((r) => setProducts(r.items)).catch(() => { });
    getCustomers({ pageSize: 200 }).then((r) => setCustomers(r.items)).catch(() => { });
  }, []);

  const tienHang = useMemo(
    () => lines.reduce((s, l) => s + l.soLuong * l.donGia * (1 - l.tileGiamGia / 100), 0),
    [lines]
  );
  const giam = Math.min(Math.max(0, tienGiamGia), tienHang);
  const tongCong = tienHang - giam + Math.max(0, tienThue) + Math.max(0, phiVanChuyen);

  const pickCustomer = (id: string | null) => {
    setKhachHangId(id);
    const c = customers.find((x) => x.id === id);
    if (c) {
      setTenKhach(c.name ?? '');
      setDiaChi(c.diaChi ?? '');
      setDienThoai(c.dienThoai ?? '');
      setEmail(c.email ?? '');
    }
  };

  const addLine = (p: Product) => {
    setLines((prev) => {
      const found = prev.find((l) => l.matHangId === p.id);
      if (found) return prev.map((l) => l.matHangId === p.id ? { ...l, soLuong: l.soLuong + 1 } : l);
      return [...prev, {
        matHangId: p.id, tenMatHang: p.name, donViTinh: p.donViTinh,
        soLuong: 1, donGia: p.giaBan, tileGiamGia: 0,
      }];
    });
  };

  const updateLine = (matHangId: string, patch: Partial<DocLine>) =>
    setLines((prev) => prev.map((l) => l.matHangId === matHangId ? { ...l, ...patch } : l));

  const removeLine = (matHangId: string) =>
    setLines((prev) => prev.filter((l) => l.matHangId !== matHangId));

  const resetForm = () => {
    setLines([]);
    setTienThue(0);
    setTienGiamGia(0);
    setPhiVanChuyen(0);
    setGhiChu('');
  };

  const save = async () => {
    if (lines.length === 0) {
      void message.warning('Chưa có mặt hàng nào');
      return;
    }
    setSaving(true);
    setErr(null);
    try {
      const payload = {
        khachHangId,
        tenKhach: tenKhach.trim() || null,
        diaChi: diaChi.trim() || null,
        dienThoai: dienThoai.trim() || null,
        email: email.trim() || null,
        lines: lines.map<QuoteLineInput>((l) => ({
          matHangId: l.matHangId, soLuong: l.soLuong, donGia: l.donGia, tileGiamGia: l.tileGiamGia, note: null,
        })),
        tienThue,
        tienGiamGia: giam,
        phiVanChuyen,
        ghiChu: ghiChu.trim() || null,
      };
      const result = isQuote ? await createQuotation(payload) : await createOrder(payload);
      setLast(result);
      void message.success(`Đã tạo ${isQuote ? 'báo giá' : 'đơn đặt hàng'} ${result.soPhieu}`);
      resetForm();
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tạo chứng từ');
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
    <Row gutter={[16, 16]}>
      {/* CỘT TRÁI: CHỌN MẶT HÀNG & DÒNG HÀNG */}
      <Col xs={24} lg={14}>
        <Card
          className="luxury-card"
          title={
            <Space>
              <ShoppingCartOutlined style={{ color: '#D4AF37' }} />
              <span style={{ fontWeight: 700, color: '#1C180E' }}>Chọn Mặt Hàng Vào Chứng Từ</span>
            </Space>
          }
          extra={
            <Input
              allowClear
              prefix={<SearchOutlined style={{ color: '#D4AF37' }} />}
              placeholder="F3 — Tìm mã / tên hàng..."
              style={{ width: 230, borderRadius: 8 }}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          }
        >
          <div style={{ maxHeight: 300, overflowY: 'auto' }}>
            <Row gutter={[8, 8]}>
              {filteredProducts.map((p) => (
                <Col key={p.id} xs={12} md={8}>
                  <div
                    className="pos-product-card"
                    style={{ padding: '10px 12px' }}
                    onClick={() => addLine(p)}
                  >
                    <div style={{ fontWeight: 700, fontSize: 13, color: '#1C180E', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      {p.name}
                    </div>
                    <div style={{ color: '#888', fontSize: 11 }}>
                      <span className="code-badge">{p.code ?? '—'}</span> · {p.donViTinh ?? ''}
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginTop: 6 }}>
                      <b style={{ color: '#8A6100', fontFamily: 'Outfit' }}>{fmtVnd(p.giaBan)}</b>
                      <PlusOutlined style={{ color: '#D4AF37' }} />
                    </div>
                  </div>
                </Col>
              ))}
            </Row>
            {filteredProducts.length === 0 && <Empty description="Không có mặt hàng" />}
          </div>
        </Card>

        <Card
          className="luxury-card"
          title={<span style={{ fontWeight: 700, color: '#1C180E' }}>Dòng Hàng Đã Chọn ({lines.length})</span>}
          style={{ marginTop: 14 }}
          styles={{ body: { padding: 0 } }}
        >
          <Table
            className="luxury-table"
            size="small"
            rowKey="matHangId"
            pagination={false}
            dataSource={lines}
            columns={[
              { title: 'Tên mặt hàng', dataIndex: 'tenMatHang', render: (v) => <b>{v}</b> },
              {
                title: 'Số lượng',
                width: 90,
                align: 'center',
                render: (_, l: DocLine) => (
                  <InputNumber
                    size="small"
                    min={1}
                    precision={0}
                    value={l.soLuong}
                    style={{ width: 60, fontWeight: 700 }}
                    onChange={(v) => updateLine(l.matHangId, { soLuong: v ?? 1 })}
                  />
                ),
              },
              {
                title: 'Đơn giá',
                width: 120,
                align: 'right',
                render: (_, l: DocLine) => (
                  <InputNumber
                    size="small"
                    min={0}
                    step={1000}
                    style={{ width: '100%', fontFamily: 'Outfit' }}
                    value={l.donGia}
                    onChange={(v) => updateLine(l.matHangId, { donGia: v ?? 0 })}
                  />
                ),
              },
              {
                title: 'CK%',
                width: 75,
                align: 'right',
                render: (_, l: DocLine) => (
                  <InputNumber
                    size="small"
                    min={0}
                    max={100}
                    style={{ width: '100%' }}
                    value={l.tileGiamGia}
                    onChange={(v) => updateLine(l.matHangId, { tileGiamGia: v ?? 0 })}
                  />
                ),
              },
              {
                title: 'Thành tiền',
                width: 130,
                align: 'right',
                render: (_, l: DocLine) => (
                  <b style={{ color: '#8A6100', fontFamily: 'Outfit' }}>
                    {fmtVnd(l.soLuong * l.donGia * (1 - l.tileGiamGia / 100))}
                  </b>
                ),
              },
              {
                title: '',
                key: 'del',
                width: 40,
                align: 'center',
                render: (_, l: DocLine) => (
                  <Button
                    type="text"
                    danger
                    size="small"
                    icon={<DeleteOutlined />}
                    onClick={() => removeLine(l.matHangId)}
                  />
                ),
              },
            ]}
            locale={{ emptyText: <Empty description="Chưa có mặt hàng nào — click thẻ phía trên để thêm" /> }}
          />
        </Card>
      </Col>

      {/* CỘT PHẢI: THÔNG TIN KHÁCH & TỔNG KẾT */}
      <Col xs={24} lg={10}>
        <Card
          className="luxury-card"
          title={<span style={{ color: '#8A6100', fontWeight: 800, fontSize: 16 }}>{title}</span>}
        >
          <Space direction="vertical" style={{ width: '100%' }} size={10}>
            <div>
              <div style={{ fontSize: 12, fontWeight: 600, color: '#7E7359', marginBottom: 4 }}>Khách hàng</div>
              <Select
                allowClear
                showSearch
                optionFilterProp="label"
                placeholder="Chọn khách hàng hoặc nhập mới bên dưới"
                style={{ width: '100%' }}
                value={khachHangId ?? undefined}
                onChange={(v) => pickCustomer(v ?? null)}
                options={customers.map((c) => ({ value: c.id, label: `${c.maKhach ?? ''} — ${c.name}` }))}
              />
            </div>

            <Row gutter={8}>
              <Col span={12}>
                <Input
                  allowClear
                  placeholder="Tên khách hàng"
                  value={tenKhach}
                  onChange={(e) => setTenKhach(e.target.value)}
                />
              </Col>
              <Col span={12}>
                <Input
                  allowClear
                  placeholder="Số điện thoại"
                  value={dienThoai}
                  onChange={(e) => setDienThoai(e.target.value)}
                />
              </Col>
            </Row>

            <Input
              allowClear
              placeholder="Địa chỉ giao hàng"
              value={diaChi}
              onChange={(e) => setDiaChi(e.target.value)}
            />
            <Input
              allowClear
              placeholder="Email liên hệ"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
            <Input
              allowClear
              placeholder="Ghi chú điều khoản / thời hạn giao..."
              value={ghiChu}
              onChange={(e) => setGhiChu(e.target.value)}
            />

            <Descriptions column={1} size="small" bordered style={{ marginTop: 6 }}>
              <Descriptions.Item label="Tiền hàng">
                <b style={{ fontFamily: 'Outfit' }}>{fmtVnd(tienHang)}</b>
              </Descriptions.Item>
              <Descriptions.Item label="Tiền thuế">
                <InputNumber
                  size="small"
                  min={0}
                  value={tienThue}
                  style={{ width: 140, fontFamily: 'Outfit' }}
                  onChange={(v) => setTienThue(v ?? 0)}
                />
              </Descriptions.Item>
              <Descriptions.Item label="Giảm giá">
                <InputNumber
                  size="small"
                  min={0}
                  max={tienHang}
                  value={tienGiamGia}
                  style={{ width: 140, fontFamily: 'Outfit' }}
                  onChange={(v) => setTienGiamGia(v ?? 0)}
                />
              </Descriptions.Item>
              <Descriptions.Item label="Phí vận chuyển">
                <InputNumber
                  size="small"
                  min={0}
                  value={phiVanChuyen}
                  style={{ width: 140, fontFamily: 'Outfit' }}
                  onChange={(v) => setPhiVanChuyen(v ?? 0)}
                />
              </Descriptions.Item>
              <Descriptions.Item label="TỔNG CỘNG">
                <span style={{
                  color: '#8A6100',
                  fontSize: 20,
                  fontWeight: 800,
                  fontFamily: 'Outfit'
                }}>
                  {fmtVnd(tongCong)}
                </span>
              </Descriptions.Item>
            </Descriptions>

            <Button
              type="primary"
              size="large"
              block
              loading={saving}
              onClick={save}
              disabled={lines.length === 0}
              className="gold-gradient-btn"
              style={{ height: 46, fontSize: 15, marginTop: 4 }}
            >
              {submitLabel}
            </Button>
            <Button block onClick={resetForm}>
              Làm mới form
            </Button>

            {err && <Alert type="error" showIcon message={err} style={{ borderRadius: 8 }} />}

            {last && (
              <div style={{
                marginTop: 8,
                padding: 14,
                background: '#ECFDF5',
                borderRadius: 8,
                border: '1px solid #A7F3D0',
                color: '#065F46'
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontWeight: 700 }}>
                  <CheckCircleOutlined />
                  ĐÃ TẠO {isQuote ? 'BÁO GIÁ' : 'ĐƠN ĐẶT HÀNG'} {last.soPhieu}
                </div>
                <div style={{ marginTop: 4, fontSize: 13 }}>
                  Tổng cộng: <b style={{ fontFamily: 'Outfit' }}>{fmtVnd(last.tongCong)}</b>
                </div>
                <Space style={{ marginTop: 10 }}>
                  <Button
                    size="small"
                    type="primary"
                    icon={<EyeOutlined />}
                    style={{ background: '#10B981', borderColor: '#10B981' }}
                    onClick={() => isQuote ? printQuotationPdf(last.id) : printOrderPdf(last.id)}
                  >
                    Xem PDF
                  </Button>
                  <Button
                    size="small"
                    icon={<DownloadOutlined />}
                    onClick={() => isQuote ? downloadQuotationPdf(last.id) : downloadOrderPdf(last.id)}
                  >
                    Tải về máy
                  </Button>
                </Space>
              </div>
            )}
          </Space>
        </Card>
      </Col>
    </Row>
  );
};

export default CustomerDocPage;