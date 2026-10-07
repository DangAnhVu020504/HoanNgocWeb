import React, { useCallback, useEffect, useState } from 'react';
import { Button, Card, Form, Input, InputNumber, Modal, Popconfirm, Select, Space, Switch, Table, Tag, message } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import {
  getEmployees, createEmployee, updateEmployee, deleteEmployee,
  type Employee,
} from '../../api/hr';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

const EmployeesPage: React.FC = () => {
  const [rows, setRows] = useState<Employee[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Employee | null>(null);
  const [saving, setSaving] = useState(false);
  const [form] = Form.useForm();

  const load = useCallback(async (p: number, s: string) => {
    setLoading(true);
    try {
      const r = await getEmployees({ search: s || undefined, activeOnly: false, page: p, pageSize: 20 });
      setRows(r.items); setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải nhân viên');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(1, ''); }, [load]);

  const openCreate = () => {
    setEditing(null);
    form.resetFields();
    form.setFieldsValue({ nghiChuNhat: true, luongCa: 0, luongThang: 0, phuCap: 0 });
    setOpen(true);
  };

  const openEdit = (e: Employee) => {
    setEditing(e);
    form.setFieldsValue({
      name: e.name, diaChi: e.diaChi, dienThoai: e.dienThoai,
      cachTinhLuong: e.cachTinhLuong, luongCa: e.luongCa, luongThang: e.luongThang,
      phuCap: e.phuCap, nghiThu7: e.nghiThu7, nghiChuNhat: e.nghiChuNhat, note: e.note,
    });
    setOpen(true);
  };

  const save = async () => {
    const v = await form.validateFields();
    setSaving(true);
    try {
      const payload = {
        name: v.name, diaChi: v.diaChi || null, dienThoai: v.dienThoai || null,
        cachTinhLuong: v.cachTinhLuong, luongCa: v.luongCa || 0, luongThang: v.luongThang || 0,
        phuCap: v.phuCap || 0, nghiThu7: !!v.nghiThu7, nghiChuNhat: !!v.nghiChuNhat, note: v.note || null,
      };
      if (editing) { await updateEmployee(editing.id, payload); void message.success('Đã cập nhật nhân viên'); }
      else { await createEmployee(payload); void message.success('Đã tạo nhân viên'); }
      setOpen(false);
      void load(page, search);
    } catch (err: unknown) {
      void message.error(err instanceof Error ? err.message : 'Lỗi lưu nhân viên');
    } finally { setSaving(false); }
  };

  const remove = async (id: string) => {
    try { await deleteEmployee(id); void message.success('Đã xóa'); void load(page, search); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi xóa nhân viên'); }
  };

  return (
    <Card size="small" title="Danh mục nhân viên" extra={
      <Space>
        <Input.Search allowClear placeholder="Tìm tên / SĐT nhân viên..." style={{ width: 260 }}
          onSearch={(v) => { setSearch(v); setPage(1); void load(1, v); }} />
        <Button type="primary" icon={<PlusOutlined />} onClick={() => openCreate()}>Thêm nhân viên</Button>
      </Space>
    }>
      <Table size="small" rowKey="id" loading={loading} dataSource={rows}
        pagination={{ current: page, pageSize: 20, total, showSizeChanger: false,
          onChange: (p) => { setPage(p); void load(p, search); } }}
        columns={[
          { title: 'Tên nhân viên', dataIndex: 'name' },
          { title: 'Điện thoại', dataIndex: 'dienThoai', width: 130 },
          { title: 'Địa chỉ', dataIndex: 'diaChi', ellipsis: true },
          { title: 'Cách tính lương', dataIndex: 'cachTinhLuong', width: 120,
            render: (v: number) => v === 1 ? <Tag color="blue">Theo tháng</Tag> : <Tag color="green">Theo ca</Tag> },
          { title: 'Lương ca', dataIndex: 'luongCa', width: 120, align: 'right', render: (v: number) => fmtVnd(v) },
          { title: 'Lương tháng', dataIndex: 'luongThang', width: 140, align: 'right', render: (v: number) => fmtVnd(v) },
          { title: 'Phụ cấp', dataIndex: 'phuCap', width: 120, align: 'right', render: (v: number) => fmtVnd(v) },
          { title: 'TT', dataIndex: 'status', width: 70,
            render: (v: number) => v === 30 ? <Tag color="green">Hoạt động</Tag> : <Tag>Ngưng</Tag> },
          { title: '', width: 120,
            render: (_: unknown, r: Employee) => (
              <Space size={4}>
                <Button size="small" type="link" onClick={() => openEdit(r)}>Sửa</Button>
                <Popconfirm title="Xóa mềm nhân viên này?" onConfirm={() => void remove(r.id)}>
                  <Button size="small" type="link" danger>Xóa</Button>
                </Popconfirm>
              </Space>
            ) },
        ]}
      />
      <Modal title={editing ? 'Sửa nhân viên' : 'Thêm nhân viên'} open={open} onCancel={() => setOpen(false)}
        onOk={save} confirmLoading={saving} width={560} destroyOnClose>
        <Form form={form} layout="vertical" initialValues={{ nghiChuNhat: true }}>
          <Form.Item name="name" label="Tên nhân viên" rules={[{ required: true, message: 'Nhập tên nhân viên' }]}>
            <Input allowClear placeholder="Nguyễn Văn A" />
          </Form.Item>
          <Space style={{ width: '100%' }} size={10}>
            <Form.Item name="dienThoai" label="Điện thoại" style={{ width: 180 }}>
              <Input allowClear />
            </Form.Item>
            <Form.Item name="diaChi" label="Địa chỉ" style={{ flex: 1, minWidth: 260 }}>
              <Input allowClear />
            </Form.Item>
          </Space>
          <Form.Item name="cachTinhLuong" label="Cách tính lương" rules={[{ required: true }]}>
            <Select options={[{ value: 0, label: 'Theo ca' }, { value: 1, label: 'Theo tháng' }]} />
          </Form.Item>
          <Form.Item noStyle shouldUpdate={(p, c) => p.cachTinhLuong !== c.cachTinhLuong}>
            {() => form.getFieldValue('cachTinhLuong') === 1 ? (
              <Form.Item name="luongThang" label="Lương tháng" initialValue={0}>
                <InputNumber min={0} step={100000} style={{ width: 220 }} formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')} />
              </Form.Item>
            ) : (
              <Form.Item name="luongCa" label="Lương / ca" initialValue={0}>
                <InputNumber min={0} step={10000} style={{ width: 220 }} formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')} />
              </Form.Item>
            )}
          </Form.Item>
          <Form.Item name="phuCap" label="Phụ cấp" initialValue={0}>
            <InputNumber min={0} step={50000} style={{ width: 220 }} formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')} />
          </Form.Item>
          <Space size={24}>
            <Form.Item name="nghiThu7" label="Nghỉ thứ 7" valuePropName="checked"><Switch /></Form.Item>
            <Form.Item name="nghiChuNhat" label="Nghỉ chủ nhật" valuePropName="checked"><Switch /></Form.Item>
          </Space>
          <Form.Item name="note" label="Ghi chú"><Input.TextArea rows={2} /></Form.Item>
        </Form>
      </Modal>
    </Card>
  );
};

export default EmployeesPage;
