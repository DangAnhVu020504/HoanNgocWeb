import React, { useCallback, useEffect, useState } from 'react';
import { Button, Card, Form, Input, InputNumber, Modal, Space, Switch, Table, Tabs, message } from 'antd';
import { SendOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import {
  getEmailTemplates, updateEmailTemplate, getEmailEvents, setEmailEventEnabled,
  getEmailQueue, retryEmailQueueItem, cancelEmailQueueItem,
  getEmailSettings, saveEmailSettings, testSendEmail,
  type EmailTemplate, type EmailEvent, type EmailQueueItem, type EmailSettings,
} from '../../api/notification';

const STATUS_COLORS: Record<string, string> = {
  SENT: '#389e0d', FAILED: '#cf1322', PENDING: '#d46b08',
  PROCESSING: '#1d39c4', CANCELLED: '#8c8c8c',
};

const TemplatesTab: React.FC = () => {
  const [rows, setRows] = useState<EmailTemplate[]>([]);
  const [loading, setLoading] = useState(false);
  const [editing, setEditing] = useState<EmailTemplate | null>(null);
  const [saving, setSaving] = useState(false);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    setLoading(true);
    try { setRows(await getEmailTemplates()); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi tải template'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const openEdit = (t: EmailTemplate) => {
    setEditing(t);
    form.setFieldsValue({ subject: t.subject, bodyHtml: t.bodyHtml ?? '', isActive: t.isActive });
  };

  const save = async () => {
    if (!editing) return;
    const v = await form.validateFields();
    setSaving(true);
    try {
      await updateEmailTemplate(editing.code, {
        subject: v.subject, bodyHtml: v.bodyHtml || null, bodyText: null, isActive: v.isActive,
      });
      void message.success(`Đã lưu template ${editing.code}`);
      setEditing(null); void load();
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi lưu template');
    } finally { setSaving(false); }
  };

  return (
    <>
    <Card size="small" title="Mẫu email (W_EMAIL_TEMPLATE)">
      <Table size="small" rowKey="code" loading={loading} dataSource={rows}
        pagination={{ pageSize: 10, showSizeChanger: false }}
        columns={[
          { title: 'Code', dataIndex: 'code', width: 180 },
          { title: 'Tên', dataIndex: 'name' },
          { title: 'Subject', dataIndex: 'subject', ellipsis: true },
          { title: 'Kích hoạt', dataIndex: 'isActive', width: 100, align: 'center',
            render: (v: boolean) => v ? '✓' : '—' },
          { title: '', width: 90, render: (_: unknown, t: EmailTemplate) => (
            <Button type="link" size="small" style={{ padding: 0 }} onClick={() => openEdit(t)}>Sửa</Button>
          ) },
        ]} />
    </Card>
    <Modal title={editing ? `Sửa template ${editing.code}` : ''} open={!!editing}
      onCancel={() => setEditing(null)} onOk={save} confirmLoading={saving} width={720} destroyOnClose>
      <Form form={form} layout="vertical">
        <Form.Item name="subject" label="Subject" rules={[{ required: true }]}>
          <Input />
        </Form.Item>
        <Form.Item name="bodyHtml" label="Body HTML (biến {{CUSTOMER_NAME}}, {{ORDER_NUMBER}}, {{TOTAL_AMOUNT}}...)">
          <Input.TextArea rows={8} />
        </Form.Item>
        <Form.Item name="isActive" label="Kích hoạt" valuePropName="checked">
          <Switch />
        </Form.Item>
      </Form>
    </Modal>
    </>
  );
};

const EventsTab: React.FC = () => {
  const [rows, setRows] = useState<EmailEvent[]>([]);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try { setRows(await getEmailEvents()); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi tải events'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const toggle = async (code: string, enabled: boolean) => {
    try {
      await setEmailEventEnabled(code, enabled);
      setRows((prev) => prev.map((r) => r.code === code ? { ...r, enabled } : r));
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi đổi trạng thái event');
    }
  };

  return (
    <Card size="small" title="Sự kiện email (19 events — bật/tắt gửi)">
      <Table size="small" rowKey="code" loading={loading} dataSource={rows}
        pagination={{ pageSize: 10, showSizeChanger: false }}
        columns={[
          { title: 'Code', dataIndex: 'code', width: 200 },
          { title: 'Tên', dataIndex: 'name' },
          { title: 'Template', dataIndex: 'templateCode', width: 180, render: (v: string | null) => v ?? '—' },
          { title: 'Bật', dataIndex: 'enabled', width: 90,
            render: (v: boolean, r: EmailEvent) => <Switch size="small" checked={v} onChange={(e) => void toggle(r.code, e)} /> },
        ]} />
    </Card>
  );
};

const QueueTab: React.FC = () => {
  const [rows, setRows] = useState<EmailQueueItem[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<string | undefined>(undefined);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (p: number, s?: string) => {
    setLoading(true);
    try {
      const r = await getEmailQueue({ page: p, pageSize: 20, status: s });
      setRows(r.items); setTotal(r.total);
    } catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi tải queue'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(page, status); }, [load, page, status]);

  const doRetry = async (id: string) => {
    try { await retryEmailQueueItem(id); void message.success('Đã đưa vào hàng đợi lại'); void load(page, status); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi retry'); }
  };

  const doCancel = async (id: string) => {
    try { await cancelEmailQueueItem(id); void load(page, status); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi huỷ'); }
  };

  return (
    <Card size="small" title="Hàng đợi email" extra={
      <select value={status ?? ''} onChange={(e) => { setPage(1); setStatus(e.target.value || undefined); }}
        style={{ padding: '4px 8px', border: '1px solid #d9d9d9', borderRadius: 6 }}>
        <option value="">Tất cả trạng thái</option>
        {Object.keys(STATUS_COLORS).map((s) => <option key={s} value={s}>{s}</option>)}
      </select>
    }>
      <Table size="small" rowKey="id" loading={loading} dataSource={rows}
        pagination={{ current: page, pageSize: 20, total, showSizeChanger: false, onChange: (p) => setPage(p) }}
        columns={[
          { title: 'Event', dataIndex: 'eventType', width: 150 },
          { title: 'Subject', dataIndex: 'subject', ellipsis: true },
          { title: 'Người nhận', dataIndex: 'soNguoiNhan', width: 100, align: 'right' },
          { title: 'Trạng thái', dataIndex: 'status', width: 120,
            render: (v: string) => <b style={{ color: STATUS_COLORS[v] ?? '#434343' }}>{v}</b> },
          { title: 'Retry', dataIndex: 'retryCount', width: 70, align: 'right' },
          { title: 'Lỗi cuối', dataIndex: 'lastError', ellipsis: true,
            render: (v: string | null) => v ?? '—' },
          { title: 'Tạo lúc', dataIndex: 'createdAt', width: 130,
            render: (v: string) => dayjs(v).format('DD/MM HH:mm') },
          { title: 'Gửi lúc', dataIndex: 'sentAt', width: 130,
            render: (v: string | null) => v ? dayjs(v).format('DD/MM HH:mm') : '—' },
          { title: '', width: 130,
            render: (_: unknown, r: EmailQueueItem) => (
              <Space size={4}>
                {r.status === 'FAILED' && <Button type="link" size="small" style={{ padding: 0 }} onClick={() => void doRetry(r.id)}>Retry</Button>}
                {(r.status === 'PENDING' || r.status === 'FAILED') && <Button type="link" size="small" danger style={{ padding: 0 }} onClick={() => void doCancel(r.id)}>Huỷ</Button>}
              </Space>
            ) },
        ]} />
    </Card>
  );
};

const SettingsTab: React.FC = () => {
  const [form] = Form.useForm();
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);

  useEffect(() => {
    getEmailSettings().then((s: EmailSettings) => {
      form.setFieldsValue({
        host: s.HOST ?? '', port: Number(s.PORT ?? 587), useSsl: s.USE_SSL !== '0',
        username: s.USERNAME ?? '', password: '',
        fromEmail: s.FROM_EMAIL ?? '', fromName: s.FROM_NAME ?? '',
      });
    }).catch(() => {});
  }, [form]);

  const save = async () => {
    const v = await form.validateFields();
    setSaving(true);
    try {
      await saveEmailSettings({
        host: v.host, port: v.port, useSsl: v.useSsl,
        username: v.username || null, password: v.password || null,
        fromEmail: v.fromEmail, fromName: v.fromName || null,
      });
      void message.success('Đã lưu cấu hình SMTP');
    } catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi lưu cấu hình'); }
    finally { setSaving(false); }
  };

  const test = async () => {
    const v = await form.validateFields(['to', 'host', 'port', 'useSsl', 'username', 'password', 'fromEmail', 'fromName']);
    setTesting(true);
    try {
      await saveEmailSettings({
        host: v.host, port: v.port, useSsl: v.useSsl,
        username: v.username || null, password: v.password || null,
        fromEmail: v.fromEmail, fromName: v.fromName || null,
      });
      const r = await testSendEmail({
        to: v.to, name: null, subject: 'Hoàn Ngọc Group — Test email', bodyHtml: '<p>Test email từ Hoàn Ngọc Group.</p>',
      });
      if (r.success) void message.success('Đã gửi email test');
      else void message.error(r.message ?? 'Gửi thất bại');
    } catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi test email'); }
    finally { setTesting(false); }
  };

  return (
    <Card size="small" title="Cấu hình SMTP (W_EMAIL_SETTINGS — password lưu DB, ưu tiên env production)">
      <Form form={form} layout="vertical" style={{ maxWidth: 520 }}>
        <Form.Item name="host" label="SMTP Host" rules={[{ required: true }]}>
          <Input placeholder="smtp.gmail.com" />
        </Form.Item>
        <Space size={16}>
          <Form.Item name="port" label="Port" initialValue={587} rules={[{ required: true }]}>
            <InputNumber min={1} max={65535} style={{ width: 120 }} />
          </Form.Item>
          <Form.Item name="useSsl" label="SSL/TLS" valuePropName="checked" initialValue={true}>
            <Switch />
          </Form.Item>
        </Space>
        <Form.Item name="username" label="Username"><Input allowClear /></Form.Item>
        <Form.Item name="password" label="Password (bỏ trống nếu giữ nguyên)"><Input.Password autoComplete="new-password" /></Form.Item>
        <Form.Item name="fromEmail" label="Email gửi" rules={[{ required: true, type: 'email' }]}>
          <Input placeholder="no-reply@congty.vn" />
        </Form.Item>
        <Form.Item name="fromName" label="Tên gửi"><Input /></Form.Item>
        <Form.Item name="to" label="Email nhận test" rules={[{ required: true, type: 'email' }]}>
          <Input placeholder="admin@congty.vn" />
        </Form.Item>
        <Space>
          <Button type="primary" loading={saving} onClick={save} icon={<SendOutlined />}>Lưu cấu hình</Button>
          <Button loading={testing} onClick={test}>Gửi email test</Button>
        </Space>
      </Form>
    </Card>
  );
};

/** Trang Admin Email (Phase 7 — FRONTEND-STRUCTURE /emails). */
const EmailsPage: React.FC = () => (
  <Tabs items={[
    { key: 'queue', label: 'Hàng đợi', children: <QueueTab /> },
    { key: 'templates', label: 'Mẫu email', children: <TemplatesTab /> },
    { key: 'events', label: 'Sự kiện', children: <EventsTab /> },
    { key: 'settings', label: 'Cấu hình SMTP', children: <SettingsTab /> },
  ]} />
);

export default EmailsPage;
