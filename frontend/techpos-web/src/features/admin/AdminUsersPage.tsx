import React, { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Button, Card, Checkbox, Form, Input, Modal, Popconfirm, Select, Space, Table, Tabs, Tag, message,
} from 'antd';
import { PlusOutlined, ReloadOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import {
  createAdminUser, deactivateAdminUser, deleteAdminRole, getAdminPermissions, getAdminRoles,
  getAdminUsers, resetAdminPassword, saveAdminRole, updateAdminUser,
  type AdminPermission, type AdminRole, type AdminUser,
} from '../../api/admin';
import { useAuthStore } from '../../stores/authStore';

const permissionGroup = (code: string): string => code.split('.')[0] ?? 'OTHER';

// ---------- Tab người dùng ----------

const UsersTab: React.FC = () => {
  const canManage = useAuthStore((s) => s.hasPermission('ADMIN.USER.MANAGE'));
  const [rows, setRows] = useState<AdminUser[]>([]);
  const [roles, setRoles] = useState<AdminRole[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<AdminUser | null>(null);
  const [resetting, setResetting] = useState<AdminUser | null>(null);
  const [saving, setSaving] = useState(false);
  const [form] = Form.useForm();
  const [pwdForm] = Form.useForm();

  const load = useCallback(async (p: number) => {
    setLoading(true);
    try {
      const r = await getAdminUsers({ page: p, pageSize: 20, search: search || undefined });
      setRows(r.items);
      setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải người dùng');
    } finally {
      setLoading(false);
    }
  }, [search]);

  useEffect(() => { void load(page); }, [load, page]);
  useEffect(() => { getAdminRoles().then(setRoles).catch(() => undefined); }, []);

  const roleOptions = roles.map((r) => ({ value: r.code, label: `${r.name} (${r.code})` }));

  const openCreate = () => {
    setEditing(null);
    form.resetFields();
    form.setFieldsValue({ isActive: true, roles: ['SALES'] });
    setOpen(true);
  };

  const openEdit = (u: AdminUser) => {
    setEditing(u);
    form.setFieldsValue({ fullName: u.fullName, email: u.email, isActive: u.isActive, roles: u.roles });
    setOpen(true);
  };

  const save = async () => {
    const v = await form.validateFields();
    setSaving(true);
    try {
      if (editing) {
        await updateAdminUser(editing.id, {
          fullName: v.fullName || null,
          email: v.email || null,
          isActive: v.isActive,
          roles: v.roles,
        });
        void message.success('Đã cập nhật người dùng');
      } else {
        await createAdminUser({
          username: v.username,
          password: v.password,
          fullName: v.fullName || null,
          email: v.email || null,
          roles: v.roles,
        });
        void message.success('Đã tạo người dùng');
      }
      setOpen(false);
      void load(page);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi lưu người dùng');
    } finally {
      setSaving(false);
    }
  };

  const resetPwd = async () => {
    if (!resetting) return;
    const v = await pwdForm.validateFields();
    setSaving(true);
    try {
      await resetAdminPassword(resetting.id, v.newPassword);
      void message.success('Đã đặt lại mật khẩu');
      setResetting(null);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi đặt lại mật khẩu');
    } finally {
      setSaving(false);
    }
  };

  const deactivate = async (u: AdminUser) => {
    try {
      await deactivateAdminUser(u.id);
      void message.success('Đã vô hiệu hoá tài khoản');
      void load(page);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi vô hiệu hoá');
    }
  };

  return (
    <Card size="small" className="luxury-card" title={<span className="card-title-gold">Người dùng hệ thống</span>}
      extra={
        <Space wrap>
          <Input.Search allowClear placeholder="Tìm tài khoản / họ tên" style={{ width: 240 }}
            onSearch={(v) => { setPage(1); setSearch(v.trim()); }} />
          <Button icon={<ReloadOutlined />} onClick={() => void load(page)}>Tải lại</Button>
          {canManage && <Button type="primary" className="gold-gradient-btn" icon={<PlusOutlined />} onClick={openCreate}>Thêm</Button>}
        </Space>
      }>
      <Table className="luxury-table" size="middle" rowKey="id" loading={loading} dataSource={rows}
        pagination={{ current: page, pageSize: 20, total, showSizeChanger: false, onChange: setPage }}
        columns={[
          { title: 'Tài khoản', dataIndex: 'username', width: 160 },
          { title: 'Họ tên', dataIndex: 'fullName', render: (v: string | null) => v ?? '—' },
          { title: 'Email', dataIndex: 'email', render: (v: string | null) => v ?? '—' },
          {
            title: 'Vai trò', dataIndex: 'roles', render: (v: string[]) => (
              <Space size={4} wrap>{v.map((r) => <Tag key={r} color="#b8860b">{r}</Tag>)}</Space>
            ),
          },
          {
            title: 'Trạng thái', dataIndex: 'isActive', width: 120,
            render: (v: boolean) => <Tag color={v ? '#389e0d' : '#cf1322'}>{v ? 'Hoạt động' : 'Khoá'}</Tag>,
          },
          {
            title: 'Đăng nhập cuối', dataIndex: 'lastLoginAt', width: 150,
            render: (v: string | null) => v ? dayjs(v).format('DD/MM/YYYY HH:mm') : '—',
          },
          {
            title: '', width: 170, render: (_: unknown, u: AdminUser) => canManage ? (
              <Space size={4}>
                <Button type="link" size="small" style={{ padding: 0 }} onClick={() => openEdit(u)}>Sửa</Button>
                <Button type="link" size="small" style={{ padding: 0 }}
                  onClick={() => { setResetting(u); pwdForm.resetFields(); }}>Mật khẩu</Button>
                {u.isActive && (
                  <Popconfirm title={`Vô hiệu hoá ${u.username}?`} onConfirm={() => void deactivate(u)}>
                    <Button type="link" size="small" danger style={{ padding: 0 }}>Khoá</Button>
                  </Popconfirm>
                )}
              </Space>
            ) : null,
          },
        ]} />

      <Modal title={editing ? `Sửa người dùng — ${editing.username}` : 'Thêm người dùng'} open={open}
        onCancel={() => setOpen(false)} onOk={() => void save()} confirmLoading={saving} destroyOnClose>
        <Form form={form} layout="vertical">
          {!editing && (
            <>
              <Form.Item name="username" label="Tài khoản" rules={[{ required: true }]}><Input autoComplete="off" /></Form.Item>
              <Form.Item name="password" label="Mật khẩu" rules={[{ required: true, min: 6 }]}>
                <Input.Password autoComplete="new-password" />
              </Form.Item>
            </>
          )}
          <Form.Item name="fullName" label="Họ tên"><Input allowClear /></Form.Item>
          <Form.Item name="email" label="Email" rules={[{ type: 'email' }]}><Input allowClear /></Form.Item>
          <Form.Item name="roles" label="Vai trò" rules={[{ required: true, message: 'Chọn ít nhất 1 vai trò' }]}>
            <Select mode="multiple" options={roleOptions} optionFilterProp="label" />
          </Form.Item>
          {editing && (
            <Form.Item name="isActive" label="Kích hoạt" valuePropName="checked">
              <Checkbox>Đang hoạt động</Checkbox>
            </Form.Item>
          )}
        </Form>
      </Modal>

      <Modal title={`Đặt lại mật khẩu — ${resetting?.username ?? ''}`} open={!!resetting}
        onCancel={() => setResetting(null)} onOk={() => void resetPwd()} confirmLoading={saving} destroyOnClose>
        <Form form={pwdForm} layout="vertical">
          <Form.Item name="newPassword" label="Mật khẩu mới" rules={[{ required: true, min: 6 }]}>
            <Input.Password autoComplete="new-password" />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
};

// ---------- Tab vai trò ----------

const RolesTab: React.FC = () => {
  const canManage = useAuthStore((s) => s.hasPermission('ADMIN.ROLE.MANAGE'));
  const [roles, setRoles] = useState<AdminRole[]>([]);
  const [permissions, setPermissions] = useState<AdminPermission[]>([]);
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<AdminRole | null>(null);
  const [saving, setSaving] = useState(false);
  const [form] = Form.useForm();

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [r, p] = await Promise.all([getAdminRoles(), getAdminPermissions()]);
      setRoles(r);
      setPermissions(p);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải phân quyền');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(); }, [load]);

  const permGroups = useMemo(() => {
    const groups = new Map<string, { label: string; options: { value: string; label: string }[] }>();
    for (const p of permissions) {
      const g = permissionGroup(p.code);
      if (!groups.has(g)) groups.set(g, { label: g, options: [] });
      groups.get(g)!.options.push({ value: p.code, label: `${p.code} — ${p.name}` });
    }
    return [...groups.values()];
  }, [permissions]);

  const openCreate = () => {
    setEditing(null);
    form.resetFields();
    setOpen(true);
  };

  const openEdit = (r: AdminRole) => {
    setEditing(r);
    form.setFieldsValue({ code: r.code, name: r.name, permissions: r.permissions });
    setOpen(true);
  };

  const save = async () => {
    const v = await form.validateFields();
    setSaving(true);
    try {
      await saveAdminRole({ code: v.code, name: v.name, permissions: v.permissions ?? [] });
      void message.success('Đã lưu vai trò');
      setOpen(false);
      void load();
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi lưu vai trò');
    } finally {
      setSaving(false);
    }
  };

  const remove = async (r: AdminRole) => {
    try {
      await deleteAdminRole(r.id);
      void message.success('Đã xoá vai trò');
      void load();
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi xoá vai trò');
    }
  };

  return (
    <Card size="small" className="luxury-card" title={<span className="card-title-gold">Vai trò &amp; quyền ({roles.length} vai trò / {permissions.length} quyền)</span>}
      extra={canManage && (
        <Button type="primary" className="gold-gradient-btn" icon={<PlusOutlined />} onClick={openCreate}>Thêm vai trò</Button>
      )}>
      <Table className="luxury-table" size="middle" rowKey="id" loading={loading} dataSource={roles} pagination={false}
        columns={[
          { title: 'Mã', dataIndex: 'code', width: 160 },
          { title: 'Tên', dataIndex: 'name', width: 220 },
          { title: 'Số quyền', dataIndex: 'permissions', width: 110, align: 'right', render: (v: string[]) => v.length },
          { title: 'Số người dùng', dataIndex: 'userCount', width: 140, align: 'right' },
          {
            title: '', width: 140, render: (_: unknown, r: AdminRole) => canManage ? (
              <Space size={4}>
                <Button type="link" size="small" style={{ padding: 0 }} onClick={() => openEdit(r)}>Sửa</Button>
                {!['ADMIN', 'MANAGER'].includes(r.code) && (
                  <Popconfirm title={`Xoá vai trò ${r.code}?`} onConfirm={() => void remove(r)}>
                    <Button type="link" size="small" danger style={{ padding: 0 }}>Xoá</Button>
                  </Popconfirm>
                )}
              </Space>
            ) : null,
          },
        ]} />

      <Modal title={editing ? `Sửa vai trò — ${editing.code}` : 'Thêm vai trò'} open={open} width={720}
        onCancel={() => setOpen(false)} onOk={() => void save()} confirmLoading={saving} destroyOnClose>
        <Form form={form} layout="vertical">
          <Form.Item name="code" label="Mã vai trò" rules={[{ required: true }]}>
            <Input disabled={!!editing} style={{ textTransform: 'uppercase' }} />
          </Form.Item>
          <Form.Item name="name" label="Tên vai trò" rules={[{ required: true }]}><Input /></Form.Item>
          <Form.Item name="permissions" label="Danh sách quyền">
            <Select mode="multiple" optionFilterProp="label" allowClear placeholder="Chọn quyền"
              options={permGroups} maxTagCount="responsive" />
          </Form.Item>
        </Form>
      </Modal>
    </Card>
  );
};

/** Phase 8 — Quản trị người dùng & phân quyền. */
const AdminUsersPage: React.FC = () => (
  <Tabs className="luxury-tabs" items={[
    { key: 'users', label: 'Người dùng', children: <UsersTab /> },
    { key: 'roles', label: 'Vai trò & quyền', children: <RolesTab /> },
  ]} />
);

export default AdminUsersPage;
