import React, { useEffect, useMemo, useState } from 'react';
import {
  Alert, Button, Card, Col, Descriptions, Divider, Empty, Input, InputNumber, Modal, Row, Select,
  Space, Table, Tabs, Tag, message
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import {
  CheckCircleOutlined, DeleteOutlined, DownloadOutlined, EyeOutlined, MinusOutlined,
  PlusOutlined, PrinterOutlined, QrcodeOutlined, SearchOutlined, ShoppingCartOutlined, UserOutlined
} from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { createSale, type PosSaleResult } from '../../api/pos';
import { downloadInvoicePdf, printInvoicePdf } from '../../api/reports';
import { getProducts, type Product } from '../../api/products';
import { getCustomers, type Customer } from '../../api/customers';
import { getWarehouses, type Warehouse } from '../../api/warehouses';
import { getInventory, type InventoryItem } from '../../api/inventory';
import { getEmployees, type Employee } from '../../api/hr';
import { buildVietQrUrl, isVietQrConfigured } from '../../app/config';
import { usePosCartStore } from '../../stores/posCartStore';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';
const round0 = (v: number) => Math.round(v);

/**
 * Màn hình Bán Lẻ POS Flagship (White & Gold Luxury Experience)
 * Giữ nguyên 100% logic tính tiền, kiểm tra tồn kho, store Zustand & in ấn hoá đơn.
 */
const RetailSalePage: React.FC = () => {
  const navigate = useNavigate();
  const khoId = usePosCartStore((s) => s.khoId);
  const khachHangId = usePosCartStore((s) => s.khachHangId);
  const nhanVienId = usePosCartStore((s) => s.nhanVienId);
  const dienGiai = usePosCartStore((s) => s.dienGiai);
  const ghiChu = usePosCartStore((s) => s.ghiChu);
  const lines = usePosCartStore((s) => s.lines);
  const tileGiamGia = usePosCartStore((s) => s.tileGiamGia);
  const tienGiamGia = usePosCartStore((s) => s.tienGiamGia);
  const tienThue = usePosCartStore((s) => s.tienThue);
  const phiVanChuyen = usePosCartStore((s) => s.phiVanChuyen);
  const khachDua = usePosCartStore((s) => s.khachDua);
  const setKho = usePosCartStore((s) => s.setKho);
  const setKhachHang = usePosCartStore((s) => s.setKhachHang);
  const setNhanVien = usePosCartStore((s) => s.setNhanVien);
  const setDienGiai = usePosCartStore((s) => s.setDienGiai);
  const setGhiChu = usePosCartStore((s) => s.setGhiChu);
  const addLine = usePosCartStore((s) => s.addLine);
  const removeLine = usePosCartStore((s) => s.removeLine);
  const updateLine = usePosCartStore((s) => s.updateLine);
  const setTileGiamGia = usePosCartStore((s) => s.setTileGiamGia);
  const setTienGiamGia = usePosCartStore((s) => s.setTienGiamGia);
  const setTienThue = usePosCartStore((s) => s.setTienThue);
  const setPhiVanChuyen = usePosCartStore((s) => s.setPhiVanChuyen);
  const setKhachDua = usePosCartStore((s) => s.setKhachDua);
  const resetCart = usePosCartStore((s) => s.resetCart);

  const [products, setProducts] = useState<Product[]>([]);
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [search, setSearch] = useState('');
  const [pickSl, setPickSl] = useState(1);
  const [thuePct, setThuePct] = useState(0);
  const [paying, setPaying] = useState(false);
  const [lastBill, setLastBill] = useState<PosSaleResult | null>(null);
  const [stockByProduct, setStockByProduct] = useState<Record<string, number>>({});
  const [showQr, setShowQr] = useState(false);
  const [previewOpen, setPreviewOpen] = useState(false);

  const loadProducts = async () => {
    try {
      const res = await getProducts({ pageSize: 200, activeOnly: true });
      setProducts(res.items);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải mặt hàng');
    }
  };

  useEffect(() => { void loadProducts(); /* eslint-disable-line react-hooks/exhaustive-deps */ }, []);

  useEffect(() => {
    getWarehouses({ activeOnly: true, pageSize: 50 }).then((r) => {
      setWarehouses(r.items);
      if (r.items.length > 0 && !usePosCartStore.getState().khoId) usePosCartStore.setState({ khoId: r.items[0].id });
    }).catch(() => { });
    getCustomers({ pageSize: 200 }).then((r) => setCustomers(r.items)).catch(() => { });
    getEmployees({ pageSize: 200 }).then((r) => setEmployees(r.items)).catch(() => { });
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

  // --- Tính tiền (khớp PosCalculator backend: làm tròn VNĐ, không âm) ---
  const lineAmount = (l: { soLuong: number; donGia: number; tileGiamGia: number; tienGiamGia: number }) => {
    const goc = round0(l.soLuong * l.donGia);
    const giam = round0(goc * (Math.max(0, Math.min(100, l.tileGiamGia)) / 100)) + Math.max(0, l.tienGiamGia);
    return Math.max(0, goc - giam);
  };
  const tienHang = useMemo(() => round0(lines.reduce((s, l) => s + lineAmount(l), 0)), [lines]);
  const giamTile = round0(tienHang * (Math.max(0, Math.min(100, tileGiamGia)) / 100));
  const giamTong = round0(Math.min(Math.max(0, tienGiamGia) + giamTile, tienHang));
  const tongCong = round0(tienHang - giamTong + Math.max(0, tienThue) + Math.max(0, phiVanChuyen));
  const traLai = round0(Math.max(0, khachDua - tongCong));
  const conNo = round0(Math.max(0, tongCong - khachDua));

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
        nhanVienId,
        lines: lines.map((l) => ({
          matHangId: l.matHangId, khoHangId: null, soLuong: l.soLuong, donGia: l.donGia,
          tileGiamGia: l.tileGiamGia, giamTheoTien: l.tienGiamGia,
        })),
        tienGiamGia,
        tileGiamGia,
        ghiChu: ghiChu.trim() || null,
        khachDua: khachDua > 0 ? khachDua : tongCong,
        dienGiai: dienGiai.trim() || null,
        tienThue,
        phiVanChuyen,
      });
      setLastBill(result);
      void message.success(`Đã tạo hoá đơn ${result.soPhieu}`);
      resetCart();
      setThuePct(0);
      setShowQr(false);
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

  const selectedCustomer = useMemo(() => customers.find((c) => c.id === khachHangId) ?? null, [customers, khachHangId]);
  const selectedWarehouse = useMemo(() => warehouses.find((w) => w.id === khoId) ?? null, [warehouses, khoId]);
  const selectedEmployee = useMemo(() => employees.find((e) => e.id === nhanVienId) ?? null, [employees, nhanVienId]);

  const tonCua = (id: string) => stockByProduct[id] ?? 0;
  const soLuongTrongGio = useMemo(() => {
    const map: Record<string, number> = {};
    for (const l of lines) map[l.matHangId] = (map[l.matHangId] ?? 0) + l.soLuong;
    return map;
  }, [lines]);
  const vuotTonIds = useMemo(() =>
    Object.keys(soLuongTrongGio).filter((id) => soLuongTrongGio[id] > tonCua(id)),
    [soLuongTrongGio, stockByProduct]);

  const addFromPicker = (p: Product) => {
    addLine({
      matHangId: p.id, maHang: p.code, tenMatHang: p.name, donViTinh: p.donViTinh,
      donGia: p.giaBan, soLuong: pickSl || 1, tileGiamGia: 0, tienGiamGia: 0, ghiChu: null,
    });
  };

  const productColumns: ColumnsType<Product> = [
    {
      title: 'Mã hàng',
      dataIndex: 'code',
      width: 105,
      render: (v) => <span className="code-badge">{v ?? '—'}</span>
    },
    {
      title: 'Tên hàng hóa',
      dataIndex: 'name',
      ellipsis: true,
      render: (v) => <b style={{ color: '#1C180E' }}>{v}</b>
    },
    {
      title: 'Giá bán lẻ',
      dataIndex: 'giaBan',
      width: 120,
      align: 'right',
      render: (v) => <b style={{ color: '#8A6100', fontFamily: 'Outfit' }}>{fmtVnd(v)}</b>
    },
    { title: 'ĐVT', dataIndex: 'donViTinh', width: 65, align: 'center' },
    {
      title: 'Tồn',
      key: 'ton',
      width: 90,
      align: 'right',
      render: (_, p) => {
        const stock = tonCua(p.id);
        const color = stock > 5 ? '#10B981' : stock > 0 ? '#F59E0B' : '#E11D48';
        return (
          <span style={{ color, fontWeight: 700, fontFamily: 'Outfit' }}>
            {stock.toLocaleString('vi-VN')}
          </span>
        );
      }
    },
    {
      title: '',
      key: 'add',
      width: 48,
      align: 'center',
      render: (_, p) => (
        <Button
          type="text"
          size="small"
          icon={<PlusOutlined style={{ color: '#D4AF37', fontSize: 16, fontWeight: 700 }} />}
          onClick={(e) => {
            e.stopPropagation();
            addFromPicker(p);
          }}
        />
      ),
    },
  ];

  const cartColumns: ColumnsType<(typeof lines)[number]> = [
    {
      title: 'Mã',
      dataIndex: 'maHang',
      width: 90,
      render: (v) => <span className="code-badge">{v ?? '—'}</span>
    },
    {
      title: 'Tên mặt hàng',
      dataIndex: 'tenMatHang',
      ellipsis: true,
      render: (v) => <b style={{ fontSize: 13 }}>{v}</b>
    },
    { title: 'ĐVT', dataIndex: 'donViTinh', width: 60, align: 'center' },
    {
      title: 'Số lượng',
      width: 125,
      align: 'center',
      render: (_, l) => (
        <Space size={2}>
          <Button
            size="small"
            type="text"
            icon={<MinusOutlined />}
            onClick={() => updateLine(l.matHangId, { soLuong: Math.max(1, l.soLuong - 1) })}
          />
          <InputNumber
            size="small"
            min={1}
            precision={0}
            style={{ width: 52, textAlign: 'center', fontWeight: 700 }}
            value={l.soLuong}
            onChange={(v) => updateLine(l.matHangId, { soLuong: v ?? 1 })}
          />
          <Button
            size="small"
            type="text"
            icon={<PlusOutlined style={{ color: '#D4AF37' }} />}
            onClick={() => updateLine(l.matHangId, { soLuong: l.soLuong + 1 })}
          />
        </Space>
      ),
    },
    {
      title: 'Đơn giá',
      width: 110,
      align: 'right',
      render: (_, l) => (
        <InputNumber
          size="small"
          min={0}
          step={5000}
          style={{ width: '100%', fontFamily: 'Outfit' }}
          value={l.donGia}
          onChange={(v) => updateLine(l.matHangId, { donGia: v ?? 0 })}
        />
      ),
    },
    {
      title: 'CK%',
      width: 70,
      align: 'right',
      render: (_, l) => (
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
      render: (_, l) => (
        <b style={{ color: '#8A6100', fontFamily: 'Outfit', fontSize: 14 }}>
          {fmtVnd(lineAmount(l))}
        </b>
      ),
    },
    {
      title: '',
      key: 'del',
      width: 40,
      align: 'center',
      render: (_, l) => (
        <Button
          type="text"
          danger
          size="small"
          icon={<DeleteOutlined />}
          onClick={() => removeLine(l.matHangId)}
        />
      ),
    },
  ];

  const cartTable = (
    <Table
      className="luxury-table"
      size="small"
      rowKey="matHangId"
      pagination={false}
      dataSource={lines}
      columns={cartColumns}
      scroll={{ y: 280 }}
      locale={{ emptyText: <Empty description="Chưa có mặt hàng trong giỏ — chọn ở lưới bên trái" /> }}
    />
  );

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={16}>
      {/* HEADER HÓA ĐƠN & THÔNG TIN CHUNG */}
      <Card
        className="luxury-card"
        title={
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <span style={{
              background: 'linear-gradient(135deg, #FFE28A, #D4AF37)',
              color: '#1A1403',
              padding: '2px 8px',
              borderRadius: 4,
              fontWeight: 800,
              fontSize: 12
            }}>
              POS RETAIL
            </span>
            <span style={{ color: '#8A6100', fontWeight: 800, fontSize: 16 }}>
              HÓA ĐƠN BÁN HÀNG
            </span>
          </div>
        }
      >
        <Row gutter={[12, 10]}>
          <Col xs={12} md={4}>
            <div style={{ fontSize: 12, fontWeight: 600, color: '#7E7359', marginBottom: 4 }}>Ngày lập</div>
            <Input readOnly value={new Date().toLocaleDateString('vi-VN')} style={{ background: '#FAF8F2' }} />
          </Col>
          <Col xs={12} md={4}>
            <div style={{ fontSize: 12, fontWeight: 600, color: '#7E7359', marginBottom: 4 }}>Số HĐ gần nhất</div>
            <Input
              readOnly
              value={lastBill?.soPhieu ?? '—'}
              style={{ color: '#10B981', fontWeight: 700, fontFamily: 'JetBrains Mono', background: '#FAF8F2' }}
            />
          </Col>
          <Col xs={24} md={6}>
            <div style={{ fontSize: 12, fontWeight: 600, color: '#7E7359', marginBottom: 4 }}>Khách hàng</div>
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              placeholder="Khách lẻ mặc định"
              style={{ width: '100%' }}
              value={khachHangId ?? undefined}
              onChange={(v) => setKhachHang(v ?? null)}
              options={customers.map((c) => ({ value: c.id, label: `${c.maKhach ?? ''} — ${c.name}` }))}
            />
          </Col>
          <Col xs={12} md={5}>
            <div style={{ fontSize: 12, fontWeight: 600, color: '#7E7359', marginBottom: 4 }}>Kho xuất hàng</div>
            <Select
              placeholder="Chọn kho xuất"
              style={{ width: '100%' }}
              value={khoId ?? undefined}
              onChange={(v) => setKho(v)}
              options={warehouses.map((w) => ({ value: w.id, label: w.name }))}
            />
          </Col>
          <Col xs={12} md={5}>
            <div style={{ fontSize: 12, fontWeight: 600, color: '#7E7359', marginBottom: 4 }}>Nhân viên xuất hoá đơn</div>
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              placeholder="Nhân viên xuất hoá đơn"
              style={{ width: '100%' }}
              value={nhanVienId ?? undefined}
              onChange={(v) => setNhanVien(v ?? null)}
              options={employees.map((e) => ({ value: e.id, label: e.name }))}
            />
          </Col>
          <Col xs={24} md={12}>
            <div style={{ fontSize: 12, fontWeight: 600, color: '#7E7359', marginBottom: 4 }}>Diễn giải</div>
            <Input allowClear placeholder="Diễn giải hoá đơn" value={dienGiai} onChange={(e) => setDienGiai(e.target.value)} />
          </Col>
          <Col xs={24} md={12}>
            <div style={{ fontSize: 12, fontWeight: 600, color: '#7E7359', marginBottom: 4 }}>Ghi chú</div>
            <Input allowClear placeholder="Ghi chú hoá đơn" value={ghiChu} onChange={(e) => setGhiChu(e.target.value)} />
          </Col>
        </Row>
      </Card>

      {/* THÔNG TIN KHÁCH HÀNG ĐANG CHỌN */}
      {selectedCustomer && (
        <Card className="luxury-card" size="small"
          title={
            <Space>
              <UserOutlined style={{ color: '#D4AF37' }} />
              <span style={{ color: '#8A6100', fontWeight: 700 }}>Thông tin khách hàng</span>
            </Space>
          }
        >
          <Descriptions size="small" column={{ xs: 1, md: 2 }} bordered>
            <Descriptions.Item label="Mã khách">
              <span className="code-badge">{selectedCustomer.maKhach ?? '—'}</span>
            </Descriptions.Item>
            <Descriptions.Item label="Tên khách"><b style={{ color: '#1C180E' }}>{selectedCustomer.name}</b></Descriptions.Item>
            <Descriptions.Item label="Điện thoại">{selectedCustomer.dienThoai ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Nhóm">{selectedCustomer.nhomKhachHang ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Địa chỉ" span={2}>{selectedCustomer.diaChi ?? '—'}</Descriptions.Item>
            <Descriptions.Item label="Nợ cho phép">{selectedCustomer.songayChoNo} ngày</Descriptions.Item>
            <Descriptions.Item label="Điểm tích luỹ">{selectedCustomer.diemTichLuy}</Descriptions.Item>
          </Descriptions>
        </Card>
      )}

      <Row gutter={[16, 16]}>
        {/* CỘT TRÁI: LƯỚI CHỌN MẶT HÀNG */}
        <Col xs={24} lg={13}>
          <Card
            className="luxury-card"
            title={
              <Space>
                <ShoppingCartOutlined style={{ color: '#D4AF37' }} />
                <span style={{ fontWeight: 700, color: '#1C180E' }}>Danh Sách Mặt Hàng</span>
              </Space>
            }
            extra={
              <Input
                allowClear
                prefix={<SearchOutlined style={{ color: '#D4AF37' }} />}
                placeholder="Tìm mã hoặc tên hàng..."
                style={{ width: 230, borderRadius: 8 }}
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            }
          >
            <div style={{
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
              marginBottom: 10,
              padding: '6px 12px',
              background: '#FFFDF5',
              borderRadius: 8,
              border: '1px solid rgba(212, 175, 55, 0.2)'
            }}>
              <Space>
                <span style={{ fontSize: 12, fontWeight: 600, color: '#7E7359' }}>Số lượng thêm:</span>
                <InputNumber
                  size="small"
                  min={1}
                  precision={0}
                  value={pickSl}
                  style={{ width: 70, fontWeight: 700 }}
                  onChange={(v) => setPickSl(v ?? 1)}
                />
              </Space>
              <span style={{ color: '#A08D6A', fontSize: 12 }}>
                💡 Click trực tiếp vào dòng để thêm nhanh vào giỏ
              </span>
            </div>

            <Table
              className="luxury-table"
              size="middle"
              rowKey="id"
              pagination={false}
              dataSource={filteredProducts}
              columns={productColumns}
              scroll={{ y: 320 }}
              onRow={(p) => ({
                onClick: () => addFromPicker(p),
                style: { cursor: 'pointer' },
              })}
              locale={{ emptyText: <Empty description="Không tìm thấy mặt hàng phù hợp" /> }}
            />
          </Card>
        </Col>

        {/* CỘT PHẢI: GIỎ HÀNG + TỔNG CỘNG THANH TOÁN */}
        <Col xs={24} lg={11}>
          <Card
            className="luxury-card"
            title={
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <span style={{ fontWeight: 700, color: '#1C180E' }}>
                  Giỏ Hàng ({lines.length} món)
                </span>
                {lines.length > 0 && (
                  <Button size="small" type="text" danger onClick={resetCart}>
                    Xóa tất cả
                  </Button>
                )}
              </div>
            }
            styles={{ body: { padding: 0 } }}
          >
            <Tabs
              size="small"
              tabBarStyle={{ paddingInline: 16, margin: 0, background: '#FAF8F2' }}
              items={[
                { key: 'lay', label: 'KHÁCH LẤY HÀNG', children: cartTable },
                {
                  key: 'tra',
                  label: 'KHÁCH TRẢ HÀNG',
                  children: (
                    <div style={{ padding: 20, textAlign: 'center' }}>
                      <p style={{ color: '#7E7359', fontSize: 13, marginBottom: 12 }}>
                        Hàng khách trả lại được ghi nhận tự động nhập kho và hoàn tiền tại màn hình riêng.
                      </p>
                      <Button onClick={() => navigate('/pos/returns')}>
                        Mở Màn Hình Khách Trả Lại
                      </Button>
                    </div>
                  ),
                },
              ]}
            />
          </Card>

          {/* KHUNG TỔNG CỘNG & THANH TOÁN */}
          <Card className="luxury-card" style={{ marginTop: 14 }}>
            <Descriptions size="small" column={1} bordered>
              <Descriptions.Item label="Tiền hàng">
                <b style={{ fontFamily: 'Outfit', fontSize: 15 }}>{fmtVnd(tienHang)}</b>
              </Descriptions.Item>
              <Descriptions.Item label="Giảm giá">
                <Space>
                  <InputNumber
                    size="small"
                    min={0}
                    max={100}
                    style={{ width: 70 }}
                    suffix="%"
                    value={tileGiamGia}
                    onChange={(v) => setTileGiamGia(v ?? 0)}
                  />
                  <InputNumber
                    size="small"
                    min={0}
                    max={tienHang}
                    style={{ width: 140, fontFamily: 'Outfit' }}
                    value={tienGiamGia}
                    onChange={(v) => setTienGiamGia(v ?? 0)}
                  />
                </Space>
              </Descriptions.Item>
              <Descriptions.Item label="Tiền thuế">
                <Space>
                  <InputNumber
                    size="small"
                    min={0}
                    max={100}
                    style={{ width: 70 }}
                    suffix="%"
                    value={thuePct}
                    onChange={(v) => {
                      const pct = v ?? 0;
                      setThuePct(pct);
                      setTienThue(round0(tienHang * pct / 100));
                    }}
                  />
                  <InputNumber
                    size="small"
                    min={0}
                    style={{ width: 140, fontFamily: 'Outfit' }}
                    value={tienThue}
                    onChange={(v) => setTienThue(v ?? 0)}
                  />
                </Space>
              </Descriptions.Item>
              <Descriptions.Item label="Phí vận chuyển">
                <InputNumber
                  size="small"
                  min={0}
                  style={{ width: 140, fontFamily: 'Outfit' }}
                  value={phiVanChuyen}
                  onChange={(v) => setPhiVanChuyen(v ?? 0)}
                />
              </Descriptions.Item>
              <Descriptions.Item label="TỔNG CỘNG">
                <span style={{
                  color: '#8A6100',
                  fontSize: 22,
                  fontWeight: 800,
                  fontFamily: 'Outfit, sans-serif'
                }}>
                  {fmtVnd(tongCong)}
                </span>
              </Descriptions.Item>
              <Descriptions.Item label="Khách đưa">
                <Space>
                  <InputNumber
                    min={0}
                    step={10000}
                    style={{ width: 150, fontFamily: 'Outfit', fontWeight: 700 }}
                    value={khachDua}
                    onChange={(v) => setKhachDua(v ?? 0)}
                  />
                  <Button size="small" onClick={() => setKhachDua(tongCong)}>
                    Đủ tiền
                  </Button>
                  <Button
                    size="small"
                    icon={<QrcodeOutlined />}
                    onClick={() => setShowQr(!showQr)}
                  >
                    VietQR
                  </Button>
                </Space>
              </Descriptions.Item>
              <Descriptions.Item label="Tiền thừa trả khách">
                <span style={{ color: '#10B981', fontSize: 18, fontWeight: 700, fontFamily: 'Outfit' }}>
                  {fmtVnd(traLai)}
                </span>
              </Descriptions.Item>
              {conNo > 0 && (
                <Descriptions.Item label="Còn nợ">
                  <Tag color="error" style={{ fontSize: 14, padding: '2px 8px' }}>
                    {fmtVnd(conNo)}
                  </Tag>
                </Descriptions.Item>
              )}
            </Descriptions>

            {/* Khung VietQR động nếu thu ngân bấm mở */}
            {showQr && (
              <div style={{
                marginTop: 12,
                padding: 14,
                background: '#FFFDF5',
                borderRadius: 8,
                border: '1px solid #D4AF37',
                textAlign: 'center'
              }}>
                <div style={{ fontWeight: 700, color: '#8A6100', marginBottom: 6 }}>
                  Quét Mã VietQR Chuyển Khoản Tức Thì
                </div>
                <div style={{ fontSize: 13, color: '#555', marginBottom: 8 }}>
                  Số tiền: <b style={{ color: '#8A6100' }}>{fmtVnd(tongCong)}</b>
                </div>
                {isVietQrConfigured() ? (
                  <img
                    src={buildVietQrUrl(tongCong)}
                    alt="VietQR code"
                    style={{ width: 180, height: 180, objectFit: 'contain', margin: '0 auto', display: 'block', borderRadius: 8 }}
                  />
                ) : (
                  <Alert
                    type="warning"
                    showIcon
                    message="Chưa cấu hình tài khoản VietQR"
                    description="Khai báo VITE_VIETQR_BANK_BIN và VITE_VIETQR_ACCOUNT trong file .env để sinh mã QR đúng người nhận."
                  />
                )}
              </div>
            )}

            <Space style={{ marginTop: 16 }} wrap>
              <Button
                type="primary"
                size="large"
                className="gold-gradient-btn gold-shimmer-sweep"
                loading={paying}
                onClick={thanhToan}
                disabled={lines.length === 0 || !khoId}
                style={{ height: 46, paddingInline: 28, fontSize: 15 }}
              >
                ⭐ THANH TOÁN
              </Button>
              <Button
                size="large"
                icon={<EyeOutlined />}
                onClick={() => setPreviewOpen(true)}
                disabled={lines.length === 0}
              >
                Xem trước hoá đơn
              </Button>
              <Button
                size="large"
                onClick={() => {
                  resetCart();
                  setThuePct(0);
                  void message.info('Đã làm mới form — tạo hoá đơn mới');
                }}
              >
                Tạo mới
              </Button>
            </Space>

            {vuotTonIds.length > 0 && (
              <Alert
                style={{ marginTop: 14, borderRadius: 8 }}
                type="warning"
                showIcon
                message="Cảnh báo vượt tồn kho tại kho đang chọn"
                description="Vui lòng giảm số lượng hoặc chọn kho xuất khác."
              />
            )}

            {lastBill && (
              <div style={{
                marginTop: 16,
                padding: 16,
                background: '#ECFDF5',
                borderRadius: 10,
                border: '1px solid #A7F3D0',
                boxShadow: '0 4px 12px rgba(16, 185, 129, 0.1)'
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 8, color: '#065F46', fontWeight: 800, fontSize: 15 }}>
                  <CheckCircleOutlined style={{ fontSize: 18 }} />
                  ĐÃ TẠO THÀNH CÔNG HÓA ĐƠN {lastBill.soPhieu}
                </div>
                <div style={{ margin: '8px 0', fontSize: 13, color: '#047857' }}>
                  Tổng thanh toán: <b>{fmtVnd(lastBill.tongCong)}</b>
                  {lastBill.traLai > 0 && ` · Tiền thừa trả khách: ${fmtVnd(lastBill.traLai)}`}
                  {lastBill.conNo > 0 && ` · Còn nợ: ${fmtVnd(lastBill.conNo)}`}
                </div>
                <Space style={{ marginTop: 8 }}>
                  <Button
                    size="small"
                    type="primary"
                    icon={<PrinterOutlined />}
                    style={{ background: '#10B981', borderColor: '#10B981' }}
                    onClick={() => printInvoicePdf(lastBill.donHangId)}
                  >
                    In hoá đơn (PDF)
                  </Button>
                  <Button
                    size="small"
                    icon={<DownloadOutlined />}
                    onClick={() => downloadInvoicePdf(lastBill.donHangId)}
                  >
                    Tải về máy
                  </Button>
                </Space>
              </div>
            )}
          </Card>
        </Col>
      </Row>

      {/* XEM TRƯỚC HOÁ ĐƠN (bản in trên màn hình) */}
      <Modal
        open={previewOpen}
        onCancel={() => setPreviewOpen(false)}
        footer={null}
        width={600}
        title={<span style={{ color: '#8A6100', fontWeight: 800 }}>Xem trước hoá đơn</span>}
      >
        <div style={{ background: '#fff', border: '1px solid rgba(212,175,55,0.35)', borderRadius: 10, padding: 18 }}>
          <div style={{ textAlign: 'center' }}>
            <div style={{ fontWeight: 800, fontSize: 16, color: '#8A6100' }}>CỬA HÀNG HOÀN NGỌC</div>
            <div style={{ fontSize: 12, color: '#7E7359' }}>HOÀN NGỌC GROUP</div>
            <div style={{ marginTop: 6, fontWeight: 700, letterSpacing: 0.4 }}>HÓA ĐƠN BÁN HÀNG</div>
          </div>
          <Divider style={{ margin: '10px 0', borderColor: '#D4AF37' }} />
          <Row justify="space-between" style={{ fontSize: 13 }}>
            <span>Số: <b style={{ color: '#8A6100' }}>{lastBill?.soPhieu ?? '(chưa lưu)'}</b></span>
            <span>Ngày: {new Date().toLocaleDateString('vi-VN')}</span>
          </Row>
          <div style={{ margin: '8px 0', fontSize: 13 }}>
            <div>Khách hàng: <b>{selectedCustomer?.name ?? 'Khách lẻ'}</b></div>
            {selectedCustomer?.dienThoai && <div style={{ color: '#7E7359' }}>ĐT: {selectedCustomer.dienThoai}</div>}
            {selectedCustomer?.diaChi && <div style={{ color: '#7E7359' }}>Địa chỉ: {selectedCustomer.diaChi}</div>}
            <div style={{ color: '#7E7359' }}>
              Nhân viên xuất HĐ: <b>{selectedEmployee?.name ?? '—'}</b> · Kho: {selectedWarehouse?.name ?? '—'}
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
              { title: 'Đơn giá', dataIndex: 'donGia', width: 100, align: 'right',
                render: (v: number) => fmtVnd(v) },
              { title: 'Thành tiền', width: 110, align: 'right',
                render: (_v, r) => <b>{fmtVnd(lineAmount(r))}</b> },
            ]}
          />
          <div style={{ marginTop: 10, marginLeft: 'auto', width: 260, fontSize: 13 }}>
            <Row justify="space-between"><span style={{ color: '#7E7359' }}>Tiền hàng:</span><span>{fmtVnd(tienHang)}</span></Row>
            {giamTong > 0 && <Row justify="space-between"><span style={{ color: '#7E7359' }}>Giảm giá:</span><span style={{ color: '#E11D48' }}>- {fmtVnd(giamTong)}</span></Row>}
            {tienThue > 0 && <Row justify="space-between"><span style={{ color: '#7E7359' }}>Thuế:</span><span>{fmtVnd(tienThue)}</span></Row>}
            {phiVanChuyen > 0 && <Row justify="space-between"><span style={{ color: '#7E7359' }}>Phí vận chuyển:</span><span>{fmtVnd(phiVanChuyen)}</span></Row>}
            <Divider style={{ margin: '6px 0', borderColor: '#D4AF37' }} />
            <Row justify="space-between">
              <b style={{ color: '#8A6100' }}>TỔNG CỘNG:</b>
              <b style={{ color: '#8A6100', fontSize: 16 }}>{fmtVnd(tongCong)}</b>
            </Row>
            {khachDua > 0 && <Row justify="space-between"><span style={{ color: '#7E7359' }}>Khách đưa:</span><span>{fmtVnd(khachDua)}</span></Row>}
            {traLai > 0 && <Row justify="space-between"><span style={{ color: '#7E7359' }}>Trả lại:</span><span style={{ color: '#10B981' }}>{fmtVnd(traLai)}</span></Row>}
            {conNo > 0 && <Row justify="space-between"><span style={{ color: '#7E7359' }}>Còn nợ:</span><b style={{ color: '#E11D48' }}>{fmtVnd(conNo)}</b></Row>}
          </div>
          <Divider style={{ margin: '12px 0 6px', borderColor: '#F4EEDC' }} />
          <div style={{ fontSize: 11, color: '#7E7359', textAlign: 'center' }}>
            Cảm ơn quý khách — Hẹn gặp lại!
          </div>
        </div>
      </Modal>
    </Space>
  );
};

export default RetailSalePage;