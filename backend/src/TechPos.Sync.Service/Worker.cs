using Serilog;
using TechPos.Data.Firebird;
using TechPos.Data.MySql;
using TechPos.Shared;
using TechPos.Sync.Core;

namespace TechPos.Sync.Service;

/// <summary>
/// Dispatcher chính (mục 7/8/38): vòng lặp 5 giây —
/// 1) W1 incremental theo W_SYNC_TABLE_POLICY, 2) nhận lệnh API qua W_SYNC_COMMAND,
/// 3) retry row lỗi đến hạn (backoff → dead letter), 4) reconcile 02:00 hằng ngày (W2, giờ VN),
/// 5) heartbeat mỗi vòng.
/// </summary>
public sealed class SyncWorker(
    SyncMetaRepository meta,
    WatermarkSyncEngine engine,
    ReconcileEngine reconcile,
    MediaExtractor media,
    SyncPolicySeeder seeder,
    TwoWaySyncEngine twoWay) : BackgroundService
{
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(5);
    private static readonly TimeZoneInfo VnTz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
    private readonly Dictionary<string, DateTime> _nextRunAt = new();
    private DateTime? _lastReconcileDate;
    private DateTime? _lastMediaDate;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Information("TechPos Sync Service starting...");
        await seeder.SeedDefaultPoliciesAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await meta.HeartbeatAsync("RUNNING", null, stoppingToken);

                // 1) W1 — incremental theo policy
                var policies = await meta.GetPoliciesAsync(stoppingToken);
                var now = DateTime.UtcNow;
                foreach (var p in policies.Where(p => p.Enabled))
                {
                    if (!_nextRunAt.TryGetValue(p.TableName, out var due) || due <= now)
                    {
                        Log.Information("Syncing {Table} (group {Group})", p.TableName, p.GroupName);
                        var result = await engine.SyncTableAsync(p.TableName, p.BatchSize, stoppingToken);
                        _nextRunAt[p.TableName] = now.AddSeconds(p.IntervalSec);
                        if (result.Errors > 0)
                            Log.Warning("{Table}: {Read} rows read, {Err} errors", result.TableName, result.RowsRead, result.Errors);
                    }
                }

                // 2) Lệnh từ API (W_SYNC_COMMAND)  |  3) retry row lỗi
                await ProcessCommandsAsync(stoppingToken);
                var retried = await engine.RetryRowErrorsAsync(50, stoppingToken);
                if (retried.Processed > 0)
                    Log.Information("Row-error retry: processed={P} recovered={R} deadLettered={D}",
                        retried.Processed, retried.Recovered, retried.DeadLettered);

                // 4) W2 — Reconcile 02:00 hằng ngày (giờ VN)
                var vnNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, VnTz);
                if (vnNow.Hour == 2 && (_lastReconcileDate is null || _lastReconcileDate.Value.Date != vnNow.Date))
                {
                    _lastReconcileDate = vnNow.Date;
                    Log.Information("Scheduled nightly reconcile starting...");
                    var results = await reconcile.ReconcileAllAsync(stoppingToken);
                    Log.Information("Nightly reconcile done: {Tables} tables, missing={M}, changed={C}, tombstoned={T}, errors={E}",
                        results.Count, results.Sum(r => r.Missing), results.Sum(r => r.Changed),
                        results.Sum(r => r.Tombstoned), results.Sum(r => r.Errors));
                }

                // 5) Media/BLOB extractor 02:30 hằng ngày (giờ VN, mục 8)
                if (vnNow.Hour == 2 && vnNow.Minute >= 30 &&
                    (_lastMediaDate is null || _lastMediaDate.Value.Date != vnNow.Date))
                {
                    _lastMediaDate = vnNow.Date;
                    Log.Information("Scheduled media extraction starting...");
                    var mediaResults = await media.ExtractAllAsync(stoppingToken);
                    Log.Information("Media extraction done: {Cols} columns, extracted={X}, errors={E}",
                        mediaResults.Count, mediaResults.Sum(r => r.Extracted), mediaResults.Sum(r => r.Errors));
                }

                // 6) GĐ7 — outbox 2 chiều (no-op khi ENABLE_TWO_WAY_SYNC=false — vàng 6)
                var tw = await twoWay.ProcessPendingAsync(stoppingToken);
                if (tw.Sent > 0 || tw.Conflicts > 0 || tw.Failed > 0)
                    Log.Information("Two-way outbox: sent={S} conflicts={C} failed={F}", tw.Sent, tw.Conflicts, tw.Failed);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Log.Error(ex, "Sync loop error — sẽ thử lại sau {Retry}", Tick);
                await meta.HeartbeatAsync("ERROR", ex.Message, stoppingToken);
            }

            await Task.Delay(Tick, stoppingToken);
        }
    }

    /// <summary>Nhận và chạy lệnh do API đưa vào W_SYNC_COMMAND (mục 37: nút Reconcile ngay trên Sync Admin).</summary>
    private async Task ProcessCommandsAsync(CancellationToken ct)
    {
        var commands = await meta.GetPendingCommandsAsync(5, ct);
        foreach (var cmd in commands)
        {
            Log.Information("Executing sync command {Command} ({Id})", cmd.Command, cmd.Id);
            await meta.MarkCommandRunningAsync(cmd.Id, ct);
            try
            {
                switch (cmd.Command.ToUpperInvariant())
                {
                    case "RECONCILE_ALL":
                    {
                        var results = await reconcile.ReconcileAllAsync(ct);
                        var summary = $"{results.Count} tables; missing={results.Sum(r => r.Missing)} " +
                                      $"changed={results.Sum(r => r.Changed)} tombstoned={results.Sum(r => r.Tombstoned)} " +
                                      $"errors={results.Sum(r => r.Errors)}";
                        await meta.MarkCommandDoneAsync(cmd.Id, results.All(r => r.Errors == 0), summary, ct);
                        break;
                    }
                    case "RECONCILE_TABLE" when !string.IsNullOrWhiteSpace(cmd.Param):
                    {
                        var r = await reconcile.ReconcileTableAsync(cmd.Param, 5000, ct);
                        await meta.MarkCommandDoneAsync(cmd.Id, r.Errors == 0,
                            $"fb={r.FbRows} my={r.MyRows} missing={r.Missing} changed={r.Changed} " +
                            $"tombstoned={r.Tombstoned} errors={r.Errors}", ct);
                        break;
                    }
                    case "RETRY_ROW_ERRORS":
                    {
                        var r = await engine.RetryRowErrorsAsync(500, ct);
                        await meta.MarkCommandDoneAsync(cmd.Id, true,
                            $"processed={r.Processed} recovered={r.Recovered} deadLettered={r.DeadLettered}", ct);
                        break;
                    }
                    case "MEDIA_EXTRACT":
                    {
                        var mr = await media.ExtractAllAsync(ct);
                        await meta.MarkCommandDoneAsync(cmd.Id, mr.All(r => r.Errors == 0),
                            $"columns={mr.Count} extracted={mr.Sum(r => r.Extracted)} errors={mr.Sum(r => r.Errors)}", ct);
                        break;
                    }
                    default:
                        await meta.MarkCommandDoneAsync(cmd.Id, false, $"Lệnh không rõ: {cmd.Command}", ct);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Command {Command} failed", cmd.Command);
                await meta.MarkCommandDoneAsync(cmd.Id, false, ex.Message, ct);
            }
        }
    }
}
