import React, { useCallback, useEffect, useState } from 'react';
import { Card, Input, Table, Tabs, message } from 'antd';
import {
  getCustomerDebts, getSupplierDebts, type CustomerDebt, type SupplierDebt,
} from '../../api/debts';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

const CustomerDebts: React.FC = () => {
  const [rows, setRows] = useState<CustomerDebt[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (p: number, s: string) => {
    setLoading(true);
    try {
      const r = await getCustomerDebts({ search: s || undefined, page: p, pageSize: 50 });
      setRows(r.items); setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải công nợ khách hàng');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(1, ''); }, [load]);

  return (
    <Card size="small" title="Công nợ khách hàng" extra={
      <Input.Search allowClear placeholder="Tìm mã / tên khách..." style={{ width: 260 }}
        onSearch={(v) => { setSearch(v); setPage(1); void load(1, v); }} />
    }>
      <Table size="small" rowKey="khachHangId" loading={loading} dataSource={rows}
        pagination={{ current: page, pageSize: 50, total, showSizeChanger: false,
          onChange: (p) => { setPage(p); void load(p, search); } }}
        columns={[
          { title: 'Mã khách', dataIndex: 'maKhach', width: 120 },
          { title: 'Tên khách hàng', dataIndex: 'tenKhach' },
          { title: 'Điện thoại', dataIndex: 'dienThoai', width: 130 },
          { title: 'Số hóa đơn nợ', dataIndex: 'soHoaDon', width: 120, align: 'right' },
          { title: 'Công nợ', dataIndex: 'congNo', width: 160, align: 'right',
            render: (v: number) => <b style={{ color: v > 0 ? '#cf1322' : undefined }}>{fmtVnd(v)}</b> },
        ]}
        summary={() => rows.length > 0 ? (
          <Table.Summary.Row>
            <Table.Summary.Cell index={0} colSpan={4}><b>TỔNG {total} KHÁCH NỢ</b></Table.Summary.Cell>
            <Table.Summary.Cell index={1} align="right">
              <b style={{ color: '#cf1322' }}>{fmtVnd(rows.reduce((s, r) => s + r.congNo, 0))}</b>
            </Table.Summary.Cell>
          </Table.Summary.Row>
        ) : null}
      />
    </Card>
  );
};

const SupplierDebts: React.FC = () => {
  const [rows, setRows] = useState<SupplierDebt[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (p: number, s: string) => {
    setLoading(true);
    try {
      const r = await getSupplierDebts({ search: s || undefined, page: p, pageSize: 50 });
      setRows(r.items); setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải công nợ NCC');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(1, ''); }, [load]);

  return (
    <Card size="small" title="Công nợ nhà cung cấp" extra={
      <Input.Search allowClear placeholder="Tìm mã / tên NCC..." style={{ width: 260 }}
        onSearch={(v) => { setSearch(v); setPage(1); void load(1, v); }} />
    }>
      <Table size="small" rowKey="nhaCungCapId" loading={loading} dataSource={rows}
        pagination={{ current: page, pageSize: 50, total, showSizeChanger: false,
          onChange: (p) => { setPage(p); void load(p, search); } }}
        columns={[
          { title: 'Mã NCC', dataIndex: 'maNhaCungCap', width: 120 },
          { title: 'Tên nhà cung cấp', dataIndex: 'tenNhaCungCap' },
          { title: 'Điện thoại', dataIndex: 'dienThoai', width: 130 },
          { title: 'Số phiếu nợ', dataIndex: 'soPhieu', width: 120, align: 'right' },
          { title: 'Công nợ', dataIndex: 'congNo', width: 160, align: 'right',
            render: (v: number) => <b style={{ color: v > 0 ? '#cf1322' : undefined }}>{fmtVnd(v)}</b> },
        ]}
        summary={() => rows.length > 0 ? (
          <Table.Summary.Row>
            <Table.Summary.Cell index={0} colSpan={4}><b>TỔNG {total} NCC NỢ</b></Table.Summary.Cell>
            <Table.Summary.Cell index={1} align="right">
              <b style={{ color: '#cf1322' }}>{fmtVnd(rows.reduce((s, r) => s + r.congNo, 0))}</b>
            </Table.Summary.Cell>
          </Table.Summary.Row>
        ) : null}
      />
    </Card>
  );
};

/** Công nợ (mục 5.2) — công nợ khách hàng / nhà cung cấp từ TDONHANG.CONNO/CONLAI. */
const DebtsPage: React.FC = () => (
  <Tabs items={[
    { key: 'kh', label: 'Công nợ khách hàng', children: <CustomerDebts /> },
    { key: 'ncc', label: 'Công nợ nhà cung cấp', children: <SupplierDebts /> },
  ]} />
);

export default DebtsPage;