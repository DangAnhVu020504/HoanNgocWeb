import React, { useCallback, useEffect, useState } from 'react';
import { Button, Card, DatePicker, Form, Input, InputNumber, Modal, Select, Space, Table, Tag, message } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import {
  getBonusPenalties, createBonusPenalty, getBonusReasons, getEmployees,
  type BonusPenalty, type Employee,
} from '../../api/hr';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

const BonusPenaltyPage: React.FC = () => {
  const [rows, setRows] = useState<BonusPenalty[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [reasons, setReasons] = useState<{ id: string; name: string }[]>([]);
  const [nhanVienId, setNhanVienId] = useState<string | undefined>();
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form] = Form.useForm();

  const load = useCallback(async (p: number, nv?: string) => {
    setLoading(true);
    try {
      const r = await getBonusPenalties({ nhanVienId: nv, page: p, pageSize: 50 });
      setRows(r.items); setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải thưởng phạt');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => {
    void load(1);
    getEmployees({ pageSize: 200 }).then((r) => setEmployees(r.items)).catch(() => {});
    getBonusReasons().then(setReasons).catch(() => {});
  }, [load]);

  const save = async () => {
    const v = await form.validateFields();
    setSaving(true);
    try {
      await createBonusPenalty({
        nhanVienId: v.nhanVienId,
        thuong: v.loai === 'thuong' ? v.tien : 0,
        phat: v.loai === 'phat' ? v.tien : 0,
        lyDoId: v.lyDoId || null,
        ghiChu: v.ghiChu || null,
        ngay: (v.ngay ?? dayjs()).format('YYYY-MM-DD'),
      });
      void message.success('Đã ghi thưởng/phạt');
      setOpen(false); form.resetFields();
      void load(page, nhanVienId);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi lưu thưởng/phạt');
    } finally { setSaving(false); }
  };

  return (
    <Card size="small" title="Thưởng phạt nhân viên" extra={
      <Space wrap>
        <Select allowClear showSearch optionFilterProp="label" placeholder="Nhân viên" style={{ width: 200 }}
          value={nhanVienId} onChange={(v) => { setNhanVienId(v ?? undefined); void load(1, v ?? undefined); }}
          options={employees.map((e) => ({ value: e.id, label: e.name }))} />
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setOpen(true)}>Thêm thưởng/phạt</Button>
      </Space>
    }>
      <Table size="small" rowKey="id" loading={loading} dataSource={rows}
        pagination={{ current: page, pageSize: 50, total, showSizeChanger: false,
          onChange: (p) => { setPage(p); void load(p, nhanVienId); } }}
        columns={[
          { title: 'Ngày', dataIndex: 'ngay', width: 110,
            render: (v: string) => v ? dayjs(v).format('DD/MM/YYYY') : '' },
          { title: 'Tên phiếu', dataIndex: 'name' },
          { title: 'Nhân viên', dataIndex: 'tenNhanVien', width: 160 },
          { title: 'Lý do', dataIndex: 'lyDo', width: 150 },
          { title: 'Thưởng', dataIndex: 'thuong', width: 130, align: 'right',
            render: (v: number) => v > 0 ? <b style={{ color: '#389e0d' }}>{fmtVnd(v)}</b> : null },
          { title: 'Phạt', dataIndex: 'phat', width: 130, align: 'right',
            render: (v: number) => v > 0 ? <b style={{ color: '#cf1322' }}>{fmtVnd(v)}</b> : null },
          { title: 'Ghi chú', dataIndex: 'note', ellipsis: true },
        ]}
        summary={() => rows.length > 0 ? (
          <Table.Summary.Row>
            <Table.Summary.Cell index={0} colSpan={4}><b>TỔNG</b></Table.Summary.Cell>
            <Table.Summary.Cell index={1} align="right"><b style={{ color: '#389e0d' }}>{fmtVnd(rows.reduce((s, r) => s + r.thuong, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={2} align="right"><b style={{ color: '#cf1322' }}>{fmtVnd(rows.reduce((s, r) => s + r.phat, 0))}</b></Table.Summary.Cell>
            <Table.Summary.Cell index={3} />
          </Table.Summary.Row>
        ) : null}
      />
      <Modal title="Thưởng / phạt nhân viên" open={open} onCancel={() => setOpen(false)} onOk={save}
        confirmLoading={saving} width={460} destroyOnClose>
        <Form form={form} layout="vertical">
          <Form.Item name="nhanVienId" label="Nhân viên" rules={[{ required: true, message: 'Chọn nhân viên' }]}>
            <Select showSearch optionFilterProp="label" options={employees.map((e) => ({ value: e.id, label: e.name }))} />
          </Form.Item>
          <Form.Item name="loai" label="Loại" rules={[{ required: true }]} initialValue="thuong">
            <Select options={[{ value: 'thuong', label: 'Thưởng' }, { value: 'phat', label: 'Phạt' }]} />
          </Form.Item>
          <Form.Item name="tien" label="Số tiền" rules={[{ required: true, message: 'Nhập số tiền' }]}>
            <InputNumber min={0} step={50000} style={{ width: '100%' }}
              formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
              parser={(v) => (Number((v ?? '').replace(/[^\d]/g, '')) || 0) as 0} />
          </Form.Item>
          <Form.Item name="ngay" label="Ngày" initialValue={dayjs()}>
            <DatePicker style={{ width: '100%' }} />
          </Form.Item>
          <Form.Item name="lyDoId" label="Lý do">
            <Select allowClear placeholder="Chọn lý do thưởng/phạt"
              options={reasons.map((r) => ({ value: r.id, label: r.name }))} />
          </Form.Item>
          <Form.Item name="ghiChu" label="Ghi chú"><Input.TextArea rows={2} /></Form.Item>
        </Form>
        <div style={{ marginTop: -16, marginBottom: 12 }}>
          <Tag>Mỗi dòng ghi TTHUONGPHAT (W_ORIGIN=WEB), tính lương tự cộng vào bảng tháng</Tag>
        </div>
      </Modal>
    </Card>
  );
};

export default BonusPenaltyPage;
