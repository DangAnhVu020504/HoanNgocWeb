import React, { useEffect, useState } from 'react';
import {
  Alert, Badge, Button, Card, Col, Empty, Row, Skeleton, Space, Statistic, Table, Tag, Tooltip
} from 'antd';
import {
  BankOutlined, DollarOutlined, ReloadOutlined, ShoppingCartOutlined, StockOutlined,
  SyncOutlined, TeamOutlined, UserOutlined, WarningOutlined
} from '@ant-design/icons';
import { getDashboardSummary, DashboardDocBucket, DashboardSummary } from '../../api/dashboard';
import { getSyncStatus, SyncStatus } from '../../api/sync';
import { useAuthStore } from '../../stores/authStore';

const fmtVnd = (n: number | string | null | undefined) => {
  if (n === null || n === undefined) return '0 ₫';
  const v = typeof n === 'string' ? Number(n) : n;
  if (!Number.isFinite(v)) return '0 ₫';
  return v.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';
};

const fmtQty = (n: number | string | null | undefined) => {
  if (n === null || n === undefined) return '0';
  const v = typeof n === 'string' ? Number(n) : n;
  if (!Number.isFinite(v)) return '0';
  return v.toLocaleString('vi-VN');
};

const docTypeLabel = (loai: number, types: { LOAI: number; DOC_TYPE: string; PREFIX: string | null; VERIFIED: number }[]) => {
  const m = types.find(t => t.LOAI === loai);
  if (m) return `${m.PREFIX ?? m.DOC_TYPE} · ${m.DOC_TYPE}`;
  return `LOAI=${loai}`;
};

const verifiedTag = (loai: number, types: { LOAI: number; VERIFIED: number }[]) => {
  const m = types.find(t => t.LOAI === loai);
  if (!m || m.VERIFIED !== 1) {
    return <Tag color="orange" icon={<WarningOutlined />}>Chưa xác minh</Tag>;
  }
  return <Tag color="green">Đã xác minh</Tag>;
};

const DashboardPage: React.FC = () => {
  const user = useAuthStore(s => s.user);
  const [data, setData] = useState<DashboardSummary | null>(null);
  const [sync, setSync] = useState<SyncStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string | null>(null);

  const load = async () => {
    try {
      setErr(null);
      const [d, s] = await Promise.all([getDashboardSummary(), getSyncStatus()]);
      setData(d);
      setSync(s);
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'Không tải được dữ liệu';
      setErr(msg);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, []);

  const todayTotal = data?.todayDocs.reduce((s, b) => s + Number(b.TongCong ?? 0), 0) ?? 0;
  const monthTotal = data?.monthDocs.reduce((s, b) => s + Number(b.TongCong ?? 0), 0) ?? 0;
  const ordersToday = data?.todayDocs.reduce((s, b) => s + Number(b.N ?? 0), 0) ?? 0;
  const cashBalance = data ? Number(data.cash.Thu ?? 0) - Number(data.cash.Chi ?? 0) : 0;

  const heartbeatState = sync?.heartbeat?.State ?? 'UNKNOWN';
  const heartbeatColor: 'green' | 'gold' | 'red' =
    heartbeatState === 'RUNNING' ? 'green' :
    heartbeatState === 'ERROR' ? 'red' : 'gold';

  const todayColumns = [
    { title: 'Loại', dataIndex: 'LOAI', key: 'loai',
      render: (v: number) => <Tag color="blue">{docTypeLabel(v, data?.docTypes ?? [])}</Tag> },
    { title: 'Xác minh', key: 'verified',
      render: (_: unknown, r: DashboardDocBucket) => verifiedTag(r.LOAI, data?.docTypes ?? []) },
    { title: 'Số phiếu', dataIndex: 'N', key: 'n', align: 'right' as const,
      render: (v: number) => fmtQty(v) },
    { title: 'Tiền hàng', dataIndex: 'TienHang', key: 't', align: 'right' as const,
      render: (v: number | string) => fmtVnd(v) },
    { title: 'Tổng cộng', dataIndex: 'TongCong', key: 'tc', align: 'right' as const,
      render: (v: number | string) => <b>{fmtVnd(v)}</b> }
  ];

  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      <Card
        title={<Space><BankOutlined /> Tổng quan</Space>}
        extra={
          <Space>
            <Tooltip title="Tự động tải lại mỗi 30 giây">
              <Badge color={heartbeatColor} text={`Sync: ${heartbeatState}`} />
            </Tooltip>
            <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Tải lại</Button>
          </Space>
        }
      >
        {err && <Alert type="error" showIcon message={err} style={{ marginBottom: 12 }} />}
        {loading && !data ? <Skeleton active paragraph={{ rows: 4 }} /> : data && (
          <>
            <Row gutter={[12, 12]}>
              <Col xs={12} md={6}>
                <Card>
                  <Statistic title="Doanh thu hôm nay" value={fmtVnd(todayTotal)} prefix={<DollarOutlined />} />
                </Card>
              </Col>
              <Col xs={12} md={6}>
                <Card>
                  <Statistic title="Số chứng từ hôm nay" value={fmtQty(ordersToday)} prefix={<ShoppingCartOutlined />} />
                </Card>
              </Col>
              <Col xs={12} md={6}>
                <Card>
                  <Statistic
                    title="Tồn kho (SKU)"
                    value={data.stock.SkuCount}
                    formatter={(v) => fmtQty(v)}
                    prefix={<StockOutlined />}
                  />
                </Card>
              </Col>
              <Col xs={12} md={6}>
                <Card>
                  <Statistic
                    title="Tồn quỹ (Thu − Chi)"
                    value={fmtVnd(cashBalance)}
                    valueStyle={{ color: cashBalance < 0 ? '#cf1322' : undefined }}
                    prefix={<BankOutlined />}
                  />
                </Card>
              </Col>
            </Row>

            <Row gutter={[12, 12]} style={{ marginTop: 12 }}>
              <Col xs={24} lg={14}>
                <Card
                  title={<Space><ShoppingCartOutlined /> Chứng từ hôm nay (TDONHANG)</Space>}
                  size="small"
                >
                  {data.todayDocs.length === 0 ? <Empty description="Chưa có chứng từ" /> : (
                    <Table
                      size="small" rowKey="LOAI" pagination={false}
                      dataSource={data.todayDocs} columns={todayColumns}
                    />
                  )}
                </Card>
              </Col>
              <Col xs={24} lg={10}>
                <Card title={<Space><TeamOutlined /> Danh mục (active)</Space>} size="small">
                  <Row gutter={[8, 8]}>
                    <Col span={12}><Statistic title="Mặt hàng" value={fmtQty(data.counts.Products)} prefix={<StockOutlined />} /></Col>
                    <Col span={12}><Statistic title="Khách hàng" value={fmtQty(data.counts.Customers)} /></Col>
                    <Col span={12}><Statistic title="Nhà cung cấp" value={fmtQty(data.counts.Suppliers)} /></Col>
                    <Col span={12}><Statistic title="Nhân viên" value={fmtQty(data.counts.Employees)} prefix={<UserOutlined />} /></Col>
                    <Col span={12}><Statistic title="Kho hàng" value={fmtQty(data.counts.Warehouses)} /></Col>
                    <Col span={12}>
                      <Statistic
                        title="Doanh thu tháng"
                        value={fmtVnd(monthTotal)}
                        valueStyle={{ color: '#3f8600' }}
                      />
                    </Col>
                  </Row>
                </Card>
              </Col>
            </Row>
          </>
        )}
      </Card>

      {user && (
        <Card size="small">
          Xin chào, <b>{user.fullName || user.username}</b> · Quyền: {user.roles.join(', ') || '—'}
        </Card>
      )}

      {sync && (
        <Card
          size="small"
          title={<Space><SyncOutlined /> Trạng thái đồng bộ</Space>}
        >
          <Row gutter={[8, 8]}>
            <Col xs={12} md={6}>
              <Statistic title="Heartbeat" value={sync.heartbeat?.State ?? '—'} />
            </Col>
            <Col xs={12} md={6}>
              <Statistic title="Last run" value={sync.heartbeat?.LastRunAt?.slice(0, 19).replace('T', ' ') ?? '—'} />
            </Col>
            <Col xs={12} md={6}>
              <Statistic title="Row errors chờ" value={fmtQty(sync.rowErrors?.Pending ?? 0)} />
            </Col>
            <Col xs={12} md={6}>
              <Statistic title="Row errors dead-letter" value={fmtQty(sync.rowErrors?.DeadLetter ?? 0)} />
            </Col>
          </Row>
        </Card>
      )}
    </Space>
  );
};

export default DashboardPage;
