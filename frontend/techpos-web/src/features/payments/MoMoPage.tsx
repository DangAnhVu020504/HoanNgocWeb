import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Alert, Button, Card, Col, Empty, Input, InputNumber, Row, Space, Table, Tag, Typography, message,
} from 'antd';
import { LinkOutlined, QrcodeOutlined } from '@ant-design/icons';
import {
  createMoMoPayment, getMoMoOrders, getMoMoStatus,
  type MoMoCreateResult, type MoMoPaymentStatus, type PaymentOrder,
} from '../../api/payments';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

const statusTag = (status: string) => {
  if (status === 'PAID') return <Tag color="green">Đã thanh toán</Tag>;
  if (status === 'FAILED') return <Tag color="red">Thất bại</Tag>;
  return <Tag color="gold">Chờ thanh toán</Tag>;
};

/** Thanh toán MoMo (Phase 8) — tạo giao dịch từ đơn web còn công nợ, hiển thị QR + trạng thái IPN. */
const MoMoPage: React.FC = () => {
  const [orders, setOrders] = useState<PaymentOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedId, setSelectedId] = useState<string | undefined>();
  const [amount, setAmount] = useState<number | null>(null);
  const [orderInfo, setOrderInfo] = useState('');
  const [creating, setCreating] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [result, setResult] = useState<MoMoCreateResult | null>(null);
  const [status, setStatus] = useState<MoMoPaymentStatus | null>(null);
  const [returnInfo, setReturnInfo] = useState<string | null>(null);
  const pollRef = useRef<number | null>(null);

  const selected = useMemo(() => orders.find((o) => o.id === selectedId), [orders, selectedId]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setOrders(await getMoMoOrders());
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tải danh sách đơn hàng');
    } finally {
      setLoading(false);
    }
  }, []);

  const refreshStatus = useCallback(async (orderId: string) => {
    try {
      const s = await getMoMoStatus(orderId);
      setStatus(s);
      if (s.status !== 'PENDING' && pollRef.current !== null) {
        window.clearInterval(pollRef.current);
        pollRef.current = null;
      }
    } catch {
      // giữ trạng thái cũ; lần poll sau sẽ thử lại
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  useEffect(() => {
    const q = new URLSearchParams(window.location.search);
    const orderId = q.get('orderId');
    if (!orderId) return;
    const code = q.get('resultCode');
    const msg = q.get('message');
    setReturnInfo(code === '0'
      ? `MoMo báo thanh toán thành công cho giao dịch ${orderId}.`
      : `MoMo trả kết quả ${code ?? '?'}${msg ? `: ${msg}` : ''}.`);
    setResult({ orderId, requestId: '', payUrl: null, qrCodeUrl: null, deeplink: null, resultCode: Number(code ?? -1), message: msg });
    void refreshStatus(orderId);
    return () => {
      if (pollRef.current !== null) window.clearInterval(pollRef.current);
    };
  }, [refreshStatus]);

  useEffect(() => {
    if (!result?.orderId) return;
    if (pollRef.current !== null) window.clearInterval(pollRef.current);
    pollRef.current = window.setInterval(() => void refreshStatus(result.orderId), 4000);
    return () => {
      if (pollRef.current !== null) window.clearInterval(pollRef.current);
      pollRef.current = null;
    };
  }, [result?.orderId, refreshStatus]);

  const selectOrder = (id: string) => {
    setSelectedId(id);
    setAmount(orders.find((o) => o.id === id)?.conNo ?? null);
    setResult(null); setStatus(null); setErr(null); setReturnInfo(null);
  };

  const create = async () => {
    if (!selected) { void message.warning('Chưa chọn đơn hàng'); return; }
    setCreating(true); setErr(null); setResult(null); setStatus(null);
    try {
      const r = await createMoMoPayment({
        donHangId: selected.id,
        amount: amount && amount > 0 ? amount : null,
        orderInfo: orderInfo.trim() || null,
      });
      setResult(r);
      setStatus({ orderId: r.orderId, status: 'PENDING', amount: amount ?? selected.conNo, resultCode: null, message: null, transId: null, payUrl: r.payUrl });
      void message.success('Đã tạo giao dịch MoMo');
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tạo giao dịch MoMo');
    } finally {
      setCreating(false);
    }
  };

  const columns = [
    { title: 'Số phiếu', dataIndex: 'soPhieu' },
    {
      title: 'Ngày', dataIndex: 'ngay', width: 110,
      render: (v: string | null) => v ? new Date(v).toLocaleDateString('vi-VN') : '—',
    },
    { title: 'Khách hàng', dataIndex: 'khachHang', ellipsis: true },
    { title: 'Tổng cộng', dataIndex: 'tongCong', width: 130, align: 'right' as const, render: fmtVnd },
    { title: 'Còn nợ', dataIndex: 'conNo', width: 130, align: 'right' as const, render: fmtVnd },
  ];

  return (
    <Space direction="vertical" style={{ width: '100%' }} size={16}>
      <Card size="small" className="luxury-card" title={<span className="card-title-gold">Thanh toán MoMo — chọn đơn hàng</span>}>
        {returnInfo && <Alert type="info" showIcon message={returnInfo} style={{ marginBottom: 12 }} />}
        <Table
          className="luxury-table"
          size="middle"
          rowKey="id"
          loading={loading}
          dataSource={orders}
          columns={columns}
          pagination={{ pageSize: 8 }}
          rowSelection={{ type: 'radio', selectedRowKeys: selectedId ? [selectedId] : [], onChange: (keys) => selectOrder(keys[0] as string) }}
          locale={{ emptyText: <Empty description="Chưa có đơn web nào còn công nợ" /> }}
        />
        {selected && (
          <Row gutter={16} style={{ marginTop: 16 }}>
            <Col xs={24} md={8}>
              <Typography.Text type="secondary">Số tiền thanh toán</Typography.Text>
              <InputNumber
                min={0} max={selected.conNo} step={1000} style={{ width: '100%' }}
                value={amount ?? undefined} onChange={(v) => setAmount(v ?? null)}
                formatter={(v) => `${v ?? ''}`.replace(/\B(?=(\d{3})+(?!\d))/g, '.')}
                parser={(v) => Number((v ?? '').replaceAll('.', ''))}
              />
              <Typography.Text type="secondary">Công nợ còn lại: {fmtVnd(selected.conNo)}</Typography.Text>
            </Col>
            <Col xs={24} md={16}>
              <Typography.Text type="secondary">Nội dung thanh toán</Typography.Text>
              <Input
                allowClear maxLength={250} value={orderInfo} onChange={(e) => setOrderInfo(e.target.value)}
                placeholder={`Thanh toan don hang ${selected.soPhieu}`}
              />
            </Col>
            <Col span={24} style={{ marginTop: 12 }}>
              <Button type="primary" className="gold-gradient-btn" icon={<QrcodeOutlined />}
                loading={creating} onClick={() => void create()}>
                TẠO MÃ QR MOMO
              </Button>
            </Col>
          </Row>
        )}
        {err && <Alert type="error" showIcon message={err} style={{ marginTop: 12 }} />}
      </Card>

      {result && (
        <Card size="small" className="luxury-card" title={<span className="card-title-gold">Giao dịch MoMo</span>}>
          <Space direction="vertical" size={10} style={{ width: '100%' }}>
            <Space wrap>
              <Typography.Text>Mã giao dịch: <Typography.Text strong>{result.orderId}</Typography.Text></Typography.Text>
              {status && statusTag(status.status)}
              {status?.amount ? <Typography.Text>Số tiền: <b>{fmtVnd(status.amount)}</b></Typography.Text> : null}
            </Space>
            {status?.message && <Alert type={status.status === 'PAID' ? 'success' : status.status === 'FAILED' ? 'error' : 'info'} showIcon message={status.message} />}
            <Space wrap>
              {result.payUrl && (
                <Button icon={<LinkOutlined />} href={result.payUrl} target="_blank" rel="noreferrer">
                  Mở trang thanh toán MoMo
                </Button>
              )}
              {result.deeplink && (
                <Button icon={<LinkOutlined />} href={result.deeplink}>Mở app MoMo</Button>
              )}
              <Button onClick={() => void refreshStatus(result.orderId)}>Làm mới trạng thái</Button>
            </Space>
            {result.qrCodeUrl && (
              <img src={result.qrCodeUrl} alt="QR MoMo" style={{ width: 240, height: 240, objectFit: 'contain', background: '#fff', borderRadius: 8 }} />
            )}
            <Typography.Text type="secondary">
              Trạng thái tự cập nhật mỗi 4 giây. IPN cần URL công khai HTTPS (Integrations:MoMo:IpnUrl).
            </Typography.Text>
          </Space>
        </Card>
      )}
    </Space>
  );
};

export default MoMoPage;
