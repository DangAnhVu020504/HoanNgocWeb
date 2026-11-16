import React, { useCallback, useEffect, useState } from 'react';
import {
  Alert, Button, Card, Col, DatePicker, Input, InputNumber, Row, Select,
  Space, Table, Tabs, Tag, message
} from 'antd';
import type { Dayjs } from 'dayjs';
import {
  CheckCircleOutlined, DownloadOutlined, PlusOutlined,
  PrinterOutlined, SearchOutlined
} from '@ant-design/icons';
import {
  getCashBalance, getCashReasons, getCashVouchers, createCashReceipt, createCashPayment,
  type CashReason, type CashVoucher,
} from '../../api/cashbook';
import { downloadCashVoucherPdf, printCashVoucherPdf } from '../../api/reports';
import { getCustomers, type Customer } from '../../api/customers';
import { getSuppliers, type Supplier } from '../../api/suppliers';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

interface AllocLine { donHangId: string; soTien: number }

const VoucherForm: React.FC<{ mode: 'receipt' | 'payment' }> = ({ mode }) => {
  const isReceipt = mode === 'receipt';
  const [reasons, setReasons] = useState<CashReason[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [doiTuong, setDoiTuong] = useState<'KH' | 'NCC'>('KH');
  const [doiTuongId, setDoiTuongId] = useState<string | null>(null);
  const [lyDoId, setLyDoId] = useState<string | null>(null);
  const [tien, setTien] = useState(0);
  const [chuyenKhoan, setChuyenKhoan] = useState(false);
  const [ghiChu, setGhiChu] = useState('');
  const [allocs, setAllocs] = useState<AllocLine[]>([]);
  const [saving, setSaving] = useState(false);
  const [last, setLast] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    getCashReasons().then(setReasons).catch(() => { });
    getCustomers({ pageSize: 200 }).then((r) => setCustomers(r.items)).catch(() => { });
    getSuppliers({ pageSize: 200 }).then((r) => setSuppliers(r.items)).catch(() => { });
  }, []);

  const allocSupported = isReceipt ? doiTuong === 'KH' : doiTuong === 'NCC';

  const save = async () => {
    if (!doiTuongId) {
      void message.warning(isReceipt ? 'Chưa chọn đối tượng thu' : 'Chưa chọn đối tượng chi');
      return;
    }
    if (tien <= 0) {
      void message.warning('Số tiền phải > 0');
      return;
    }
    setSaving(true);
    setErr(null);
    try {
      const payload = {
        doiTuong,
        khachHangId: doiTuong === 'KH' ? doiTuongId : null,
        nhaCungCapId: doiTuong === 'NCC' ? doiTuongId : null,
        nhanVienId: null,
        lyDoId,
        tien,
        chuyenKhoan,
        ghiChu: ghiChu.trim() || null,
        allocations: allocSupported && allocs.length > 0 ? allocs : null,
      };
      const r = isReceipt ? await createCashReceipt(payload) : await createCashPayment(payload);
      setLast(`${isReceipt ? 'Phiếu thu' : 'Phiếu chi'} ${r.soPhieu} — ${fmtVnd(r.tien)}` +
        (r.daGanNo > 0 ? ` (trả nợ ${fmtVnd(r.daGanNo)})` : ''));
      void message.success(`Đã tạo ${isReceipt ? 'phiếu thu' : 'phiếu chi'} ${r.soPhieu}`);
      setTien(0);
      setGhiChu('');
      setAllocs([]);
      setLyDoId(null);
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : `Lỗi tạo phiếu ${isReceipt ? 'thu' : 'chi'}`);
    } finally {
      setSaving(false);
    }
  };

  const doiTuongOptions = doiTuong === 'KH'
    ? customers.map((c) => ({ value: c.id, label: `${c.maKhach ?? ''} — ${c.name}` }))
    : suppliers.map((s) => ({ value: s.id, label: `${s.maNhaCungCap ?? ''} — ${s.name}` }));

  const addAlloc = () => setAllocs((p) => [...p, { donHangId: '', soTien: 0 }]);
  const updAlloc = (i: number, patch: Partial<AllocLine>) =>
    setAllocs((p) => p.map((a, idx) => idx === i ? { ...a, ...patch } : a));
  const rmAlloc = (i: number) => setAllocs((p) => p.filter((_, idx) => idx !== i));
  const tongAlloc = allocs.reduce((s, a) => s + a.soTien, 0);

  return (
    <Card
      className="luxury-card"
      title={
        <span style={{ color: isReceipt ? '#10B981' : '#E11D48', fontWeight: 800, fontSize: 16 }}>
          {isReceipt ? 'LẬP PHIẾU THU TIỀN' : 'LẬP PHIẾU CHI TIỀN'}
        </span>
      }
      style={{ maxWidth: 860, margin: '0 auto' }}
    >
      <Space direction="vertical" style={{ width: '100%' }} size={14}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={8}>
            <div style={{ fontSize: 12, fontWeight: 600, color: '#7E7359', marginBottom: 4 }}>Đối tượng</div>
            <Select
              value={doiTuong}
              onChange={(v) => { setDoiTuong(v); setDoiTuongId(null); setAllocs([]); }}
              style={{ width: '100%' }}
              options={[
                { value: 'KH', label: 'Khách hàng' },
                { value: 'NCC', label: 'Nhà cung cấp' },
              ]}
            />
          </Col>
          <Col xs={24} sm={16}>
            <div style={{ fontSize: 12, fontWeight: 600, color: '#7E7359', marginBottom: 4 }}>Chọn đối tượng</div>
            <Select
              showSearch
              optionFilterProp="label"
              placeholder={doiTuong === 'KH' ? 'Chọn khách hàng' : 'Chọn nhà cung cấp'}
              style={{ width: '100%' }}
              value={doiTuongId ?? undefined}
              onChange={(v) => setDoiTuongId(v)}
              options={doiTuongOptions}
            />
          </Col>
        </Row>

        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12}>
            <div style={{ fontSize: 12, fontWeight: 600, color: '#7E7359', marginBottom: 4 }}>Lý do thu / chi</div>
            <Select
              allowClear
              showSearch
              optionFilterProp="label"
              placeholder="Chọn lý do"
              style={{ width: '100%' }}
              value={lyDoId ?? undefined}
              onChange={(v) => setLyDoId(v ?? null)}
              options={reasons.filter((r) => isReceipt ? r.laLyDoThu : !r.laLyDoThu)
                .map((r) => ({ value: r.id, label: r.name }))}
            />
          </Col>
          <Col xs={24} sm={12}>
            <div style={{ fontSize: 12, fontWeight: 600, color: '#7E7359', marginBottom: 4 }}>Hình thức thanh toán</div>
            <Select
              value={chuyenKhoan}
              onChange={setChuyenKhoan}
              style={{ width: '100%' }}
              options={[
                { value: false, label: '💵 Tiền mặt' },
                { value: true, label: '🏦 Chuyển khoản ngân hàng' },
              ]}
            />
          </Col>
        </Row>

        <div>
          <div style={{ fontSize: 12, fontWeight: 600, color: '#7E7359', marginBottom: 4 }}>Diễn giải / Ghi chú</div>
          <Input
            allowClear
            placeholder="Nội dung diễn giải chi tiết cho phiếu..."
            value={ghiChu}
            onChange={(e) => setGhiChu(e.target.value)}
          />
        </div>

        <div style={{
          background: '#FFFDF5',
          padding: '16px 20px',
          borderRadius: 10,
          border: '1px solid rgba(212, 175, 55, 0.3)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 12
        }}>
          <div>
            <div style={{ fontSize: 12, fontWeight: 700, color: '#8A6100', textTransform: 'uppercase' }}>
              {isReceipt ? 'Số tiền thực thu' : 'Số tiền thực chi'}
            </div>
            <div style={{ fontSize: 12, color: '#7E7359' }}>Nhập số tiền VNĐ phát sinh</div>
          </div>
          <InputNumber
            min={0}
            step={50000}
            style={{ width: 240, fontFamily: 'Outfit', fontSize: 18, fontWeight: 700 }}
            value={tien}
            onChange={(v) => setTien(v ?? 0)}
          />
        </div>

        {allocSupported && (
          <Card
            size="small"
            className="luxury-card"
            title={<span style={{ fontSize: 13, fontWeight: 700, color: '#8A6100' }}>Phân Bổ Trả Nợ Cho Đơn Hàng (Tùy chọn)</span>}
            extra={
              <Button size="small" icon={<PlusOutlined />} onClick={addAlloc}>
                Thêm dòng phân bổ
              </Button>
            }
          >
            {allocs.length === 0 && (
              <div style={{ color: '#888', fontSize: 12, textAlign: 'center', padding: '10px 0' }}>
                Không phân bổ = phiếu quỹ độc lập (không cấn trừ công nợ đơn hàng cụ thể).
              </div>
            )}
            {allocs.map((a, i) => (
              <Row key={i} gutter={8} style={{ marginBottom: 8 }}>
                <Col span={12}>
                  <Input
                    placeholder="Mã / ID hóa đơn hoặc phiếu"
                    value={a.donHangId}
                    onChange={(e) => updAlloc(i, { donHangId: e.target.value })}
                  />
                </Col>
                <Col span={10}>
                  <InputNumber
                    min={0}
                    style={{ width: '100%', fontFamily: 'Outfit' }}
                    value={a.soTien}
                    onChange={(v) => updAlloc(i, { soTien: v ?? 0 })}
                  />
                </Col>
                <Col span={2}>
                  <Button type="text" danger onClick={() => rmAlloc(i)}>X</Button>
                </Col>
              </Row>
            ))}
            {allocs.length > 0 && (
              <div style={{ marginTop: 8, fontSize: 13 }}>
                Tổng đã phân bổ: <b style={{ fontFamily: 'Outfit', color: '#8A6100' }}>{fmtVnd(tongAlloc)}</b>
                {tongAlloc > tien && (
                  <Tag color="error" style={{ marginLeft: 8 }}>
                    Vượt quá số tiền phiếu!
                  </Tag>
                )}
              </div>
            )}
          </Card>
        )}

        <Button
          type="primary"
          size="large"
          block
          loading={saving}
          onClick={save}
          disabled={tien <= 0 || !doiTuongId || tongAlloc > tien}
          className="gold-gradient-btn"
          style={{ height: 46, fontSize: 15 }}
        >
          {isReceipt ? 'XÁC NHẬN TẠO PHIẾU THU' : 'XÁC NHẬN TẠO PHIẾU CHI'}
        </Button>

        {err && <Alert type="error" showIcon message={err} style={{ borderRadius: 8 }} />}
        {last && (
          <div style={{
            padding: 12,
            borderRadius: 8,
            background: '#ECFDF5',
            border: '1px solid #A7F3D0',
            color: '#065F46',
            display: 'flex',
            alignItems: 'center',
            gap: 8,
            fontWeight: 600
          }}>
            <CheckCircleOutlined style={{ fontSize: 16 }} />
            {last}
          </div>
        )}
      </Space>
    </Card>
  );
};

const VoucherList: React.FC = () => {
  const [rows, setRows] = useState<CashVoucher[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [isThu, setIsThu] = useState<boolean | undefined>(undefined);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (p: number, thu: boolean | undefined, s: string) => {
    setLoading(true);
    try {
      const r = await getCashVouchers({ isThu: thu, search: s || undefined, page: p, pageSize: 15 });
      setRows(r.items);
      setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải danh sách phiếu');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(1, undefined, ''); }, [load]);

  return (
    <Card
      className="luxury-card"
      title={<span style={{ color: '#8A6100', fontWeight: 800, fontSize: 15 }}>Lịch Sử Phiếu Thu / Chi</span>}
      extra={
        <Space>
          <Select
            value={isThu}
            onChange={(v) => { setIsThu(v); setPage(1); void load(1, v, search); }}
            style={{ width: 140 }}
            options={[
              { value: undefined, label: 'Tất cả loại' },
              { value: true, label: 'Phiếu thu (TM)' },
              { value: false, label: 'Phiếu chi (TM)' },
            ]}
          />
          <Input.Search
            allowClear
            prefix={<SearchOutlined style={{ color: '#D4AF37' }} />}
            placeholder="Tìm số phiếu / đối tượng..."
            style={{ width: 260 }}
            onSearch={(v) => { setSearch(v); setPage(1); void load(1, isThu, v); }}
          />
        </Space>
      }
    >
      <Table
        className="luxury-table"
        size="middle"
        rowKey="id"
        loading={loading}
        dataSource={rows}
        pagination={{
          current: page,
          pageSize: 15,
          total,
          onChange: (p) => { setPage(p); void load(p, isThu, search); },
        }}
        columns={[
          {
            title: 'HT',
            width: 70,
            align: 'center',
            render: (_: unknown, r: CashVoucher) => (
              <span style={{
                background: r.chuyenKhoan ? '#F0F9FF' : '#FFFDF0',
                color: r.chuyenKhoan ? '#0284C7' : '#8A6100',
                padding: '2px 6px',
                borderRadius: 4,
                fontWeight: 700,
                fontSize: 11,
                border: '1px solid rgba(212, 175, 55, 0.3)'
              }}>
                {r.chuyenKhoan ? 'CK' : 'TM'}
              </span>
            ),
          },
          { title: 'Lý do thu / chi', dataIndex: 'lyDo', render: (v) => <b>{v ?? '—'}</b> },
          { title: 'Nội dung diễn giải', dataIndex: 'ghiChu', ellipsis: true },
          {
            title: '',
            key: 'print',
            width: 140,
            align: 'center',
            render: (_: unknown, r: CashVoucher) => (
              <Space size={6}>
                <Button size="small" icon={<PrinterOutlined />} onClick={() => printCashVoucherPdf(r.id)}>
                  In
                </Button>
                <Button size="small" icon={<DownloadOutlined />} onClick={() => downloadCashVoucherPdf(r.id)}>
                  Tải
                </Button>
              </Space>
            ),
          },
        ]}
      />
    </Card>
  );
};

const BalanceCard: React.FC = () => {
  const [balance, setBalance] = useState<Record<string, number> | null>(null);
  const [from, setFrom] = useState<Dayjs | null>(null);
  const [to, setTo] = useState<Dayjs | null>(null);
  const [err, setErr] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const b = await getCashBalance({
        from: from ? from.format('YYYY-MM-DD') : undefined,
        to: to ? to.format('YYYY-MM-DD') : undefined,
      });
      setBalance(b as unknown as Record<string, number>);
      setErr(null);
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tải tồn quỹ');
    }
  }, [from, to]);

  useEffect(() => { void load(); }, [load]);

  const b = balance as {
    tongThu: number; tongChi: number; tonQuy: number;
    tongThuChuyenKhoan: number; tongChiChuyenKhoan: number; tonChuyenKhoan: number;
  } | null;

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        flexWrap: 'wrap',
        gap: 12,
        background: '#ffffff',
        padding: '12px 18px',
        borderRadius: 10,
        border: '1px solid rgba(212, 175, 55, 0.22)'
      }}>
        <div style={{ fontWeight: 700, color: '#8A6100', fontSize: 15 }}>
          Tra Cứu Cân Đối Tồn Quỹ Theo Khoảng Thời Gian
        </div>
        <Space>
          <DatePicker placeholder="Từ ngày" value={from} onChange={setFrom} format="DD/MM/YYYY" />
          <DatePicker placeholder="Đến ngày" value={to} onChange={setTo} format="DD/MM/YYYY" />
          <Button type="primary" onClick={load} className="gold-gradient-btn">
            Lọc Dữ Liệu
          </Button>
        </Space>
      </div>

      {err && <Alert type="error" showIcon message={err} />}

      {b && (
        <Row gutter={[16, 16]}>
          <Col xs={24} sm={12} lg={6}>
            <div className="luxury-kpi-card">
              <div className="kpi-label">TỔNG THU (TIỀN MẶT)</div>
              <div className="kpi-value" style={{ color: '#10B981' }}>{fmtVnd(b.tongThu)}</div>
              <div className="kpi-sub">Thu tiền mặt trực tiếp tại quầy</div>
            </div>
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <div className="luxury-kpi-card">
              <div className="kpi-label">TỔNG CHI (TIỀN MẶT)</div>
              <div className="kpi-value" style={{ color: '#E11D48' }}>{fmtVnd(b.tongChi)}</div>
              <div className="kpi-sub">Chi tiền mặt nhập hàng & chi phí</div>
            </div>
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <div className="luxury-kpi-card">
              <div className="kpi-label">TỒN QUỸ TIỀN MẶT HIỆN TẠI</div>
              <div className="kpi-value text-gold" style={{ fontSize: 24 }}>{fmtVnd(b.tonQuy)}</div>
              <div className="kpi-sub">Cân đối Thu − Chi tiền mặt</div>
            </div>
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <div className="luxury-kpi-card">
              <div className="kpi-label">TỒN CHUYỂN KHOẢN (NGÂN HÀNG)</div>
              <div className="kpi-value" style={{ color: '#0284C7' }}>{fmtVnd(b.tonChuyenKhoan)}</div>
              <div className="kpi-sub">
                Thu {fmtVnd(b.tongThuChuyenKhoan)} − Chi {fmtVnd(b.tongChiChuyenKhoan)}
              </div>
            </div>
          </Col>
        </Row>
      )}
    </Space>
  );
};

/** Quỹ (mục 5.1) — phiếu thu/chi + tồn quỹ + danh sách. */
const CashbookPage: React.FC = () => (
  <Tabs
    className="luxury-tabs"
    items={[
      { key: 'receipt', label: '💵 LẬP PHIẾU THU', children: <VoucherForm mode="receipt" /> },
      { key: 'payment', label: '💸 LẬP PHIẾU CHI', children: <VoucherForm mode="payment" /> },
      { key: 'balance', label: '🏦 TỒN QUỸ TIỀN MẶT & CK', children: <BalanceCard /> },
      { key: 'list', label: '📋 DANH SÁCH CHỨNG TỪ QUỸ', children: <VoucherList /> },
    ]}
  />
);

export default CashbookPage;