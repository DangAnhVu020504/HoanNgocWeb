# PHASE 0 FINDINGS — Discovery & xác minh DB thật (23/09/2026)

> Nguồn: DEMO.FDB (bản copy READ-ONLY tại `.tmp-fb\DEMO.FDB` — DB gốc KHÔNG bị động vào).
> Tool: `tools/TechPos.FirebirdExtractor` (.NET 10 x86 + fbembed.dll 2.5.5 của app + FirebirdClient 10.3.4).
> Output đầy đủ: `discovery/tables/*.json` (86 bảng) + `SCHEMA_DISCOVERY.md`.

## 1. Kết quả discovery

| Hạng mục | Kết quả |
|---|---|
| Tổng relation | **86** (86 TABLE, **0 view**) — DEMO.FDB |
| PK | Mọi bảng = `ID` (GUID) |
| Bảng nhiều cột nhất | `DMATHANG` 59 cột · `TDONHANG` ? cột (xem JSON) |
| `DKHUVUC` / `CNHANVIEN` | ❌ **KHÔNG tồn tại** trong DEMO.FDB này (SQL của WebProcessor.EXE trỏ tới DB production khác bản DEMO — đúng cảnh báo §4.3 khảo sát) |
| Generators | 0 (số phiếu sinh từ app, không sequence DB) |
| Triggers | 0 (xác nhận sync không bị trigger can thiệp) |

⚠️ Kết luận: trên **DEMO.FDB** danh mục 86 bảng khớp hoàn toàn. Với **production DB** (nếu khác DEMO)
phải chạy lại extractor trước khi sinh schema — đây là quy trình bắt buộc theo mục 4.

## 2. Xác minh TDONHANG đa năng (query thực tế trên DEMO.FDB)

```text
LOAI | MODE | LOAIGIA | SO_PHIEU | PHIEU_DAU  | PHIEU_CUOI
 1   |  -1  |    0    |    1     | PN18/00001 | PN18/00001    → PN = Nhập kho
 5   |  -1  | (null)  |    2     | KTL17/00001| KTL18/00001   → KTL = Kiểm kê
```

- **LOAI=1 → nhập kho**, **LOAI=5 → kiểm kê** (đúng tiền tố phiếu PN/KTL).
- `MODE = -1` trên cả 2 loại. `LOAIGIA` null với kiểm kê.
- Bán lẻ/bán sỉ **không nằm trong TDONHANG của DEMO** (demo chỉ 3 phiếu) — phải xác minh lại trên DB production.
- **Không hard-code** mapping đầy đủ; ghi nhận vào `W_DOC_TYPE_MAP` khi có thêm dữ liệu.

## 3. Xác minh STATUS (xóa mềm)

```text
DMATHANG.STATUS: 0 (1 dòng) · 30 (7 dòng)
```

- **STATUS = 30 = hoạt động** (khớp hằng số bóc được trong WebProcessor.EXE: `STATUS = 30`).
- **STATUS = 0 → nhiều khả năng xóa mềm / không dùng** — cần xác minh thêm với desktop UI (dòng nào bị thùng rác).
- Quy ước migrate: mirror giữ nguyên STATUS; web filter theo `STATUS = 30` cho dữ liệu đọc, chốt bảng mapping sau khi xác minh 100%.

## 4. TTHUCHI

```text
LOAI | LOAIDOITUONG | SL
 0   |      0       | 2
 0   |      1       | 1
 1   |      1       | 1
```
- LOAI 0/1 = thu/chi; LOAIDOITUONG 0/1 = phân loại đối tượng — tập giá trị demo quá mỏng, bổ sung khi sync production.

## 5. Điều kiện kỹ thuật đã xác nhận khi chạy fbembed

1. **fbembed.dll là 32-bit** ⇒ mọi process đọc Firebird (extractor + Sync Service) phải build `PlatformTarget=x86`.
2. Thư mục cài đặt bị ACL chống ghi ⇒ Firebird Embedded (mở file read-write) **không chạy được trực tiếp trên DB gốc**
   ⇒ Sync Service chạy trên máy này phải **copy DB** hoặc deploy cạnh thư mục có quyền ghi (khuyến nghị: copy snapshot vào thư mục sync riêng theo lịch — xem cập nhật SYNC-STRATEGY khi chốt).
3. FirebirdClient 10.3.4 + `ClientLibrary=fbembed.dll` hoạt động OK với ODS 11.2.

## 6. Việc tiếp theo (Phase 1 tiếp)

1. Chạy `TechPos.SchemaGenerator` trên `discovery/tables` → `mirror_ddl.sql`.
2. Tạo MySQL schema `techposweb`, chạy migration 001 + mirror DDL.
3. Cập nhật `W_DOC_TYPE_MAP` + policy sync theo findings trên.