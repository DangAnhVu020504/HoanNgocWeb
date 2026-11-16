import React, { useCallback, useEffect, useState } from 'react';
import { Card, Col, Empty, Image, Input, Pagination, Row, Select, Skeleton, Space, Typography, message } from 'antd';
import dayjs from 'dayjs';
import { fetchMediaBlob, getMedia, getMediaReferenceTypes, type MediaItem } from '../../api/media';

const MediaThumb: React.FC<{ item: MediaItem }> = ({ item }) => {
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    let alive = true;
    let objectUrl: string | null = null;
    void fetchMediaBlob(item.url)
      .then((blob) => {
        objectUrl = URL.createObjectURL(blob);
        if (alive) setUrl(objectUrl);
      })
      .catch(() => undefined);
    return () => {
      alive = false;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [item.url]);

  if (!url) return <Skeleton.Image active style={{ width: '100%', height: 140 }} />;
  return (
    <Image
      src={url}
      height={140}
      style={{ objectFit: 'cover', borderRadius: 8 }}
      preview={{ mask: 'Xem ảnh' }}
    />
  );
};

const formatSize = (size: number | null): string => {
  if (!size) return '—';
  if (size < 1024) return `${size} B`;
  if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
  return `${(size / 1024 / 1024).toFixed(2)} MB`;
};

/** Thư viện media (Phase 8) — ảnh mặt hàng, logo, file đính kèm từ W_MEDIA. */
const MediaPage: React.FC = () => {
  const [items, setItems] = useState<MediaItem[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [referenceType, setReferenceType] = useState<string | undefined>();
  const [types, setTypes] = useState<string[]>([]);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async (p: number, s: string, rt?: string) => {
    setLoading(true);
    try {
      const result = await getMedia({ page: p, pageSize: 24, search: s || undefined, referenceType: rt });
      setItems(result.items);
      setTotal(result.total);
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải thư viện media');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { void load(page, search, referenceType); }, [load, page, search, referenceType]);

  useEffect(() => {
    getMediaReferenceTypes().then(setTypes).catch(() => undefined);
  }, []);

  return (
    <Card size="small" className="luxury-card" title={<span className="card-title-gold">Thư viện media ({total} file)</span>}
      extra={
        <Space wrap>
          <Input.Search
            allowClear
            placeholder="Tìm theo đường dẫn / mã tham chiếu"
            style={{ width: 260 }}
            onSearch={(v) => { setPage(1); setSearch(v.trim()); }}
          />
          <Select
            allowClear
            placeholder="Loại tham chiếu"
            style={{ width: 180 }}
            value={referenceType}
            options={types.map((t) => ({ value: t, label: t }))}
            onChange={(v) => { setPage(1); setReferenceType(v); }}
          />
        </Space>
      }>
      {loading ? <Skeleton active /> : items.length === 0 ? <Empty description="Chưa có file media" /> : (
        <>
          <Row gutter={[16, 16]}>
            {items.map((item) => (
              <Col key={item.id} xs={12} sm={8} md={6} lg={4}>
                <Card size="small" hoverable styles={{ body: { padding: 8 } }} style={{ borderRadius: 10 }}>
                  <MediaThumb item={item} />
                  <Typography.Text strong style={{ display: 'block', marginTop: 8, fontSize: 12 }}
                    ellipsis={{ tooltip: item.path }}>
                    {item.path.split(/[\\/]/).pop()}
                  </Typography.Text>
                  <Typography.Text type="secondary" style={{ fontSize: 11 }}>
                    {formatSize(item.size)} · {item.mimeType ?? '—'}
                  </Typography.Text>
                  <div style={{ fontSize: 11, color: '#A08D6A' }}>
                    {item.referenceType ?? 'Không tham chiếu'} · {dayjs(item.createdAt).format('DD/MM/YYYY')}
                  </div>
                </Card>
              </Col>
            ))}
          </Row>
          <div style={{ marginTop: 16, textAlign: 'right' }}>
            <Pagination current={page} pageSize={24} total={total} showSizeChanger={false}
              onChange={(p) => setPage(p)} />
          </div>
        </>
      )}
    </Card>
  );
};

export default MediaPage;
