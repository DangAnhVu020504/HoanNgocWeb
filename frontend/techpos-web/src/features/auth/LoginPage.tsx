import React, { useState } from 'react';
import { Button, Card, Form, Input, message } from 'antd';
import { LockOutlined, UserOutlined } from '@ant-design/icons';
import { useAuthStore } from '../../stores/authStore';

/** Login — Phase 3 đầy đủ (refresh token, forgot password) */
const LoginPage: React.FC = () => {
  const login = useAuthStore((s) => s.login);
  const [loading, setLoading] = useState(false);

  const onFinish = async (values: { username: string; password: string }) => {
    setLoading(true);
    const ok = await login(values.username, values.password);
    setLoading(false);
    if (ok) window.location.href = '/';
    else void message.error('Tên đăng nhập hoặc mật khẩu không đúng');
  };

  return (
    <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', background: '#faf7ef' }}>
      <Card style={{ width: 380, boxShadow: '0 4px 24px rgba(0,0,0,.08)' }}>
        <div style={{ textAlign: 'center', marginBottom: 12 }}>
          <img src="/logo.png" alt="HN logo" style={{ width: 88, height: 88, objectFit: 'contain' }} />
        </div>
        <h2 style={{ textAlign: 'center', color: '#8a6100' }}>Hoàn Ngọc Group</h2>
        <p style={{ textAlign: 'center', color: '#888' }}>Quản lý bán hàng — versión web</p>
        <Form onFinish={onFinish}>
          <Form.Item name="username" rules={[{ required: true, message: 'Nhập tên đăng nhập' }]}>
            <Input prefix={<UserOutlined />} placeholder="Tên đăng nhập" size="large" />
          </Form.Item>
          <Form.Item name="password" rules={[{ required: true, message: 'Nhập mật khẩu' }]}>
            <Input.Password prefix={<LockOutlined />} placeholder="Mật khẩu" size="large" />
          </Form.Item>
          <Button type="primary" htmlType="submit" block size="large" loading={loading}>
            Đăng nhập
          </Button>
        </Form>
      </Card>
    </div>
  );
};

export default LoginPage;