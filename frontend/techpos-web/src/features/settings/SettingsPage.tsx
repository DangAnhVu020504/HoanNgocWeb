import React, { useCallback, useEffect, useState } from 'react';
import { Alert, Button, Card, Col, Form, Input, InputNumber, Row, Skeleton, Space, Tag, message } from 'antd';
import { SaveOutlined } from '@ant-design/icons';
import { getSettings, saveSettings } from '../../api/settings';
import { getIntegrationStatus, type IntegrationsStatus } from '../../api/integrations';
import { useAuthStore } from '../../stores/authStore';

const SETTING_KEYS = [
  'COMPANY_NAME', 'COMPANY_TAX_CODE', 'COMPANY_PHONE', 'COMPANY_EMAIL', 'COMPANY_ADDRESS',
  'COMPANY_WEBSITE', 'COMPANY_BANK_ACCOUNT', 'COMPANY_BANK_NAME', 'COMPANY_INVOICE_FOOTER',
  'LOYALTY_VND_PER_POINT', 'LOYALTY_POINT_VALUE', 'CRM_CARE_REMIND_DAYS',
];

/** Cài đặt hệ thống (Phase 8) — thông tin công ty dùng cho PDF + tham số điểm thân thiết. */
const SettingsPage: React.FC = () => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [integrations, setIntegrations] = useState<IntegrationsStatus | null>(null);
  const canEdit = useAuthStore((s) => s.hasPermission('ADMIN.SETTINGS.MANAGE'));

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const rows = await getSettings();
      const map = new Map(rows.map((r) => [r.key, r.value]));
      form.setFieldsValue({
        COMPANY_NAME: map.get('COMPANY_NAME') ?? '',
        COMPANY_TAX_CODE: map.get('COMPANY_TAX_CODE') ?? '',
        COMPANY_PHONE: map.get('COMPANY_PHONE') ?? '',
        COMPANY_EMAIL: map.get('COMPANY_EMAIL') ?? '',
        COMPANY_ADDRESS: map.get('COMPANY_ADDRESS') ?? '',
        COMPANY_WEBSITE: map.get('COMPANY_WEBSITE') ?? '',
        COMPANY_BANK_ACCOUNT: map.get('COMPANY_BANK_ACCOUNT') ?? '',
        COMPANY_BANK_NAME: map.get('COMPANY_BANK_NAME') ?? '',
        COMPANY_INVOICE_FOOTER: map.get('COMPANY_INVOICE_FOOTER') ?? '',
        LOYALTY_VND_PER_POINT: Number(map.get('LOYALTY_VND_PER_POINT') ?? 10000),
        LOYALTY_POINT_VALUE: Number(map.get('LOYALTY_POINT_VALUE') ?? 1),
        CRM_CARE_REMIND_DAYS: Number(map.get('CRM_CARE_REMIND_DAYS') ?? 7),
      });
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi tải cài đặt');
    } finally {
      setLoading(false);
    }
    if (canEdit) {
      try { setIntegrations(await getIntegrationStatus()); } catch { setIntegrations(null); }
    }
  }, [form, canEdit]);

  useEffect(() => { void load(); }, [load]);

  const save = async () => {
    const v = await form.validateFields();
    const payload: Record<string, string | null> = {};
    for (const key of SETTING_KEYS) {
      const raw = v[key];
      payload[key] = raw === undefined || raw === null || raw === '' ? null : String(raw);
    }
    setSaving(true);
    try {
      await saveSettings(payload);
      void message.success('Đã lưu cài đặt hệ thống');
      void load();
    } catch (e: unknown) {
      void message.error(e instanceof Error ? e.message : 'Lỗi lưu cài đặt');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <Skeleton active />;

  return (
    <Form form={form} layout="vertical" disabled={!canEdit}>
      <Card size="small" className="luxury-card" title={<span className="card-title-gold">Thông tin công ty (hiển thị trên hoá đơn / phiếu in)</span>}
        style={{ marginBottom: 16 }}>
        <Row gutter={16}>
          <Col xs={24} md={12}>
            <Form.Item name="COMPANY_NAME" label="Tên công ty" rules={[{ required: true }]}>
              <Input placeholder="Cửa hàng Hoàn Ngọc" />
            </Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="COMPANY_TAX_CODE" label="Mã số thuế"><Input allowClear /></Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="COMPANY_PHONE" label="Điện thoại"><Input allowClear /></Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="COMPANY_EMAIL" label="Email" rules={[{ type: 'email' }]}><Input allowClear /></Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="COMPANY_WEBSITE" label="Website"><Input allowClear /></Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="COMPANY_ADDRESS" label="Địa chỉ"><Input allowClear /></Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="COMPANY_BANK_ACCOUNT" label="Số tài khoản ngân hàng"><Input allowClear /></Form.Item>
          </Col>
          <Col xs={24} md={12}>
            <Form.Item name="COMPANY_BANK_NAME" label="Ngân hàng"><Input allowClear /></Form.Item>
          </Col>
          <Col span={24}>
            <Form.Item name="COMPANY_INVOICE_FOOTER" label="Ghi chú cuối hoá đơn">
              <Input.TextArea rows={2} />
            </Form.Item>
          </Col>
        </Row>
      </Card>

      <Card size="small" className="luxury-card" title={<span className="card-title-gold">Điểm thân thiết &amp; CSKH</span>}>
        <Row gutter={16}>
          <Col xs={24} md={8}>
            <Form.Item name="LOYALTY_VND_PER_POINT" label="Số tiền để tích 1 điểm (VNĐ)">
              <InputNumber min={0} step={1000} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="LOYALTY_POINT_VALUE" label="Giá trị quy đổi 1 điểm (VNĐ)">
              <InputNumber min={0} step={100} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
          <Col xs={24} md={8}>
            <Form.Item name="CRM_CARE_REMIND_DAYS" label="Nhắc chăm sóc khách sau (ngày)">
              <InputNumber min={1} max={365} style={{ width: '100%' }} />
            </Form.Item>
          </Col>
        </Row>
        <Space>
          <Button type="primary" className="gold-gradient-btn" icon={<SaveOutlined />}
            loading={saving} disabled={!canEdit} onClick={() => void save()}>
            Lưu cài đặt
          </Button>
        </Space>
      </Card>

      {integrations && (
        <Card size="small" className="luxury-card" style={{ marginTop: 16 }}
          title={<span className="card-title-gold">Tích hợp vận chuyển &amp; thanh toán</span>}>
          <Space direction="vertical" size={10} style={{ width: '100%' }}>
            {([
              ['COD', integrations.cod, integrations.cod.provider || 'Chưa chọn hãng vận chuyển'],
              ['MoMo', integrations.momo, integrations.momo.provider],
            ] as const).map(([name, s, provider]) => (
              <div key={name}>
                <Space wrap>
                  <b>{name}</b>
                  <Tag color={s.configured ? 'green' : 'orange'}>{s.configured ? 'Đã cấu hình' : 'Chưa cấu hình'}</Tag>
                  <span>{provider}</span>
                </Space>
                {!s.configured && s.missing.length > 0 && (
                  <div style={{ color: '#8c8c8c' }}>Thiếu: {s.missing.join(', ')}</div>
                )}
              </div>
            ))}
            <Alert type="info" showIcon message="Credentials nạp qua biến môi trường Integrations__Cod__* / Integrations__MoMo__* — không lưu trong W_APP_SETTING."
              description="Xem deploy/RUNBOOK.md mục 8 để biết cách bật MoMo sandbox và COD placeholder." />
          </Space>
        </Card>
      )}
    </Form>
  );
};

export default SettingsPage;
