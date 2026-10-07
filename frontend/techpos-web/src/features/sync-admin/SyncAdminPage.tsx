import React, { useEffect, useState } from 'react';
import {
  Alert, Badge, Button, Card, Col, Descriptions, Empty, Popconfirm, Row, Space, Statistic,
  Switch, Table, Tabs, Tag, Tooltip, message
} from 'antd';
import {
  CheckCircleTwoTone, ExclamationCircleTwoTone, PlayCircleOutlined, ReloadOutlined,
  SyncOutlined, UndoOutlined, WarningOutlined
} from '@ant-design/icons';
import {
  getRowErrors, getSyncStatus, getSyncTables, ignoreRowError, requeueRowError,
  toggleSyncTable, triggerExtractMedia, triggerReconcile, triggerRetryRowErrors,
  SyncLog, SyncRowError, SyncStatus, SyncTablePolicy
} from '../../api/sync';
import { useAuthStore } from '../../stores/authStore';

const fmtDt = (s: string | null | undefined) => s ? s.slice(0, 19).replace('T', ' ') : '—';

const SyncAdminPage: React.FC = () => {
  const user = useAuthStore(s => s.user);
  const isAdmin = !!user && (user.roles.includes('SUPER_ADMIN') || user.roles.includes('ADMIN') || user.roles.includes('IT'));
  const [data, setData] = useState<SyncStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string | null>(null);
  const [actionBusy, setActionBusy] = useState<string | null>(null);
  const [showDead, setShowDead] = useState(false);
  const [rowErrors, setRowErrors] = useState<SyncRowError[]>([]);

  const load = async () => {
    try {
      setErr(null);
      const [s, re] = await Promise.all([getSyncStatus(), getRowErrors(showDead, 50)]);
      setData(s);
      setRowErrors(re);
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'Không tải được trạng thái sync';
      setErr(msg);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, [showDead]);

  if (!isAdmin) {
    return (
      <Alert
        type="error" showIcon
        message="Truy cập bị từ chối"
        description="Trang Sync Admin chỉ dành cho SUPER_ADMIN / ADMIN / IT."
      />
    );
  }

  const runCmd = async (name: string, fn: () => Promise<unknown>) => {
    try {
      setActionBusy(name);
      await fn();
      void message.success(`Đã đưa lệnh ${name} vào hàng đợi`);
      setTimeout(load, 1500);
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'Lỗi';
      void message.error(`${name} thất bại: ${msg}`);
    } finally {
      setActionBusy(null);
    }
  };

  const cursorColumns = [
    { title: 'Nhóm', dataIndex: 'GroupName', key: 'g', width: 110,
      render: (v: string) => <Tag color={v === 'NGHIEP_VU' ? 'red' : v === 'DANH_MUC' ? 'blue' : 'default'}>{v}</Tag> },
    { title: 'Bảng', dataIndex: 'TableName', key: 't' },
    { title: 'Interval (s)', dataIndex: 'IntervalSec', key: 'i', width: 110, align: 'right' as const },
    { title: 'Last success', dataIndex: 'LastSuccessAt', key: 'ls', width: 170,
      render: (v: string | null) => fmtDt(v) },
    { title: 'Last count', dataIndex: 'LastCount', key: 'lc', width: 110, align: 'right' as const }
  ];

  const logColumns = [
    { title: 'Bảng', dataIndex: 'TableName', key: 't', width: 200 },
    { title: 'Run type', dataIndex: 'RunType', key: 'rt', width: 120,
      render: (v: string) => <Tag color={v === 'RECONCILE' ? 'purple' : v === 'MEDIA' ? 'cyan' : 'blue'}>{v}</Tag> },
    { title: 'Read', dataIndex: 'RowsRead', key: 'rr', width: 80, align: 'right' as const },
    { title: 'Upsert', dataIndex: 'RowsUpserted', key: 'ru', width: 80, align: 'right' as const },
    { title: 'Errors', dataIndex: 'Errors', key: 'e', width: 80, align: 'right' as const,
      render: (v: number) => v === 0 ? <span style={{ color: '#52c41a' }}>0</span> : <span style={{ color: '#cf1322' }}>{v}</span> },
    { title: 'Bắt đầu', dataIndex: 'StartedAt', key: 's', width: 170, render: fmtDt },
    { title: 'Message', dataIndex: 'Message', key: 'm', ellipsis: true }
  ];

  const commandColumns = [
    { title: 'ID', dataIndex: 'Id', key: 'id', width: 90, ellipsis: true },
    { title: 'Lệnh', dataIndex: 'Command', key: 'c', width: 180 },
    { title: 'Trạng thái', dataIndex: 'Status', key: 's', width: 110,
      render: (v: string) => {
        const color = v === 'DONE' ? 'green' : v === 'FAILED' ? 'red' : v === 'RUNNING' ? 'blue' : 'default';
        return <Tag color={color}>{v}</Tag>;
      } },
    { title: 'Tạo', dataIndex: 'CreatedAt', key: 'ca', width: 170, render: fmtDt },
    { title: 'Xong', dataIndex: 'FinishedAt', key: 'fa', width: 170, render: fmtDt },
    { title: 'Kết quả', dataIndex: 'Result', key: 'r', ellipsis: true }
  ];

  const rowErrorColumns = [
    { title: 'Bảng', dataIndex: 'TABLE_NAME', key: 't', width: 180 },
    { title: 'Row ID', dataIndex: 'ROW_ID', key: 'rid', width: 240, ellipsis: true },
    { title: 'Retry', dataIndex: 'RETRY_COUNT', key: 'rc', width: 80, align: 'right' as const },
    { title: 'Dead', dataIndex: 'DEAD_LETTER', key: 'dl', width: 70, align: 'center' as const,
      render: (v: number) => v ? <Tag color="red">YES</Tag> : <Tag color="blue">NO</Tag> },
    { title: 'Lỗi', dataIndex: 'ERROR', key: 'e', ellipsis: true,
      render: (v: string) => <Tooltip title={v}><span style={{ color: '#cf1322' }}>{v}</span></Tooltip> },
    { title: 'Tạo', dataIndex: 'CREATED_AT', key: 'ca', width: 170, render: fmtDt },
    { title: 'Hành động', key: 'act', width: 200,
      render: (_: unknown, r: { ID: string; DEAD_LETTER: number }) => (
        <Space size="small">
          <Popconfirm title="Đưa row này vào retry ngay?" onConfirm={async () => {
            await requeueRowError(r.ID);
            void message.success('Đã requeue');
            void load();
          }}>
            <Button size="small" icon={<PlayCircleOutlined />}>Retry</Button>
          </Popconfirm>
          {!r.DEAD_LETTER ? (
            <Popconfirm title="Bỏ qua row lỗi này (dead-letter)?" onConfirm={async () => {
              await ignoreRowError(r.ID);
              void message.success('Đã đánh dấu dead-letter');
              void load();
            }}>
              <Button size="small" danger icon={<WarningOutlined />}>Ignore</Button>
            </Popconfirm>
          ) : null}
        </Space>
      ) }
  ];

  const heartbeat = data?.heartbeat;
  const isHealthy = heartbeat?.State === 'RUNNING';

  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      {err && <Alert type="error" showIcon message={err} closable />}

      <Card
        title={<Space><SyncOutlined /> Sync Admin</Space>}
        extra={
          <Space>
            <Badge
              status={isHealthy ? 'success' : heartbeat?.State === 'ERROR' ? 'error' : 'processing'}
              text={heartbeat?.State ?? 'UNKNOWN'}
            />
            <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Tải lại</Button>
          </Space>
        }
      >
        <Row gutter={[12, 12]}>
          <Col xs={12} md={4}><Statistic title="Cursor" value={data?.cursors.length ?? 0} /></Col>
          <Col xs={12} md={4}><Statistic title="Logs" value={data?.recentLogs.length ?? 0} /></Col>
          <Col xs={12} md={4}><Statistic title="Row errors chờ" value={data?.rowErrors?.Pending ?? 0}
            valueStyle={{ color: (data?.rowErrors?.Pending ?? 0) > 0 ? '#cf1322' : undefined }} /></Col>
          <Col xs={12} md={4}><Statistic title="Dead-letter" value={data?.rowErrors?.DeadLetter ?? 0} /></Col>
          <Col xs={12} md={4}><Statistic title="Commands" value={data?.commands.length ?? 0} /></Col>
          <Col xs={12} md={4}><Statistic title="Last run" value={fmtDt(heartbeat?.LastRunAt)} /></Col>
        </Row>

        <Card type="inner" title="Lệnh nhanh" size="small" style={{ marginTop: 12 }}>
          <Space wrap>
            <Button
              type="primary" icon={<CheckCircleTwoTone twoToneColor="#52c41a" />}
              loading={actionBusy === 'RECONCILE'}
              onClick={() => runCmd('RECONCILE_ALL', () => triggerReconcile())}
            >
              Reconcile toàn bộ ngay
            </Button>
            <Button
              icon={<UndoOutlined />}
              loading={actionBusy === 'RETRY'}
              onClick={() => runCmd('RETRY_ROW_ERRORS', () => triggerRetryRowErrors())}
            >
              Retry row errors
            </Button>
            <Button
              icon={<ExclamationCircleTwoTone />}
              loading={actionBusy === 'MEDIA'}
              onClick={() => runCmd('MEDIA_EXTRACT', () => triggerExtractMedia())}
            >
              Trích BLOB → media-store
            </Button>
            <Button icon={<ReloadOutlined />} onClick={load}>Tải lại</Button>
          </Space>
        </Card>
      </Card>

      <Card size="small" title="Heartbeat chi tiết">
        {heartbeat ? (
          <Descriptions size="small" column={2} bordered>
            <Descriptions.Item label="State">{heartbeat.State}</Descriptions.Item>
            <Descriptions.Item label="Last run">{fmtDt(heartbeat.LastRunAt)}</Descriptions.Item>
            <Descriptions.Item label="Detail" span={2}>{heartbeat.Detail || '—'}</Descriptions.Item>
          </Descriptions>
        ) : <Empty description="Chưa có heartbeat" />}
      </Card>

      <Tabs
        defaultActiveKey="cursors"
        items={[
          {
            key: 'cursors', label: `Cursor (${data?.cursors.length ?? 0})`,
            children: (
              <Table
                size="small" rowKey="TableName" loading={loading} pagination={{ pageSize: 15 }}
                dataSource={data?.cursors ?? []} columns={cursorColumns}
              />
            )
          },
          {
            key: 'logs', label: `Logs gần đây (${data?.recentLogs.length ?? 0})`,
            children: (
              <Table
                size="small" rowKey={(r: SyncLog) => `${r.StartedAt}-${r.TableName}-${r.RunType}`}
                loading={loading} pagination={{ pageSize: 15 }}
                dataSource={data?.recentLogs ?? []} columns={logColumns}
              />
            )
          },
          {
            key: 'cmds', label: `Lệnh (${data?.commands.length ?? 0})`,
            children: (
              <Table
                size="small" rowKey="Id" loading={loading} pagination={{ pageSize: 10 }}
                dataSource={data?.commands ?? []} columns={commandColumns}
              />
            )
          },
          {
            key: 'errs', label: `Row errors (${rowErrors.length})`,
            children: (
              <Space direction="vertical" style={{ width: '100%' }}>
                <Space>
                  <Button size="small" type={showDead ? 'default' : 'primary'} onClick={() => setShowDead(false)}>Đang chờ</Button>
                  <Button size="small" type={showDead ? 'primary' : 'default'} onClick={() => setShowDead(true)}>Dead-letter</Button>
                </Space>
                <Table
                  size="small" rowKey="ID" loading={loading} pagination={{ pageSize: 10 }}
                  dataSource={rowErrors} columns={rowErrorColumns}
                />
              </Space>
            )
          },
          { key: 'tables', label: 'Cấu hình bảng', children: <TablesPolicyTab /> }
        ]}
      />
    </Space>
  );
};

/** Tab cấu hình bảng sync — toggle theo W_SYNC_TABLE_POLICY (việc #6 README). */
const TablesPolicyTab: React.FC = () => {
  const [rows, setRows] = useState<SyncTablePolicy[]>([]);
  const [loading, setLoading] = useState(false);

  const load = async () => {
    setLoading(true);
    try { setRows(await getSyncTables()); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi tải policy'); }
    finally { setLoading(false); }
  };

  useEffect(() => { void load(); }, []);

  const toggle = async (tableName: string, enabled: boolean) => {
    try {
      await toggleSyncTable(tableName, enabled);
      setRows((prev) => prev.map((r) => r.TableName === tableName ? { ...r, Enabled: enabled } : r));
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi đổi trạng thái bảng');
    }
  };

  return (
    <Table size="small" rowKey="TableName" loading={loading} dataSource={rows}
      pagination={{ pageSize: 20 }}
      columns={[
        { title: 'Nhóm', dataIndex: 'GroupName', width: 110,
          render: (v: string) => <Tag color={v === 'NGHIEP_VU' ? 'red' : v === 'DANH_MUC' ? 'blue' : 'default'}>{v}</Tag> },
        { title: 'Bảng', dataIndex: 'TableName' },
        { title: 'Interval (s)', dataIndex: 'IntervalSec', width: 100, align: 'right' as const },
        { title: 'Priority', dataIndex: 'Priority', width: 80, align: 'right' as const },
        { title: 'Batch', dataIndex: 'BatchSize', width: 80, align: 'right' as const },
        { title: 'Bật sync', dataIndex: 'Enabled', width: 90,
          render: (v: boolean | number, r: SyncTablePolicy) => (
            <Switch size="small" checked={!!v} onChange={(e) => void toggle(r.TableName, e)} />
          ) },
      ]} />
  );
};

export default SyncAdminPage;
