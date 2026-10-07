import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Card, Input, Pagination, Select, Space, Table, message } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import { getWarehouses, type Warehouse } from '../../api/warehouses';
import { getAllWarehouseStock, type AllWarehouseStock, type WarehouseStockCell } from '../../api/inventory';

const fmtQty = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 3 });
const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

const PAGE_SIZE = 50;

/** Tồn tất cả các kho (mục 4.8) — ma trận mặt hàng × kho, cột sinh động theo danh sách kho. */
const AllWarehousesPage: React.FC = () => {
  const [warehouses, setWarehouses] = useState<Warehouse[]>([]);
  const [rows, setRows] = useState<AllWarehouseStock[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [nonZeroOnly, setNonZeroOnly] = useState(true);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    getWarehouses({ activeOnly: true, pageSize: 100 }).then((r) => setWarehouses(r.items)).catch(() => { });
  }, []);

  const load = useCallback(async (p: number, s: string, nz: boolean) => {
    setLoading(true);
    try {
      const res = await getAllWarehouseStock({ search: s || undefined, nonZeroOnly: nz, page: p, pageSize: PAGE_SIZE });
      setRows(res.items);
      setTotal(res.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải tồn tất cả các kho');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(1, '', true); }, [load]);

  const onSearch = (value: string) => {
    setSearch(value);
    setPage(1);
    void load(1, value, nonZeroOnly);
  };
  const onNonZeroChange = (nz: boolean) => {
    setNonZeroOnly(nz);
    setPage(1);
    void load(1, search, nz);
  };
  const onPageChange = (p: number) => {
    setPage(p);
    void load(p, search, nonZeroOnly);
  };

  const tonOf = (row: AllWarehouseStock, khoId: string): number =>
    row.stocks.find((c: WarehouseStockCell) => c.khoId === khoId)?.ton ?? 0;

  const columns = useMemo(() => {
    const base = [
      { title: 'Mã hàng', dataIndex: 'code', width: 110 },
      { title: 'Mặt hàng', dataIndex: 'name' },
      { title: 'ĐVT', dataIndex: 'donViTinh', width: 70 },
    ];
    const khoCols = warehouses.map((w) => ({
      title: w.name,
      key: `kho-${w.id}`,
      width: 110,
      align: 'right' as const,
      render: (_: unknown, row: AllWarehouseStock) => fmtQty(tonOf(row, w.id)),
    }));
    const tail = [
      { title: 'Tổng tồn', dataIndex: 'tongTon', width: 110, align: 'right' as const, render: fmtQty },
      { title: 'Giá vốn', dataIndex: 'giaVon', width: 120, align: 'right' as const, render: fmtVnd },
      { title: 'Giá trị tồn', dataIndex: 'giaTriTon', width: 140, align: 'right' as const, render: fmtVnd },
    ];
    return [...base, ...khoCols, ...tail];
  }, [warehouses]);

  return (
    <Card size="small" title="Tồn tất cả các kho" extra={
      <Space>
        <Select value={nonZeroOnly} onChange={onNonZeroChange} style={{ width: 190 }} options={[
          { value: true, label: 'Chỉ hàng có tồn ≠ 0' },
          { value: false, label: 'Tất cả mặt hàng' },
        ]} />
        <Input allowClear prefix={<SearchOutlined />} placeholder="Tìm mã / tên hàng..."
          style={{ width: 220 }} value={search} onChange={(e) => onSearch(e.target.value)} />
      </Space>
    }>
      <Table
        size="small" rowKey="matHangId" loading={loading} dataSource={rows} columns={columns}
        pagination={false} scroll={{ x: 'max-content' }}
      />
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: 12 }}>
        <Pagination current={page} pageSize={PAGE_SIZE} total={total}
          showTotal={(t) => `Tổng ${t} mặt hàng`} onChange={onPageChange} />
      </div>
    </Card>
  );
};

export default AllWarehousesPage;