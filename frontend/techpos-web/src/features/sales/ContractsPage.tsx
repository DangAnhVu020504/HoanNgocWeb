import React, { useEffect, useMemo, useState } from 'react';
import {
  Button, Card, Col, DatePicker, Descriptions, Divider, Empty, Input, InputNumber, Modal,
  Row, Select, Space, Table, message
} from 'antd';
import { DeleteOutlined, DownloadOutlined, EyeOutlined, PlusOutlined, SearchOutlined } from '@ant-design/icons';
import dayjs, { Dayjs } from 'dayjs';
import { createContract, getContractTypes, getContractStatuses, type QuoteLineInput, type LookupItem } from '../../api/salesdocs';
import { printContractPdf, downloadContractPdf } from '../../api/reports';
import { getProducts, type Product } from '../../api/products';
import { getCustomers, type Customer } from '../../api/customers';
import { getEmployees, type Employee } from '../../api/hr';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

interface ContractLine {
  matHangId: string;
  tenMatHang: string;
  donViTinh: string | null;
  soLuong: number;
  donGia: number;
  tileGiamGia: number;
}

/** Khối mục có đánh số — tách bố cục form hợp đồng thành từng phần rõ ràng. */
const Section: React.FC<{ step: string; title: string; children: React.ReactNode }> = ({ step, title, children }) => (
  <Card
    className="luxury-card"
    size="small"
    style={{ marginBottom: 12 }}
    title={
      <span className="card-title-gold">
        <span style={{
          display: 'inline-flex', alignItems: 'center', justifyContent: 'center',
          width: 20, height: 20, borderRadius: '50%', marginRight: 8,
          background: 'linear-gradient(135deg, #FFE28A, #D4AF37)', color: '#1A1403',
          fontSize: 12, fontWeight: 800
        }}>{step}</span>
        {title}
      </span>
    }
  >
    {children}
  </Card>
);

/** Nhãn + điều khiển theo chiều dọc. */
const Field: React.FC<{ label: string; children: React.ReactNode; required?: boolean }> = ({ label, children, required }) => (
  <div style={{ marginBottom: 10 }}>
    <div style={{ fontSize: 12, fontWeight: 600, color: '#7E7359', marginBottom: 4 }}>
      {label}{required && <span style={{ color: '#E11D48' }}> *</span>}
    </div>
    {children}
  </div>
);

/** Hợp đồng (mục 4.9) — ngày ký, hiệu lực, đặt cọc, trạng thái. Bố cục theo mục nghiệp vụ. */
const ContractsPage: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [loaiHopDongs, setLoaiHopDongs] = useState<LookupItem[]>([]);
  const [trangThais, setTrangThais] = useState<LookupItem[]>([]);
  const [search, setSearch] = useState('');
  const [khachHangId, setKhachHangId] = useState<string | null>(null);
  const [nhanVienId, setNhanVienId] = useState<string | null>(null);
  const [loaiHopDongId, setLoaiHopDongId] = useState<string | null>(null);
  const [trangThaiHopDongId, setTrangThaiHopDongId] = useState<string | null>(null);
  const [tenKhach, setTenKhach] = useState('');
  const [ngayKy, setNgayKy] = useState<Dayjs | null>(dayjs());
  const [tuNgay, setTuNgay] = useState<Dayjs | null>(dayjs());
  const [denNgay, setDenNgay] = useState<Dayjs | null>(dayjs().add(1, 'year'));
  const [lines, setLines] = useState<ContractLine[]>([]);
  const [datCoc, setDatCoc] = useState(0);
  const [tienGiamGia, setTienGiamGia] = useState(0);
  const [ghiChu, setGhiChu] = useState('');
  const [saving, setSaving] = useState(false);
  const [last, setLast] = useState<{ id: string; soPhieu: string; tongCong: number } | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [previewOpen, setPreviewOpen] = useState(false);

  useEffect(() => {
    getCustomers({ pageSize: 200 }).then((r) => setCustomers(r.items)).catch(() => { });
    getProducts({ pageSize: 200, activeOnly: true }).then((r) => setProducts(r.items)).catch(() => { });
    getEmployees({ pageSize: 200 }).then((r) => setEmployees(r.items)).catch(() => { });
    getContractTypes().then(setLoaiHopDongs).catch(() => { });
    getContractStatuses().then(setTrangThais).catch(() => { });
  }, []);

  const tienHang = useMemo(
    () => lines.reduce((s, l) => s + l.soLuong * l.donGia * (1 - l.tileGiamGia / 100), 0),
    [lines]
  );
  const giam = Math.min(Math.max(0, tienGiamGia), tienHang);
  const tongCong = tienHang - giam;
  const conLai = Math.max(0, tongCong - datCoc);

  const selectedCustomer = useMemo(() => customers.find((c) => c.id === khachHangId) ?? null, [customers, khachHangId]);
  const selectedEmployee = useMemo(() => employees.find((e) => e.id === nhanVienId) ?? null, [employees, nhanVienId]);
  const selectedLoai = useMemo(() => loaiHopDongs.find((l) => l.id === loaiHopDongId) ?? null, [loaiHopDongs, loaiHopDongId]);
  const selectedTrangThai = useMemo(() => trangThais.find((t) => t.id === trangThaiHopDongId) ?? null, [trangThais, trangThaiHopDongId]);

  const pickCustomer = (id: string | null) => {
    setKhachHangId(id);
    const c = customers.find((x) => x.id === id);
    if (c) setTenKhach(c.name ?? '');
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
  const updateLine = (matHangId: string, patch: Partial<ContractLine>) =>
    setLines((prev) => prev.map((l) => l.matHangId === matHangId ? { ...l, ...patch } : l));
  const removeLine = (matHangId: string) =>
    setLines((prev) => prev.filter((l) => l.matHangId !== matHangId));
  const resetForm = () => { setLines([]); setDatCoc(0); setTienGiamGia(0); setGhiChu(''); };

  const save = async () => {
    if (lines.length === 0) { void message.warning('Chưa có mặt hàng nào'); return; }
    setSaving(true);
    setErr(null);
    try {
      const result = await createContract({
        khachHangId,
        tenKhach: tenKhach.trim() || null,
        nhanVienId,
        loaiHopDongId,
        trangThaiHopDongId,
        ngayKy: ngayKy ? ngayKy.format('YYYY-MM-DD') : null,
        tuNgay: tuNgay ? tuNgay.format('YYYY-MM-DD') : null,
        denNgay: denNgay ? denNgay.format('YYYY-MM-DD') : null,
        lines: lines.map<QuoteLineInput>((l) => ({
          matHangId: l.matHangId, soLuong: l.soLuong, donGia: l.donGia, tileGiamGia: l.tileGiamGia, note: null,
        })),
        datCoc,
        tienGiamGia: giam,
        ghiChu: ghiChu.trim() || null,
      });
      setLast(result);
      void message.success(`Đã tạo hợp đồng ${result.soPhieu}`);
      resetForm();
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tạo hợp đồng');
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
      {/* CỘT TRÁI: DANH MỤC HÀNG HOÁ */}
      <Col xs={24} lg={14}>
        <Card
          className="luxury-card"
          title={<span className="card-title-gold">Danh mục hàng hoá</span>}
          extra={
            <Input allowClear prefix={<SearchOutlined style={{ color: '#D4AF37' }} />} placeholder="Tìm mã / tên hàng..."
              style={{ width: 220 }} value={search} onChange={(e) => setSearch(e.target.value)} />
          }
        >
          <div style={{ maxHeight: 300, overflowY: 'auto' }}>
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

        <Card
          className="luxury-card"
          style={{ marginTop: 12 }}
          styles={{ body: { padding: 0 } }}
          title={<span className="card-title-gold">Dòng hợp đồng ({lines.length})</span>}
        >
          <Table
            className="luxury-table"
            size="small" rowKey="matHangId" pagination={false}
            dataSource={lines}
            scroll={{ y: 260 }}
            columns={[
              { title: 'Mặt hàng', dataIndex: 'tenMatHang' },
              { title: 'SL', width: 90, render: (_, l: ContractLine) => (
                <InputNumber size="small" min={1} precision={0} value={l.soLuong}
                  onChange={(v) => updateLine(l.matHangId, { soLuong: v ?? 1 })} />
              ) },
              { title: 'Đơn giá', width: 130, render: (_, l: ContractLine) => (
                <InputNumber size="small" min={0} step={1000} style={{ width: '100%' }} value={l.donGia}
                  onChange={(v) => updateLine(l.matHangId, { donGia: v ?? 0 })} />
              ) },
              { title: 'Giảm %', width: 90, render: (_, l: ContractLine) => (
                <InputNumber size="small" min={0} max={100} value={l.tileGiamGia}
                  onChange={(v) => updateLine(l.matHangId, { tileGiamGia: v ?? 0 })} />
              ) },
              { title: 'Thành tiền', width: 130, align: 'right',
                render: (_, l: ContractLine) => <b className="money">{fmtVnd(l.soLuong * l.donGia * (1 - l.tileGiamGia / 100))}</b> },
              { title: '', key: 'del', width: 40,
                render: (_, l: ContractLine) => <Button type="text" danger size="small" icon={<DeleteOutlined />}
                  onClick={() => removeLine(l.matHangId)} /> },
            ]}
            locale={{ emptyText: <Empty description="Chưa có hàng — click thẻ mặt hàng để thêm" /> }}
          />
        </Card>
      </Col>

      {/* CỘT PHẢI: CÁC MỤC THÔNG TIN HỢP ĐỒNG */}
      <Col xs={24} lg={10}>
        <Section step="1" title="Thông tin khách hàng">
          <Field label="Khách hàng (chọn từ danh mục)">
            <Select allowClear showSearch optionFilterProp="label" placeholder="Chọn khách hàng"
              style={{ width: '100%' }} value={khachHangId ?? undefined}
              onChange={(v) => pickCustomer(v ?? null)}
              options={customers.map((c) => ({ value: c.id, label: `${c.maKhach ?? ''} — ${c.name}` }))} />
          </Field>
          <Field label="Tên khách hàng trên hợp đồng">
            <Input allowClear placeholder="Tên khách" value={tenKhach} onChange={(e) => setTenKhach(e.target.value)} />
          </Field>
          {selectedCustomer && (
            <div style={{ fontSize: 12, color: '#7E7359', marginBottom: 10, padding: '6px 10px', background: '#FFFDF5', borderRadius: 6, border: '1px solid rgba(212,175,55,0.25)' }}>
              ĐT: <b>{selectedCustomer.dienThoai ?? '—'}</b> · Nợ cho phép: <b>{selectedCustomer.songayChoNo} ngày</b> · Điểm: <b>{selectedCustomer.diemTichLuy}</b>
            </div>
          )}
          <Field label="Nhân viên phụ trách">
            <Select allowClear showSearch optionFilterProp="label" placeholder="Nhân viên phụ trách"
              style={{ width: '100%' }} value={nhanVienId ?? undefined}
              onChange={(v) => setNhanVienId(v ?? null)}
              options={employees.map((e) => ({ value: e.id, label: e.name }))} />
          </Field>
        </Section>

        <Section step="2" title="Điều khoản & hiệu lực">
          <Row gutter={8}>
            <Col span={12}>
              <Field label="Loại hợp đồng">
                <Select allowClear showSearch optionFilterProp="label" placeholder="Loại hợp đồng"
                  style={{ width: '100%' }} value={loaiHopDongId ?? undefined}
                  onChange={(v) => setLoaiHopDongId(v ?? null)}
                  options={loaiHopDongs.map((l) => ({ value: l.id, label: l.name }))} />
              </Field>
            </Col>
            <Col span={12}>
              <Field label="Trạng thái hợp đồng">
                <Select allowClear showSearch optionFilterProp="label" placeholder="Trạng thái"
                  style={{ width: '100%' }} value={trangThaiHopDongId ?? undefined}
                  onChange={(v) => setTrangThaiHopDongId(v ?? null)}
                  options={trangThais.map((t) => ({ value: t.id, label: t.name }))} />
              </Field>
            </Col>
          </Row>
          <Row gutter={8}>
            <Col span={8}>
              <Field label="Ngày ký">
                <DatePicker placeholder="Ngày ký" style={{ width: '100%' }} value={ngayKy} onChange={setNgayKy} format="DD/MM/YYYY" />
              </Field>
            </Col>
            <Col span={8}>
              <Field label="Hiệu lực từ">
                <DatePicker placeholder="Từ ngày" style={{ width: '100%' }} value={tuNgay} onChange={setTuNgay} format="DD/MM/YYYY" />
              </Field>
            </Col>
            <Col span={8}>
              <Field label="Hiệu lực đến">
                <DatePicker placeholder="Đến ngày" style={{ width: '100%' }} value={denNgay} onChange={setDenNgay} format="DD/MM/YYYY" />
              </Field>
            </Col>
          </Row>
        </Section>

        <Section step="3" title="Thanh toán & đặt cọc">
          <Descriptions column={1} size="small" bordered>
            <Descriptions.Item label="Tiền hàng"><b className="money">{fmtVnd(tienHang)}</b></Descriptions.Item>
            <Descriptions.Item label="Giảm giá">
              <InputNumber min={0} max={tienHang} value={tienGiamGia} style={{ width: 150 }}
                onChange={(v) => setTienGiamGia(v ?? 0)} />
            </Descriptions.Item>
            <Descriptions.Item label="Tổng cộng">
              <span className="money" style={{ color: '#8A6100', fontSize: 16 }}>{fmtVnd(tongCong)}</span>
            </Descriptions.Item>
            <Descriptions.Item label="Đặt cọc">
              <InputNumber min={0} max={tongCong} step={10000} value={datCoc} style={{ width: 150 }}
                onChange={(v) => setDatCoc(v ?? 0)} />
            </Descriptions.Item>
            <Descriptions.Item label="Còn lại">
              {conLai > 0
                ? <b className="money money-neg">{fmtVnd(conLai)}</b>
                : <span className="money money-pos">{fmtVnd(0)}</span>}
            </Descriptions.Item>
          </Descriptions>
          <div style={{ marginTop: 10 }}>
            <Field label="Ghi chú / điều khoản bổ sung">
              <Input.TextArea rows={2} allowClear value={ghiChu} onChange={(e) => setGhiChu(e.target.value)}
                placeholder="Điều khoản, thoả thuận thêm..." />
            </Field>
          </div>
        </Section>

        <Section step="4" title="Hoàn tất">
          <Space direction="vertical" style={{ width: '100%' }} size={8}>
            <Space wrap>
              <Button type="primary" size="large" className="gold-gradient-btn" loading={saving} onClick={save} disabled={lines.length === 0}>
                TẠO HỢP ĐỒNG
              </Button>
              <Button size="large" icon={<EyeOutlined />} onClick={() => setPreviewOpen(true)} disabled={lines.length === 0}>
                Xem trước bản in
              </Button>
              <Button size="large" onClick={resetForm}>Xoá form</Button>
            </Space>
            {err && <Card size="small" style={{ background: '#fff2f0', borderColor: '#ffccc7' }}>{err}</Card>}
            {last && (
              <Card size="small" style={{ background: '#ECFDF5', borderColor: '#A7F3D0' }}>
                <b style={{ color: '#065F46' }}>Hợp đồng {last.soPhieu}</b> — Tổng {fmtVnd(last.tongCong)}
                <div style={{ marginTop: 6 }}>
                  <Space>
                    <Button size="small" type="primary" className="gold-gradient-btn" icon={<EyeOutlined />} onClick={() => printContractPdf(last.id)}>
                      In PDF
                    </Button>
                    <Button size="small" icon={<DownloadOutlined />} onClick={() => downloadContractPdf(last.id)}>
                      Tải về máy
                    </Button>
                  </Space>
                </div>
              </Card>
            )}
          </Space>
        </Section>
      </Col>

      {/* XEM TRƯỚC BẢN IN HỢP ĐỒNG */}
      <Modal
        open={previewOpen}
        onCancel={() => setPreviewOpen(false)}
        footer={null}
        width={620}
        title={<span style={{ color: '#8A6100', fontWeight: 800 }}>Xem trước bản in hợp đồng</span>}
      >
        <div style={{ background: '#fff', border: '1px solid rgba(212,175,55,0.35)', borderRadius: 10, padding: 20 }}>
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontWeight: 800, fontSize: 16, color: '#8A6100' }}>CỬA HÀNG HOÀN NGỌC</div>
            <div style={{ fontSize: 12, color: '#7E7359' }}>HOÀN NGỌC GROUP</div>
            <div style={{ marginTop: 8, fontWeight: 700, letterSpacing: 0.4 }}>HỢP ĐỒNG BÁN HÀNG</div>
          </div>
          <Divider style={{ margin: '10px 0', borderColor: '#D4AF37' }} />
          <Row justify="space-between" style={{ fontSize: 13 }}>
            <span>Số: <b style={{ color: '#8A6100' }}>{last?.soPhieu ?? '(chưa lưu)'}</b></span>
            <span>Ngày ký: {ngayKy ? ngayKy.format('DD/MM/YYYY') : '—'}</span>
          </Row>
          <div style={{ margin: '8px 0', fontSize: 13 }}>
            <div>Khách hàng: <b>{tenKhach || selectedCustomer?.name || '—'}</b></div>
            {selectedCustomer?.dienThoai && <div style={{ color: '#7E7359' }}>ĐT: {selectedCustomer.dienThoai}</div>}
            <div style={{ color: '#7E7359' }}>
              Loại: {selectedLoai?.name ?? '—'} · Trạng thái: {selectedTrangThai?.name ?? '—'} · NV: {selectedEmployee?.name ?? '—'}
            </div>
            <div style={{ color: '#7E7359' }}>
              Hiệu lực: {tuNgay ? tuNgay.format('DD/MM/YYYY') : '—'} → {denNgay ? denNgay.format('DD/MM/YYYY') : '—'}
            </div>
          </div>
          <Table
            size="small"
            rowKey="matHangId"
            pagination={false}
            dataSource={lines}
            columns={[
              { title: 'STT', width: 44, align: 'center', render: (_v, _r, i) => i + 1 },
              { title: 'Tên hàng hóa', dataIndex: 'tenMatHang' },
              { title: 'ĐVT', dataIndex: 'donViTinh', width: 58, align: 'center' },
              { title: 'SL', dataIndex: 'soLuong', width: 58, align: 'right',
                render: (v: number) => v.toLocaleString('vi-VN') },
              { title: 'Đơn giá', dataIndex: 'donGia', width: 105, align: 'right',
                render: (v: number) => fmtVnd(v) },
              { title: 'Thành tiền', width: 115, align: 'right',
                render: (_v, r) => <b>{fmtVnd(r.soLuong * r.donGia * (1 - r.tileGiamGia / 100))}</b> },
            ]}
          />
          <div style={{ marginTop: 10, marginLeft: 'auto', width: 280, fontSize: 13 }}>
            <Row justify="space-between"><span style={{ color: '#7E7359' }}>Tiền hàng:</span><span>{fmtVnd(tienHang)}</span></Row>
            {giam > 0 && <Row justify="space-between"><span style={{ color: '#7E7359' }}>Giảm giá:</span><span style={{ color: '#E11D48' }}>- {fmtVnd(giam)}</span></Row>}
            <Divider style={{ margin: '6px 0', borderColor: '#D4AF37' }} />
            <Row justify="space-between"><b style={{ color: '#8A6100' }}>TỔNG CỘNG:</b><b style={{ color: '#8A6100', fontSize: 16 }}>{fmtVnd(tongCong)}</b></Row>
            <Row justify="space-between"><span style={{ color: '#7E7359' }}>Đặt cọc:</span><span>{fmtVnd(datCoc)}</span></Row>
            <Row justify="space-between"><span style={{ color: '#7E7359' }}>Còn lại:</span><b style={{ color: conLai > 0 ? '#E11D48' : '#10B981' }}>{fmtVnd(conLai)}</b></Row>
          </div>
          {ghiChu && (
            <div style={{ marginTop: 10, fontSize: 12, color: '#7E7359' }}>Ghi chú: {ghiChu}</div>
          )}
          <Divider style={{ margin: '14px 0 8px', borderColor: '#F4EEDC' }} />
          <Row justify="space-around" style={{ fontSize: 12, color: '#8A6100', fontWeight: 700 }}>
            <span>Người lập</span>
            <span>Khách hàng</span>
          </Row>
        </div>
      </Modal>
    </Row>
  );
};

export default ContractsPage;
