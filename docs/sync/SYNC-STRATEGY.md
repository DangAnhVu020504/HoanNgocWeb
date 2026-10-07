# SYNC STRATEGY — Firebird 2.5 Embedded → MySQL 8

> Service: `TechPos.Sync.Service` (.NET 10 Worker, Windows Service, chạy **cùng máy** với DEMO.FDB).
> Data access: `FirebirdSql.Data.FirebirdClient` 2.5.x (cùng dòng provider với app cũ) + Dapper + MySqlConnector.
> **Firebird là READ-ONLY tuyệt đối ở giai đoạn đầu.** Không trigger, không ghi, không backup từ web.

## 1. Tại sao không CDC

Firebird 2.5 không có Change Views (chỉ có từ FB 3). Không có transaction log đọc được như SQL CDC.
⇒ Kết hợp: **W1 Watermark (incremental) + W2 Reconcile (đối soát) + Tombstone (phát hiện xóa)**.
W3 (Trigger trên Firebird) chỉ thiết kế cơ chế dự phòng, **không tự tạo** ở phiên bản đầu.

## 2. W1 — Watermark incremental

Với mỗi bảng có `TIMEMODIFIED/TIMECREATED`:

```sql
SELECT ... FROM {TABLE}
WHERE (TIMEMODIFIED > @LastModified)
   OR (TIMEMODIFIED = @LastModified AND ID > @LastId)
ORDER BY TIMEMODIFIED, ID
LIMIT @BatchSize
```

- Cursor lưu `W_SYNC_CURSOR(TABLE_NAME, LAST_MODIFIED, LAST_ID, LAST_SUCCESS_AT, LAST_COUNT)`.
- Upsert mirror theo PK `ID` (INSERT ... ON DUPLICATE KEY UPDATE), so `W_ROW_HASH` (MD5 của row normalized)
  để bỏ qua row không đổi → giảm write.
- Cập nhật `W_SYNC_AT = now()`, `W_ORIGIN='FB'` cho row mirror.
- Bảng **không có** TIMEMODIFIED (bảng nhỏ/config): full reload có hash compare.

## 3. W2 — Reconcile (02:00 hằng ngày)

Đối chiếu Firebird ↔ MySQL theo từng bảng:
1. `COUNT` hai bên.
2. So tập `ID` (page bằng hash bucket) → phát hiện thiếu/dư.
3. So `CHECKSUM` (hash cột nghiệp vụ) + `STATUS` + `TIMEMODIFIED` → phát hiện row đổi nhưng không chạy qua watermark
   (update không bump TIMEMODIFIED).
4. Dư ở MySQL & không có ở FB → nghi hard delete → verify thêm bằng trạng thái `STATUS` → ghi `W_SYNC_TOMBSTONE`
   và đặt `W_DELETED=1` (không xóa vật lý).
5. Mọi lệch ghi `W_SYNC_ERROR` + tự repair (re-pull record bằng ID) — repair giới hạn số record/night.

## 4. Scheduler (mục 8)

| Nhóm | Chu kỳ | Bảng |
|---|---|---|
| Nghiệp vụ | 30 giây | `TDONHANG`, `TDONHANGCHITIET`, `TTHUCHI`, `TCHITIETTHANHTOAN`, `TDATHANGNCC`, `TDATHANGNCCCHITIET`, `TGUIHANGCOD`, `TKETQUAGUICOD`, `TBANGLUONG*`, `TLUUVET` |
| Danh mục | 2 phút | `DMATHANG`, `DKHACHHANG`, `DNHACUNGCAP`, `DNHANVIEN`, `DKHOHANG`, `DCUAHANG`, `DDINHLUONG`, `DDOTKHUYENMAI*` |
| Bảng nhỏ/config | 10 phút | còn lại nhóm D/S |
| Ledger/tổng hợp | 5 phút | tính `W_KHOLEDGER`, `W_CHANGELOG` từ mirror mới |
| Reconcile | 02:00 hằng ngày | toàn bộ |
| Media/BLOB extractor | 02:30 hằng ngày | `DMATHANG.ANH`, `SIMAGE` → file storage + `W_MEDIA` |

- Policy per-table lưu `W_SYNC_TABLE_POLICY` (interval, enabled, priority) — Admin chỉnh được qua Sync Admin Dashboard.
- Dispatcher: vòng lặp 5s, chọn bảng đến hạn, chạy tuần tự mỗi bảng (giới hạn 1 worker/bảng), có lock chống overlap.
- Full-load đầu tiên: batch 2.000 row/bảng, transaction nhỏ, resume bằng cursor.

## 5. Error handling (mục 38)

- 1 row lỗi **không** chết batch: bắt lỗi per-row → ghi `W_SYNC_ROW_ERROR(TABLE_NAME, ID, ERROR, PAYLOAD JSON, RETRY_COUNT, NEXT_RETRY_AT)` → tiếp tục batch.
- Retry per-row: 3 lần rồi đưa Dead Letter (admin Retry/Ignore/View payload).
- Lỗi connection: circuit breaker, backoff 30s→1m→5m; heartbeat `W_SYNC_HEARTBEAT` + ghi `W_SYNC_LOG`.
- Service tự phục hồi: mọi vòng lặp có try/catch tổng, cursor chỉ advance khi batch commit thành công.
- Event `SYNC_FAILED` / `SYNC_RECOVERY` → Notification + Email (module 7).

## 6. Health & Admin Dashboard (mục 37)

- `W_SYNC_HEARTBEAT(HEARTBEAT_ID, LAST_RUN_AT, STATE)` — API đọc để hiển thị 🟢/🔴.
- Trang `/sync-admin` (quyền ADMIN/SUPER_ADMIN): status FB/MySQL, cursor từng bảng, rows inserted/updated/failed,
  last reconcile, parity status, retry row-error, toggle bảng.

## 7. Giai đoạn đồng bộ (mục 51) — Feature flags

| Giai đoạn | Hướng | Feature flag |
|---|---|---|
| 1 (mặc định) | FB → MySQL, web READ chứng từ FB; web ghi được bảng web-only | `ENABLE_WEB_TRANSACTION=false` |
| 2 | Bật ghi web cho nhóm bảng chọn (theo parity OK) | `ENABLE_WEB_TRANSACTION=true` per module |
| 3 | MySQL = MASTER, desktop legacy read | `ENABLE_TWO_WAY_SYNC` + migration plan — **không tự chuyển** |

- Chứng từ có nguồn FB: UI hiển thị badge `Nguồn dữ liệu: Desktop` và **không cho sửa/xóa** khi `W_ORIGIN='FB'`.
- Mapping người dùng: `SUSER` (mirror, không đổi) ↔ `W_USER` — web auth độc lập, không ghi mật khẩu về Firebird.