# ============================================================
# RUNBOOK VẬN HÀNH — TechPosWeb (Phase 9, mục 54/59)
# Hệ thống: Firebird 2.5 Embedded (desktop) → Sync Service → MySQL 8 → Web API → React SPA
# Không Docker. Windows native.
# ============================================================

## 1. Kiến trúc triển khai & dịch vụ

| Thành phần | Windows Service / tiến trình | Cổng | Ghi chú |
|---|---|---|---|
| TechPos Sync Service | `TechPosSync` | — (đọc file FDB) | win-x86, chạy CÙNG MÁY với bản copy DEMO.FDB |
| TechPos Web API | `TechPosApi` | 5005 (LAN nội bộ) | win-x64; migration tự chạy lúc khởi động; SignalR `/hubs/notifications` |
| MySQL 8.0 | service `MySQL80` | 3306 (bind 127.0.0.1) | schema `techposweb` |
| Web SPA | thư mục tĩnh IIS (hoặc `npm run preview`) | 80/443 | gọi API qua reverse proxy |

## 2. Cài đặt lần đầu (theo thứ tự)

```powershell
# 1) Database + seed
.\deploy\powershell\setup-database.ps1 -MySqlRootPassword "xxx"

# 2) Web API
.\deploy\powershell\install-web-api.ps1          # sửa appsettings.Production.json rồi Start-Service TechPosApi

# 3) Tạo admin đầu tiên
dotnet run --project backend\tools\TechPos.AdminSeed -- "<conn>" admin MatKhau@123

# 4) Sync Service (copy DEMO.FDB ra thư mục sync riêng — PHASE0-FINDINGS §5.2)
.\deploy\powershell\install-sync-service.ps1     # sửa appsettings.json (FirebirdDatabasePath trỏ bản COPY)
```

## 3. Backup / Phục hồi (RPO ≤ 15 phút — M4)

### 3.1 Backup MySQL hằng đêm (Task Scheduler 23:30)
```powershell
# 1) Tạo login-path (một lần — không lưu mật khẩu trong task):
mysql_config_editor set --login-path=techpos-backup --host=localhost --user=root --password

# 2) Đăng ký task (PowerShell Administrator):
.\deploy\powershell\register-backup-task.ps1 -LoginPath techpos-backup -BackupDir "D:\Backup" -KeepDays 14 -RunAsSystem

# 3) Kiểm tra:
Get-ScheduledTask -TaskName "TechPos MySQL Backup" | Get-ScheduledTaskInfo
Start-ScheduledTask -TaskName "TechPos MySQL Backup"     # chạy thử
# Kết quả mong đợi: LastTaskResult = 0 và có file D:\Backup\techposweb_yyyyMMdd_HHmm.zip
```
- Script: `backup-mysql.ps1` (mysqldump → zip → giữ 14 ngày). Có thể chạy tay: `.\backup-mysql.ps1 -LoginPath techpos-backup`.
- Khuyến nghị `-RunAsSystem` trên server; nếu chạy dưới tài khoản user, dùng `-TaskUser`/`-TaskPassword`.

### 3.2 Phục hồi MySQL
```powershell
Expand-Archive D:\Backup\techposweb_20261004_2330.zip -DestinationPath $env:TEMP
mysql -u root -p techposweb -e "source $env:TEMP\techposweb_20261004_2330.sql"
# Sau restore: restart TechPosApi (migration idempotent sẽ bổ sung phần thiếu)
```

### 3.3 Firebird (nguồn gốc — KHÔNG bao giờ ghi)
- Desktop tự backup `Data\Backup\bak_*.gbk` hằng ngày — **giữ tối thiểu 3 bản ở 3 nơi** (đĩa ngoài + NAS + cloud).
- Không bao giờ mở file FDB gốc bằng fbembed (read-write) — sync chỉ đọc bản copy.

## 4. Monitoring hàng ngày

| Kiểm tra | Cách | Ngưỡng cảnh báo |
|---|---|---|
| Sync còn sống | `GET /api/sync/status` → heartbeat.State = RUNNING | STATE=ERROR hoặc LastRunAt > 5 phút |
| Lag đồng bộ | trang /sync-admin → cursor LastSuccessAt | > 5 phút với bảng TRANSACTION |
| Parity | chạy `TechPos.DataCompare` (đêm, cron) | lệch > 0 (ngoài WEB-origin) |
| Email queue | `GET /api/emails/queue?status=FAILED` | FAILED > 10/ngày |
| Conflict GĐ7 | `GET /api/sync/conflicts` | OPEN > 0 |
| Dung lượng đĩa | WMI / manual | < 20% còn trống |

## 5. Sự cố thường gặp

| Triệu chứng | Chẩn đoán | Xử lý |
|---|---|---|
| Sync ERROR "access denied" mở FDB | bản copy DEMO.FDB đang bị desktop giữ (đang backup/sweep) | đợi + copy lại file; service tự retry |
| Web không đăng nhập được | JWT secret đổi / token hết hạn | đăng nhập lại; kiểm tra `Jwt:SecretKey` ổn định |
| Migration failed lúc khởi động | xem log `logs/api-*.log` | API vẫn chạy; sửa SQL rồi xoá dòng W_MIGRATION tương ứng + restart |
| Email không gửi | SMTP chưa cấu hình (W_EMAIL_SETTINGS HOST rỗng) | Admin → /emails → Cấu hình SMTP → Test Email |
| Thông báo không hiện | SignalR bị proxy chặn WebSocket | kiểm tra poll 30s vẫn hoạt động (badge tự cập nhật) |
| Parity lệch sau sync | xem W_SYNC_ROW_ERROR | Sync Admin → Row errors → Retry; dead-letter nếu vô cứu |

## 6. Quy trình nâng cấp phiên bản

1. Backup MySQL + ghi chú version hiện tại (`SELECT MAX(APPLIED_AT) FROM W_MIGRATION`).
2. Deploy API mới: `install-web-api.ps1` (script tự stop → publish → tạo lại service).
3. Kiểm tra `/api/health` = UP + login OK + migration log không ERROR.
4. Deploy Sync: `install-sync-service.ps1`; kiểm tra heartbeat RUNNING.
5. Chạy `TechPos.DataCompare` xác nhận parity; theo dõi log 24h đầu.

## 7. Rollback

- **API lỗi**: Stop-Service TechPosApi → publish lại version cũ (giữ thư mục builds theo version `C:\Releases\{version}`) → Start.
- **MySQL lỗi dữ liệu**: restore dump gần nhất (§3.2) — sync sẽ tự nạp lại phần thiếu (watermark con trỏ vẫn còn).
- **Toàn hệ thống**: desktop TechPos SM4 **không bao giờ bị động vào** — mở lại dùng bình thường; đóng băng web.

## 8. Tích hợp COD & thanh toán MoMo (Phase 8)

**Nguyên tắc bảo mật**: credentials chỉ nạp qua biến môi trường `Integrations__*` (hoặc secret store), **không** lưu `W_APP_SETTING`/appsettings commit — vì `GET /api/settings` trả plain text và audit log ghi request body. Kiểm tra trạng thái: `GET /api/integrations/status` (quyền `ADMIN.SETTINGS.MANAGE`) hoặc thẻ "Tích hợp" trong trang Cài đặt.

### 8.1 COD placeholder (chưa có API hãng vận chuyển)

- Cấu hình dự kiến: `Integrations__Cod__Enabled`, `Provider`, `BaseUrl`, `ApiKey`, `ApiSecret`, `ShopCode`.
- Hiện chưa gọi HTTP ra hãng vận chuyển: phiếu gửi COD/kết quả COD vẫn tạo thủ công (`SALES.COD.CREATE` / `SALES.COD.RESULT`). Khi có tài liệu API hãng, bổ sung client vào `DeliveryService`.
- UI `Giao COD` hiển thị cảnh báo "chưa kết nối API" — không ảnh hưởng nghiệp vụ hiện tại.

### 8.2 MoMo Payment Gateway v2 (captureWallet)

1. Đăng ký merchant sandbox tại https://developers.momo.vn, lấy `partnerCode`, `accessKey`, `secretKey`.
2. Set biến môi trường cho service API rồi **restart service**:
   ```
   Integrations__MoMo__Enabled=true
   Integrations__MoMo__PartnerCode=<partnerCode>
   Integrations__MoMo__AccessKey=<accessKey>
   Integrations__MoMo__SecretKey=<secretKey>
   Integrations__MoMo__Endpoint=https://test-payment.momo.vn/v2/gateway/api/create
   Integrations__MoMo__RedirectUrl=https://<domain>/payments/momo
   Integrations__MoMo__IpnUrl=https://<domain>/api/payments/momo/ipn
   Integrations__MoMo__RequestType=captureWallet
   ```
   Đổi production: dùng endpoint/credentials MoMo production và domain HTTPS thật.
3. **IPN phải là URL công khai HTTPS** — MoMo không gọi được `localhost`. Test local dùng cloudflared/ngrok trỏ về cổng 5005 và đặt `IpnUrl` theo tunnel.
4. Test: menu **BÁN HÀNG → Thanh toán MoMo**, chọn đơn web còn công nợ (`CONNO > 0`), tạo QR, quét bằng app MoMo sandbox. Theo dõi `W_PAYMENT_TXN`: `PENDING → PAID` (hoặc `FAILED`); log API phải có `POST /api/payments/momo/ipn - 204`.
5. Đối soát: `STATUS`, `RESULT_CODE`, `TRANS_ID`, `RAW_IPN_JSON`; IPN idempotent theo `ORDER_ID`. Hệ thống **không tự trừ công nợ** — kế toán lập phiếu thu sau khi `PAID` để tránh ghi trùng.
6. Lỗi thường gặp: 400 "Cổng MoMo chưa được cấu hình" → thiếu env/restart; 400 "Chữ ký IPN không hợp lệ" → sai secretKey/accessKey; 403 → thiếu quyền `SALES.INVOICE.PAY`.
7. Migration `018_payment_momo.sql` tạo `W_PAYMENT_TXN`; xoay secretKey bằng đổi env + restart (không cần migrate lại).

