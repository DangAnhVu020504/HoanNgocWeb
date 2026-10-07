import React, { useCallback, useEffect, useState } from 'react';
import { Button, Card, Descriptions, Form, Input, InputNumber, Modal, Select, Space, Table, Tabs, message } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import {
  getPayrolls, getPayroll, createPayroll, createSalaryAdvance, getEmployees,
  type Payroll, type PayrollSummary, type Employee,
} from '../../api/hr';
import { printPayrollPdf } from '../../api/reports';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

const PayrollList: React.FC<{ onOpenDetail: (id: string) => void }> = ({ onOpenDetail }) => {
  const [rows, setRows] = useState<PayrollSummary[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form] = Form.useForm();

  const load = useCallback(async (p: number) => {
    setLoading(true);
    try {
      const r = await getPayrolls({ page: p, pageSize: 20 });
      setRows(r.items); setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải bảng lương');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(1); }, [load]);

  const save = async () => {
    const v = await form.validateFields();
    setSaving(true);
    try {
      const created = await createPayroll({ thang: v.thang, nam: v.nam, ghiChu: v.ghiChu || null });
      void message.success(`Đã tạo bảng lương ${created.soPhieu}`);
      setOpen(false); form.resetFields();
      void load(page);
      onOpenDetail(created.id);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tạo bảng lương');
    } finally { setSaving(false); }
  };

  return (
    <Card size="small" title="Bảng lương theo tháng" extra={
      <Button type="primary" icon={<PlusOutlined />} onClick={() => setOpen(true)}>Tính lương tháng</Button>
    }>
      <Table size="small" rowKey="id" loading={loading} dataSource={rows}
        pagination={{ current: page, pageSize: 20, total, showSizeChanger: false, onChange: (p) => { setPage(p); void load(p); } }}
        columns={[
          { title: 'Số phiếu', dataIndex: 'soPhieu', width: 130,
            render: (v: string, r: PayrollSummary) => <Button type="link" size="small" style={{ padding: 0 }} onClick={() => onOpenDetail(r.id)}>{v}</Button> },
          { title: 'Kỳ lương', width: 130,
            render: (_: unknown, r: PayrollSummary) => `${String(r.thang).padStart(2, '0')}/${r.nam}` },
          { title: 'Ngày tạo', dataIndex: 'ngay', width: 120,
            render: (v: string) => v ? dayjs(v).format('DD/MM/YYYY') : '' },
          { title: 'Số nhân viên', dataIndex: 'soNhanVien', width: 120, align: 'right' },
          { title: 'Tổng thực nhận', dataIndex: 'tongThucNhan', width: 160, align: 'right',
            render: (v: number) => <b>{fmtVnd(v)}</b> },
          { title: 'Ghi chú', dataIndex: 'note', ellipsis: true },
        ]}
      />
      <Modal title="Tính lương tháng" open={open} onCancel={() => setOpen(false)} onOk={save}
        confirmLoading={saving} width={400} destroyOnClose>
        <Form form={form} layout="vertical">
          <Space size={10}>
            <Form.Item name="thang" label="Tháng" initialValue={dayjs().month() + 1} rules={[{ required: true }]}>
              <InputNumber min={1} max={12} style={{ width: 100 }} />
            </Form.Item>
            <Form.Item name="nam" label="Năm" initialValue={dayjs().year()} rules={[{ required: true }]}>
              <InputNumber min={2000} max={2100} style={{ width: 120 }} />
            </Form.Item>
          </Space>
          <Form.Item name="ghiChu" label="Ghi chú"><Input.TextArea rows={2} /></Form.Item>
        </Form>
        <div style={{ marginBottom: 12, color: '#888' }}>
          Tổng hợp từ chấm công (× tỉ lệ ca × lương ca) + thưởng/phạt + tạm ứng + phụ cấp của tháng.
          Mỗi tháng chỉ tạo 1 bảng lương.
        </div>
      </Modal>
    </Card>
  );
};

const AdvanceForm: React.FC = () => {
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [nhanVienId, setNhanVienId] = useState<string | undefined>();
  const [tien, setTien] = useState(0);
  const [ghiChu, setGhiChu] = useState('');
  const [saving, setSaving] = useState(false);
  const [last, setLast] = useState<string | null>(null);

  useEffect(() => { getEmployees({ pageSize: 200 }).then((r) => setEmployees(r.items)).catch(() => {}); }, []);

  const save = async () => {
    if (!nhanVienId) { void message.warning('Chưa chọn nhân viên'); return; }
    if (tien <= 0) { void message.warning('Số tiền tạm ứng phải > 0'); return; }
    setSaving(true);
    try {
      const r = await createSalaryAdvance({ nhanVienId, tien, ghiChu: ghiChu.trim() || null });
      setLast(`Phiếu chi tạm ứng ${r.soPhieu} — ${fmtVnd(r.tien)}`);
      void message.success(`Đã tạo phiếu tạm ứng ${r.soPhieu}`);
      setTien(0); setGhiChu('');
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tạo tạm ứng lương');
    } finally { setSaving(false); }
  };

  return (
    <Card size="small" title="Tạm ứng lương">
      <Space direction="vertical" style={{ width: '100%' }} size={10}>
        <Select showSearch optionFilterProp="label" placeholder="Nhân viên tạm ứng" style={{ width: '100%' }}
          value={nhanVienId ?? undefined} onChange={(v) => setNhanVienId(v)}
          options={employees.map((e) => ({ value: e.id, label: e.name }))} />
        <InputNumber min={0} step={100000} placeholder="Số tiền tạm ứng" style={{ width: '100%' }}
          value={tien || undefined} onChange={(v) => setTien(v ?? 0)}
          formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')} parser={(v) => Number((v ?? '').replace(/\./g, ''))} />
        <Input allowClear placeholder="Ghi chú (lý do tạm ứng...)" value={ghiChu} onChange={(e) => setGhiChu(e.target.value)} />
        <Button type="primary" loading={saving} onClick={save}>Tạo phiếu chi tạm ứng</Button>
        {last && <Descriptions column={1} size="small" bordered><Descriptions.Item label="Kết quả">{last}</Descriptions.Item></Descriptions>}
        <div style={{ color: '#888' }}>Ghi TTHUCHI (LOAI=1 chi, LATAMUNG=1) — tự trừ vào lương tháng của nhân viên.</div>
      </Space>
    </Card>
  );
};

const PayrollPage: React.FC = () => {
  const [detail, setDetail] = useState<Payroll | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);

  const openDetail = async (id: string) => {
    try {
      const p = await getPayroll(id);
      setDetail(p); setDetailOpen(true);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải bảng lương');
    }
  };

  return (
    <>
      <Tabs items={[
        { key: 'list', label: 'Bảng lương', children: <PayrollList onOpenDetail={(id) => void openDetail(id)} /> },
        { key: 'advance', label: 'Tạm ứng lương', children: <AdvanceForm /> },
      ]} />
      <Modal title={detail ? `Bảng lương ${detail.soPhieu} — ${String(detail.thang).padStart(2, '0')}/${detail.nam}` : ''}
        open={detailOpen} onCancel={() => setDetailOpen(false)} footer={null} width={980} destroyOnClose>
        {detail && (
          <>
          <Button size="small" style={{ marginBottom: 8 }} onClick={() => printPayrollPdf(detail.id)}>
            In bảng lương (PDF)
          </Button>
          <Table size="small" rowKey="id" dataSource={detail.lines} pagination={false}
            columns={[
              { title: 'Nhân viên', dataIndex: 'tenNhanVien' },
              { title: 'Số công', dataIndex: 'soCong', width: 80, align: 'right' },
              { title: 'Lương ca', dataIndex: 'luongCa', width: 110, align: 'right', render: (v: number) => fmtVnd(v) },
              { title: 'Lương tháng', dataIndex: 'luongThang', width: 120, align: 'right', render: (v: number) => fmtVnd(v) },
              { title: 'Thưởng', dataIndex: 'thuong', width: 110, align: 'right', render: (v: number) => fmtVnd(v) },
              { title: 'Phụ cấp', dataIndex: 'phuCap', width: 110, align: 'right', render: (v: number) => fmtVnd(v) },
              { title: 'Phạt', dataIndex: 'phat', width: 110, align: 'right',
                render: (v: number) => v > 0 ? <span style={{ color: '#cf1322' }}>{fmtVnd(v)}</span> : null },
              { title: 'Tạm ứng', dataIndex: 'tamUng', width: 110, align: 'right',
                render: (v: number) => v > 0 ? <span style={{ color: '#cf1322' }}>{fmtVnd(v)}</span> : null },
              { title: 'Thực nhận', dataIndex: 'thucNhan', width: 130, align: 'right',
                render: (v: number) => <b>{fmtVnd(v)}</b> },
            ]}
            summary={() => (
              <Table.Summary.Row>
                <Table.Summary.Cell index={0} colSpan={8}><b>TỔNG THỰC NHẬN</b></Table.Summary.Cell>
                <Table.Summary.Cell index={1} align="right"><b style={{ color: '#cf1322' }}>{fmtVnd(detail.tongThucNhan)}</b></Table.Summary.Cell>
              </Table.Summary.Row>
            )}
          />
          </>
        )}
      </Modal>
    </>
  );
};

export default PayrollPage;
