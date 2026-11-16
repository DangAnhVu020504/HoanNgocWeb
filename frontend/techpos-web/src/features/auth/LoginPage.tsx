import React, { useState } from 'react';
import { Button, Card, Form, Input, message } from 'antd';
import { LockOutlined, SafetyOutlined, UserOutlined } from '@ant-design/icons';
import { useAuthStore } from '../../stores/authStore';

/**
 * Màn hình Đăng Nhập Luxury Aurora White & Gold
 * Giữ nguyên 100% logic xác thực JWT & refresh token.
 */
const LoginPage: React.FC = () => {
  const login = useAuthStore((s) => s.login);
  const [loading, setLoading] = useState(false);

  const onFinish = async (values: { username: string; password: string }) => {
    setLoading(true);
    const ok = await login(values.username, values.password);
    setLoading(false);
    if (ok) {
      window.location.href = '/';
    } else {
      void message.error('Tên đăng nhập hoặc mật khẩu không đúng');
    }
  };

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        position: 'relative',
        overflow: 'hidden',
        background: 'linear-gradient(135deg, #FFFDF8 0%, #FFF6DC 35%, #FAF7EF 70%, #FFFDF5 100%)',
        backgroundSize: '400% 400%',
        animation: 'auroraFlow 16s ease infinite',
        padding: 20,
      }}
    >
      {/* Vòng sáng trang trí nền */}
      <div
        style={{
          position: 'absolute',
          width: 500,
          height: 500,
          borderRadius: '50%',
          background: 'radial-gradient(circle, rgba(212, 175, 55, 0.15) 0%, rgba(212, 175, 55, 0) 70%)',
          top: '-10%',
          right: '-5%',
          pointerEvents: 'none',
        }}
      />
      <div
        style={{
          position: 'absolute',
          width: 450,
          height: 450,
          borderRadius: '50%',
          background: 'radial-gradient(circle, rgba(255, 224, 130, 0.2) 0%, rgba(255, 224, 130, 0) 70%)',
          bottom: '-10%',
          left: '-5%',
          pointerEvents: 'none',
        }}
      />

      {/* Thẻ đăng nhập kính pha lê (Floating Crystal Card) */}
      <Card
        className="glass-panel"
        style={{
          width: '100%',
          maxWidth: 420,
          borderRadius: 16,
          boxShadow: '0 20px 50px -10px rgba(212, 175, 55, 0.25), 0 8px 24px -4px rgba(0, 0, 0, 0.05)',
          border: '1px solid rgba(212, 175, 55, 0.35)',
          padding: '12px 10px',
          animation: 'fadeInUpStagger 0.5s cubic-bezier(0.16, 1, 0.3, 1)',
        }}
        styles={{ body: { padding: '28px 24px' } }}
      >
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
          <div
            style={{
              width: 80,
              height: 80,
              margin: '0 auto 14px',
              borderRadius: 18,
              background: '#FFFFFF',
              border: '2px solid rgba(212, 175, 55, 0.45)',
              boxShadow: '0 8px 24px rgba(212, 175, 55, 0.25)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              padding: 6,
            }}
          >
            <img
              src="/logo.png"
              alt="HN logo"
              style={{ width: '100%', height: '100%', objectFit: 'contain' }}
            />
          </div>
          <h2
            style={{
              margin: '0 0 6px',
              color: '#8A6100',
              fontWeight: 800,
              fontSize: 22,
              letterSpacing: '0.5px',
            }}
          >
            HOÀN NGỌC GROUP
          </h2>
          <p style={{ margin: 0, color: '#7E7359', fontSize: 13, fontWeight: 500 }}>
            Hệ thống Quản lý Bán hàng & Doanh nghiệp
          </p>
        </div>

        <Form layout="vertical" onFinish={onFinish} size="large">
          <Form.Item
            name="username"
            rules={[{ required: true, message: 'Vui lòng nhập tên đăng nhập' }]}
            style={{ marginBottom: 18 }}
          >
            <Input
              prefix={<UserOutlined style={{ color: '#D4AF37' }} />}
              placeholder="Tên đăng nhập"
              style={{
                borderRadius: 10,
                border: '1px solid #EFE9D9',
                background: '#FFFDF9',
              }}
            />
          </Form.Item>

          <Form.Item
            name="password"
            rules={[{ required: true, message: 'Vui lòng nhập mật khẩu' }]}
            style={{ marginBottom: 24 }}
          >
            <Input.Password
              prefix={<LockOutlined style={{ color: '#D4AF37' }} />}
              placeholder="Mật khẩu"
              style={{
                borderRadius: 10,
                border: '1px solid #EFE9D9',
                background: '#FFFDF9',
              }}
            />
          </Form.Item>

          <Button
            type="primary"
            htmlType="submit"
            block
            loading={loading}
            className="gold-gradient-btn"
            style={{
              height: 46,
              borderRadius: 10,
              fontSize: 15,
              letterSpacing: 0.5,
              marginBottom: 16,
            }}
          >
            ĐĂNG NHẬP HỆ THỐNG
          </Button>
        </Form>

        <div
          style={{
            textAlign: 'center',
            fontSize: 12,
            color: '#A08D6A',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            gap: 6,
            borderTop: '1px solid #F4EEDC',
            paddingTop: 14,
          }}
        >
          <SafetyOutlined style={{ color: '#D4AF37' }} />
          <span>Phiên bản 2.0 · Kết nối bảo mật SSL</span>
        </div>
      </Card>
    </div>
  );
};

export default LoginPage;