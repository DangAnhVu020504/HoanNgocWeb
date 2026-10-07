import React, { useEffect, useState } from 'react';
import { Alert, Badge, Card, Input, Select, Space, Table, Tag } from 'antd';
import { SearchOutlined, HistoryOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { getHistory, type HistoryItem } from '../../api/history';

const fmtVnd = (n: number | null | undefined) =>
  n === null || n === undefined ? '—' : n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

const nhomColor = (nhom: string): string =>
  nhom === 'Bán hàng' ? 'gold' :
  nhom === 'Kho hàng' ? 'blue' :
  nhom === 'Quỹ' ? 'green' :
  nhom === 'Vận chuyển' ? 'cyan' :
  nhom === 'Mua hàng' ? 'purple' :
  'magenta';

const statusBadge = (v: number) => v === 30
  ? <Badge status="success" text="Hoạt động" />
  : <Badge status="default" text={`Ngừng (${v})`} />;

const fmtTime = (iso: string | null) => {
  if (!iso) return '—';
  const d = new Date(iso);
  return d.toLocaleString('vi-VN', { hour12: false });
};

/** Lịch sử — toàn bộ hoá đơn / phiếu / hợp đồng đã tạo trên web (W_ORIGIN='WEB'). */
const HistoryPage: React.FC = () => {
  const [allItems, setAllItems] = useState<HistoryItem[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [nhom, setNhom] = useState<string | undefined>(undefined);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  const load = async () => {
    try {
      setErr(null);
      setLoading(true);
      const res = await getHistory({ search, page: 1, pageSize: 500 });
      setAllItems(res.items);
      setTotal(res.total);
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tải lịch sử');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); /* eslint-disable-line react-hooks/exhaustive-deps */ }, [search]);

  // Lọc nhóm + phân trang phía client (dữ liệu đã tải đủ)
  const filtered = nhom ? allItems.filter((r) => r.nhom === nhom) : allItems;
  const paged = filtered.slice((page - 1) * pageSize, page * pageSize);

  const columns: ColumnsType<HistoryItem> = [
    { title: 'Số phiếu', dataIndex: 'soPhieu', width: 130, render: (v) => <b style={{ color: '#8a6100' }}>{v}</b> },
    { title: 'Loại', dataIndex: 'loai', width: 170 },
    { title: 'Nhóm', dataIndex: 'nhom', width: 110, render: (v) => <Tag color={nhomColor(v)}>{v}</Tag> },
    { title: 'Ngày chứng từ', dataIndex: 'ngay', width: 110, render: (v: string | null) => v ? new Date(v).toLocaleDateString('vi-VN') : '—' },
    { title: 'Thời gian tạo', dataIndex: 'timeCreated', width: 150, render: fmtTime },
    { title: 'Người tạo', dataIndex: 'nguoiTao', width: 110, render: (v) => v ? <Tag>{v}</Tag> : '—' },
    { title: 'Tiền', dataIndex: 'tien', width: 130, align: 'right', render: fmtVnd },
    { title: 'Diễn giải', dataIndex: 'dienGiai', ellipsis: true },
    { title: 'Trạng thái', dataIndex: 'status', width: 110, align: 'center', render: statusBadge },
  ];

  const nhomOptions = Array.from(new Set(allItems.map((i) => i.nhom)));

  return (
    <Space direction="vertical" size={12} style={{ width: '100%' }}>
      <Card size="small">
        <Space wrap>
          <Input allowClear placeholder="Tìm số phiếu / người tạo / diễn giải..." prefix={<SearchOutlined />}
            style={{ width: 300 }} value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
          <Select allowClear placeholder="Lọc nhóm" style={{ width: 160 }} value={nhom}
            onChange={(v) => { setNhom(v); setPage(1); }}
            options={nhomOptions.map((n) => ({ value: n, label: n }))} />
          <span style={{ color: '#888', fontSize: 13 }}>
            <HistoryOutlined /> Tổng <b>{filtered.length}</b>{nhom ? ` (${nhom})` : ''} / {total} chứng từ
          </span>
        </Space>
      </Card>
      {err && <Alert type="error" showIcon message={err} closable />}
      <Card size="small" title={`Lịch sử chứng từ (${filtered.length}/${total})`} styles={{ body: { padding: 0 } }}>
        <Table size="middle" rowKey="id" loading={loading} columns={columns} dataSource={paged}
          pagination={{
            current: page, pageSize, total: filtered.length, showSizeChanger: true,
            showTotal: (t) => `Tổng ${t} chứng từ`, pageSizeOptions: [20, 50, 100, 200]
          }}
          onChange={(p) => { setPage(p.current ?? 1); setPageSize(p.pageSize ?? 20); }} />
      </Card>
    </Space>
  );
};

export default HistoryPage;
