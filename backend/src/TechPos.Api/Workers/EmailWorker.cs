using Serilog;
using Dapper;
using TechPos.Application.Notifications;
using TechPos.Data.MySql;

namespace TechPos.Api.Workers;

/// <summary>
/// Email Worker (Phase 7 — EMAIL-ARCHITECTURE §1): poll W_EMAIL_QUEUE PENDING mỗi 20 giây,
/// claim atomic, gửi SMTP từng người nhận, retry backoff 1/5/15/30 phút (tối đa 4 lần).
/// Mọi exception bị nuốt + log — worker không bao giờ chết, nghiệp vụ không bao giờ bị ảnh hưởng.
/// </summary>
public sealed class EmailWorker(EmailRepository repo, IEmailSender sender) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(20);
    private static readonly Serilog.ILogger Log_ = Serilog.Log.ForContext<EmailWorker>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log_.Information("EmailWorker started (poll {Interval}s)", PollInterval.TotalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Log_.Error(ex, "EmailWorker loop error — tiếp tục (email fail không fail hệ thống)");
            }
            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
        Log_.Information("EmailWorker stopped");
    }

    /// <summary>Xử lý 1 lô email — public internal để unit test (retry backoff).</summary>
    internal async Task<int> ProcessPendingAsync(CancellationToken ct = default)
    {
        var batch = await repo.ClaimPendingAsync(10, ct);
        if (batch.Count == 0) return 0;

        var s = await repo.GetSettingsAsync(ct);
        var host = s.GetValueOrDefault("HOST");
        if (string.IsNullOrWhiteSpace(host))
        {
            // Chưa cấu hình SMTP — bỏ qua vòng này (đã claim PROCESSING ⇒ trả lại PENDING).
            await ReleaseAllAsync(ct);
            return 0;
        }

        var port = int.TryParse(s.GetValueOrDefault("PORT"), out var p) ? p : 587;
        var useSsl = s.GetValueOrDefault("USE_SSL") != "0";
        var user = s.GetValueOrDefault("USERNAME");
        var pass = s.GetValueOrDefault("PASSWORD");
        var fromEmail = s.GetValueOrDefault("FROM_EMAIL") ?? "no-reply@localhost";
        var fromName = s.GetValueOrDefault("FROM_NAME");

        foreach (var q in batch)
        {
            var recipients = await repo.GetPendingRecipientsAsync(q.Id, ct);
            foreach (var r in recipients)
            {
                var (sent, response, error) = await sender.SendAsync(
                    host, port, useSsl, user, pass, fromEmail, fromName,
                    r.Email, r.Name, q.Subject, q.BodyHtml, ct);
                if (sent)
                {
                    await repo.MarkSentAsync(q.Id, r.Id, response, ct);
                }
                else
                {
                    var dead = await repo.MarkRecipientFailedAsync(q.Id, r.Id, error ?? "SMTP error", q.RetryCount + 1, ct);
                    if (dead)
                        Log_.Warning("Email {QueueId} FAILED vĩnh viễn sau {Retries} lần retry (đến {Email})",
                            q.Id, q.RetryCount + 1, r.Email);
                }
            }
        }
        return batch.Count;
    }

    /// <summary>Trả mọi PROCESSING về PENDING (SMTP chưa cấu hình — thử lại sau khi admin cấu hình).</summary>
    private async Task ReleaseAllAsync(CancellationToken ct)
    {
        try
        {
            await using var conn = await repo.OpenProbeAsync(ct);
            await conn.ExecuteAsync("UPDATE W_EMAIL_QUEUE SET STATUS='PENDING' WHERE STATUS='PROCESSING'");
        }
        catch (Exception ex)
        {
            Log_.Warning(ex, "EmailWorker release-all lỗi — bỏ qua");
        }
    }
}
