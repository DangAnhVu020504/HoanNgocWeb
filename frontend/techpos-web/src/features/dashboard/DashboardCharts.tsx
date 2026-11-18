import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Alert, Card, Col, Empty, Row, Segmented, Skeleton, Space } from 'antd';
import {
  Area, Bar, BarChart, CartesianGrid, Cell, ComposedChart, Legend, Line,
  Pie, PieChart, ResponsiveContainer, Tooltip as ChartTooltip, XAxis, YAxis,
} from 'recharts';
import { BarChartOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import {
  getCashByDay, getCashByReason, getSalesByDay, getSalesByItem,
  type CashByDayRowDto, type CashByReasonRowDto,
  type SalesByDayRowDto, type SalesByItemRowDto,
} from '../../api/reports';
import { useAuthStore } from '../../stores/authStore';

const GOLD = '#D4AF37';
const GOLD_DEEP = '#B8860B';
const GOLD_DARK = '#8A6100';
const GREEN = '#10B981';
const RED = '#E11D48';
const MUTED = '#7E7359';
const PIE_COLORS = ['#D4AF37', '#B8860B', '#8A6100', '#E5C158', '#C9A227'];

const fmtVnd = (n: number) => `${Math.round(n).toLocaleString('vi-VN')} ₫`;

const fmtShort = (n: number): string => {
  const abs = Math.abs(n);
  if (abs >= 1_000_000_000) return `${(n / 1_000_000_000).toFixed(abs >= 10_000_000_000 ? 0 : 1).replace('.', ',')} tỷ`;
  if (abs >= 1_000_000) return `${(n / 1_000_000).toFixed(abs >= 10_000_000 ? 0 : 1).replace('.', ',')} tr`;
  if (abs >= 1_000) return `${Math.round(n / 1_000)} k`;
  return String(Math.round(n));
};

const truncate = (s: string, n = 24) => (s.length > n ? `${s.slice(0, n - 1)}…` : s);

interface DailyPoint {
  date: string;
  label: string;
  doanhThu: number;
  conNo: number;
  soHoaDon: number;
  thu: number;
  chi: number;
}

interface PeriodItem {
  name: string;
  value: number;
  soLuong: number;
}

interface ReasonItem {
  name: string;
  thu: number;
  chi: number;
}

const chartTooltipProps = {
  contentStyle: {
    borderRadius: 10,
    border: '1px solid rgba(212, 175, 55, 0.35)',
    boxShadow: '0 6px 18px rgba(0, 0, 0, 0.08)',
    fontSize: 12,
  },
  labelStyle: { color: GOLD_DARK, fontWeight: 700 },
  itemStyle: { padding: 0 },
} as const;

const DashboardCharts: React.FC = () => {
  const canViewReports = useAuthStore(s => s.hasPermission('REPORT.VIEW'));
  const [range, setRange] = useState(90);
  const [sales, setSales] = useState<SalesByDayRowDto[]>([]);
  const [cash, setCash] = useState<CashByDayRowDto[]>([]);
  const [items, setItems] = useState<SalesByItemRowDto[]>([]);
  const [reasons, setReasons] = useState<CashByReasonRowDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setErr(null);
    const to = dayjs();
    const from = to.subtract(range - 1, 'day');
    const params = { from: from.format('YYYY-MM-DD'), to: to.format('YYYY-MM-DD') };
    try {
      const [s, c, i, r] = await Promise.all([
        getSalesByDay(params),
        getCashByDay(params),
        getSalesByItem({ ...params, top: 5 }),
        getCashByReason(params),
      ]);
      setSales(s);
      setCash(c);
      setItems(i);
      setReasons(r);
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Không tải được dữ liệu biểu đồ');
    } finally {
      setLoading(false);
    }
  }, [range]);

  useEffect(() => {
    if (canViewReports) void load();
  }, [canViewReports, load]);

  const daily = useMemo<DailyPoint[]>(() => {
    const to = dayjs();
    const from = to.subtract(range - 1, 'day');
    const salesMap = new Map(sales.map(r => [dayjs(r.ngay).format('YYYY-MM-DD'), r]));
    const cashMap = new Map(cash.map(r => [dayjs(r.ngay).format('YYYY-MM-DD'), r]));
    const out: DailyPoint[] = [];
    for (let i = 0; i < range; i++) {
      const d = from.add(i, 'day');
      const key = d.format('YYYY-MM-DD');
      const s = salesMap.get(key);
      const c = cashMap.get(key);
      out.push({
        date: key,
        label: d.format('DD/MM'),
        doanhThu: Number(s?.tongCong ?? 0),
        conNo: Number(s?.conNo ?? 0),
        soHoaDon: Number(s?.soHoaDon ?? 0),
        thu: Number(c?.thu ?? 0),
        chi: Number(c?.chi ?? 0),
      });
    }
    return out;
  }, [sales, cash, range]);

  const topItems = useMemo<PeriodItem[]>(
    () => items
      .map(i => ({ name: i.name, value: Number(i.thanhTien ?? 0), soLuong: Number(i.soLuong ?? 0) }))
      .filter(i => i.value > 0)
      .slice(0, 5),
    [items]
  );

  const topReasons = useMemo<ReasonItem[]>(
    () => reasons
      .map(r => ({ name: truncate(r.lyDo || '(chưa phân loại)'), thu: Number(r.thu ?? 0), chi: Number(r.chi ?? 0) }))
      .filter(r => r.thu > 0 || r.chi > 0)
      .sort((a, b) => (b.thu + b.chi) - (a.thu + a.chi))
      .slice(0, 5),
    [reasons]
  );

  const salesTotal = useMemo(() => daily.reduce((s, d) => s + d.doanhThu, 0), [daily]);
  const invoiceTotal = useMemo(() => daily.reduce((s, d) => s + d.soHoaDon, 0), [daily]);
  const cashThu = useMemo(() => daily.reduce((s, d) => s + d.thu, 0), [daily]);
  const cashChi = useMemo(() => daily.reduce((s, d) => s + d.chi, 0), [daily]);
  const topTotal = useMemo(() => topItems.reduce((s, i) => s + i.value, 0), [topItems]);

  if (!canViewReports) return null;

  const rangeBar = (
    <div
      className="luxury-card"
      style={{
        padding: '10px 18px',
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        flexWrap: 'wrap',
        gap: 10,
      }}
    >
      <Space size={8}>
        <BarChartOutlined style={{ color: GOLD }} />
        <span style={{ fontWeight: 700, color: '#1C180E' }}>Báo Cáo Trực Quan</span>
        <span style={{ fontSize: 12, color: MUTED }}>
          {dayjs().subtract(range - 1, 'day').format('DD/MM/YYYY')} – {dayjs().format('DD/MM/YYYY')}
        </span>
      </Space>
      <Segmented
        value={range}
        onChange={(v) => setRange(Number(v))}
        options={[
          { label: '30 ngày', value: 30 },
          { label: '90 ngày', value: 90 },
          { label: '1 năm', value: 365 },
        ]}
      />
    </div>
  );

  if (loading && sales.length === 0 && cash.length === 0 && items.length === 0) {
    return (
      <Space direction="vertical" size={16} style={{ width: '100%' }}>
        {rangeBar}
        <Card className="luxury-card"><Skeleton active paragraph={{ rows: 6 }} /></Card>
      </Space>
    );
  }

  const salesEmpty = daily.every(d => d.doanhThu === 0 && d.conNo === 0);
  const cashEmpty = daily.every(d => d.thu === 0 && d.chi === 0);

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {rangeBar}
      {err && <Alert type="error" showIcon message={err} style={{ borderRadius: 8 }} />}

      <Row gutter={[16, 16]}>
        <Col xs={24} lg={15}>
          <Card
            className="luxury-card"
            title={<span style={{ fontWeight: 700, color: '#1C180E' }}>Doanh Thu Theo Ngày</span>}
            extra={
              <span style={{ fontSize: 12, color: MUTED }}>
                Tổng: <b style={{ color: GOLD_DARK }}>{fmtVnd(salesTotal)}</b> · {invoiceTotal.toLocaleString('vi-VN')} hoá đơn
              </span>
            }
          >
            {salesEmpty ? (
              <Empty description="Chưa có doanh thu trong kỳ" style={{ padding: '40px 0' }} />
            ) : (
              <ResponsiveContainer width="100%" height={280}>
                <ComposedChart data={daily} margin={{ top: 8, right: 12, left: 4, bottom: 0 }}>
                  <defs>
                    <linearGradient id="goldFill" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="0%" stopColor={GOLD} stopOpacity={0.45} />
                      <stop offset="100%" stopColor={GOLD} stopOpacity={0.02} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(212, 175, 55, 0.18)" vertical={false} />
                  <XAxis dataKey="label" tick={{ fontSize: 11, fill: MUTED }} interval="preserveStartEnd" minTickGap={24} />
                  <YAxis tickFormatter={(v) => fmtShort(Number(v))} tick={{ fontSize: 11, fill: MUTED }} width={70} />
                  <ChartTooltip
                    {...chartTooltipProps}
                    formatter={(value, name) => [fmtVnd(Number(value)), name]}
                    labelFormatter={(label) => `Ngày ${label}`}
                  />
                  <Legend wrapperStyle={{ fontSize: 12 }} />
                  <Area
                    type="monotone" dataKey="doanhThu" name="Doanh thu"
                    stroke={GOLD_DEEP} strokeWidth={2.2} fill="url(#goldFill)"
                    dot={false} activeDot={{ r: 4 }}
                  />
                  <Line
                    type="monotone" dataKey="conNo" name="Công nợ"
                    stroke={RED} strokeWidth={1.6} dot={false} activeDot={{ r: 4 }}
                  />
                </ComposedChart>
              </ResponsiveContainer>
            )}
          </Card>
        </Col>

        <Col xs={24} lg={9}>
          <Card
            className="luxury-card"
            title={<span style={{ fontWeight: 700, color: '#1C180E' }}>Thu Chi Quỹ Theo Ngày</span>}
            extra={
              <span style={{ fontSize: 12, color: MUTED }}>
                Thu <b style={{ color: GREEN }}>{fmtVnd(cashThu)}</b> · Chi <b style={{ color: RED }}>{fmtVnd(cashChi)}</b>
              </span>
            }
          >
            {cashEmpty ? (
              <Empty description="Chưa có thu chi trong kỳ" style={{ padding: '40px 0' }} />
            ) : (
              <ResponsiveContainer width="100%" height={280}>
                <BarChart data={daily} margin={{ top: 8, right: 8, left: 4, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(212, 175, 55, 0.18)" vertical={false} />
                  <XAxis dataKey="label" tick={{ fontSize: 11, fill: MUTED }} interval="preserveStartEnd" minTickGap={24} />
                  <YAxis tickFormatter={(v) => fmtShort(Number(v))} tick={{ fontSize: 11, fill: MUTED }} width={60} />
                  <ChartTooltip
                    {...chartTooltipProps}
                    formatter={(value, name) => [fmtVnd(Number(value)), name]}
                    labelFormatter={(label) => `Ngày ${label}`}
                  />
                  <Legend wrapperStyle={{ fontSize: 12 }} />
                  <Bar dataKey="thu" name="Thu" fill={GREEN} radius={[3, 3, 0, 0]} maxBarSize={18} />
                  <Bar dataKey="chi" name="Chi" fill={RED} radius={[3, 3, 0, 0]} maxBarSize={18} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </Card>
        </Col>
      </Row>

      <Row gutter={[16, 16]}>
        <Col xs={24} lg={12}>
          <Card
            className="luxury-card"
            title={<span style={{ fontWeight: 700, color: '#1C180E' }}>Top 5 Mặt Hàng Bán Chạy</span>}
            extra={<span style={{ fontSize: 12, color: MUTED }}>Theo doanh thu trong kỳ</span>}
          >
            {topItems.length === 0 ? (
              <Empty description="Chưa có dữ liệu bán hàng trong kỳ" style={{ padding: '32px 0' }} />
            ) : (
              <div style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 8 }}>
                <div style={{ position: 'relative', width: 220, height: 220, flexShrink: 0 }}>
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={topItems} dataKey="value" nameKey="name"
                        innerRadius="58%" outerRadius="85%" paddingAngle={2}
                        stroke="#fff" strokeWidth={2}
                      >
                        {topItems.map((entry, idx) => (
                          <Cell key={entry.name} fill={PIE_COLORS[idx % PIE_COLORS.length]} />
                        ))}
                      </Pie>
                      <ChartTooltip
                        {...chartTooltipProps}
                        formatter={(value) => [fmtVnd(Number(value)), 'Doanh thu']}
                      />
                    </PieChart>
                  </ResponsiveContainer>
                  <div style={{
                    position: 'absolute', inset: 0, display: 'flex', flexDirection: 'column',
                    alignItems: 'center', justifyContent: 'center', pointerEvents: 'none',
                  }}>
                    <div style={{ fontSize: 11, color: MUTED, letterSpacing: 0.6 }}>TOP 5</div>
                    <div style={{ fontSize: 15, fontWeight: 800, color: GOLD_DARK }}>{fmtShort(topTotal)}</div>
                  </div>
                </div>
                <div style={{ flex: '1 1 240px', minWidth: 220 }}>
                  {topItems.map((it, i) => {
                    const pct = topTotal > 0 ? (it.value / topTotal) * 100 : 0;
                    return (
                      <div
                        key={it.name}
                        style={{
                          display: 'flex', gap: 8, alignItems: 'center', padding: '7px 0',
                          borderBottom: i < topItems.length - 1 ? '1px dashed rgba(212, 175, 55, 0.18)' : 'none',
                        }}
                      >
                        <span style={{
                          width: 10, height: 10, borderRadius: '50%', flexShrink: 0,
                          background: PIE_COLORS[i % PIE_COLORS.length],
                        }} />
                        <div style={{ flex: 1, minWidth: 0 }}>
                          <div
                            title={it.name}
                            style={{
                              fontSize: 12, fontWeight: 600, color: '#403825',
                              whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis',
                            }}
                          >
                            {it.name}
                          </div>
                          <div style={{ fontSize: 11, color: MUTED }}>
                            {fmtVnd(it.value)} · {pct.toFixed(1).replace('.', ',')}%
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            )}
          </Card>
        </Col>

        <Col xs={24} lg={12}>
          <Card
            className="luxury-card"
            title={<span style={{ fontWeight: 700, color: '#1C180E' }}>Top 5 Lý Do Thu / Chi</span>}
            extra={<span style={{ fontSize: 12, color: MUTED }}>Tổng hợp trong kỳ</span>}
          >
            {topReasons.length === 0 ? (
              <Empty description="Chưa có thu chi trong kỳ" style={{ padding: '32px 0' }} />
            ) : (
              <ResponsiveContainer width="100%" height={220}>
                <BarChart data={topReasons} layout="vertical" margin={{ top: 4, right: 16, left: 8, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(212, 175, 55, 0.18)" horizontal={false} />
                  <XAxis type="number" tickFormatter={(v) => fmtShort(Number(v))} tick={{ fontSize: 11, fill: MUTED }} />
                  <YAxis type="category" dataKey="name" width={140} tick={{ fontSize: 11, fill: '#403825' }} />
                  <ChartTooltip
                    {...chartTooltipProps}
                    formatter={(value, name) => [fmtVnd(Number(value)), name]}
                  />
                  <Legend wrapperStyle={{ fontSize: 12 }} />
                  <Bar dataKey="thu" name="Thu" fill={GREEN} radius={[0, 3, 3, 0]} maxBarSize={14} />
                  <Bar dataKey="chi" name="Chi" fill={RED} radius={[0, 3, 3, 0]} maxBarSize={14} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </Card>
        </Col>
      </Row>
    </Space>
  );
};

export default DashboardCharts;
