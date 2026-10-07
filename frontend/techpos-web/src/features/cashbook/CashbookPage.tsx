import React, { useCallback, useEffect, useState } from 'react';
import {
  Alert, Button, Card, Col, DatePicker, Descriptions, Input, InputNumber, Row, Select,
  Space, Table, Tabs, Tag, message,
} from 'antd';
import type { Dayjs } from 'dayjs';
import {
  getCashBalance, getCashReasons, getCashVouchers, createCashReceipt, createCashPayment,
  type CashReason, type CashVoucher,
} from '../../api/cashbook';
import { printCashVoucherPdf } from '../../api/reports';
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

  // Phân bổ nợ chỉ áp dụng: THU_KH (trả nợ KH) hoặc CHI_NCC (trả NCC)
  const allocSupported = isReceipt ? doiTuong === 'KH' : doiTuong === 'NCC';

  const save = async () => {
    if (!doiTuongId) { void message.warning(isReceipt ? 'Chưa chọn đối tượng thu' : 'Chưa chọn đối tượng chi'); return; }
    if (tien <= 0) { void message.warning('Số tiền phải > 0'); return; }
    setSaving(true); setErr(null);
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
      setTien(0); setGhiChu(''); setAllocs([]); setLyDoId(null);
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : `Lỗi tạo phiếu ${isReceipt ? 'thu' : 'chi'}`);
    } finally { setSaving(false); }
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
    <Space direction="vertical" style={{ width: '100%' }} size={10}>
      <Select value={doiTuong} onChange={(v) => { setDoiTuong(v); setDoiTuongId(null); setAllocs([]); }} style={{ width: 200 }} options={[
        { value: 'KH', label: 'Khách hàng' },
        { value: 'NCC', label: 'Nhà cung cấp' },
      ]} />
      <Select showSearch optionFilterProp="label" placeholder={doiTuong === 'KH' ? 'Khách hàng' : 'Nhà cung cấp'}
        style={{ width: '100%' }} value={doiTuongId ?? undefined}
        onChange={(v) => setDoiTuongId(v)} options={doiTuongOptions} />
      <Select allowClear showSearch optionFilterProp="label" placeholder="Lý do thu/chi"
        style={{ width: '100%' }} value={lyDoId ?? undefined} onChange={(v) => setLyDoId(v ?? null)}
        options={reasons.filter((r) => isReceipt ? r.laLyDoThu : !r.laLyDoThu)
          .map((r) => ({ value: r.id, label: r.name }))} />
      <Input allowClear placeholder="Ghi chú / Diễn giải" value={ghiChu} onChange={(e) => setGhiChu(e.target.value)} />
      <Descriptions column={1} size="small" bordered>
        <Descriptions.Item label={isReceipt ? 'Số tiền thu' : 'Số tiền chi'}>
          <InputNumber min={0} step={10000} style={{ width: 200 }} value={tien} onChange={(v) => setTien(v ?? 0)} />
        </Descriptions.Item>
        <Descriptions.Item label="Hình thức">
          <Select value={chuyenKhoan} onChange={setChuyenKhoan} style={{ width: 200 }} options={[
            { value: false, label: 'Tiền mặt' },
            { value: true, label: 'Chuyển khoản' },
          ]} />
        </Descriptions.Item>
      </Descriptions>

      {allocSupported && (
        <Card size="small" title="Phân bổ trả nợ (tuỳ chọn — nhập ID hóa đơn/phiếu web)"
          extra={<Button size="small" icon={undefined} onClick={addAlloc}>+ Thêm dòng</Button>}>
          {allocs.length === 0 && <div style={{ color: '#999' }}>Không phân bổ = phiếu thường (không đổi công nợ).</div>}
          {allocs.map((a, i) => (
            <Row key={i} gutter={8} style={{ marginBottom: 6 }}>
              <Col span={12}><Input placeholder="ID hóa đơn / phiếu" value={a.donHangId}
                onChange={(e) => updAlloc(i, { donHangId: e.target.value })} /></Col>
              <Col span={10}><InputNumber min={0} style={{ width: '100%' }} value={a.soTien}
                onChange={(v) => updAlloc(i, { soTien: v ?? 0 })} /></Col>
              <Col span={2}><Button type="text" danger size="small" onClick={() => rmAlloc(i)}>X</Button></Col>
            </Row>
          ))}
          {allocs.length > 0 && (
            <div>Đã phân bổ: <b>{fmtVnd(tongAlloc)}</b>{tongAlloc > tien && <Tag color="red" style={{ marginLeft: 8 }}>Vượt số tiền phiếu!</Tag>}</div>
          )}
        </Card>
      )}

      <Button type="primary" size="large" block loading={saving} onClick={save}
        disabled={tien <= 0 || !doiTuongId || tongAlloc > tien}>
        {isReceipt ? 'TẠO PHIẾU THU' : 'TẠO PHIẾU CHI'}
      </Button>
      {err && <Alert type="error" showIcon message={err} />}
      {last && <Alert type="success" showIcon message={last} />}
    </Space>
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
      setRows(r.items); setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải danh sách phiếu');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(1, undefined, ''); }, [load]);

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={8}>
      <Space>
        <Select value={isThu} onChange={(v) => { setIsThu(v); setPage(1); void load(1, v, search); }}
          style={{ width: 160 }} options={[
            { value: undefined, label: 'Tất cả' },
            { value: true, label: 'Phiếu thu' },
            { value: false, label: 'Phiếu chi' },
          ]} />
        <Input.Search allowClear placeholder="Tìm số phiếu / đối tượng..." style={{ width: 260 }}
          onSearch={(v) => { setSearch(v); setPage(1); void load(1, isThu, v); }} />
      </Space>
      <Table size="small" rowKey="id" loading={loading} dataSource={rows}
        pagination={{ current: page, pageSize: 15, total, onChange: (p) => { setPage(p); void load(p, isThu, search); } }}
        columns={[
          { title: 'HT', width: 60, render: (_: unknown, r: CashVoucher) => r.chuyenKhoan ? 'CK' : 'TM' },
          { title: 'Lý do', dataIndex: 'lyDo' },
          { title: 'Ghi chú', dataIndex: 'ghiChu' },
          { title: '', key: 'print', width: 70,
            render: (_: unknown, r: CashVoucher) => (
              <Button size="small" onClick={() => printCashVoucherPdf(r.id)}>In</Button>
            ) },
        ]} />
    </Space>
  );
};

const BalanceCard: React.FC = () => {
  const [balance, setBalance] = useState<Record<string, number> | null>(null);
  const [from, setFrom] = useState<Dayjs | null>(null);
  const [to, setTo] = useState<Dayjs | null>(null);
  const [err, setErr] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const b = await getCashBalance({ from: from ? from.format('YYYY-MM-DD') : undefined, to: to ? to.format('YYYY-MM-DD') : undefined });
      setBalance(b as unknown as Record<string, number>);
      setErr(null);
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tải tồn quỹ');
    }
  }, [from, to]);

  useEffect(() => { void load(); }, [load]);

  const b = balance as { tongThu: number; tongChi: number; tonQuy: number; tongThuChuyenKhoan: number; tongChiChuyenKhoan: number; tonChuyenKhoan: number } | null;
  return (
    <Card size="small" title="Tồn quỹ" extra={
      <Space>
        <DatePicker placeholder="Từ ngày" value={from} onChange={setFrom} format="DD/MM/YYYY" />
        <DatePicker placeholder="Đến ngày" value={to} onChange={setTo} format="DD/MM/YYYY" />
      </Space>
    }>
      {err && <Alert type="error" showIcon message={err} />}
      {b && (
        <Descriptions column={2} size="small" bordered>
          <Descriptions.Item label="Tổng thu (TM)"><b style={{ color: '#389e0d' }}>{fmtVnd(b.tongThu)}</b></Descriptions.Item>
          <Descriptions.Item label="Tổng chi (TM)"><b style={{ color: '#cf1322' }}>{fmtVnd(b.tongChi)}</b></Descriptions.Item>
          <Descriptions.Item label="TỒN QUỸ (tiền mặt)" span={2}>
            <span style={{ color: '#cf1322', fontSize: 20, fontWeight: 700 }}>{fmtVnd(b.tonQuy)}</span>
          </Descriptions.Item>
          <Descriptions.Item label="Thu chuyển khoản">{fmtVnd(b.tongThuChuyenKhoan)}</Descriptions.Item>
          <Descriptions.Item label="Chi chuyển khoản">{fmtVnd(b.tongChiChuyenKhoan)}</Descriptions.Item>
          <Descriptions.Item label="Tồn chuyển khoản" span={2}><b>{fmtVnd(b.tonChuyenKhoan)}</b></Descriptions.Item>
        </Descriptions>
      )}
    </Card>
  );
};

/** Quỹ (mục 5.1) — phiếu thu/chi + tồn quỹ + danh sách. */
const CashbookPage: React.FC = () => (
  <Tabs items={[
    { key: 'receipt', label: 'Phiếu thu', children: <VoucherForm mode="receipt" /> },
    { key: 'payment', label: 'Phiếu chi', children: <VoucherForm mode="payment" /> },
    { key: 'balance', label: 'Tồn quỹ', children: <BalanceCard /> },
    { key: 'list', label: 'Danh sách phiếu', children: <VoucherList /> },
  ]} />
);

export default CashbookPage;