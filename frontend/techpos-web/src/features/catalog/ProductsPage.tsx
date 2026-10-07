import React, { useEffect, useMemo, useState } from 'react';
import {
  Alert, App, Badge, Button, Card, Checkbox, Descriptions, Drawer, Empty, Form, Input,
  InputNumber, Modal, Select, Space, Spin, Table, Tag, message
} from 'antd';
import { EditOutlined, PlusOutlined, ReloadOutlined, SearchOutlined, UploadOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import {
  createProduct, deleteProduct, getProduct, getProductGroups, getProductStock, getProducts,
  updateProduct,
  Product, ProductGroup, ProductStock
} from '../../api/products';
import { http } from '../../api/http';
import { openBarcode } from '../../api/reports';
import { useAuthStore } from '../../stores/authStore';

interface ImportResultDto {
  totalRows: number;
  created: number;
  skipped: number;
  errors: { row: number; message: string }[];
}

const fmtVnd = (n: number | null | undefined) => {
  if (n === null || n === undefined) return '—';
  return n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';
};

const apiErrMsg = (e: unknown, fallback: string): string => {
  const anyE = e as { message?: unknown; errors?: { message: string }[] };
  if (anyE?.errors?.length) return anyE.errors.map((x) => x.message).join('; ');
  if (typeof anyE?.message === 'string') return anyE.message;
  if (e instanceof Error) return e.message;
  return fallback;
};

const ProductsPage: React.FC = () => {
  const { modal } = App.useApp();
  const { hasPermission } = useAuthStore();
  const canCreate = hasPermission('SALES.PRODUCT.CREATE');
  const canUpdate = hasPermission('SALES.PRODUCT.UPDATE');
  const canDelete = hasPermission('SALES.PRODUCT.DELETE');

  const [items, setItems] = useState<Product[]>([]);
  const [groups, setGroups] = useState<ProductGroup[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string | null>(null);

  const [search, setSearch] = useState('');
  const [groupId, setGroupId] = useState<string | undefined>(undefined);
  const [activeOnly, setActiveOnly] = useState<boolean>(true);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const [detail, setDetail] = useState<Product | null>(null);
  const [detailStock, setDetailStock] = useState<ProductStock[]>([]);
  const [detailOpen, setDetailOpen] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);

  const [form] = Form.useForm();
  const [editing, setEditing] = useState<Product | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [saving, setSaving] = useState(false);

  const load = async () => {
    try {
      setErr(null);
      setLoading(true);
      const res = await getProducts({ search, groupId, activeOnly, page, pageSize });
      setItems(res.items);
      setTotal(res.total);
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tải danh mục');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, [search, groupId, activeOnly, page, pageSize]);

  useEffect(() => {
    getProductGroups().then(setGroups).catch(() => { /* nhóm không bắt buộc */ });
  }, []);

  const openDetail = async (id: string) => {
    setDetailOpen(true);
    setDetailLoading(true);
    try {
      const [p, stock] = await Promise.all([getProduct(id), getProductStock(id)]);
      setDetail(p);
      setDetailStock(stock);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải chi tiết');
    } finally {
      setDetailLoading(false);
    }
  };

  const openCreate = () => { setEditing(null); form.resetFields(); setFormOpen(true); };

  const openEdit = (p: Product) => {
    setEditing(p);
    form.setFieldsValue({
      name: p.name, note: p.note, giaBan: p.giaBan, giaNhap: p.giaNhap,
      tonToiThieu: p.tonToiThieu, tonToiDa: p.tonToiDa,
      nhomMatHangId: groups.find((g) => g.name === p.nhomMatHang)?.id,
      xuatXu: p.xuatXu, mauSac: p.mauSac, chatLieu: p.chatLieu,
      coHanSuDung: p.coHanSuDung, coSeri: p.coSeri, status: p.status
    });
    setFormOpen(true);
  };

  const submit = async () => {
    try {
      const v = await form.validateFields();
      setSaving(true);
      if (editing) {
        await updateProduct(editing.id, v);
        message.success('Đã cập nhật mặt hàng');
      } else {
        await createProduct(v);
        message.success('Đã thêm mặt hàng');
      }
      setFormOpen(false);
      await load();
    } catch (e: unknown) {
      if ((e as { errorFields?: unknown }).errorFields) return;
      message.error(apiErrMsg(e, 'Lỗi lưu mặt hàng'));
    } finally {
      setSaving(false);
    }
  };

  const confirmDelete = (p: Product) => {
    modal.confirm({
      title: `Xóa mặt hàng "${p.name}"?`,
      content: 'Mặt hàng sẽ bị ẩn khỏi danh sách (soft delete). Dữ liệu bán hàng không bị ảnh hưởng.',
      okType: 'danger', okText: 'Xóa', cancelText: 'Bỏ qua',
      onOk: async () => {
        try {
          await deleteProduct(p.id);
          message.success('Đã xóa mặt hàng');
          await load();
        } catch (e: unknown) {
          message.error(apiErrMsg(e, 'Lỗi xóa mặt hàng'));
        }
      }
    });
  };

  const columns: ColumnsType<Product> = useMemo(() => [
    { title: 'Mã', dataIndex: 'code', width: 130, sorter: true,
      render: (v: string | null) => <b>{v ?? '—'}</b> },
    { title: 'Tên mặt hàng', dataIndex: 'name', sorter: true },
    { title: 'Nhóm', dataIndex: 'nhomMatHang', width: 120, render: (v) => v ? <Tag>{v}</Tag> : '—' },
    { title: 'ĐVT', dataIndex: 'donViTinh', width: 80 },
    { title: 'Giá bán', dataIndex: 'giaBan', width: 120, align: 'right', sorter: true, render: fmtVnd },
    { title: 'Giá nhập', dataIndex: 'giaNhap', width: 120, align: 'right', render: fmtVnd },
    { title: 'Giá vốn', dataIndex: 'giaVon', width: 120, align: 'right', render: fmtVnd },
    { title: 'Trạng thái', dataIndex: 'status', width: 110, align: 'center',
      render: (v: number) => v === 30
        ? <Badge status="success" text="Hoạt động" />
        : <Badge status="default" text={`Không dùng (${v})`} /> },
    {
      title: '', key: 'act', width: 160, align: 'center',
      render: (_, r) => (
        <Space size={4}>
          <Button size="small" onClick={() => openDetail(r.id)}>Chi tiết</Button>
          {canUpdate && <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)}>Sửa</Button>}
          {canDelete && <Button size="small" danger onClick={() => confirmDelete(r)}>Xóa</Button>}
        </Space>
      )
    }
  ], [canUpdate, canDelete]);

  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      <Card size="small">
        <Space wrap>
          <Input
            allowClear placeholder="Tìm mã / tên mặt hàng..." prefix={<SearchOutlined />}
            style={{ width: 280 }} value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          />
          <Select
            allowClear placeholder="Nhóm mặt hàng" style={{ width: 180 }}
            value={groupId}
            onChange={(v) => { setGroupId(v); setPage(1); }}
            options={groups.map(g => ({ value: g.id, label: g.name }))}
          />
          <Select
            style={{ width: 150 }} value={activeOnly ? 'active' : 'all'}
            onChange={(v) => { setActiveOnly(v === 'active'); setPage(1); }}
            options={[
              { value: 'active', label: 'Chỉ hoạt động' },
              { value: 'all', label: 'Tất cả' }
            ]}
          />
          <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Tải lại</Button>
          {canCreate && (
            <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>
              Thêm mặt hàng
            </Button>
          )}
          {canCreate && (
            <label style={{ display: 'inline-block' }}>
              <input type="file" accept=".xlsx,.xls" style={{ display: 'none' }}
                onChange={async (e) => {
                  const f = e.target.files?.[0];
                  if (!f) return;
                  const fd = new FormData();
                  fd.append('file', f);
                  try {
                    const res = await http.post('/products/import', fd,
                      { headers: { 'Content-Type': 'multipart/form-data' } });
                    const env = res.data as { success: boolean; data?: ImportResultDto; message?: string };
                    if (env.success && env.data) {
                      const r = env.data;
                      void message.success(`Import: ${r.created} tạo mới, ${r.skipped} bỏ qua (trùng), ${r.errors.length} lỗi`);
                      load();
                    } else {
                      void message.error(env.message ?? 'Import thất bại');
                    }
                  } catch (e2: unknown) {
                    void message.error(e2 instanceof Error ? e2.message : 'Lỗi import');
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

      <Card size="small" title={`Danh mục mặt hàng (${total})`} styles={{ body: { padding: 0 } }}>
        <Table
          size="middle" rowKey="id" loading={loading}
          columns={columns} dataSource={items}
          pagination={{
            current: page, pageSize, total, showSizeChanger: true, showTotal: (t) => `Tổng ${t} mặt hàng`
          }}
          onChange={(p) => {
            setPage(p.current ?? 1);
            setPageSize(p.pageSize ?? 20);
          }}
          onRow={(r) => ({ onDoubleClick: () => openDetail(r.id), style: { cursor: 'pointer' } })}
        />
      </Card>

      <Drawer
        title="Chi tiết mặt hàng" width={480} open={detailOpen} onClose={() => setDetailOpen(false)}
      >
        {detailLoading ? <Spin /> : detail && (
          <>
            <Descriptions column={1} bordered size="small">
              <Descriptions.Item label="Mã">{detail.code ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Tên">{detail.name}</Descriptions.Item>
              <Descriptions.Item label="Nhóm">{detail.nhomMatHang ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="ĐVT">{detail.donViTinh ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Hãng sản xuất">{detail.hangSanXuat ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Xuất xứ">{detail.xuatXu ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Màu sắc">{detail.mauSac ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Chất liệu">{detail.chatLieu ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Giá bán">{fmtVnd(detail.giaBan)}</Descriptions.Item>
              <Descriptions.Item label="Giá nhập">{fmtVnd(detail.giaNhap)}</Descriptions.Item>
              <Descriptions.Item label="Giá vốn">{fmtVnd(detail.giaVon)}</Descriptions.Item>
              <Descriptions.Item label="Tồn tối thiểu">{detail.tonToiThieu}</Descriptions.Item>
              <Descriptions.Item label="Tồn tối đa">{detail.tonToiDa}</Descriptions.Item>
              <Descriptions.Item label="Có hạn sử dụng">{detail.coHanSuDung ? 'Có' : 'Không'}</Descriptions.Item>
              <Descriptions.Item label="Có seri">{detail.coSeri ? 'Có' : 'Không'}</Descriptions.Item>
              <Descriptions.Item label="Trạng thái">
                {detail.status === 30 ? <Tag color="green">Hoạt động</Tag> : <Tag>Không dùng ({detail.status})</Tag>}
              </Descriptions.Item>
            </Descriptions>

            <h4 style={{ marginTop: 16 }}>Tồn kho theo kho</h4>
            {detailStock.length === 0
              ? <Empty description="Chưa có dữ liệu tồn kho" />
              : (
                <Table
                  size="small" rowKey="wareHouseId" pagination={false}
                  dataSource={detailStock}
                  columns={[
                    { title: 'Kho', dataIndex: 'wareHouseName', render: (v) => v ?? '—' },
                    { title: 'Tồn', dataIndex: 'ton', align: 'right', render: (v: number) => v.toLocaleString('vi-VN') }
                  ]}
                />
              )}
            {canUpdate && (
              <Button style={{ marginTop: 12 }} icon={<EditOutlined />}
                onClick={() => { setDetailOpen(false); openEdit(detail); }}>
                Sửa mặt hàng
              </Button>
            )}
            <Button style={{ marginTop: 12, marginLeft: 8 }}
              onClick={() => openBarcode(detail.id)}
              disabled={!detail.code}>
              In tem mã vạch
            </Button>
          </>
        )}
      </Drawer>

      <Modal
        title={editing ? `Sửa mặt hàng — ${editing.name}` : 'Thêm mặt hàng'}
        open={formOpen} onCancel={() => setFormOpen(false)} onOk={submit}
        confirmLoading={saving} okText="Lưu" cancelText="Bỏ qua" width={680}
        destroyOnClose
      >
        <Form form={form} layout="vertical" initialValues={{ giaBan: 0, giaNhap: 0, tonToiThieu: 0, tonToiDa: 0 }}>
          <Form.Item name="name" label="Tên mặt hàng" rules={[{ required: true, max: 255, message: 'Tên bắt buộc, ≤ 255 ký tự' }]}>
            <Input placeholder="Tên mặt hàng" />
          </Form.Item>
          {!editing && (
            <Space wrap>
              <Form.Item name="code" label="Mã hàng" style={{ width: 200 }}><Input /></Form.Item>
              <Form.Item name="maSanCo" label="Mã sản cố / barcode" style={{ width: 220 }}><Input /></Form.Item>
            </Space>
          )}
          <Form.Item name="nhomMatHangId" label="Nhóm mặt hàng">
            <Select allowClear options={groups.map((g) => ({ value: g.id, label: g.name }))} />
          </Form.Item>
          <Space wrap>
            <Form.Item name="giaBan" label="Giá bán" style={{ width: 160 }}>
              <InputNumber min={0} style={{ width: '100%' }} formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')} />
            </Form.Item>
            <Form.Item name="giaNhap" label="Giá nhập" style={{ width: 160 }}>
              <InputNumber min={0} style={{ width: '100%' }} />
            </Form.Item>
          </Space>
          <Space wrap>
            <Form.Item name="tonToiThieu" label="Tồn tối thiểu" style={{ width: 150 }}>
              <InputNumber min={0} style={{ width: '100%' }} />
            </Form.Item>
            <Form.Item name="tonToiDa" label="Tồn tối đa" style={{ width: 150 }}>
              <InputNumber min={0} style={{ width: '100%' }} />
            </Form.Item>
          </Space>
          <Space wrap>
            <Form.Item name="xuatXu" label="Xuất xứ" style={{ width: 160 }}><Input /></Form.Item>
            <Form.Item name="mauSac" label="Màu sắc" style={{ width: 140 }}><Input /></Form.Item>
            <Form.Item name="chatLieu" label="Chất liệu" style={{ width: 160 }}><Input /></Form.Item>
          </Space>
          <Form.Item name="note" label="Ghi chú"><Input.TextArea rows={2} /></Form.Item>
          <Space wrap>
            <Form.Item name="coHanSuDung" valuePropName="checked"><Checkbox>Có hạn sử dụng</Checkbox></Form.Item>
            <Form.Item name="coSeri" valuePropName="checked"><Checkbox>Theo dõi seri</Checkbox></Form.Item>
          </Space>
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

export default ProductsPage;
