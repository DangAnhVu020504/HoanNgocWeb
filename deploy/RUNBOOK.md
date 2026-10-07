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
dotnet run --project tools\TechPos.AdminSeed -- "<conn>" admin MatKhau@123

# 4) Sync Service (copy DEMO.FDB ra thư mục sync riêng — PHASE0-FINDINGS §5.2)
.\deploy\powershell\install-sync-service.ps1     # sửa appsettings.json (FirebirdDatabasePath trỏ bản COPY)
```

## 3. Backup / Phục hồi (RPO ≤ 15 phút — M4)

### 3.1 Backup MySQL hằng đêm (Task Scheduler 23:30)
```powershell
# backup-mysql.ps1 — chạy trên server
& mysqldump -u root -p"<pass>" --single-transaction --routines --triggers techposweb |
  Compress-Archive -DestinationPath "D:\Backup\techposweb_$(Get-Date -Format yyyyMMdd_HHmm).zip" -Force
# Giữ 14 ngày:
Get-ChildItem D:\Backup -Filter "techposweb_*.zip" | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-14) } | Remove-Item
```

### 3.2 Phục hồi MySQL
```powershell
Expand-Archive D:\Backup\techposweb_20261004_2330.zip -DestinationPath $env:TEMP
mysql -u root -p"<pass>" techposweb < $env:TEMP\techposweb.sql
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
