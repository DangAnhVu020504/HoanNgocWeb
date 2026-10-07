# TESTING STRATEGY (mục 52)

## 1. Tầng test

| Tầng | Công cụ | Phạm vi |
|---|---|---|
| Unit | xUnit + FluentAssertions | Tính tiền/chiết khấu/thuế/giá vốn/tồn kho/công nợ, watermark cursor, mapping FB↔MySQL, email queue logic |
| Integration | xUnit + Testcontainers-free (local MySQL + Firebird test file) | Repository SQL, migration, sync end-to-end trên TEMPLATE.FDB |
| Sync | xUnit + fixture FDB | Watermark advance, conflict, row-error retry, tombstone |
| Parity | `tools/TechPos.DataCompare` + CI script | COUNT/SUM/ID/CHECKSUM: Doanh thu, Tồn kho, Công nợ KH/NCC, Tồn quỹ, KQKD — mục tiêu 0 lệch |
| E2E | Playwright | Login → Dashboard → Product → Customer → Sale → Payment → Print → Inventory → Debt → Email |
| Performance | k6 / NBomber | Dashboard < 3s, list < 2s |
| Security | OWASP checklist manual + headers | JWT, rate limit, injection, XSS |

## 2. Chiến lược

- Unit test tính toán tiền dùng DECIMAL — cấm floating point case.
- Integration dùng CSDL trắng TEMPLATE.FDB (không đụng DEMO.FDB) + MySQL schema test riêng.
- Sync test: seed FB → chạy sync → so parity → modify FB → chạy lại → verify incremental.
- E2E chạy headless trên CI (local runner); mobile viewport test (iPhone/Android profile).
- Parity là **gate** trước khi bật ghi 2 chiều (feature flag).

## 3. Quy ước

- Mỗi module Phase 4–5: test đi kèm ngay trong phase, không dồn Phase 8.
- CI gate: `dotnet build` + unit/integration + `npm run build` phải xanh trước khi merge.
- Test data builder pattern cho DTO nghiệp vụ; không phụ thuộc thứ tự test.