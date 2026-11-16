using Serilog;
using Dapper;
using TechPos.Application.Notifications;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Api.Workers;

/// <summary>
/// Scheduled jobs (Phase 6.6 + Phase 8): mỗi 30 phút
///   1) phát hiện hóa đơn sắp đến hạn/quá hạn → W_NOTIFICATION (DEBT) + email DEBT_DUE/DEBT_OVERDUE;
///   2) cảnh báo tồn kho thấp (LOW_STOCK) → W_NOTIFICATION (INVENTORY) + email tổng hợp.
/// Push SignalR qua NotificationService. Lỗi không bao giờ làm chết worker (mục 23).
/// </summary>
public sealed class ScheduledJobsWorker(
    MySqlConnectionFactory db,
    NotificationService notifications,
    EmailService emails) : BackgroundService
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
            try { await RunLowStockScanAsync(stoppingToken); }
            catch (Exception ex) { Log_.Error(ex, "Scheduled low-stock scan lỗi — bỏ qua chu kỳ này"); }
            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
        Log_.Information("ScheduledJobs stopped");
    }

    internal async Task RunDueDebtScanAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenAsync(ct);
        // Hóa đơn bán còn nợ + SONGAYCHONO: hạn = NGAY + SONGAYCHONO
        var due = (await conn.QueryAsync<DueRow>(
            """
            SELECT h.ID AS DonHangId, h.NAME AS SoPhieu, h.NGAY AS Ngay,
                   CAST(COALESCE(h.CONNO,0) AS DECIMAL(18,2)) AS ConNo,
                   CAST(COALESCE(k.SONGAYCHONO,0) AS SIGNED) AS SongayChoNo,
                   k.NAME AS TenKhach, k.DIENTHOAI AS DienThoai, k.EMAIL AS Email
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
            await notifications.CreateAndBroadcastAsync(
                "DEBT",
                overdue ? $"Nợ quá hạn: {d.TenKhach} — {d.ConNo:N0} đ" : $"Nợ sắp đến hạn: {d.TenKhach} — {d.ConNo:N0} đ",
                $"Hóa đơn {d.SoPhieu} đến hạn {dueDate:dd/MM/yyyy} (còn {d.SongayChoNo} ngày cho nợ)",
                icon: overdue ? "warning" : "clock",
                actionUrl: "/debt",
                referenceType: "TDONHANG", referenceId: d.DonHangId, ct: ct);

            if (!string.IsNullOrWhiteSpace(d.Email))
            {
                _ = emails.EnqueueFromEventAsync(overdue ? "DEBT_OVERDUE" : "DEBT_DUE",
                    [(d.Email!, d.TenKhach)],
                    new Dictionary<string, string?>
                    {
                        ["TEN_KHACH"] = d.TenKhach,
                        ["SO_HOA_DON"] = d.SoPhieu,
                        ["CON_NO"] = d.ConNo.ToString("N0"),
                        ["NGAY_DEN_HAN"] = dueDate.ToString("dd/MM/yyyy"),
                        ["NGAY"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                    }, "TDONHANG", d.DonHangId, ct);
            }
        }
        Log_.Information("Debt scan: {Count} hóa đơn đến hạn/quá hạn", due.Count);
    }

    internal async Task RunLowStockScanAsync(CancellationToken ct = default)
    {
        await using var conn = await db.OpenAsync(ct);

        var low = (await conn.QueryAsync<LowStockRow>(
            """
            SELECT m.ID AS MatHangId, m.CODE AS Code, m.NAME AS Name,
                   CAST(COALESCE(m.TONTOITHIEU,0) AS DECIMAL(18,4)) AS TonToiThieu,
                   CAST(SUM(COALESCE(o.TON,0)) AS DECIMAL(18,4)) AS Ton
            FROM DMATHANG m
            JOIN OTONKHO o ON o.DMATHANGID = m.ID
            WHERE m.W_DELETED = 0 AND o.W_DELETED = 0 AND COALESCE(m.TONTOITHIEU,0) > 0
            GROUP BY m.ID, m.CODE, m.NAME, m.TONTOITHIEU
            HAVING SUM(COALESCE(o.TON,0)) < COALESCE(m.TONTOITHIEU,0)
            ORDER BY m.NAME
            LIMIT 100
            """, ct)).ToList();
        if (low.Count == 0)
        {
            Log_.Information("Low-stock scan: không có mặt hàng dưới tồn tối thiểu");
            return;
        }

        var notifiedToday = (await conn.QueryAsync<string>(
            """
            SELECT REFERENCE_ID FROM W_NOTIFICATION
            WHERE GROUP_ = 'INVENTORY' AND REFERENCE_TYPE = 'DMATHANG' AND CREATED_AT >= @today
            """, new { today = DateTime.Today })).ToHashSet(StringComparer.Ordinal);

        foreach (var item in low.Where(i => !notifiedToday.Contains(i.MatHangId)))
        {
            await notifications.CreateAndBroadcastAsync(
                "INVENTORY",
                $"Tồn kho thấp: {item.Name}",
                $"Còn {item.Ton:N0} / tối thiểu {item.TonToiThieu:N0}{(string.IsNullOrWhiteSpace(item.Code) ? "" : $" (mã {item.Code})")}",
                icon: "warning", actionUrl: "/inventory",
                referenceType: "DMATHANG", referenceId: item.MatHangId, ct: ct);
        }

        // Email tổng hợp 1 lần/ngày (tránh spam mỗi 30 phút)
        var alreadyQueued = await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM W_EMAIL_QUEUE WHERE EVENT_TYPE = 'LOW_STOCK' AND CREATED_AT >= @today",
            new { today = DateTime.Today });
        if (alreadyQueued > 0) return;

        var smtp = await emails.GetSettingsAsync(ct);
        var companyEmail = smtp.GetValueOrDefault("FROM_EMAIL");
        if (string.IsNullOrWhiteSpace(companyEmail)) return;

        var lines = string.Join("<br/>", low.Select(i =>
            $"- {i.Name}{(string.IsNullOrWhiteSpace(i.Code) ? "" : $" ({i.Code})")}: còn {i.Ton:N0} / tối thiểu {i.TonToiThieu:N0}"));
        _ = emails.EnqueueFromEventAsync("LOW_STOCK", [(companyEmail, "Quản trị")],
            new Dictionary<string, string?>
            {
                ["SO_MAT_HANG"] = low.Count.ToString(),
                ["DANH_SACH"] = lines,
                ["NGAY"] = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
            }, "INVENTORY", null, ct);

        Log_.Information("Low-stock scan: {Count} mặt hàng dưới tồn tối thiểu", low.Count);
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
        public string? Email { get; set; }
    }

    private sealed class LowStockRow
    {
        public string MatHangId { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Name { get; set; }
        public decimal TonToiThieu { get; set; }
        public decimal Ton { get; set; }
    }
}
