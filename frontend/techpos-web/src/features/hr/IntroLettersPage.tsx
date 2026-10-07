import React, { useCallback, useEffect, useState } from 'react';
import { Button, Card, DatePicker, Descriptions, Form, Input, Modal, Space, Table, message } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import { getIntroLetters, createIntroLetter, deleteIntroLetter, type IntroLetter } from '../../api/hr';

const IntroLettersPage: React.FC = () => {
  const [rows, setRows] = useState<IntroLetter[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [saving, setSaving] = useState(false);
  const [detail, setDetail] = useState<IntroLetter | null>(null);
  const [form] = Form.useForm();

  const load = useCallback(async (p: number, s: string) => {
    setLoading(true);
    try {
      const r = await getIntroLetters({ search: s || undefined, page: p, pageSize: 20 });
      setRows(r.items); setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải giấy giới thiệu');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(1, ''); }, [load]);

  const save = async () => {
    const v = await form.validateFields();
    setSaving(true);
    try {
      await createIntroLetter({
        ngay: (v.ngay ?? dayjs()).format('YYYY-MM-DD'),
        hieuLucDen: v.hieuLucDen ? v.hieuLucDen.format('YYYY-MM-DD') : null,
        kinhGui: v.kinhGui || null,
        diaChi: v.diaChi || null,
        nguoiLienHe: v.nguoiLienHe || null,
        ongBa: v.ongBa,
        duocCuDen: v.duocCuDen,
        veViec: v.veViec || null,
        ghiChu: v.ghiChu || null,
      });
      void message.success('Đã tạo giấy giới thiệu');
      setOpen(false); form.resetFields();
      void load(page, search);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tạo giấy giới thiệu');
    } finally { setSaving(false); }
  };

  const remove = async (id: string) => {
    try { await deleteIntroLetter(id); void message.success('Đã xóa'); void load(page, search); }
    catch (e: unknown) { void message.error(e instanceof Error ? e.message : 'Lỗi xóa'); }
  };

  const openDetail = (r: IntroLetter) => setDetail(r);

  return (
    <Card size="small" title="Giấy giới thiệu" extra={
      <Space>
        <Input.Search allowClear placeholder="Tìm số phiếu / họ tên / nơi đến..." style={{ width: 260 }}
          onSearch={(v) => { setSearch(v); setPage(1); void load(1, v); }} />
        <Button type="primary" icon={<PlusOutlined />} onClick={() => { form.resetFields(); setOpen(true); }}>
          Tạo giấy giới thiệu
        </Button>
      </Space>
    }>
      <Table size="small" rowKey="id" loading={loading} dataSource={rows}
        pagination={{ current: page, pageSize: 20, total, showSizeChanger: false,
          onChange: (p) => { setPage(p); void load(p, search); } }}
        columns={[
          { title: 'Số phiếu', dataIndex: 'soPhieu', width: 130,
            render: (v: string, r: IntroLetter) => <Button type="link" size="small" style={{ padding: 0 }} onClick={() => openDetail(r)}>{v}</Button> },
          { title: 'Ngày', dataIndex: 'ngay', width: 110,
            render: (v: string) => v ? dayjs(v).format('DD/MM/YYYY') : '' },
          { title: 'Ông/bà', dataIndex: 'ongBa', width: 200 },
          { title: 'Được cử đến', dataIndex: 'duocCuDen' },
          { title: 'Về việc', dataIndex: 'veViec', ellipsis: true },
          { title: 'Hiệu lực đến', dataIndex: 'hieuLucDen', width: 120,
            render: (v: string) => v ? dayjs(v).format('DD/MM/YYYY') : '' },
          { title: '', width: 80,
            render: (_: unknown, r: IntroLetter) => (
              <Button size="small" type="link" danger onClick={() => void remove(r.id)}>Xóa</Button>
            ) },
        ]}
      />
      <Modal title="Tạo giấy giới thiệu" open={open} onCancel={() => setOpen(false)}
        onOk={async () => { await save(); }} confirmLoading={saving} width={620} destroyOnClose>
        <Form form={form} layout="vertical">
          <Space style={{ width: '100%' }} size={10}>
            <Form.Item name="ngay" label="Ngày" initialValue={dayjs()} rules={[{ required: true }]}>
              <DatePicker style={{ width: 140 }} />
            </Form.Item>
            <Form.Item name="hieuLucDen" label="Hiệu lực đến">
              <DatePicker style={{ width: 140 }} />
            </Form.Item>
          </Space>
          <Form.Item name="ongBa" label="Ông/bà (người được giới thiệu)" rules={[{ required: true, message: 'Nhập họ tên' }]}>
            <Input allowClear />
          </Form.Item>
          <Form.Item name="duocCuDen" label="Được cử đến" rules={[{ required: true, message: 'Nhập nơi đến' }]}>
            <Input allowClear />
          </Form.Item>
          <Form.Item name="kinhGui" label="Kính gửi"><Input allowClear /></Form.Item>
          <Form.Item name="diaChi" label="Địa chỉ"><Input allowClear /></Form.Item>
          <Form.Item name="nguoiLienHe" label="Người liên hệ"><Input allowClear /></Form.Item>
          <Form.Item name="veViec" label="Về việc"><Input.TextArea rows={2} /></Form.Item>
          <Form.Item name="ghiChu" label="Ghi chú"><Input.TextArea rows={2} /></Form.Item>
        </Form>
      </Modal>
      <Modal title={detail ? `Giấy giới thiệu ${detail.soPhieu}` : ''} open={!!detail}
        onCancel={() => setDetail(null)} footer={null} width={560}>
        {detail && (
          <Descriptions column={1} size="small" bordered>
            <Descriptions.Item label="Số phiếu">{detail.soPhieu}</Descriptions.Item>
            <Descriptions.Item label="Ngày">{detail.ngay ? dayjs(detail.ngay).format('DD/MM/YYYY') : ''}</Descriptions.Item>
            <Descriptions.Item label="Hiệu lực đến">{detail.hieuLucDen ? dayjs(detail.hieuLucDen).format('DD/MM/YYYY') : '—'}</Descriptions.Item>
            <Descriptions.Item label="Ông/bà">{detail.ongBa}</Descriptions.Item>
            <Descriptions.Item label="Được cử đến">{detail.duocCuDen}</Descriptions.Item>
            <Descriptions.Item label="Kính gửi">{detail.kinhGui}</Descriptions.Item>
            <Descriptions.Item label="Địa chỉ">{detail.diaChi}</Descriptions.Item>
            <Descriptions.Item label="Người liên hệ">{detail.nguoiLienHe}</Descriptions.Item>
            <Descriptions.Item label="Về việc">{detail.veViec}</Descriptions.Item>
            {detail.note && <Descriptions.Item label="Ghi chú">{detail.note}</Descriptions.Item>}
          </Descriptions>
        )}
      </Modal>
    </Card>
  );
};

export default IntroLettersPage;
