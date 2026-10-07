import React, { useCallback, useEffect, useState } from 'react';
import { Badge, Button, Card, Empty, List, Segmented, Space, message } from 'antd';
import { BellOutlined, CheckOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import {
  getNotifications, markNotificationRead, markAllNotificationsRead,
  type NotificationDto,
} from '../../api/notification';

const GROUP_COLORS: Record<string, string> = {
  SALES: '#d4a017', INVENTORY: '#08979c', DEBT: '#d46b08',
  COD: '#531dab', SYSTEM: '#434343', EMAIL: '#1d39c4',
};

/** Notification Center (Phase 7 — mục 25/47): All · Sales · Inventory · Debt · COD · System · Email. */
const NotificationsPage: React.FC = () => {
  const [rows, setRows] = useState<NotificationDto[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [group, setGroup] = useState('ALL');
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (p: number, g: string) => {
    setLoading(true);
    try {
      const r = await getNotifications({ page: p, pageSize: 20, group: g });
      setRows(r.items); setTotal(r.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải thông báo');
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { void load(page, group); }, [load, page, group]);

  const markRead = async (n: NotificationDto) => {
    if (n.isRead) return;
    try {
      await markNotificationRead(n.id);
      setRows((prev) => prev.map((r) => r.id === n.id ? { ...r, isRead: true } : r));
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi đánh dấu đã đọc');
    }
  };

  const markAll = async () => {
    try {
      await markAllNotificationsRead();
      setRows((prev) => prev.map((r) => ({ ...r, isRead: true })));
      void message.success('Đã đọc tất cả');
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi');
    }
  };

  return (
    <Card size="small" title={
      <Space>
        <BellOutlined /> Trung tâm thông báo
        <Segmented size="small" value={group} onChange={(v) => { setPage(1); setGroup(v as string); }}
          options={['ALL', 'SALES', 'INVENTORY', 'DEBT', 'COD', 'SYSTEM', 'EMAIL']} />
      </Space>
    } extra={<Button size="small" icon={<CheckOutlined />} onClick={() => void markAll()}>Đọc tất cả</Button>}>
      <List
        loading={loading}
        dataSource={rows}
        locale={{ emptyText: <Empty description="Không có thông báo" /> }}
        pagination={{ current: page, pageSize: 20, total, showSizeChanger: false,
          onChange: (p) => setPage(p) }}
        renderItem={(n) => (
          <List.Item
            style={{ cursor: 'pointer', background: n.isRead ? undefined : '#fffbe6', borderRadius: 6, paddingInline: 8 }}
            onClick={() => void markRead(n)}
            actions={n.isRead ? undefined : [<Badge key="new" status="processing" text="mới" />]}
          >
            <List.Item.Meta
              avatar={<Badge color={GROUP_COLORS[n.group] ?? '#434343'} text={n.group} />}
              title={n.title}
              description={
                <>
                  {n.description && <div>{n.description}</div>}
                  <div style={{ color: '#999', fontSize: 12 }}>{dayjs(n.createdAt).format('DD/MM/YYYY HH:mm')}</div>
                </>
              }
            />
          </List.Item>
        )}
      />
    </Card>
  );
};

export default NotificationsPage;
