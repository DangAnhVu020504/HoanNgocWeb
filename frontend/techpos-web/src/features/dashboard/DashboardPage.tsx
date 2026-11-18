import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Alert, Button, Card, Col, Empty, Row, Skeleton, Space, Table, Tag, Tooltip
} from 'antd';
import {
  BankOutlined, DollarOutlined, ReloadOutlined, ShoppingCartOutlined,
  StockOutlined, SyncOutlined, TeamOutlined, WarningOutlined
} from '@ant-design/icons';
import { getDashboardSummary, DashboardDocBucket, DashboardSummary } from '../../api/dashboard';
import { getSyncStatus, SyncStatus } from '../../api/sync';
import { useAuthStore } from '../../stores/authStore';
import DashboardCharts from './DashboardCharts';

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
    return <Tag color="warning" icon={<WarningOutlined />}>Chưa xác minh</Tag>;
  }
  return <Tag color="success">Đã xác minh</Tag>;
};

const DashboardPage: React.FC = () => {
  const navigate = useNavigate();
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
  const isHeartbeatOk = heartbeatState === 'RUNNING';

  const todayColumns = [
    {
      title: 'Loại chứng từ',
      dataIndex: 'LOAI',
      key: 'loai',
      render: (v: number) => (
        <span style={{
          background: '#FFF9E6',
          color: '#8A6100',
          padding: '3px 8px',
          borderRadius: 4,
          fontSize: 12,
          fontWeight: 600,
          border: '1px solid rgba(212, 175, 55, 0.35)'
        }}>
          {docTypeLabel(v, data?.docTypes ?? [])}
        </span>
      ),
    },
    {
      title: 'Xác minh',
      key: 'verified',
      width: 140,
      render: (_: unknown, r: DashboardDocBucket) => verifiedTag(r.LOAI, data?.docTypes ?? []),
    },
    {
      title: 'Số phiếu',
      dataIndex: 'N',
      key: 'n',
      width: 110,
      align: 'right' as const,
      render: (v: number) => <b style={{ fontFamily: 'Outfit, sans-serif' }}>{fmtQty(v)}</b>,
    },
    {
      title: 'Tiền hàng',
      dataIndex: 'TienHang',
      key: 't',
      width: 160,
      align: 'right' as const,
      render: (v: number | string) => fmtVnd(v),
    },
    {
      title: 'Tổng cộng',
      dataIndex: 'TongCong',
      key: 'tc',
      width: 180,
      align: 'right' as const,
      render: (v: number | string) => (
        <b style={{ color: '#8A6100', fontFamily: 'Outfit, sans-serif', fontSize: 15 }}>
          {fmtVnd(v)}
        </b>
      ),
    },
  ];

  return (
    <Space direction="vertical" size={20} style={{ width: '100%' }}>
      {/* HEADER TỔNG QUAN & SYNC STATUS */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        flexWrap: 'wrap',
        gap: 12,
        background: '#ffffff',
        padding: '14px 20px',
        borderRadius: 12,
        border: '1px solid rgba(212, 175, 55, 0.22)',
        boxShadow: '0 2px 10px rgba(212, 175, 55, 0.05)'
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <div style={{
            width: 38,
            height: 38,
            borderRadius: 8,
            background: 'linear-gradient(135deg, #FFE28A, #D4AF37)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: '#1A1403',
            fontSize: 18,
            boxShadow: '0 2px 6px rgba(212, 175, 55, 0.3)'
          }}>
            <BankOutlined />
          </div>
          <div>
            <div style={{ fontSize: 16, fontWeight: 800, color: '#1C180E' }}>
              Trung Tâm Chỉ Huy Điều Hành
            </div>
            <div style={{ fontSize: 12, color: '#7E7359' }}>
              Xin chào, <b>{user?.fullName || user?.username}</b> · Quyền: {user?.roles.join(', ') || 'Nhân viên'}
            </div>
          </div>
        </div>

        <Space size={10}>
          <Tooltip title="Trạng thái dịch vụ đồng bộ nền Firebird ↔ MySQL">
            <div style={{
              display: 'flex',
              alignItems: 'center',
              padding: '6px 12px',
              borderRadius: 20,
              background: isHeartbeatOk ? '#ECFDF5' : '#FFF1F2',
              border: `1px solid ${isHeartbeatOk ? '#A7F3D0' : '#FECDD3'}`,
              fontSize: 12,
              fontWeight: 600,
              color: isHeartbeatOk ? '#065F46' : '#9F1239'
            }}>
              <span className={`sync-pulse-indicator ${isHeartbeatOk ? 'running' : 'error'}`} />
              Sync: {heartbeatState}
            </div>
          </Tooltip>
          <Button icon={<ReloadOutlined />} onClick={load} loading={loading} className="luxury-card">
            Tải lại
          </Button>
        </Space>
      </div>

      {err && <Alert type="error" showIcon message={err} style={{ borderRadius: 8 }} />}

      {/* 4 THẺ EXECUTIVE KPI CARDS MẠ VÀNG HOÀNG GIA */}
      {loading && !data ? (
        <Skeleton active paragraph={{ rows: 5 }} />
      ) : data && (
        <>
          <Row gutter={[16, 16]}>
            <Col xs={24} sm={12} lg={6}>
              <div className="luxury-kpi-card">
                <div className="kpi-label">
                  <DollarOutlined style={{ color: '#D4AF37' }} /> DOANH THU HÔM NAY
                </div>
                <div className="kpi-value text-gold">{fmtVnd(todayTotal)}</div>
                <div className="kpi-sub">
                  Doanh thu tháng: <b>{fmtVnd(monthTotal)}</b>
                </div>
              </div>
            </Col>

            <Col xs={24} sm={12} lg={6}>
              <div className="luxury-kpi-card">
                <div className="kpi-label">
                  <ShoppingCartOutlined style={{ color: '#D4AF37' }} /> CHỨNG TỪ HÔM NAY
                </div>
                <div className="kpi-value">{fmtQty(ordersToday)} <span style={{ fontSize: 16, fontWeight: 500, color: '#888' }}>phiếu</span></div>
                <div className="kpi-sub">
                  Đơn hàng & phiếu phát sinh trong ngày
                </div>
              </div>
            </Col>

            <Col xs={24} sm={12} lg={6}>
              <div className="luxury-kpi-card">
                <div className="kpi-label">
                  <StockOutlined style={{ color: '#D4AF37' }} /> TỒN KHO HOẠT ĐỘNG
                </div>
                <div className="kpi-value">{fmtQty(data.stock.SkuCount)} <span style={{ fontSize: 16, fontWeight: 500, color: '#888' }}>SKU</span></div>
                <div className="kpi-sub">
                  Tổng {fmtQty(data.counts.Products)} mặt hàng trong danh mục
                </div>
              </div>
            </Col>

            <Col xs={24} sm={12} lg={6}>
              <div className="luxury-kpi-card">
                <div className="kpi-label">
                  <BankOutlined style={{ color: '#D4AF37' }} /> TỒN QUỸ TIỀN MẶT
                </div>
                <div
                  className="kpi-value"
                  style={{ color: cashBalance < 0 ? '#E11D48' : '#10B981' }}
                >
                  {fmtVnd(cashBalance)}
                </div>
                <div className="kpi-sub">
                  Thu {fmtVnd(data.cash.Thu)} − Chi {fmtVnd(data.cash.Chi)}
                </div>
              </div>
            </Col>
          </Row>

          {/* QUICK ACTIONS DOCK (BÀN LÀM VIỆC NHANH) */}
          <Card
            className="luxury-card"
            title={<span style={{ color: '#8A6100', fontWeight: 700 }}>⚡ Bàn Làm Việc Nhanh (Lối Tắt Nghiệp Vụ)</span>}
          >
            <Row gutter={[12, 12]}>
              <Col xs={12} sm={8} md={4}>
                <Button
                  block
                  className="gold-gradient-btn"
                  style={{ height: 44, borderRadius: 8 }}
                  onClick={() => navigate('/pos/retail')}
                >
                  🛒 Bán lẻ
                </Button>
              </Col>
              <Col xs={12} sm={8} md={4}>
                <Button
                  block
                  style={{ height: 44, borderRadius: 8, borderColor: 'rgba(212, 175, 55, 0.4)', fontWeight: 600 }}
                  onClick={() => navigate('/inventory/receipts')}
                >
                  📦 Nhập kho
                </Button>
              </Col>
              <Col xs={12} sm={8} md={4}>
                <Button
                  block
                  style={{ height: 44, borderRadius: 8, borderColor: 'rgba(212, 175, 55, 0.4)', fontWeight: 600 }}
                  onClick={() => navigate('/inventory')}
                >
                  📋 Tồn kho
                </Button>
              </Col>
              <Col xs={12} sm={8} md={4}>
                <Button
                  block
                  style={{ height: 44, borderRadius: 8, borderColor: 'rgba(212, 175, 55, 0.4)', fontWeight: 600 }}
                  onClick={() => navigate('/cashbook')}
                >
                  💵 Thu / Chi Quỹ
                </Button>
              </Col>
              <Col xs={12} sm={8} md={4}>
                <Button
                  block
                  style={{ height: 44, borderRadius: 8, borderColor: 'rgba(212, 175, 55, 0.4)', fontWeight: 600 }}
                  onClick={() => navigate('/debt')}
                >
                  ⚖ Quản lý Công nợ
                </Button>
              </Col>
              <Col xs={12} sm={8} md={4}>
                <Button
                  block
                  style={{ height: 44, borderRadius: 8, borderColor: 'rgba(212, 175, 55, 0.4)', fontWeight: 600 }}
                  onClick={() => navigate('/reports')}
                >
                  📊 Báo cáo Doanh thu
                </Button>
              </Col>
            </Row>
          </Card>

          {/* BÁO CÁO TRỰC QUAN — BIỂU ĐỒ DOANH THU / THU CHI / TOP MẶT HÀNG */}
          <DashboardCharts />

          {/* DỮ LIỆU CHỨNG TỪ & DANH MỤC */}
          <Row gutter={[16, 16]}>
            <Col xs={24} lg={15}>
              <Card
                className="luxury-card"
                title={
                  <Space>
                    <ShoppingCartOutlined style={{ color: '#D4AF37' }} />
                    <span style={{ fontWeight: 700, color: '#1C180E' }}>Chứng Từ Phát Sinh Hôm Nay (TDONHANG)</span>
                  </Space>
                }
              >
                {data.todayDocs.length === 0 ? (
                  <Empty description="Chưa có chứng từ phát sinh hôm nay" />
                ) : (
                  <Table
                    className="luxury-table"
                    size="middle"
                    rowKey="LOAI"
                    pagination={false}
                    dataSource={data.todayDocs}
                    columns={todayColumns}
                  />
                )}
              </Card>
            </Col>

            <Col xs={24} lg={9}>
              <Space direction="vertical" size={16} style={{ width: '100%' }}>
                <Card
                  className="luxury-card"
                  title={
                    <Space>
                      <TeamOutlined style={{ color: '#D4AF37' }} />
                      <span style={{ fontWeight: 700, color: '#1C180E' }}>Danh Mục Hoạt Động</span>
                    </Space>
                  }
                >
                  <Row gutter={[12, 12]}>
                    <Col span={12}>
                      <div className="luxury-kpi-card" style={{ padding: '10px 14px' }}>
                        <div style={{ fontSize: 11, color: '#7E7359' }}>MẶT HÀNG</div>
                        <div style={{ fontSize: 20, fontWeight: 700, fontFamily: 'Outfit' }}>{fmtQty(data.counts.Products)}</div>
                      </div>
                    </Col>
                    <Col span={12}>
                      <div className="luxury-kpi-card" style={{ padding: '10px 14px' }}>
                        <div style={{ fontSize: 11, color: '#7E7359' }}>KHÁCH HÀNG</div>
                        <div style={{ fontSize: 20, fontWeight: 700, fontFamily: 'Outfit' }}>{fmtQty(data.counts.Customers)}</div>
                      </div>
                    </Col>
                    <Col span={12}>
                      <div className="luxury-kpi-card" style={{ padding: '10px 14px' }}>
                        <div style={{ fontSize: 11, color: '#7E7359' }}>NHÀ CUNG CẤP</div>
                        <div style={{ fontSize: 20, fontWeight: 700, fontFamily: 'Outfit' }}>{fmtQty(data.counts.Suppliers)}</div>
                      </div>
                    </Col>
                    <Col span={12}>
                      <div className="luxury-kpi-card" style={{ padding: '10px 14px' }}>
                        <div style={{ fontSize: 11, color: '#7E7359' }}>NHÂN VIÊN</div>
                        <div style={{ fontSize: 20, fontWeight: 700, fontFamily: 'Outfit' }}>{fmtQty(data.counts.Employees)}</div>
                      </div>
                    </Col>
                    <Col span={12}>
                      <div className="luxury-kpi-card" style={{ padding: '10px 14px' }}>
                        <div style={{ fontSize: 11, color: '#7E7359' }}>KHO HÀNG</div>
                        <div style={{ fontSize: 20, fontWeight: 700, fontFamily: 'Outfit' }}>{fmtQty(data.counts.Warehouses)}</div>
                      </div>
                    </Col>
                    <Col span={12}>
                      <div className="luxury-kpi-card" style={{ padding: '10px 14px' }}>
                        <div style={{ fontSize: 11, color: '#7E7359' }}>DOANH THU THÁNG</div>
                        <div style={{ fontSize: 16, fontWeight: 800, fontFamily: 'Outfit', color: '#10B981' }}>{fmtVnd(monthTotal)}</div>
                      </div>
                    </Col>
                  </Row>
                </Card>

                {sync && (
                  <Card
                    className="luxury-card"
                    title={
                      <Space>
                        <SyncOutlined style={{ color: '#D4AF37' }} />
                        <span style={{ fontWeight: 700, color: '#1C180E' }}>Chi Tiết Tiến Trình Đồng Bộ</span>
                      </Space>
                    }
                  >
                    <Row gutter={[12, 12]}>
                      <Col span={12}>
                        <div style={{ fontSize: 12, color: '#7E7359' }}>Lần chạy gần nhất</div>
                        <b style={{ fontSize: 13 }}>{sync.heartbeat?.LastRunAt?.slice(0, 19).replace('T', ' ') ?? '—'}</b>
                      </Col>
                      <Col span={12}>
                        <div style={{ fontSize: 12, color: '#7E7359' }}>Trạng thái tiến trình</div>
                        <Tag color={isHeartbeatOk ? 'green' : 'red'}>{sync.heartbeat?.State ?? '—'}</Tag>
                      </Col>
                      <Col span={12}>
                        <div style={{ fontSize: 12, color: '#7E7359' }}>Hàng đợi chờ xử lý</div>
                        <b style={{ color: '#F59E0B' }}>{fmtQty(sync.rowErrors?.Pending ?? 0)} dòng</b>
                      </Col>
                      <Col span={12}>
                        <div style={{ fontSize: 12, color: '#7E7359' }}>Lỗi Dead-Letter</div>
                        <b style={{ color: '#E11D48' }}>{fmtQty(sync.rowErrors?.DeadLetter ?? 0)} dòng</b>
                      </Col>
                    </Row>
                  </Card>
                )}
              </Space>
            </Col>
          </Row>
        </>
      )}
    </Space>
  );
};

export default DashboardPage;
