using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Data.MySql;
using TechPos.Shared;
using TechPos.Sync.Core;

namespace TechPos.Api.Controllers;

/// <summary>Sync Admin Dashboard API (mục 37/38) — chỉ SUPER_ADMIN / ADMIN / IT (mục 17).</summary>
[ApiController]
[Route("api/sync")]
[Authorize(Roles = "SUPER_ADMIN,ADMIN,IT")]
public sealed class SyncController(
    MySqlConnectionFactory mySql,
    SyncMetaRepository meta) : ControllerBase
{
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        await using var conn = await mySql.OpenAsync(ct);

        var heartbeat = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT STATE as State, LAST_RUN_AT as LastRunAt, DETAIL as Detail FROM W_SYNC_HEARTBEAT WHERE ID = 1");

        var lastReconcile = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT TABLE_NAME as TableName, STARTED_AT as StartedAt, FINISHED_AT as FinishedAt, MESSAGE as Message " +
            "FROM W_SYNC_LOG WHERE RUN_TYPE = 'RECONCILE' ORDER BY STARTED_AT DESC LIMIT 1");

        var cursors = (await conn.QueryAsync<dynamic>("""
            SELECT p.TABLE_NAME as TableName, p.GROUP_NAME as GroupName, p.INTERVAL_SEC as IntervalSec, p.ENABLED as Enabled,
                   c.LAST_MODIFIED as LastModified, c.LAST_ID as LastId, c.LAST_SUCCESS_AT as LastSuccessAt, c.LAST_COUNT as LastCount
            FROM W_SYNC_TABLE_POLICY p LEFT JOIN W_SYNC_CURSOR c ON c.TABLE_NAME = p.TABLE_NAME
            WHERE p.ENABLED = 1
            ORDER BY p.GROUP_NAME, p.TABLE_NAME
            LIMIT 30
            """)).ToList();

        var recentLogs = (await conn.QueryAsync<dynamic>(
            "SELECT TABLE_NAME as TableName, RUN_TYPE as RunType, ROWS_READ as RowsRead, ROWS_UPSERTED as RowsUpserted, " +
            "ROWS_SKIPPED as RowsSkipped, ERRORS as Errors, STARTED_AT as StartedAt, MESSAGE as Message " +
            "FROM W_SYNC_LOG ORDER BY STARTED_AT DESC LIMIT 20")).ToList();

        var rowErrors = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT SUM(CASE WHEN DEAD_LETTER = 0 THEN 1 ELSE 0 END) as Pending, " +
            "SUM(CASE WHEN DEAD_LETTER = 1 THEN 1 ELSE 0 END) as DeadLetter FROM W_SYNC_ROW_ERROR");

        var commands = (await conn.QueryAsync<dynamic>(
            "SELECT ID as Id, COMMAND as Command, PARAM as Param, STATUS as Status, CREATED_AT as CreatedAt, " +
            "FINISHED_AT as FinishedAt, RESULT as Result FROM W_SYNC_COMMAND ORDER BY CREATED_AT DESC LIMIT 10")).ToList();

        return Ok(ApiResult<object>.Ok(new
        {
            heartbeat, lastReconcile, cursors, recentLogs, rowErrors, commands
        }, HttpContext.TraceIdentifier));
    }

    /// <summary>Đưa lệnh RECONCILE_ALL vào hàng đợi — Sync Worker sẽ nhận và chạy (mục 37).</summary>
    [HttpPost("reconcile")]
    public async Task<IActionResult> TriggerReconcile(CancellationToken ct)
    {
        if (await meta.HasPendingCommandAsync("RECONCILE_ALL", ct))
            return Ok(ApiResult<string>.Fail("Đã có lệnh Reconcile đang chờ/chạy", HttpContext.TraceIdentifier));

        var id = await meta.EnqueueCommandAsync("RECONCILE_ALL", null, User.Identity?.Name, ct);
        return Ok(ApiResult<object>.Ok(new { commandId = id, message = "Đã đưa lệnh Reconcile vào hàng đợi" },
            HttpContext.TraceIdentifier));
    }

    /// <summary>Đưa lệnh RETRY_ROW_ERRORS vào hàng đợi (mục 38).</summary>
    [HttpPost("retry-row-errors")]
    public async Task<IActionResult> TriggerRetryRowErrors(CancellationToken ct)
    {
        var id = await meta.EnqueueCommandAsync("RETRY_ROW_ERRORS", null, User.Identity?.Name, ct);
        return Ok(ApiResult<object>.Ok(new { commandId = id }, HttpContext.TraceIdentifier));
    }

    /// <summary>Đưa lệnh MEDIA_EXTRACT (trích BLOB → file storage, mục 32) vào hàng đợi.</summary>
    [HttpPost("extract-media")]
    public async Task<IActionResult> TriggerExtractMedia(CancellationToken ct)
    {
        if (await meta.HasPendingCommandAsync("MEDIA_EXTRACT", ct))
            return Ok(ApiResult<string>.Fail("Đã có lệnh MEDIA_EXTRACT đang chờ/chạy", HttpContext.TraceIdentifier));

        var id = await meta.EnqueueCommandAsync("MEDIA_EXTRACT", null, User.Identity?.Name, ct);
        return Ok(ApiResult<object>.Ok(new { commandId = id, message = "Đã đưa lệnh MEDIA_EXTRACT vào hàng đợi" },
            HttpContext.TraceIdentifier));
    }

    [HttpGet("row-errors")]
    public async Task<IActionResult> GetRowErrors([FromQuery] bool dead = false, [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var rows = await meta.GetRowErrorsAsync(Math.Clamp(limit, 1, 200), dead ? 1 : 0, ct);
        return Ok(ApiResult<object>.Ok(rows, HttpContext.TraceIdentifier));
    }

    /// <summary>Đưa 1 row lỗi vào retry ngay (mục 38: nút Retry).</summary>
    [HttpPost("row-errors/{id}/requeue")]
    public async Task<IActionResult> RequeueRowError(string id, CancellationToken ct)
    {
        await using var conn = await mySql.OpenAsync(ct);
        var n = await conn.ExecuteAsync(
            "UPDATE W_SYNC_ROW_ERROR SET DEAD_LETTER = 0, RETRY_COUNT = 0, NEXT_RETRY_AT = @now WHERE ID = @id",
            new { id, now = DateTime.UtcNow });
        return n == 1
            ? Ok(ApiResult<object>.Ok(new { id }, HttpContext.TraceIdentifier))
            : Ok(ApiResult<string>.Fail("Không tìm thấy row error", HttpContext.TraceIdentifier));
    }

    /// <summary>Bỏ qua row lỗi (dead-letter, mục 38: nút Ignore).</summary>
    [HttpPost("row-errors/{id}/ignore")]
    public async Task<IActionResult> IgnoreRowError(string id, CancellationToken ct)
    {
        await using var conn = await mySql.OpenAsync(ct);
        var n = await conn.ExecuteAsync("UPDATE W_SYNC_ROW_ERROR SET DEAD_LETTER = 1 WHERE ID = @id", new { id });
        return n == 1
            ? Ok(ApiResult<object>.Ok(new { id }, HttpContext.TraceIdentifier))
            : Ok(ApiResult<string>.Fail("Không tìm thấy row error", HttpContext.TraceIdentifier));
    }

    /// <summary>Bật/tắt đồng bộ 1 bảng (W_SYNC_TABLE_POLICY — việc #6 README).</summary>
    [HttpPost("tables/{tableName}/toggle")]
    public async Task<IActionResult> ToggleTable(string tableName, [FromBody] bool enabled, CancellationToken ct)
    {
        await using var conn = await mySql.OpenAsync(ct);
        var n = await conn.ExecuteAsync(
            "UPDATE W_SYNC_TABLE_POLICY SET ENABLED = @e WHERE TABLE_NAME = @t",
            new { e = enabled, t = tableName.ToUpperInvariant() });
        return n == 1
            ? Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier))
            : Ok(ApiResult<string>.Fail($"Không tìm thấy bảng '{tableName}' trong policy", HttpContext.TraceIdentifier));
    }

    /// <summary>Danh sách policy đầy đủ (86 bảng × enabled — cho tab cấu hình Sync Admin).</summary>
    [HttpGet("tables")]
    public async Task<IActionResult> GetTables(CancellationToken ct)
    {
        await using var conn = await mySql.OpenAsync(ct);
        var rows = (await conn.QueryAsync<object>(
            "SELECT TABLE_NAME AS TableName, GROUP_NAME AS GroupName, INTERVAL_SEC AS IntervalSec, " +
            "ENABLED AS Enabled, PRIORITY AS Priority, BATCH_SIZE AS BatchSize " +
            "FROM W_SYNC_TABLE_POLICY ORDER BY GROUP_NAME, TABLE_NAME")).ToList();
        return Ok(ApiResult<object>.Ok(rows, HttpContext.TraceIdentifier));
    }

    // ---------- GĐ7: Outbox 2 chiều + xung đột ----------

    /// <summary>Thống kê outbox (pending/failed/conflicts) cho /admin/sync — GĐ7 §7.2.</summary>
    [HttpGet("outbox")]
    public async Task<IActionResult> GetOutbox([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        await using var conn = await mySql.OpenAsync(ct);
        var stats = await conn.QueryFirstAsync<object>(
            "SELECT COALESCE(SUM(CASE WHEN STATUS='PENDING' THEN 1 ELSE 0 END),0) AS Pending, " +
            "COALESCE(SUM(CASE WHEN STATUS='FAILED' THEN 1 ELSE 0 END),0) AS Failed, " +
            "COALESCE(SUM(CASE WHEN STATUS='CONFLICT' THEN 1 ELSE 0 END),0) AS Conflicts FROM W_OUTBOX");
        var recent = (await conn.QueryAsync<object>(
            "SELECT ID, TABLE_NAME AS TableName, ROW_ID AS RowId, OP, STATUS, RETRY_COUNT AS RetryCount, " +
            "LAST_ERROR AS LastError, CREATED_AT AS CreatedAt, SENT_AT AS SentAt " +
            "FROM W_OUTBOX ORDER BY CREATED_AT DESC LIMIT @lim", new { lim = Math.Clamp(limit, 1, 200) })).ToList();
        return Ok(ApiResult<object>.Ok(new { stats, recent }, HttpContext.TraceIdentifier));
    }

    /// <summary>Danh sách xung đột OPEN (GĐ7 §7.3 — người dùng chọn FB/WEB thắng).</summary>
    [HttpGet("conflicts")]
    public async Task<IActionResult> GetConflicts(CancellationToken ct)
    {
        await using var conn = await mySql.OpenAsync(ct);
        var rows = (await conn.QueryAsync<object>(
            "SELECT ID, TABLE_NAME AS TableName, ROW_ID AS RowId, FB_MODIFIED AS FbModified, " +
            "MY_MODIFIED AS MyModified, DETAIL, STATUS, DETECTED_AT AS DetectedAt " +
            "FROM W_CONFLICT WHERE STATUS = 'OPEN' ORDER BY DETECTED_AT DESC LIMIT 100")).ToList();
        return Ok(ApiResult<object>.Ok(rows, HttpContext.TraceIdentifier));
    }

    /// <summary>Giải xung đột: keep = FB_WINS | WEB_WINS (§7.3).</summary>
    [HttpPost("conflicts/{id}/resolve")]
    public async Task<IActionResult> ResolveConflict(string id, [FromBody] string keep, CancellationToken ct)
    {
        var status = keep.ToUpperInvariant() switch
        {
            "FB" => "FB_WINS",
            "WEB" => "WEB_WINS",
            _ => null
        };
        if (status is null)
            return Ok(ApiResult<string>.Fail("keep phải là 'FB' hoặc 'WEB'", HttpContext.TraceIdentifier));

        await using var conn = await mySql.OpenAsync(ct);
        var n = await conn.ExecuteAsync(
            "UPDATE W_CONFLICT SET STATUS = @s, RESOLVED_AT = @now WHERE ID = @id AND STATUS = 'OPEN'",
            new { s = status, now = DateTime.UtcNow, id });
        return n == 1
            ? Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier))
            : Ok(ApiResult<string>.Fail("Không tìm thấy xung đột đang mở", HttpContext.TraceIdentifier));
    }
}
