import React, { useCallback, useEffect, useState } from 'react';
import {
  Button, Card, DatePicker, Form, Input, InputNumber, Modal, Popconfirm, Rate, Select, Space,
  Table, Tabs, Tag, message,
} from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import dayjs, { type Dayjs } from 'dayjs';
import {
  adjustLoyalty, createCare, createFeedback, deleteCare, getCare, getFeedback, getLoyalty,
  getLoyaltyTxns, getMessages, respondFeedback, sendMessage, updateCare,
  type CustomerCare, type CustomerFeedback, type LoyaltySummary, type LoyaltyTxn, type Message,
} from '../../api/crm';
import { getCustomers } from '../../api/customers';
import { useAuthStore } from '../../stores/authStore';

const CARE_TYPES = [
  { value: 'CALL', label: 'Gọi điện' },
  { value: 'EMAIL', label: 'Email' },
  { value: 'SMS', label: 'SMS' },
  { value: 'ZALO', label: 'Zalo' },
  { value: 'VISIT', label: 'Gặp trực tiếp' },
  { value: 'OTHER', label: 'Khác' },
];

const careTypeLabel = (v: string) => CARE_TYPES.find((t) => t.value === v)?.label ?? v;

const FEEDBACK_STATUS: Record<string, { label: string; color: string }> = {
  NEW: { label: 'Mới', color: '#d46b08' },
  PROCESSING: { label: 'Đang xử lý', color: '#1d39c4' },
  RESOLVED: { label: 'Đã xử lý', color: '#389e0d' },
};

const MESSAGE_STATUS: Record<string, string> = {
  PENDING: '#d46b08', SENT: '#389e0d', FAILED: '#cf1322', NOT_CONFIGURED: '#8c8c8c',
};

const CustomerSelect: React.FC<{
  value?: string;
  onChange?: (v: string) => void;
  allowClear?: boolean;
}> = ({ value, onChange, allowClear }) => {
  const [options, setOptions] = useState<{ value: string; label: string }[]>([]);

  useEffect(() => {
    getCustomers({ activeOnly: true, pageSize: 200 })
      .then((r) => setOptions(r.items.map((c) => ({
        value: c.id,
        label: `${c.name}${c.dienThoai ? ` — ${c.dienThoai}` : ''}`,
      }))))
      .catch(() => undefined);
  }, []);

  return (
    <Select
      showSearch
      allowClear={allowClear}
      optionFilterProp="label"
      value={value}
      onChange={onChange}
      options={options}
      placeholder="Chọn khách hàng"
    />
  );
};

// ---------- Tab chăm sóc ----------

const CareTab: React.FC = () => {
  const canCreate = useAuthStore((s) => s.hasPermission('CRM.CARE.CREATE'));
  const canUpdate = useAuthStore((s) => s.hasPermission('CRM.CARE.UPDATE'));
  const canDelete = useAuthStore((s) => s.hasPermission('CRM.CARE.DELETE'));
  const [rows, setRows] = useState<CustomerCare[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [careType, setCareType] = useState<string | undefined>();
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<CustomerCare | null>(null);
  const [saving, setSaving] = useState(false);
  const [form] = Form.useForm();

  const load = useCallback(async (p: number) => {
    setLoading(true);
    try {
      const r = await getCare({ page: p, pageSize: 20, search: search || undefined, careType });
      setRows(r.items);
      setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải phiếu chăm sóc');
    } finally {
      setLoading(false);
    }
  }, [search, careType]);

  useEffect(() => { void load(page); }, [load, page]);

  const openCreate = () => {
    setEditing(null);
    form.resetFields();
    form.setFieldsValue({ careType: 'CALL' });
    setOpen(true);
  };

  const openEdit = (row: CustomerCare) => {
    setEditing(row);
    form.setFieldsValue({
      customerId: row.customerId,
      careType: row.careType,
      content: row.content,
      result: row.result,
      nextCareAt: row.nextCareAt ? dayjs(row.nextCareAt) : null,
    });
    setOpen(true);
  };

  const save = async () => {
    const v = await form.validateFields();
    const nextCareAt = v.nextCareAt ? (v.nextCareAt as Dayjs).toISOString() : null;
    setSaving(true);
    try {
      if (editing) {
        await updateCare(editing.id, {
          careType: v.careType,
          content: v.content || null,
          result: v.result || null,
          nextCareAt,
        });
      } else {
        await createCare({
          customerId: v.customerId,
          careType: v.careType,
          content: v.content || null,
          result: v.result || null,
          nextCareAt,
        });
      }
      void message.success('Đã lưu phiếu chăm sóc');
      setOpen(false);
      void load(page);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi lưu phiếu chăm sóc');
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: string) => {
    try {
      await deleteCare(id);
      void message.success('Đã xoá phiếu chăm sóc');
      void load(page);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi xoá phiếu');
    }
  };

  return (
    <Card size="small" className="luxury-card" title={<span className="card-title-gold">Lịch sử chăm sóc khách hàng</span>}
      extra={
        <Space wrap>
          <Input.Search allowClear placeholder="Tìm khách / nội dung" style={{ width: 220 }}
            onSearch={(v) => { setPage(1); setSearch(v.trim()); }} />
          <Select allowClear placeholder="Hình thức" style={{ width: 150 }} options={CARE_TYPES}
            value={careType} onChange={(v) => { setPage(1); setCareType(v); }} />
          {canCreate && <Button type="primary" className="gold-gradient-btn" icon={<PlusOutlined />} onClick={openCreate}>Thêm</Button>}
        </Space>
      }>
      <Table className="luxury-table" size="middle" rowKey="id" loading={loading} dataSource={rows}
        pagination={{ current: page, pageSize: 20, total, showSizeChanger: false, onChange: setPage }}
        columns={[
          { title: 'Ngày', dataIndex: 'createdAt', width: 140, render: (v: string) => dayjs(v).format('DD/MM/YYYY HH:mm') },
          { title: 'Khách hàng', dataIndex: 'tenKhach', render: (v: string | null, r: CustomerCare) => v ?? r.customerId },
          { title: 'Hình thức', dataIndex: 'careType', width: 120, render: (v: string) => careTypeLabel(v) },
          { title: 'Nội dung', dataIndex: 'content', ellipsis: true, render: (v: string | null) => v ?? '—' },
          { title: 'Kết quả', dataIndex: 'result', ellipsis: true, render: (v: string | null) => v ?? '—' },
          { title: 'Chăm sóc tiếp', dataIndex: 'nextCareAt', width: 130,
            render: (v: string | null) => v ? dayjs(v).format('DD/MM/YYYY') : '—' },
          { title: 'Phụ trách', dataIndex: 'assigneeName', width: 130, render: (v: string | null) => v ?? '—' },
          {
            title: '', width: 110, render: (_: unknown, r: CustomerCare) => (
              <Space size={4}>
                {canUpdate && <Button type="link" size="small" style={{ padding: 0 }} onClick={() => openEdit(r)}>Sửa</Button>}
                {canDelete && (
                  <Popconfirm title="Xoá phiếu chăm sóc?" onConfirm={() => void remove(r.id)}>
                    <Button type="link" size="small" danger style={{ padding: 0 }}>Xoá</Button>
                  </Popconfirm>
                )}
              </Space>
            ),
          },
        ]} />

      <Modal title={editing ? 'Sửa phiếu chăm sóc' : 'Thêm phiếu chăm sóc'} open={open}
        onCancel={() => setOpen(false)} onOk={() => void save()} confirmLoading={saving} destroyOnClose>
        <Form form={form} layout="vertical">
          <Form.Item name="customerId" label="Khách hàng" rules={[{ required: true, message: 'Chọn khách hàng' }]}>
            <CustomerSelect />
          </Form.Item>
          <Form.Item name="careType" label="Hình thức" rules={[{ required: true }]}>
            <Select options={CARE_TYPES} />
          </Form.Item>
          <Form.Item name="content" label="Nội dung"><Input.TextArea rows={3} /></Form.Item>
          <Form.Item name="result" label="Kết quả"><Input.TextArea rows={2} /></Form.Item>
          <Form.Item name="nextCareAt" label="Ngày chăm sóc tiếp">
            <DatePicker showTime style={{ width: '100%' }} format="DD/MM/YYYY HH:mm" />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
};

// ---------- Tab phản hồi ----------

const FeedbackTab: React.FC = () => {
  const canCreate = useAuthStore((s) => s.hasPermission('CRM.FEEDBACK.CREATE'));
  const canRespond = useAuthStore((s) => s.hasPermission('CRM.FEEDBACK.RESPOND'));
  const [rows, setRows] = useState<CustomerFeedback[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [status, setStatus] = useState<string | undefined>();
  const [loading, setLoading] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [responding, setResponding] = useState<CustomerFeedback | null>(null);
  const [saving, setSaving] = useState(false);
  const [form] = Form.useForm();
  const [respondForm] = Form.useForm();

  const load = useCallback(async (p: number) => {
    setLoading(true);
    try {
      const r = await getFeedback({ page: p, pageSize: 20, status });
      setRows(r.items);
      setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải phản hồi');
    } finally {
      setLoading(false);
    }
  }, [status]);

  useEffect(() => { void load(page); }, [load, page]);

  const save = async () => {
    const v = await form.validateFields();
    setSaving(true);
    try {
      await createFeedback({
        customerId: v.customerId || null,
        invoiceId: v.invoiceId || null,
        rating: v.rating ?? 5,
        content: v.content || null,
      });
      void message.success('Đã ghi nhận phản hồi');
      setCreateOpen(false);
      void load(page);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi lưu phản hồi');
    } finally {
      setSaving(false);
    }
  };

  const respond = async () => {
    if (!responding) return;
    const v = await respondForm.validateFields();
    setSaving(true);
    try {
      await respondFeedback(responding.id, v.response, v.status);
      void message.success('Đã phản hồi khách hàng');
      setResponding(null);
      void load(page);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi phản hồi');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Card size="small" className="luxury-card" title={<span className="card-title-gold">Phản hồi / đánh giá khách hàng</span>}
      extra={
        <Space wrap>
          <Select allowClear placeholder="Trạng thái" style={{ width: 160 }}
            options={Object.entries(FEEDBACK_STATUS).map(([value, s]) => ({ value, label: s.label }))}
            value={status} onChange={(v) => { setPage(1); setStatus(v); }} />
          {canCreate && (
            <Button type="primary" className="gold-gradient-btn" icon={<PlusOutlined />}
              onClick={() => { form.resetFields(); form.setFieldsValue({ rating: 5 }); setCreateOpen(true); }}>
              Ghi nhận
            </Button>
          )}
        </Space>
      }>
      <Table className="luxury-table" size="middle" rowKey="id" loading={loading} dataSource={rows}
        pagination={{ current: page, pageSize: 20, total, showSizeChanger: false, onChange: setPage }}
        columns={[
          { title: 'Ngày', dataIndex: 'createdAt', width: 140, render: (v: string) => dayjs(v).format('DD/MM/YYYY HH:mm') },
          { title: 'Khách hàng', dataIndex: 'tenKhach', render: (v: string | null) => v ?? '—' },
          { title: 'Hoá đơn', dataIndex: 'soHoaDon', width: 130, render: (v: string | null) => v ?? '—' },
          { title: 'Đánh giá', dataIndex: 'rating', width: 130, render: (v: number) => <Rate disabled value={v} style={{ fontSize: 13 }} /> },
          { title: 'Nội dung', dataIndex: 'content', ellipsis: true, render: (v: string | null) => v ?? '—' },
          {
            title: 'Trạng thái', dataIndex: 'status', width: 120,
            render: (v: string) => {
              const s = FEEDBACK_STATUS[v] ?? { label: v, color: '#434343' };
              return <Tag color={s.color}>{s.label}</Tag>;
            },
          },
          {
            title: '', width: 90, render: (_: unknown, r: CustomerFeedback) => canRespond ? (
              <Button type="link" size="small" style={{ padding: 0 }}
                onClick={() => { setResponding(r); respondForm.setFieldsValue({ response: r.response ?? '', status: r.status }); }}>
                Phản hồi
              </Button>
            ) : null,
          },
        ]} />

      <Modal title="Ghi nhận phản hồi" open={createOpen} onCancel={() => setCreateOpen(false)}
        onOk={() => void save()} confirmLoading={saving} destroyOnClose>
        <Form form={form} layout="vertical">
          <Form.Item name="customerId" label="Khách hàng (hoặc nhập mã hoá đơn)"><CustomerSelect allowClear /></Form.Item>
          <Form.Item name="invoiceId" label="Mã hoá đơn"><Input allowClear /></Form.Item>
          <Form.Item name="rating" label="Đánh giá" rules={[{ required: true }]}><Rate /></Form.Item>
          <Form.Item name="content" label="Nội dung"><Input.TextArea rows={3} /></Form.Item>
        </Form>
      </Modal>

      <Modal title="Phản hồi khách hàng" open={!!responding} onCancel={() => setResponding(null)}
        onOk={() => void respond()} confirmLoading={saving} destroyOnClose>
        <Form form={respondForm} layout="vertical">
          <Form.Item name="response" label="Nội dung phản hồi" rules={[{ required: true }]}>
            <Input.TextArea rows={4} />
          </Form.Item>
          <Form.Item name="status" label="Trạng thái">
            <Select options={Object.entries(FEEDBACK_STATUS).map(([value, s]) => ({ value, label: s.label }))} />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
};

// ---------- Tab tin nhắn ----------

const MessagesTab: React.FC = () => {
  const canSend = useAuthStore((s) => s.hasPermission('CRM.MESSAGE.SEND'));
  const [rows, setRows] = useState<Message[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [channel, setChannel] = useState<string | undefined>();
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [sending, setSending] = useState(false);
  const [form] = Form.useForm();

  const load = useCallback(async (p: number) => {
    setLoading(true);
    try {
      const r = await getMessages({ page: p, pageSize: 20, channel });
      setRows(r.items);
      setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải tin nhắn');
    } finally {
      setLoading(false);
    }
  }, [channel]);

  useEffect(() => { void load(page); }, [load, page]);

  const send = async () => {
    const v = await form.validateFields();
    setSending(true);
    try {
      await sendMessage({
        channel: v.channel,
        customerId: v.customerId || null,
        recipient: v.recipient || null,
        subject: v.subject || null,
        body: v.body,
      });
      void message.success('Đã đưa tin nhắn vào hàng đợi');
      setOpen(false);
      void load(page);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi gửi tin nhắn');
    } finally {
      setSending(false);
    }
  };

  return (
    <Card size="small" className="luxury-card" title={<span className="card-title-gold">Tin nhắn Email / SMS / Zalo</span>}
      extra={
        <Space wrap>
          <Select allowClear placeholder="Kênh" style={{ width: 130 }}
            options={[{ value: 'EMAIL', label: 'Email' }, { value: 'SMS', label: 'SMS' }, { value: 'ZALO', label: 'Zalo' }]}
            value={channel} onChange={(v) => { setPage(1); setChannel(v); }} />
          {canSend && (
            <Button type="primary" className="gold-gradient-btn" icon={<PlusOutlined />}
              onClick={() => { form.resetFields(); form.setFieldsValue({ channel: 'EMAIL' }); setOpen(true); }}>
              Gửi tin
            </Button>
          )}
        </Space>
      }>
      <Table className="luxury-table" size="middle" rowKey="id" loading={loading} dataSource={rows}
        pagination={{ current: page, pageSize: 20, total, showSizeChanger: false, onChange: setPage }}
        columns={[
          { title: 'Ngày', dataIndex: 'createdAt', width: 140, render: (v: string) => dayjs(v).format('DD/MM/YYYY HH:mm') },
          { title: 'Kênh', dataIndex: 'channel', width: 90 },
          { title: 'Người nhận', dataIndex: 'recipient', width: 200, ellipsis: true },
          { title: 'Khách hàng', dataIndex: 'tenKhach', width: 160, render: (v: string | null) => v ?? '—' },
          { title: 'Tiêu đề', dataIndex: 'subject', ellipsis: true, render: (v: string | null) => v ?? '—' },
          {
            title: 'Trạng thái', dataIndex: 'status', width: 130,
            render: (v: string) => <b style={{ color: MESSAGE_STATUS[v] ?? '#434343' }}>{v}</b>,
          },
          { title: 'Lỗi', dataIndex: 'error', ellipsis: true, render: (v: string | null) => v ?? '—' },
        ]} />

      <Modal title="Gửi tin nhắn" open={open} onCancel={() => setOpen(false)}
        onOk={() => void send()} confirmLoading={sending} destroyOnClose>
        <Form form={form} layout="vertical">
          <Form.Item name="channel" label="Kênh gửi" rules={[{ required: true }]}>
            <Select options={[{ value: 'EMAIL', label: 'Email' }, { value: 'SMS', label: 'SMS' }, { value: 'ZALO', label: 'Zalo' }]} />
          </Form.Item>
          <Form.Item name="customerId" label="Khách hàng (tự lấy email / SĐT)"><CustomerSelect allowClear /></Form.Item>
          <Form.Item name="recipient" label="Người nhận (nếu khách chưa có)"><Input allowClear /></Form.Item>
          <Form.Item name="subject" label="Tiêu đề (email)"><Input allowClear /></Form.Item>
          <Form.Item name="body" label="Nội dung" rules={[{ required: true }]}>
            <Input.TextArea rows={5} />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
};

// ---------- Tab điểm thân thiết ----------

const LoyaltyTab: React.FC = () => {
  const canAdjust = useAuthStore((s) => s.hasPermission('CRM.LOYALTY.ADJUST'));
  const [rows, setRows] = useState<LoyaltySummary[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);
  const [adjustOpen, setAdjustOpen] = useState(false);
  const [txnOpen, setTxnOpen] = useState(false);
  const [txns, setTxns] = useState<LoyaltyTxn[]>([]);
  const [current, setCurrent] = useState<LoyaltySummary | null>(null);
  const [saving, setSaving] = useState(false);
  const [form] = Form.useForm();

  const load = useCallback(async (p: number) => {
    setLoading(true);
    try {
      const r = await getLoyalty({ page: p, pageSize: 20, search: search || undefined });
      setRows(r.items);
      setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải điểm thân thiết');
    } finally {
      setLoading(false);
    }
  }, [search]);

  useEffect(() => { void load(page); }, [load, page]);

  const openAdjust = (row?: LoyaltySummary) => {
    setCurrent(row ?? null);
    form.resetFields();
    form.setFieldsValue({ customerId: row?.customerId, points: 0 });
    setAdjustOpen(true);
  };

  const adjust = async () => {
    const v = await form.validateFields();
    setSaving(true);
    try {
      await adjustLoyalty(v.customerId, v.points, v.note || null);
      void message.success('Đã điều chỉnh điểm');
      setAdjustOpen(false);
      void load(page);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi điều chỉnh điểm');
    } finally {
      setSaving(false);
    }
  };

  const openTxns = async (row: LoyaltySummary) => {
    setCurrent(row);
    setTxnOpen(true);
    try {
      setTxns(await getLoyaltyTxns(row.customerId));
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải lịch sử điểm');
      setTxns([]);
    }
  };

  return (
    <Card size="small" className="luxury-card" title={<span className="card-title-gold">Điểm thân thiết</span>}
      extra={
        <Space wrap>
          <Input.Search allowClear placeholder="Tìm khách hàng" style={{ width: 220 }}
            onSearch={(v) => { setPage(1); setSearch(v.trim()); }} />
          {canAdjust && (
            <Button type="primary" className="gold-gradient-btn" icon={<PlusOutlined />}
              onClick={() => openAdjust()}>
              Điều chỉnh
            </Button>
          )}
        </Space>
      }>
      <Table className="luxury-table" size="middle" rowKey="customerId" loading={loading} dataSource={rows}
        pagination={{ current: page, pageSize: 20, total, showSizeChanger: false, onChange: setPage }}
        columns={[
          { title: 'Khách hàng', dataIndex: 'tenKhach', render: (v: string | null) => v ?? '—' },
          { title: 'Điện thoại', dataIndex: 'dienThoai', width: 150, render: (v: string | null) => v ?? '—' },
          { title: 'Điểm hiện tại', dataIndex: 'points', width: 120, align: 'right' },
          { title: 'Tổng tích', dataIndex: 'totalEarned', width: 110, align: 'right' },
          { title: 'Tổng dùng', dataIndex: 'totalRedeemed', width: 110, align: 'right' },
          { title: 'Lần cuối', dataIndex: 'lastTxnAt', width: 140, render: (v: string | null) => v ? dayjs(v).format('DD/MM/YYYY HH:mm') : '—' },
          {
            title: '', width: 140, render: (_: unknown, r: LoyaltySummary) => (
              <Space size={4}>
                <Button type="link" size="small" style={{ padding: 0 }} onClick={() => void openTxns(r)}>Lịch sử</Button>
                {canAdjust && <Button type="link" size="small" style={{ padding: 0 }} onClick={() => openAdjust(r)}>Điều chỉnh</Button>}
              </Space>
            ),
          },
        ]} />

      <Modal title="Điều chỉnh điểm" open={adjustOpen} onCancel={() => setAdjustOpen(false)}
        onOk={() => void adjust()} confirmLoading={saving} destroyOnClose>
        <Form form={form} layout="vertical">
          <Form.Item name="customerId" label="Khách hàng" rules={[{ required: true }]}>
            <CustomerSelect />
          </Form.Item>
          <Form.Item name="points" label="Số điểm (dương = cộng, âm = trừ)" rules={[{ required: true }]}>
            <InputNumber style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="note" label="Ghi chú"><Input.TextArea rows={2} /></Form.Item>
        </Form>
      </Modal>

      <Modal title={`Lịch sử điểm — ${current?.tenKhach ?? ''}`} open={txnOpen}
        onCancel={() => setTxnOpen(false)} footer={null} width={720} destroyOnClose>
        <Table size="small" rowKey="id" dataSource={txns} pagination={{ pageSize: 10 }}
          columns={[
            { title: 'Ngày', dataIndex: 'createdAt', width: 150, render: (v: string) => dayjs(v).format('DD/MM/YYYY HH:mm') },
            { title: 'Loại', dataIndex: 'txnType', width: 90 },
            { title: 'Điểm', dataIndex: 'points', width: 90, align: 'right' },
            { title: 'Ghi chú', dataIndex: 'note', render: (v: string | null) => v ?? '—' },
          ]} />
      </Modal>
    </Card>
  );
};

/** Phase 8 — CSKH: chăm sóc, phản hồi, tin nhắn, điểm thân thiết. */
const CrmPage: React.FC = () => (
  <Tabs className="luxury-tabs" items={[
    { key: 'care', label: 'Chăm sóc', children: <CareTab /> },
    { key: 'feedback', label: 'Phản hồi', children: <FeedbackTab /> },
    { key: 'messages', label: 'Tin nhắn', children: <MessagesTab /> },
    { key: 'loyalty', label: 'Điểm thân thiết', children: <LoyaltyTab /> },
  ]} />
);

export default CrmPage;
