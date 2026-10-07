import React, { useEffect, useState } from 'react';
import {
  Alert, App, Badge, Button, Card, Checkbox, Descriptions, Drawer, Form, Input,
  InputNumber, Modal, Select, Space, Table, Tabs, Tag
} from 'antd';
import { EditOutlined, PlusOutlined, ReloadOutlined, SearchOutlined, UploadOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import {
  createCustomer, deleteCustomer, getCustomer, getCustomerGroups, getCustomers,
  updateCustomer, type Customer, type CustomerGroup
} from '../../api/customers';
import {
  createSupplier, deleteSupplier, getSuppliers, getSupplierGroups,
  updateSupplier, type Supplier, type SupplierGroup
} from '../../api/suppliers';
import {
  createWarehouse, deleteWarehouse, getWarehouses, updateWarehouse, type Warehouse
} from '../../api/warehouses';
import { http } from '../../api/http';
import { useAuthStore } from '../../stores/authStore';

interface ImportResultDto {
  totalRows: number;
  created: number;
  skipped: number;
  errors: { row: number; message: string }[];
}

const statusBadge = (v: number) => v === 30
  ? <Badge status="success" text="Hoạt động" />
  : <Badge status="default" text={`Không dùng (${v})`} />;

const dash = (v: string | null | undefined) => v ?? '—';

// Xử lý lỗi API envelope (axios interceptor reject với err.response.data)
const apiErrMsg = (e: unknown, fallback: string): string => {
  const anyE = e as { message?: unknown; errors?: { message: string }[] };
  if (anyE?.errors?.length) return anyE.errors.map((x) => x.message).join('; ');
  if (typeof anyE?.message === 'string') return anyE.message;
  if (e instanceof Error) return e.message;
  return fallback;
};

// ---------- Khách hàng ----------
const CustomersTab: React.FC = () => {
  const { message, modal } = App.useApp();
  const { hasPermission } = useAuthStore();
  const canCreate = hasPermission('SALES.CUSTOMER.CREATE');
  const canUpdate = hasPermission('SALES.CUSTOMER.UPDATE');
  const canDelete = hasPermission('SALES.CUSTOMER.DELETE');

  const [items, setItems] = useState<Customer[]>([]);
  const [groups, setGroups] = useState<CustomerGroup[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [groupId, setGroupId] = useState<string | undefined>(undefined);
  const [activeOnly, setActiveOnly] = useState(true);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [detail, setDetail] = useState<Customer | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);

  const [form] = Form.useForm();
  const [editing, setEditing] = useState<Customer | null>(null); // null = tạo mới
  const [formOpen, setFormOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const load = async () => {
    try {
      setErr(null);
      setLoading(true);
      const res = await getCustomers({ search, groupId, activeOnly, page, pageSize });
      setItems(res.items);
      setTotal(res.total);
    } catch (e: unknown) {
      setErr(apiErrMsg(e, 'Lỗi tải khách hàng'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); /* eslint-disable-line react-hooks/exhaustive-deps */ }, [search, groupId, activeOnly, page, pageSize]);
  useEffect(() => { getCustomerGroups().then(setGroups).catch(() => { }); }, []);

  const openDetail = async (id: string) => {
    setDetailOpen(true);
    try {
      setDetail(await getCustomer(id));
    } catch { /* giữ drawer mở với dữ liệu cũ */ }
  };

  const openCreate = () => { setEditing(null); form.resetFields(); setFormOpen(true); };
  const openEdit = (c: Customer) => {
    setEditing(c);
    form.setFieldsValue({
      name: c.name, maKhach: c.maKhach, dienThoai: c.dienThoai, email: c.email,
      maSoThue: c.maSoThue, nguoiDaiDien: c.nguoiDaiDien, chucVu: c.chucVu,
      facebook: c.facebook, diaChi: c.diaChi, diaChiGiaoHang: c.diaChiGiaoHang,
      nhomKhachHangId: groups.find((g) => g.name === c.nhomKhachHang)?.id,
      songayChoNo: c.songayChoNo, diemTichLuy: c.diemTichLuy, status: c.status
    });
    setFormOpen(true);
  };

  const submit = async () => {
    try {
      const v = await form.validateFields();
      setSaving(true);
      if (editing) {
        await updateCustomer(editing.id, v);
        message.success('Đã cập nhật khách hàng');
      } else {
        await createCustomer(v);
        message.success('Đã thêm khách hàng');
      }
      setFormOpen(false);
      await load();
    } catch (e: unknown) {
      if ((e as { errorFields?: unknown }).errorFields) return; // lỗi validate form
      message.error(apiErrMsg(e, 'Lỗi lưu khách hàng'));
    } finally {
      setSaving(false);
    }
  };

  const confirmDelete = (c: Customer) => {
    modal.confirm({
      title: `Xóa khách hàng "${c.name}"?`,
      content: 'Khách hàng sẽ bị ẩn khỏi danh sách (soft delete). Dữ liệu không bị mất.',
      okType: 'danger', okText: 'Xóa', cancelText: 'Bỏ qua',
      onOk: async () => {
        try {
          await deleteCustomer(c.id);
          message.success('Đã xóa khách hàng');
          await load();
        } catch (e: unknown) {
          message.error(apiErrMsg(e, 'Lỗi xóa khách hàng'));
        }
      }
    });
  };

  const columns: ColumnsType<Customer> = [
    { title: 'Mã khách', dataIndex: 'maKhach', width: 120, render: (v) => <b>{v ?? '—'}</b> },
    { title: 'Tên khách hàng', dataIndex: 'name' },
    { title: 'Điện thoại', dataIndex: 'dienThoai', width: 130 },
    { title: 'Nhóm', dataIndex: 'nhomKhachHang', width: 140, render: (v) => v ? <Tag>{v}</Tag> : '—' },
    { title: 'Địa chỉ', dataIndex: 'diaChi', ellipsis: true, render: dash },
    { title: 'Email', dataIndex: 'email', width: 180, render: dash },
    { title: 'Nợ cho phép (ngày)', dataIndex: 'songayChoNo', width: 120, align: 'right' },
    { title: 'Trạng thái', dataIndex: 'status', width: 110, align: 'center', render: statusBadge },
    {
      title: '', key: 'act', width: 130, align: 'center',
      render: (_, r) => (
        <Space size={4}>
          {canUpdate && <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)}>Sửa</Button>}
          {canDelete && <Button size="small" danger onClick={() => confirmDelete(r)}>Xóa</Button>}
        </Space>
      )
    },
  ];

  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      <Card size="small">
        <Space wrap>
          <Input allowClear placeholder="Tìm tên / mã / ĐT / MST..." prefix={<SearchOutlined />}
            style={{ width: 260 }} value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
          <Select allowClear placeholder="Nhóm khách hàng" style={{ width: 180 }} value={groupId}
            onChange={(v) => { setGroupId(v); setPage(1); }}
            options={groups.map((g) => ({ value: g.id, label: g.name }))} />
          <Select style={{ width: 140 }} value={activeOnly ? 'active' : 'all'}
            onChange={(v) => { setActiveOnly(v === 'active'); setPage(1); }}
            options={[{ value: 'active', label: 'Chỉ hoạt động' }, { value: 'all', label: 'Tất cả' }]} />
          <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Tải lại</Button>
          {canCreate && <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Thêm khách hàng</Button>}
          {canCreate && (
            <label style={{ display: 'inline-block' }}>
              <input type="file" accept=".xlsx,.xls" style={{ display: 'none' }}
                onChange={async (e) => {
                  const f = e.target.files?.[0];
                  if (!f) return;
                  const fd = new FormData();
                  fd.append('file', f);
                  try {
                    const res = await http.post('/customers/import', fd,
                      { headers: { 'Content-Type': 'multipart/form-data' } });
                    const env = res.data as { success: boolean; data?: ImportResultDto; message?: string };
                    if (env.success && env.data) {
                      const r = env.data;
                      message.success(`Import: ${r.created} tạo mới, ${r.skipped} bỏ qua (trùng), ${r.errors.length} lỗi`);
                      load();
                    } else {
                      message.error(env.message ?? 'Import thất bại');
                    }
                  } catch (e2: unknown) {
                    message.error(apiErrMsg(e2, 'Lỗi import'));
                  } finally {
                    e.target.value = '';
                  }
                }} />
              <Button icon={<UploadOutlined />} onClick={(e) => {
                (e.currentTarget.previousSibling as HTMLInputElement)?.click();
              }}>Nhập Excel</Button>
            </label>
          )}
        </Space>
      </Card>
      {err && <Alert type="error" showIcon message={err} closable />}
      <Card size="small" title={`Danh mục khách hàng (${total})`} styles={{ body: { padding: 0 } }}>
        <Table size="middle" rowKey="id" loading={loading} columns={columns} dataSource={items}
          pagination={{ current: page, pageSize, total, showSizeChanger: true, showTotal: (t) => `Tổng ${t} khách hàng` }}
          onChange={(p) => { setPage(p.current ?? 1); setPageSize(p.pageSize ?? 20); }}
          onRow={(r) => ({ onDoubleClick: () => { void openDetail(r.id); }, style: { cursor: 'pointer' } })} />
      </Card>
      <Drawer title="Chi tiết khách hàng" width={480} open={detailOpen} onClose={() => setDetailOpen(false)}>
        {detail && (
          <>
            <Descriptions column={1} bordered size="small">
              <Descriptions.Item label="Mã khách">{detail.maKhach ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Tên">{detail.name}</Descriptions.Item>
              <Descriptions.Item label="Nhóm">{detail.nhomKhachHang ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Điện thoại">{detail.dienThoai ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Email">{detail.email ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="MST">{detail.maSoThue ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Địa chỉ">{detail.diaChi ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Địa chỉ giao hàng">{detail.diaChiGiaoHang ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="NV phụ trách">{detail.nhanVienPhuTrach ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Điểm tích luỹ">{detail.diemTichLuy}</Descriptions.Item>
            </Descriptions>
            {canUpdate && (
              <Button style={{ marginTop: 12 }} icon={<EditOutlined />}
                onClick={() => { setDetailOpen(false); openEdit(detail); }}>
                Sửa khách hàng
              </Button>
            )}
          </>
        )}
      </Drawer>

      <Modal
        title={editing ? `Sửa khách hàng — ${editing.name}` : 'Thêm khách hàng'}
        open={formOpen} onCancel={() => setFormOpen(false)} onOk={submit}
        confirmLoading={saving} okText="Lưu" cancelText="Bỏ qua" width={640}
        destroyOnClose
      >
        <Form form={form} layout="vertical" initialValues={{ songayChoNo: 0, diemTichLuy: 0 }}>
          <Form.Item name="name" label="Tên khách hàng" rules={[{ required: true, max: 255, message: 'Tên bắt buộc, ≤ 255 ký tự' }]}>
            <Input placeholder="Tên khách hàng" />
          </Form.Item>
          {!editing && (
            <Form.Item name="maKhach" label="Mã khách"><Input placeholder="Mã khách (tự sinh nếu để trống)" /></Form.Item>
          )}
          <Space wrap style={{ width: '100%' }}>
            <Form.Item name="dienThoai" label="Điện thoại" style={{ width: 200 }}><Input /></Form.Item>
            <Form.Item name="email" label="Email" style={{ width: 220 }}
              rules={[{ type: 'email', message: 'Email không hợp lệ' }]}><Input /></Form.Item>
            <Form.Item name="maSoThue" label="Mã số thuế" style={{ width: 200 }}><Input /></Form.Item>
          </Space>
          <Form.Item name="nhomKhachHangId" label="Nhóm khách hàng">
            <Select allowClear options={groups.map((g) => ({ value: g.id, label: g.name }))} />
          </Form.Item>
          <Form.Item name="diaChi" label="Địa chỉ"><Input.TextArea rows={2} /></Form.Item>
          <Form.Item name="diaChiGiaoHang" label="Địa chỉ giao hàng"><Input.TextArea rows={2} /></Form.Item>
          <Space wrap>
            <Form.Item name="nguoiDaiDien" label="Người đại diện" style={{ width: 180 }}><Input /></Form.Item>
            <Form.Item name="chucVu" label="Chức vụ" style={{ width: 160 }}><Input /></Form.Item>
            <Form.Item name="facebook" label="Facebook" style={{ width: 200 }}><Input /></Form.Item>
          </Space>
          <Space wrap>
            <Form.Item name="songayChoNo" label="Nợ cho phép (ngày)" style={{ width: 160 }}>
              <InputNumber min={0} style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item name="diemTichLuy" label="Điểm tích luỹ" style={{ width: 160 }}>
              <InputNumber min={0} style={{ width: '100%' }} />
            </Form.Item>
            {editing && (
              <Form.Item name="status" label="Trạng thái" style={{ width: 180 }}>
                <Select options={[{ value: 30, label: 'Hoạt động' }, { value: 0, label: 'Ngừng hoạt động' }]} />
              </Form.Item>
            )}
          </Space>
        </Form>
      </Modal>
    </Space>
  );
};

// ---------- Nhà cung cấp ----------
const SuppliersTab: React.FC = () => {
  const { message, modal } = App.useApp();
  const { hasPermission } = useAuthStore();
  const canCreate = hasPermission('DEBT.SUPPLIER.CREATE');
  const canUpdate = hasPermission('DEBT.SUPPLIER.UPDATE');
  const canDelete = hasPermission('DEBT.SUPPLIER.DELETE');

  const [items, setItems] = useState<Supplier[]>([]);
  const [groups, setGroups] = useState<SupplierGroup[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [groupId, setGroupId] = useState<string | undefined>(undefined);
  const [activeOnly, setActiveOnly] = useState(true);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const [form] = Form.useForm();
  const [editing, setEditing] = useState<Supplier | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const load = async () => {
    try {
      setErr(null);
      setLoading(true);
      const res = await getSuppliers({ search, groupId, activeOnly, page, pageSize });
      setItems(res.items);
      setTotal(res.total);
    } catch (e: unknown) {
      setErr(apiErrMsg(e, 'Lỗi tải nhà cung cấp'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); /* eslint-disable-line react-hooks/exhaustive-deps */ }, [search, groupId, activeOnly, page, pageSize]);
  useEffect(() => { getSupplierGroups().then(setGroups).catch(() => { }); }, []);

  const openCreate = () => { setEditing(null); form.resetFields(); setFormOpen(true); };
  const openEdit = (s: Supplier) => {
    setEditing(s);
    form.setFieldsValue({
      name: s.name, dienThoai: s.dienThoai, email: s.email, website: s.website,
      note: s.note, diaChi: s.diaChi,
      nhomNhaCungCapId: groups.find((g) => g.name === s.nhomNhaCungCap)?.id,
      status: s.status
    });
    setFormOpen(true);
  };

  const submit = async () => {
    try {
      const v = await form.validateFields();
      setSaving(true);
      if (editing) {
        await updateSupplier(editing.id, v);
        message.success('Đã cập nhật nhà cung cấp');
      } else {
        await createSupplier(v);
        message.success('Đã thêm nhà cung cấp');
      }
      setFormOpen(false);
      await load();
    } catch (e: unknown) {
      if ((e as { errorFields?: unknown }).errorFields) return;
      message.error(apiErrMsg(e, 'Lỗi lưu nhà cung cấp'));
    } finally {
      setSaving(false);
    }
  };

  const confirmDelete = (s: Supplier) => {
    modal.confirm({
      title: `Xóa nhà cung cấp "${s.name}"?`,
      content: 'NCC sẽ bị ẩn khỏi danh sách (soft delete).',
      okType: 'danger', okText: 'Xóa', cancelText: 'Bỏ qua',
      onOk: async () => {
        try {
          await deleteSupplier(s.id);
          message.success('Đã xóa nhà cung cấp');
          await load();
        } catch (e: unknown) {
          message.error(apiErrMsg(e, 'Lỗi xóa nhà cung cấp'));
        }
      }
    });
  };

  const columns: ColumnsType<Supplier> = [
    { title: 'Mã NCC', dataIndex: 'maNhaCungCap', width: 130, render: (v) => <b>{v ?? '—'}</b> },
    { title: 'Tên nhà cung cấp', dataIndex: 'name' },
    { title: 'Nhóm', dataIndex: 'nhomNhaCungCap', width: 130, render: (v) => v ? <Tag>{v}</Tag> : '—' },
    { title: 'Điện thoại', dataIndex: 'dienThoai', width: 130, render: dash },
    { title: 'Email', dataIndex: 'email', width: 180, render: dash },
    { title: 'Địa chỉ', dataIndex: 'diaChi', ellipsis: true, render: dash },
    { title: 'Trạng thái', dataIndex: 'status', width: 110, align: 'center', render: statusBadge },
    {
      title: '', key: 'act', width: 130, align: 'center',
      render: (_, r) => (
        <Space size={4}>
          {canUpdate && <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)}>Sửa</Button>}
          {canDelete && <Button size="small" danger onClick={() => confirmDelete(r)}>Xóa</Button>}
        </Space>
      )
    },
  ];

  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      <Card size="small">
        <Space wrap>
          <Input allowClear placeholder="Tìm tên / mã / ĐT / email NCC..." prefix={<SearchOutlined />}
            style={{ width: 280 }} value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
          <Select allowClear placeholder="Nhóm NCC" style={{ width: 180 }} value={groupId}
            onChange={(v) => { setGroupId(v); setPage(1); }}
            options={groups.map((g) => ({ value: g.id, label: g.name }))} />
          <Select style={{ width: 150 }} value={activeOnly ? 'active' : 'all'}
            onChange={(v) => { setActiveOnly(v === 'active'); setPage(1); }}
            options={[{ value: 'active', label: 'Chỉ hoạt động' }, { value: 'all', label: 'Tất cả' }]} />
          <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Tải lại</Button>
          {canCreate && <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Thêm NCC</Button>}
        </Space>
      </Card>
      {err && <Alert type="error" showIcon message={err} closable />}
      <Card size="small" title={`Danh mục nhà cung cấp (${total})`} styles={{ body: { padding: 0 } }}>
        <Table size="middle" rowKey="id" loading={loading} columns={columns} dataSource={items}
          pagination={{ current: page, pageSize, total, showSizeChanger: true, showTotal: (t) => `Tổng ${t} nhà cung cấp` }}
          onChange={(p) => { setPage(p.current ?? 1); setPageSize(p.pageSize ?? 20); }} />
      </Card>

      <Modal
        title={editing ? `Sửa nhà cung cấp — ${editing.name}` : 'Thêm nhà cung cấp'}
        open={formOpen} onCancel={() => setFormOpen(false)} onOk={submit}
        confirmLoading={saving} okText="Lưu" cancelText="Bỏ qua" width={640}
        destroyOnClose
      >
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="Tên nhà cung cấp" rules={[{ required: true, max: 255, message: 'Tên bắt buộc, ≤ 255 ký tự' }]}>
            <Input />
          </Form.Item>
          {!editing && <Form.Item name="maNhaCungCap" label="Mã NCC"><Input /></Form.Item>}
          <Space wrap>
            <Form.Item name="dienThoai" label="Điện thoại" style={{ width: 200 }}><Input /></Form.Item>
            <Form.Item name="email" label="Email" style={{ width: 220 }}
              rules={[{ type: 'email', message: 'Email không hợp lệ' }]}><Input /></Form.Item>
            <Form.Item name="website" label="Website" style={{ width: 220 }}><Input /></Form.Item>
          </Space>
          <Form.Item name="nhomNhaCungCapId" label="Nhóm NCC">
            <Select allowClear options={groups.map((g) => ({ value: g.id, label: g.name }))} />
          </Form.Item>
          <Form.Item name="diaChi" label="Địa chỉ"><Input.TextArea rows={2} /></Form.Item>
          <Form.Item name="note" label="Ghi chú"><Input.TextArea rows={2} /></Form.Item>
          {editing && (
            <Form.Item name="status" label="Trạng thái" style={{ width: 200 }}>
              <Select options={[{ value: 30, label: 'Hoạt động' }, { value: 0, label: 'Ngừng hoạt động' }]} />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </Space>
  );
};

// ---------- Kho hàng ----------
const WarehousesTab: React.FC = () => {
  const { message, modal } = App.useApp();
  const { hasPermission } = useAuthStore();
  const canCreate = hasPermission('INVENTORY.WAREHOUSE.CREATE');
  const canUpdate = hasPermission('INVENTORY.WAREHOUSE.UPDATE');
  const canDelete = hasPermission('INVENTORY.WAREHOUSE.DELETE');

  const [items, setItems] = useState<Warehouse[]>([]);
  const [allWarehouses, setAllWarehouses] = useState<Warehouse[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [activeOnly, setActiveOnly] = useState(true);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const [form] = Form.useForm();
  const [editing, setEditing] = useState<Warehouse | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const load = async () => {
    try {
      setErr(null);
      setLoading(true);
      const res = await getWarehouses({ search, activeOnly, page, pageSize });
      setItems(res.items);
      setTotal(res.total);
    } catch (e: unknown) {
      setErr(apiErrMsg(e, 'Lỗi tải kho hàng'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); /* eslint-disable-line react-hooks/exhaustive-deps */ }, [search, activeOnly, page, pageSize]);
  useEffect(() => { getWarehouses({ activeOnly: false, pageSize: 200 }).then((r) => setAllWarehouses(r.items)).catch(() => { }); }, []);

  const openCreate = () => { setEditing(null); form.resetFields(); setFormOpen(true); };
  const openEdit = (w: Warehouse) => {
    setEditing(w);
    form.setFieldsValue({
      name: w.name, note: w.note, parentId: w.parentId,
      choPhepAmKho: w.choPhepAmKho, status: w.status
    });
    setFormOpen(true);
  };

  const submit = async () => {
    try {
      const v = await form.validateFields();
      setSaving(true);
      if (editing) {
        await updateWarehouse(editing.id, v);
        message.success('Đã cập nhật kho hàng');
      } else {
        await createWarehouse(v);
        message.success('Đã thêm kho hàng');
      }
      setFormOpen(false);
      await load();
    } catch (e: unknown) {
      if ((e as { errorFields?: unknown }).errorFields) return;
      message.error(apiErrMsg(e, 'Lỗi lưu kho hàng'));
    } finally {
      setSaving(false);
    }
  };

  const confirmDelete = (w: Warehouse) => {
    modal.confirm({
      title: `Xóa kho "${w.name}"?`,
      content: 'Kho sẽ bị ẩn khỏi danh sách (soft delete). Chỉ nên xóa kho không còn dữ liệu.',
      okType: 'danger', okText: 'Xóa', cancelText: 'Bỏ qua',
      onOk: async () => {
        try {
          await deleteWarehouse(w.id);
          message.success('Đã xóa kho hàng');
          await load();
        } catch (e: unknown) {
          message.error(apiErrMsg(e, 'Lỗi xóa kho hàng'));
        }
      }
    });
  };

  const columns: ColumnsType<Warehouse> = [
    { title: 'Tên kho', dataIndex: 'name' },
    { title: 'Kho cha', dataIndex: 'parentName', width: 160, render: (v) => v ?? '—' },
    { title: 'Cửa hàng', dataIndex: 'cuaHang', width: 160, render: (v) => v ?? '—' },
    { title: 'Cho phép âm kho', dataIndex: 'choPhepAmKho', width: 130, align: 'center',
      render: (v: boolean) => v ? <Tag color="orange">Có</Tag> : <Tag>Không</Tag> },
    { title: 'Ghi chú', dataIndex: 'note', ellipsis: true, render: (v) => v ?? '—' },
    { title: 'Trạng thái', dataIndex: 'status', width: 110, align: 'center', render: statusBadge },
    {
      title: '', key: 'act', width: 130, align: 'center',
      render: (_, r) => (
        <Space size={4}>
          {canUpdate && <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)}>Sửa</Button>}
          {canDelete && <Button size="small" danger onClick={() => confirmDelete(r)}>Xóa</Button>}
        </Space>
      )
    },
  ];

  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      <Card size="small">
        <Space wrap>
          <Input allowClear placeholder="Tìm tên kho..." prefix={<SearchOutlined />}
            style={{ width: 260 }} value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
          <Select style={{ width: 150 }} value={activeOnly ? 'active' : 'all'}
            onChange={(v) => { setActiveOnly(v === 'active'); setPage(1); }}
            options={[{ value: 'active', label: 'Chỉ hoạt động' }, { value: 'all', label: 'Tất cả' }]} />
          <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Tải lại</Button>
          {canCreate && <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>Thêm kho</Button>}
        </Space>
      </Card>
      {err && <Alert type="error" showIcon message={err} closable />}
      <Card size="small" title={`Danh mục kho hàng (${total})`} styles={{ body: { padding: 0 } }}>
        <Table size="middle" rowKey="id" loading={loading} columns={columns} dataSource={items}
          pagination={{ current: page, pageSize, total, showSizeChanger: true, showTotal: (t) => `Tổng ${t} kho` }}
          onChange={(p) => { setPage(p.current ?? 1); setPageSize(p.pageSize ?? 20); }} />
      </Card>

      <Modal
        title={editing ? `Sửa kho hàng — ${editing.name}` : 'Thêm kho hàng'}
        open={formOpen} onCancel={() => setFormOpen(false)} onOk={submit}
        confirmLoading={saving} okText="Lưu" cancelText="Bỏ qua" width={560}
        destroyOnClose
      >
        <Form form={form} layout="vertical">
          <Form.Item name="name" label="Tên kho" rules={[{ required: true, max: 255, message: 'Tên kho bắt buộc, ≤ 255 ký tự' }]}>
            <Input />
          </Form.Item>
          <Form.Item name="parentId" label="Kho cha">
            <Select allowClear
              options={allWarehouses.filter((w) => w.id !== editing?.id).map((w) => ({ value: w.id, label: w.name }))} />
          </Form.Item>
          <Form.Item name="note" label="Ghi chú"><Input.TextArea rows={2} /></Form.Item>
          <Form.Item name="choPhepAmKho" valuePropName="checked">
            <Checkbox>Cho phép tồn kho âm</Checkbox>
          </Form.Item>
          {editing && (
            <Form.Item name="status" label="Trạng thái" style={{ width: 200 }}>
              <Select options={[{ value: 30, label: 'Hoạt động' }, { value: 0, label: 'Ngừng hoạt động' }]} />
            </Form.Item>
          )}
        </Form>
      </Modal>
    </Space>
  );
};

const CatalogPage: React.FC = () => (
  <Tabs
    defaultActiveKey="customers"
    items={[
      { key: 'customers', label: 'Khách hàng', children: <CustomersTab /> },
      { key: 'suppliers', label: 'Nhà cung cấp', children: <SuppliersTab /> },
      { key: 'warehouses', label: 'Kho hàng', children: <WarehousesTab /> },
    ]}
  />
);

export default CatalogPage;
