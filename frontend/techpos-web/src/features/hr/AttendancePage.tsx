import React, { useCallback, useEffect, useState } from 'react';
import { Button, Card, DatePicker, Form, Modal, Select, Space, Table, Tag, message } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import {
  getAttendance, createAttendance, getShifts, getEmployees,
  type Attendance, type Shift, type Employee,
} from '../../api/hr';

const fmtPct = (n: number) => `${n.toLocaleString('vi-VN')}%`;

const AttendancePage: React.FC = () => {
  const [rows, setRows] = useState<Attendance[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [shifts, setShifts] = useState<Shift[]>([]);
  const [nhanVienId, setNhanVienId] = useState<string | undefined>();
  const [khoangNgay, setKhoangNgay] = useState<[dayjs.Dayjs | null, dayjs.Dayjs | null] | null>(null);
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [form] = Form.useForm();

  const load = useCallback(async (p: number, nv?: string, from?: string, to?: string) => {
    setLoading(true);
    try {
      const r = await getAttendance({ nhanVienId: nv, from, to, page: p, pageSize: 50 });
      setRows(r.items); setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải chấm công');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => {
    void load(1);
    getShifts().then(setShifts).catch(() => {});
    getEmployees({ pageSize: 200 }).then((r) => setEmployees(r.items)).catch(() => {});
  }, [load]);

  const save = async () => {
    const v = await form.validateFields();
    setSaving(true);
    try {
      await createAttendance({
        nhanVienId: v.nhanVienId,
        ngay: v.ngay.format('YYYY-MM-DD'),
        caLamViecId: v.caLamViecId,
      });
      void message.success('Đã ghi chấm công');
      setOpen(false); form.resetFields();
      void load(page, nhanVienId);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi chấm công');
    } finally { setSaving(false); }
  };

  const filter = (nv?: string) => {
    setNhanVienId(nv);
    const from = khoangNgay?.[0]?.format('YYYY-MM-DD');
    const to = khoangNgay?.[1]?.format('YYYY-MM-DD');
    void load(1, nv, from, to);
  };

  return (
    <Card size="small" title="Chấm công" extra={
      <Space wrap>
        <DatePicker.RangePicker value={khoangNgay} onChange={(v) => setKhoangNgay(v)} allowEmpty={[true, true]} />
        <Select allowClear showSearch optionFilterProp="label" placeholder="Nhân viên" style={{ width: 200 }}
          value={nhanVienId} onChange={(v) => { setNhanVienId(v ?? undefined); filter(v ?? undefined); }}
          options={employees.map((e) => ({ value: e.id, label: e.name }))} />
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setOpen(true)}>Chấm công</Button>
      </Space>
    }>
      <Table size="small" rowKey="id" loading={loading} dataSource={rows}
        pagination={{ current: page, pageSize: 50, total, showSizeChanger: false,
          onChange: (p) => { setPage(p);
            void load(p, nhanVienId, khoangNgay?.[0]?.format('YYYY-MM-DD'), khoangNgay?.[1]?.format('YYYY-MM-DD')); } }}
        columns={[
          { title: 'Ngày', dataIndex: 'ngay', width: 110,
            render: (v: string) => v ? dayjs(v).format('DD/MM/YYYY') : '' },
          { title: 'Nhân viên', dataIndex: 'tenNhanVien' },
          { title: 'Ca làm việc', dataIndex: 'tenCa', width: 160 },
          { title: 'Tỉ lệ lương', dataIndex: 'tileLuong', width: 100, align: 'right',
            render: (v: number) => fmtPct(v) },
          { title: 'Trạng thái', dataIndex: 'trangThai', width: 100,
            render: (v: number) => v === 0 ? <Tag>Chưa xác nhận</Tag> : <Tag color="green">Đã làm</Tag> },
        ]}
      />
      <Modal title="Ghi chấm công" open={open} onCancel={() => setOpen(false)} onOk={save}
        confirmLoading={saving} width={420} destroyOnClose>
        <Form form={form} layout="vertical">
          <Form.Item name="nhanVienId" label="Nhân viên" rules={[{ required: true, message: 'Chọn nhân viên' }]}>
            <Select showSearch optionFilterProp="label" placeholder="Chọn nhân viên"
              options={employees.map((e) => ({ value: e.id, label: e.name }))} />
          </Form.Item>
          <Form.Item name="ngay" label="Ngày" rules={[{ required: true }]}
            initialValue={dayjs()}>
            <DatePicker style={{ width: '100%' }} disabledDate={(d) => d.isAfter(dayjs(), 'day')} />
          </Form.Item>
          <Form.Item name="caLamViecId" label="Ca làm việc" rules={[{ required: true, message: 'Chọn ca' }]}>
            <Select options={shifts.map((s) => ({ value: s.id, label: `${s.name} (${fmtPct(s.tileLuong)})` }))} />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
};

export default AttendancePage;
