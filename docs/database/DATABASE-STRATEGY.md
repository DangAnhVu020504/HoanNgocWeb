# DATABASE STRATEGY — TechPosWeb (MySQL 8)

## 0. Nguyên tắc số 1: Phase 0 Discovery trước khi sinh schema

Danh mục 86 bảng trong `TechPos_SM4_Database_Structure.md` là **bản tham khảo**, ĐÃ chứng minh thiếu
(`DKHUVUC`, `CNHANVIEN`...). Bắt buộc chạy discovery trên DB thật trước khi sinh DDL:

```sql
SELECT RDB$RELATION_NAME FROM RDB$RELATIONS WHERE RDB$SYSTEM_FLAG = 0 ORDER BY 1;
SELECT * FROM RDB$RELATION_FIELDS ...
SELECT * FROM RDB$FIELDS ...
-- và: indexes (RDB$INDICES), constraints (RDB$RELATION_CONSTRAINTS),
--     triggers (RDB$TRIGGERS), generators (RDB$GENERATORS), views
```

Tool: `tools/TechPos.FirebirdExtractor` → xuất `docs/database/discovery/*.json` + `SCHEMA_DISCOVERY.md`.
Tool `tools/TechPos.SchemaGenerator` sinh DDL mirror từ JSON đó. **Cấm viết tay mirror DDL từ tài liệu.**

## 1. Phân loại dữ liệu (mục 58)

| Loại | Nguồn | Ví dụ | Quy tắc |
|---|---|---|---|
| SOURCE DATA | Firebird mirror | `TDONHANG`, `DMATHANG`… | Giữ nguyên tên bảng/cột (UPPERCASE), không đổi |
| WEB DATA | Web tự tạo | `W_USER`, `W_ROLE` | Prefix `W_` |
| DERIVED DATA | Tính toán | `W_KHOLEDGER`, view tổng hợp | Sinh lại được, có watermark |
| CACHE DATA | Report cache | `W_REPORT_CACHE` | TTL, xoá được |
| AUDIT DATA | Web actions | `W_AUDIT_LOG` | Append-only, không sửa |
| SYNC DATA | Sync meta | `W_SYNC_*` | Chỉ sync service ghi |
| EMAIL DATA | Email engine | `W_EMAIL_*` | Queue append-only |

## 2. Quy tắc bảng mirror (từ Firebird)

- Tên bảng & cột giữ nguyên, UPPERCASE: `TDONHANG`, `DKHACHHANG`, `NAME`, `NGAY`…
- `ID CHAR(36) CHARACTER SET ascii COLLATE ascii_bin PRIMARY KEY` (GUID Firebird VARCHAR(36)).
- Mirror đủ: `STATUS`, `TIMECREATED`, `TIMEMODIFIED`, `USERCREATEDID`, `USERMODIFIEDID`.
- Mọi bảng mirror thêm cột kỹ thuật sync (đặt trước cột, đều có mặt):
  `W_SYNC_AT DATETIME(3)`, `W_SYNC_REV BIGINT`, `W_ORIGIN VARCHAR(8) DEFAULT 'FB'`,
  `W_ROW_HASH CHAR(32)`, `W_DELETED TINYINT(1) DEFAULT 0`, `W_DELETED_AT DATETIME(3)`.
- Index bắt buộc: `(TIMEMODIFIED, ID)` watermark, `(TIMECREATED, ID)`, FK theo quy ước `…ID`,
  index ngày chứng từ (`NGAY`).
- Charset: `utf8mb4` / `utf8mb4_0900_ai_ci`; cột ID dùng ascii_bin để index nhỏ.
- Tiền: `DECIMAL(15,2)` (VND scale 2 giữ tương thích nguồn), số lượng `DECIMAL(18,4)`,
  tỉ lệ % `DECIMAL(9,4)`. **Không FLOAT/DOUBLE cho tiền.**
- Mọi cột `…ID` là GUID CHAR(36); FK thật chỉ khai khi Phase 0 xác minh quan hệ.

## 3. Bảng web-only `W_*` (danh sách chuẩn)

```text
SYNC:    W_SYNC_CONFIG, W_SYNC_CURSOR, W_SYNC_LOG, W_SYNC_ERROR, W_SYNC_ROW_ERROR,
         W_SYNC_TOMBSTONE, W_SYNC_HEARTBEAT, W_SYNC_TABLE_POLICY
AUTH:    W_USER, W_ROLE, W_PERMISSION, W_ROLE_PERMISSION, W_USER_ROLE,
         W_USER_SCOPE, W_REFRESH_TOKEN, W_USER_SESSION
AUDIT:   W_AUDIT_LOG, W_SYSLOG, W_CHANGELOG
NOTIFY:  W_NOTIFICATION, W_NOTIFICATION_READ
EMAIL:   W_EMAIL_TEMPLATE, W_EMAIL_EVENT, W_EMAIL_QUEUE, W_EMAIL_LOG,
         W_EMAIL_RECIPIENT, W_EMAIL_SETTINGS
MEDIA:   W_MEDIA (PATH, HASH, SIZE, MIME_TYPE — không lưu BLOB ảnh)
LEDGER:  W_KHOLEDGER (tồn đầu → nhập → xuất → tồn theo kho/mặt hàng/ngày)
OTHER:   W_REPORT_CACHE, W_FEATURE_FLAG, W_APP_SETTING, W_DOC_SEQUENCE
```

## 4. ERD dự kiến (khối chính — chi tiết chốt sau Phase 0)

```text
DMATHANG ──< TDONHANGCHITIET >── TDONHANG ──> DKHACHHANG
   │  (MATHANGID/               │  (đa năng: LOAI/MODE/
   │   DHANGSANXUATID…)         │   LOAIGIA/LOAITHANHTOAN)
   │                            ├──> DKHOXUAT (DKHOHANG) / DKHONHAP (DKHOHANG)
   ├──< OTONKHO >── DKHOHANG    ├──> DNHACUNGCAP / DCUAHANG / DNHANVIEN*
   └──< W_KHOLEDGER             ├──< TCHITIETTHANHTOAN >── TTHUCHI ──> DLYDOTHUCHI
DKHACHHANG ──< TDOINO, TTANGGIAMDIEM, TCHAMSOC          (quỹ: THU/CHI)
DNHANVIEN ──< TBANGLUONG ──< TBANGLUONGCHITIET / TTHUONGPHAT
SUSER (mirror) ──(map)── W_USER;  W_USER ──< W_USER_ROLE >── W_ROLE ──< W_ROLE_PERMISSION >── W_PERMISSION
W_EMAIL_EVENT ──< W_EMAIL_QUEUE ──< W_EMAIL_LOG ;  W_EMAIL_QUEUE ──< W_EMAIL_RECIPIENT
TDONHANG/… ──(media)── W_MEDIA (file storage, DB chỉ PATH/HASH)
```

> ⚠️ Các mũi tên dựa trên quy ước đặt tên `…ID` — chưa có FK thật trong dump Firebird.
> Phase 0 phải xác minh từng quan hệ trước khi khai FK trong MySQL.

## 5. TDONHANG — bảng chứng từ đa năng (mục 29)

1 bảng cho ~9 nghiệp vụ (bán lẻ, bán sỉ, nhập/xuất/chuyển kho, kiểm kê, trả NCC, khách trả,
xuất vật tư) phân biệt bằng `LOAI` / `MODE` / `LOAIGIA` / `LOAITHANHTOAN` + cặp kho/nhân viên.

- **Bắt buộc** chạy SQL xác minh tập giá trị trên dữ liệu thật trước khi code logic:
  ```sql
  SELECT LOAI, MODE, LOAIGIA, COUNT(*), MIN(NAME), MAX(NAME)
  FROM TDONHANG GROUP BY LOAI, MODE, LOAIGIA ORDER BY 1,2,3;
  ```
- MySQL thêm cột generated `W_DOC_TYPE` (STORED) suy ra loại chứng từ để lọc/route —
  giá trị mapping chỉ ghi vào code sau khi Phase 0 xác minh, kèm bảng tra `W_DOC_TYPE_MAP`.
- Web đọc mirror theo `W_DOC_TYPE`; khi tạo chứng từ mới (giai đoạn 2 chiều) ghi đúng
  `LOAI/MODE` đã xác minh.

## 6. Migration & seed

- `database/migrations/NNN_*.sql` (không dùng ORM migration cho mirror — DDL sinh từ discovery).
- `database/seed/` — roles, permissions, email templates, feature flags, admin user.
- Migration runner trong `TechPos.Data.MySql` (bảng `W_MIGRATION`): idempotent, chạy lúc API/sync start.
- Seed chỉ tạo dữ liệu hệ thống; **không** seed dữ liệu nghiệp vụ (mọi dữ liệu nghiệp vụ đến từ sync).