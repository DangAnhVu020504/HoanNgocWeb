# ROADMAP — 10 Phase (mục 59)

| Phase | Nội dung | Deliverable | Gate kiểm tra trước khi chuyển |
|---|---|---|---|
| **0** | Phân tích & xác nhận: DB thật (RDB$RELATIONS/FIELDS/INDICES/CONSTRAINTS/TRIGGERS/GENERATORS), tập giá trị `TDONHANG.LOAI/MODE`, `SREPORT.SQL`, `SMENU`, `SCOLUMN`, business rules | `discovery/*.json`, `SCHEMA_DISCOVERY.md`, ADR mapping bảng | Trích xuất chạy được trên DEMO.FDB, không sửa DB |
| **1** | Solution + MySQL foundation + Backend + Frontend skeleton | sln build, migration W_* chạy, API health/auth skeleton, web shell build | `dotnet build` ✓, migration chạy ✓, `npm run build` ✓ |
| **2** | Sync Service: Watermark, Scheduler, Reconcile, RowError, Tombstone, Heartbeat | Windows service cài được, sync test DB ✓ | Parity COUNT khớp trên nhóm bảng pilot |
| **3** | Auth, RBAC, Dashboard | login/refresh, permission filter, dashboard API + UI | Unit test auth ✓, E2E login ✓ |
| **4** | Danh mục + Bán hàng (POS bán lẻ/bán sỉ) + Kho | screens xuyên suốt DB→UI | Build + E2E sale→payment→stock ✓ |
| **5** | Quỹ + Công nợ + Nhân sự | phiếu thu/chi, công nợ, lương | Build + test nghiệp vụ ✓ |
| **6** | Report Center + PDF (QuestPDF) + Excel (ClosedXML) + Barcode (ZXing) + in | export mọi danh sách chính | File mở được, số khớp báo cáo desktop |
| **7** | Email (queue, retry, templates, settings) + Notification + SignalR | queue worker chạy, retry backoff test | Email fail không fail transaction ✓ |
| **8** | Testing: Unit / Integration / Parity / E2E (Playwright) / Performance / Security | test suites xanh | Parity 0 lệch trên báo cáo then chốt |
| **9** | Deploy Windows: IIS/Kestrel, Windows Service, MySQL backup, monitoring | deploy scripts, runbook | UAT trên dữ liệu thật |

Thứ tự module Phase 4–5 mỗi module đi xuyên suốt: Database → Entity/DTO → Repository → Service →
API → React Query → React UI → Test (mục 60).

Phase 0 là **điều kiện tiên quyết**: không sinh mirror schema trước khi có discovery.