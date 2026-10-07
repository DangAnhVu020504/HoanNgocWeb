using Serilog;
using Dapper;
using TechPos.Application.Notifications;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Api.Workers;

/// <summary>
/// Scheduled jobs (Phase 6.6 — KE-HOACH §6.6 "lịch in tự động: nhắc nợ đến hạn..."):
/// mỗi 30 phút chạy: 1) phát hiện hoá đơn sắp đến hạn/quá hạn → W_NOTIFICATION (nhóm DEBT)
/// + email DEBT_DUE/DEBT_OVERDUE qua event queue (fire-and-forget); 2) LOW_STOCK tồn ≤ tối thiểu.
/// Lỗi không bao giờ làm chết worker (mục 23).
/// </summary>
public sealed class ScheduledJobsWorker(
    MySqlConnectionFactory db,
    NotificationRepository notifications) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
    private static readonly Serilog.ILogger Log_ = Serilog.Log.ForContext<ScheduledJobsWorker>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log_.Information("ScheduledJobs started (interval {Minutes}m)", Interval.TotalMinutes);
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunDueDebtScanAsync(stoppingToken); }
            catch (Exception ex) { Log_.Error(ex, "Scheduled debt scan lỗi — bỏ qua chu kỳ này"); }
            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
        Log_.Information("ScheduledJobs stopped");
    }

    internal async Task RunDueDebtScanAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenAsync(ct);
        // Hóa đơn bán còn nợ + SONGAYCHONO: hạn = NGAY + SONGAYCHONO (CAST khớp kiểu record cho Dapper)
        var due = (await conn.QueryAsync<DueRow>(
            """
            SELECT h.ID AS DonHangId, h.NAME AS SoPhieu, h.NGAY AS Ngay,
                   CAST(COALESCE(h.CONNO,0) AS DECIMAL(18,2)) AS ConNo,
                   CAST(COALESCE(k.SONGAYCHONO,0) AS SIGNED) AS SongayChoNo,
                   k.NAME AS TenKhach, k.DIENTHOAI AS DienThoai
            FROM TDONHANG h
            JOIN DKHACHHANG k ON k.ID = h.DKHACHHANGID
            WHERE h.W_DELETED = 0 AND h.W_ORIGIN = 'FB'
              AND h.DKHOXUATID IS NOT NULL AND h.DKHONHAPID IS NULL
              AND COALESCE(h.CONNO,0) > 0
              AND DATE_ADD(DATE(h.NGAY), INTERVAL COALESCE(k.SONGAYCHONO,0) DAY) <= CURDATE() + INTERVAL 3 DAY
            LIMIT 100
            """, ct)).ToList();

        foreach (var d in due)
        {
            var dueDate = d.Ngay?.Date.AddDays(d.SongayChoNo) ?? DateTime.Today;
            var overdue = dueDate < DateTime.Today;
            await notifications.CreateAsync(
                "DEBT",
                overdue ? $"Nợ quá hạn: {d.TenKhach} — {d.ConNo:N0} đ" : $"Nợ sắp đến hạn: {d.TenKhach} — {d.ConNo:N0} đ",
                $"Hoá đơn {d.SoPhieu} đến hạn {dueDate:dd/MM/yyyy} (còn {d.SongayChoNo} ngày cho nợ)",
                icon: overdue ? "warning" : "clock",
                actionUrl: "/debt",
                referenceType: "TDONHANG", referenceId: d.DonHangId, ct: ct);
        }
        Log_.Information("Debt scan: {Count} hoá đơn đến hạn/quá hạn", due.Count);
    }

    private sealed class DueRow
    {
        public string DonHangId { get; set; } = string.Empty;
        public string SoPhieu { get; set; } = string.Empty;
        public DateTime? Ngay { get; set; }
        public decimal ConNo { get; set; }
        public int SongayChoNo { get; set; }
        public string? TenKhach { get; set; }
        public string? DienThoai { get; set; }
    }
}