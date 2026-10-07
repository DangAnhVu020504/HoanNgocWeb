import React, { useState } from 'react';
import { Alert, Button, Card, DatePicker, Descriptions, Result, Select, Space, message } from 'antd';
import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';
import { recalculateCost, type CostRecalcResult } from '../../api/inventory';

const fmtVnd = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 0 }) + ' ₫';

const METHODS = [
  { value: 'FIFO', label: 'Nhập trước xuất trước (FIFO)' },
  { value: 'BQ_LIEN_HOAN', label: 'Bình quân gia quyền (liên hoàn)' },
  { value: 'BQ_CUOI_KY', label: 'Bình quân cuối kỳ' },
];

/** Tính lại giá vốn (mục 4.8) — dialog giống desktop: phương pháp + "Tính từ ngày" + Thực hiện. */
const CostRecalcPage: React.FC = () => {
  const [phuongPhap, setPhuongPhap] = useState<string>('FIFO');
  const [tuNgay, setTuNgay] = useState<Dayjs | null>(dayjs().startOf('month'));
  const [running, setRunning] = useState(false);
  const [result, setResult] = useState<CostRecalcResult | null>(null);
  const [err, setErr] = useState<string | null>(null);

  const run = async () => {
    if (!tuNgay) { void message.warning('Chọn ngày bắt đầu tính'); return; }
    setRunning(true);
    setErr(null);
    try {
      const r = await recalculateCost(phuongPhap, tuNgay.format('YYYY-MM-DD'));
      setResult(r);
      void message.success('Đã tính lại giá vốn');
    } catch (e: unknown) {
      setErr(e instanceof Error ? e.message : 'Lỗi tính lại giá vốn');
      setResult(null);
    } finally {
      setRunning(false);
    }
  };

  return (
    <Card size="small" title="Tính lại giá vốn" style={{ maxWidth: 720 }}>
      <Space direction="vertical" style={{ width: '100%' }} size={12}>
        <Alert type="info" showIcon
          message="Kết quả được ghi vào W_KHOLEDGER.GIA_VON (thẻ kho) và TDONHANGCHITIET.GIAVON của chứng từ do web tạo. Dữ liệu Desktop không bị thay đổi." />
        <Select style={{ width: '100%' }} value={phuongPhap} onChange={setPhuongPhap} options={METHODS} />
        <DatePicker style={{ width: '100%' }} placeholder="Tính từ ngày" value={tuNgay}
          onChange={setTuNgay} format="DD/MM/YYYY" />

        <Button type="primary" size="large" block loading={running} onClick={run}>
          THỰC HIỆN
        </Button>

        {err && <Alert type="error" showIcon message={err} />}
        {result && (
          <Result
            status="success"
            title={`Đã tính lại theo ${
              METHODS.find((m) => m.value === result.phuongPhap)?.label ?? result.phuongPhap
            }`}
            subTitle={`Từ ngày ${new Date(result.tuNgay).toLocaleDateString('vi-VN')}`}
            extra={
              <Descriptions column={1} size="small" bordered>
                <Descriptions.Item label="Số mặt hàng xử lý">{result.soMatHang}</Descriptions.Item>
                <Descriptions.Item label="Dòng xuất đã ghi giá vốn">{result.soDongXuat}</Descriptions.Item>
                <Descriptions.Item label="Tổng giá trị xuất">{fmtVnd(result.tongGiaTriXuat)}</Descriptions.Item>
              </Descriptions>
            }
          />
        )}
      </Space>
    </Card>
  );
};

export default CostRecalcPage;