# RISK LIST

| # | Rủi ro | Mức | Giảm thiểu |
|---|---|---|---|
| 1 | **Danh mục 86 bảng Firebird thiếu bảng/cột** (đã thấy DKHUVUC, CNHANVIEN) | 🔴 Critical | Phase 0 bắt buộc: `RDB$RELATIONS` discovery qua tool; schema chỉ sinh từ discovery |
| 2 | `TDONHANG` đa năng — không biết tập giá trị `LOAI/MODE/LOAIGIA` | 🔴 Critical | Query phân nhóm trên dữ liệu thật; mapping vào `W_DOC_TYPE_MAP` không hard-code |
| 3 | Firebird Embedded chỉ truy cập file cục bộ, ODS 11.2 (FB 3/4/5 không mở được) | 🔴 Critical | Sync service dùng fbembed.dll 2.5.5 + ICU 3.0 kèm theo, chạy cùng máy, provider 2.5.x |
| 4 | Update không bump TIMEMODIFIED → watermark sót | 🟠 High | Reconcile hàng đêm + checksum; repair theo ID |
| 5 | Hard delete ở Firebird không dấu vết | 🟠 High | Reconcile phát hiện dư MySQL → W_SYNC_TOMBSTONE, không xóa vật lý |
| 6 | Báo cáo cũ nằm trong `SREPORT.SQL` (BLOB) — mất nếu không export trước | 🟠 High | Dump SREPORT (SQL/PARAMS/FILTERCONFIG) trong Phase 0; báo cáo web tái hiện theo logic cũ |
| 7 | Quy ước `STATUS` (1 hoạt động, 0/-1 xóa?) chưa chốt | 🟠 High | Xác minh Phase 0; chưa chốt thì mirror nguyên trạng, không tự suy |
| 8 | Bản quyền desktop kích hoạt online — hết hạn/đổi máy | 🟡 Medium | Chốt quyền sử dụng với hãng trước go-live (Q1–Q17 trong kế hoạch tổng) |
| 9 | `UDF ib_util` không dùng được (fbembed thiếu) — đã chứng minh app chạy được | 🟡 Medium | Không dùng UDF trong sync query; test trên TEMPLATE.FDB |
| 10 | Hiệu năng FDB khi sync chạy cùng giờ cao điểm bán hàng | 🟡 Medium | Sync read-only, batch nhỏ, off-peak cho reconcile; embedded đọc file trực tiếp |
| 11 | Bảo mật: teamviewer.exe, credential trong .dat | 🟡 Medium | Chỉ đọc; khuyến nghị đổi/thu hồi khi go-live; secret web qua env/DPAPI |
| 12 | Email chết làm lỗi nghiệp vụ | 🟡 Medium | Queue + worker độc lập, retry backoff, max-retry → FAILED |
| 13 | Dữ liệu tiền lệch sau sync (rounding) | 🟡 Medium | DECIMAL 15,2; parity SUM theo báo cáo then chốt; DataCompare tool |
| 14 | UI web khác thói quen desktop → user kháng cự | 🟡 Medium | Giữ tên chức năng/phím tắt/cây menu 76 mục; badge "Nguồn: Desktop" |
| 15 | Docker bị cấm — deploy thủ công lỗi nền | 🟢 Low | deploy scripts PowerShell + Windows Service + runbook |
| 16 | Hai nguồn ghi (web vs desktop) xung đột khi bật 2 chiều | 🔴 High (giai đoạn sau) | `ENABLE_TWO_WAY_SYNC=false` mặc định; parity ổn định mới mở theo bảng; W_ORIGIN + W_CHANGELOG |