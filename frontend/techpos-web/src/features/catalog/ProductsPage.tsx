import React, { useEffect, useMemo, useState } from 'react';
import {
  Alert, App, Badge, Button, Card, Checkbox, Col, Descriptions, Drawer, Empty, Form, Input,
  InputNumber, Modal, Row, Select, Space, Spin, Table, Tag, message
} from 'antd';
import {
  BarcodeOutlined, EditOutlined, PlusOutlined, ReloadOutlined,
  SearchOutlined, TagsOutlined, UploadOutlined
} from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import {
  createProduct, createProductGroup, deleteProduct, getProduct, getProductGroups, getProductStock,
  getProducts, getProductUnits, updateProduct,
  Product, ProductGroup, ProductStock, ProductUnit
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
  const [units, setUnits] = useState<ProductUnit[]>([]);
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

  const [groupModalOpen, setGroupModalOpen] = useState(false);
  const [groupName, setGroupName] = useState('');
  const [groupSaving, setGroupSaving] = useState(false);
  const [groupErr, setGroupErr] = useState<string | null>(null);

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
    getProductUnits().then(setUnits).catch(() => { /* ĐVT không bắt buộc */ });
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

  const openCreate = () => {
    setEditing(null);
    form.resetFields();
    setFormOpen(true);
  };

  const openEdit = (p: Product) => {
    setEditing(p);
    form.setFieldsValue({
      name: p.name,
      note: p.note,
      giaBan: p.giaBan,
      giaNhap: p.giaNhap,
      tonToiThieu: p.tonToiThieu,
      tonToiDa: p.tonToiDa,
      nhomMatHangId: p.nhomMatHangId ?? groups.find((g) => g.name === p.nhomMatHang)?.id,
      donViTinhId: p.donViTinhId ?? units.find((u) => u.name === p.donViTinh)?.id,
      xuatXu: p.xuatXu,
      mauSac: p.mauSac,
      chatLieu: p.chatLieu,
      coHanSuDung: p.coHanSuDung,
      coSeri: p.coSeri,
      status: p.status,
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

  const openGroupManager = () => {
    setGroupErr(null);
    setGroupModalOpen(true);
  };

  const saveGroup = async () => {
    const name = groupName.trim();
    if (!name) {
      void message.warning('Nhập tên nhóm mặt hàng');
      return;
    }
    setGroupSaving(true);
    setGroupErr(null);
    try {
      const created = await createProductGroup({ name });
      void message.success(`Đã tạo nhóm "${created.name}"`);
      setGroupName('');
      const list = await getProductGroups();
      setGroups(list);
      if (formOpen) form.setFieldValue('nhomMatHangId', created.id);
    } catch (e: unknown) {
      setGroupErr(apiErrMsg(e, 'Lỗi tạo nhóm mặt hàng'));
    } finally {
      setGroupSaving(false);
    }
  };

  const confirmDelete = (p: Product) => {
    modal.confirm({
      title: `Xóa mặt hàng "${p.name}"?`,
      content: 'Mặt hàng sẽ bị ẩn khỏi danh sách (soft delete). Dữ liệu bán hàng không bị ảnh hưởng.',
      okType: 'danger',
      okText: 'Xóa',
      cancelText: 'Bỏ qua',
      onOk: async () => {
        try {
          await deleteProduct(p.id);
          message.success('Đã xóa mặt hàng');
          await load();
        } catch (e: unknown) {
          message.error(apiErrMsg(e, 'Lỗi xóa mặt hàng'));
        }
      },
    });
  };

  const columns: ColumnsType<Product> = useMemo(() => [
    {
      title: 'Mã hàng',
      dataIndex: 'code',
      width: 130,
      sorter: true,
      render: (v: string | null) => <span className="code-badge">{v ?? '—'}</span>,
    },
    {
      title: 'Tên mặt hàng',
      dataIndex: 'name',
      sorter: true,
      render: (v: string) => <b style={{ color: '#1C180E' }}>{v}</b>,
    },
    {
      title: 'Nhóm',
      dataIndex: 'nhomMatHang',
      width: 140,
      render: (v) => v ? <Tag color="gold">{v}</Tag> : '—',
    },
    { title: 'ĐVT', dataIndex: 'donViTinh', width: 75, align: 'center' },
    {
      title: 'Giá bán lẻ',
      dataIndex: 'giaBan',
      width: 130,
      align: 'right',
      sorter: true,
      render: (v) => <b style={{ color: '#8A6100', fontFamily: 'Outfit' }}>{fmtVnd(v)}</b>,
    },
    {
      title: 'Giá nhập',
      dataIndex: 'giaNhap',
      width: 120,
      align: 'right',
      render: (v) => <span style={{ fontFamily: 'Outfit' }}>{fmtVnd(v)}</span>,
    },
    {
      title: 'Giá vốn',
      dataIndex: 'giaVon',
      width: 120,
      align: 'right',
      render: (v) => <span style={{ fontFamily: 'Outfit' }}>{fmtVnd(v)}</span>,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      width: 120,
      align: 'center',
      render: (v: number) => v === 30
        ? <Badge status="success" text="Hoạt động" />
        : <Badge status="default" text={`Không dùng (${v})`} />,
    },
    {
      title: 'Thao tác',
      key: 'act',
      width: 170,
      align: 'center',
      render: (_, r) => (
        <Space size={6}>
          <Button size="small" onClick={() => openDetail(r.id)}>Chi tiết</Button>
          {canUpdate && <Button size="small" icon={<EditOutlined />} onClick={() => openEdit(r)}>Sửa</Button>}
          {canDelete && <Button size="small" danger onClick={() => confirmDelete(r)}>Xóa</Button>}
        </Space>
      ),
    },
  ], [canUpdate, canDelete]);

  return (
    <Space direction="vertical" size={16} style={{ width: '100%' }}>
      {/* THANH CÔNG CỤ TÌM KIẾM & BỘ LỌC */}
      <Card className="luxury-card">
        <Space wrap size={12}>
          <Input
            allowClear
            placeholder="Tìm mã / tên mặt hàng..."
            prefix={<SearchOutlined style={{ color: '#D4AF37' }} />}
            style={{ width: 280, borderRadius: 8 }}
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          />
          <Select
            allowClear
            placeholder="Tất cả nhóm hàng"
            style={{ width: 190 }}
            value={groupId}
            onChange={(v) => { setGroupId(v); setPage(1); }}
            options={groups.map(g => ({ value: g.id, label: g.name }))}
          />
          <Select
            style={{ width: 160 }}
            value={activeOnly ? 'active' : 'all'}
            onChange={(v) => { setActiveOnly(v === 'active'); setPage(1); }}
            options={[
              { value: 'active', label: 'Chỉ hoạt động' },
              { value: 'all', label: 'Hiển thị tất cả' },
            ]}
          />
          <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Tải lại</Button>
          {canCreate && (
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={openCreate}
              className="gold-gradient-btn"
            >
              Thêm mặt hàng
            </Button>
          )}
          {canCreate && (
            <Button icon={<TagsOutlined />} onClick={openGroupManager}>
              Nhóm mặt hàng
            </Button>
          )}
          {canCreate && (
            <label style={{ display: 'inline-block' }}>
              <input
                type="file"
                accept=".xlsx,.xls"
                style={{ display: 'none' }}
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
                      void message.success(`Import: ${r.created} tạo mới, ${r.skipped} bỏ qua, ${r.errors.length} lỗi`);
                      load();
                    } else {
                      void message.error(env.message ?? 'Import thất bại');
                    }
                  } catch (e2: unknown) {
                    void message.error(e2 instanceof Error ? e2.message : 'Lỗi import');
                  } finally {
                    e.target.value = '';
                  }
                }}
              />
              <Button
                icon={<UploadOutlined />}
                onClick={(e) => {
                  (e.currentTarget.previousSibling as HTMLInputElement)?.click();
                }}
              >
                Nhập Excel
              </Button>
            </label>
          )}
        </Space>
      </Card>

      {err && <Alert type="error" showIcon message={err} closable style={{ borderRadius: 8 }} />}

      <Card
        className="luxury-card"
        title={
          <span style={{ color: '#1C180E', fontWeight: 800, fontSize: 16 }}>
            Danh Mục Mặt Hàng ({total.toLocaleString('vi-VN')} sản phẩm)
          </span>
        }
        styles={{ body: { padding: 0 } }}
      >
        <Table
          className="luxury-table"
          size="middle"
          rowKey="id"
          loading={loading}
          columns={columns}
          dataSource={items}
          pagination={{
            current: page,
            pageSize,
            total,
            showSizeChanger: true,
            showTotal: (t) => `Tổng ${t.toLocaleString('vi-VN')} mặt hàng`,
          }}
          onChange={(p) => {
            setPage(p.current ?? 1);
            setPageSize(p.pageSize ?? 20);
          }}
          onRow={(r) => ({
            onDoubleClick: () => openDetail(r.id),
            style: { cursor: 'pointer' },
          })}
        />
      </Card>

      {/* DRAWER CHI TIẾT MẶT HÀNG & TỒN THEO KHO */}
      <Drawer
        title={<span style={{ color: '#8A6100', fontWeight: 800 }}>Chi Tiết Mặt Hàng</span>}
        width={500}
        open={detailOpen}
        onClose={() => setDetailOpen(false)}
      >
        {detailLoading ? <Spin /> : detail && (
          <Space direction="vertical" size={14} style={{ width: '100%' }}>
            <Descriptions column={1} bordered size="small">
              <Descriptions.Item label="Mã hàng"><span className="code-badge">{detail.code ?? '—'}</span></Descriptions.Item>
              <Descriptions.Item label="Tên mặt hàng"><b>{detail.name}</b></Descriptions.Item>
              <Descriptions.Item label="Nhóm hàng">{detail.nhomMatHang ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Đơn vị tính">{detail.donViTinh ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Hãng sản xuất">{detail.hangSanXuat ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Xuất xứ">{detail.xuatXu ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Màu sắc">{detail.mauSac ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Chất liệu">{detail.chatLieu ?? '—'}</Descriptions.Item>
              <Descriptions.Item label="Giá bán lẻ"><b style={{ color: '#8A6100', fontFamily: 'Outfit' }}>{fmtVnd(detail.giaBan)}</b></Descriptions.Item>
              <Descriptions.Item label="Giá nhập">{fmtVnd(detail.giaNhap)}</Descriptions.Item>
              <Descriptions.Item label="Giá vốn">{fmtVnd(detail.giaVon)}</Descriptions.Item>
              <Descriptions.Item label="Tồn định mức">{detail.tonToiThieu} ~ {detail.tonToiDa}</Descriptions.Item>
              <Descriptions.Item label="Trạng thái">
                {detail.status === 30 ? <Tag color="green">Hoạt động</Tag> : <Tag>Ngừng ({detail.status})</Tag>}
              </Descriptions.Item>
            </Descriptions>

            <h4 style={{ margin: '14px 0 6px', color: '#8A6100', fontWeight: 700 }}>
              Tồn Kho Tại Các Chi Nhánh
            </h4>
            {detailStock.length === 0 ? (
              <Empty description="Chưa có dữ liệu tồn kho" />
            ) : (
              <Table
                className="luxury-table"
                size="small"
                rowKey="wareHouseId"
                pagination={false}
                dataSource={detailStock}
                columns={[
                  { title: 'Tên kho hàng', dataIndex: 'wareHouseName', render: (v) => <b>{v ?? '—'}</b> },
                  {
                    title: 'Số lượng tồn',
                    dataIndex: 'ton',
                    align: 'right',
                    render: (v: number) => (
                      <b style={{ color: v > 0 ? '#10B981' : '#E11D48', fontFamily: 'Outfit' }}>
                        {v.toLocaleString('vi-VN')}
                      </b>
                    ),
                  },
                ]}
              />
            )}

            <Space style={{ marginTop: 12 }}>
              {canUpdate && (
                <Button
                  className="gold-gradient-btn"
                  icon={<EditOutlined />}
                  onClick={() => { setDetailOpen(false); openEdit(detail); }}
                >
                  Sửa mặt hàng
                </Button>
              )}
              <Button
                icon={<BarcodeOutlined />}
                onClick={() => openBarcode(detail.id)}
                disabled={!detail.code}
              >
                In tem mã vạch
              </Button>
            </Space>
          </Space>
        )}
      </Drawer>

      {/* MODAL THÊM / SỬA MẶT HÀNG */}
      <Modal
        title={<span style={{ color: '#8A6100', fontWeight: 800 }}>{editing ? `Sửa Mặt Hàng — ${editing.name}` : 'Thêm Mặt Hàng Mới'}</span>}
        open={formOpen}
        onCancel={() => setFormOpen(false)}
        onOk={submit}
        confirmLoading={saving}
        okText="Lưu dữ liệu"
        cancelText="Đóng"
        width={700}
        destroyOnClose
      >
        <Form form={form} layout="vertical" initialValues={{ giaBan: 0, giaNhap: 0, tonToiThieu: 0, tonToiDa: 0 }}>
          <Form.Item name="name" label="Tên mặt hàng" rules={[{ required: true, max: 255, message: 'Tên bắt buộc, ≤ 255 ký tự' }]}>
            <Input placeholder="Tên mặt hàng" />
          </Form.Item>
          {!editing && (
            <Row gutter={12}>
              <Col span={12}>
                <Form.Item name="code" label="Mã hàng"><Input placeholder="Để trống để tự sinh (VD: SP000001)" /></Form.Item>
              </Col>
              <Col span={12}>
                <Form.Item name="maSanCo" label="Mã vạch / Barcode"><Input placeholder="Quét mã vạch có sẵn" /></Form.Item>
              </Col>
            </Row>
          )}
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item label="Nhóm mặt hàng" style={{ marginBottom: 24 }}>
                <Space.Compact style={{ width: '100%' }}>
                  <Form.Item name="nhomMatHangId" noStyle>
                    <Select
                      allowClear
                      showSearch
                      optionFilterProp="label"
                      placeholder="Chọn nhóm hàng"
                      options={groups.map((g) => ({ value: g.id, label: g.name }))}
                      style={{ width: '100%' }}
                    />
                  </Form.Item>
                  {canCreate && (
                    <Button
                      icon={<PlusOutlined />}
                      title="Tạo nhóm mặt hàng mới"
                      onClick={openGroupManager}
                    />
                  )}
                </Space.Compact>
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="donViTinhId" label="Đơn vị tính">
                <Select
                  allowClear
                  showSearch
                  optionFilterProp="label"
                  placeholder="Chọn đơn vị tính"
                  options={units.map((u) => ({ value: u.id, label: u.name }))}
                />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="giaBan" label="Giá bán lẻ (VNĐ)">
                <InputNumber min={0} style={{ width: '100%' }} formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="giaNhap" label="Giá nhập (VNĐ)">
                <InputNumber min={0} style={{ width: '100%' }} formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')} />
              </Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={12}>
              <Form.Item name="tonToiThieu" label="Tồn tối thiểu (báo động sắp hết)"><InputNumber min={0} style={{ width: '100%' }} /></Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="tonToiDa" label="Tồn tối đa"><InputNumber min={0} style={{ width: '100%' }} /></Form.Item>
            </Col>
          </Row>
          <Row gutter={12}>
            <Col span={8}><Form.Item name="xuatXu" label="Xuất xứ"><Input /></Form.Item></Col>
            <Col span={8}><Form.Item name="mauSac" label="Màu sắc"><Input /></Form.Item></Col>
            <Col span={8}><Form.Item name="chatLieu" label="Chất liệu"><Input /></Form.Item></Col>
          </Row>
          <Form.Item name="note" label="Ghi chú"><Input.TextArea rows={2} /></Form.Item>
          <Space wrap size={16}>
            <Form.Item name="coHanSuDung" valuePropName="checked" noStyle><Checkbox>Có hạn sử dụng</Checkbox></Form.Item>
            <Form.Item name="coSeri" valuePropName="checked" noStyle><Checkbox>Theo dõi seri</Checkbox></Form.Item>
          </Space>
          {editing && (
            <Form.Item name="status" label="Trạng thái" style={{ width: 200, marginTop: 12 }}>
              <Select options={[{ value: 30, label: 'Hoạt động' }, { value: 0, label: 'Ngừng hoạt động' }]} />
            </Form.Item>
          )}
        </Form>
      </Modal>

      {/* MODAL QUẢN LÝ / TẠO NHÓM MẶT HÀNG */}
      <Modal
        title={<span style={{ color: '#8A6100', fontWeight: 800 }}>Nhóm Mặt Hàng</span>}
        open={groupModalOpen}
        onCancel={() => setGroupModalOpen(false)}
        footer={null}
        width={560}
      >
        <Space direction="vertical" size={14} style={{ width: '100%' }}>
          {groupErr && <Alert type="error" showIcon message={groupErr} closable onClose={() => setGroupErr(null)} />}
          {canCreate && (
            <Space.Compact style={{ width: '100%' }}>
              <Input
                autoFocus
                allowClear
                maxLength={255}
                placeholder="Tên nhóm mới (VD: TẠP HÓA)"
                value={groupName}
                onChange={(e) => setGroupName(e.target.value)}
                onPressEnter={() => void saveGroup()}
              />
              <Button
                type="primary"
                icon={<PlusOutlined />}
                loading={groupSaving}
                className="gold-gradient-btn"
                onClick={() => void saveGroup()}
              >
                Thêm nhóm
              </Button>
            </Space.Compact>
          )}
          <div>
            <div style={{ fontSize: 12, color: '#7E7359', marginBottom: 8 }}>
              Danh sách nhóm hiện có ({groups.length})
            </div>
            {groups.length === 0 ? (
              <Empty description="Chưa có nhóm mặt hàng" />
            ) : (
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, maxHeight: 240, overflowY: 'auto' }}>
                {groups.map((g) => (
                  <Tag key={g.id} color="gold" style={{ marginInlineEnd: 0 }}>{g.name}</Tag>
                ))}
              </div>
            )}
          </div>
        </Space>
      </Modal>
    </Space>
  );
};

export default ProductsPage;
