using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository Email (Phase 7 — EMAIL-ARCHITECTURE §1–4): hàng đợi W_EMAIL_QUEUE,
/// template, event, settings, log. Enqueue fire-and-forget — chỉ INSERT, không throws
/// nghiệp vụ khi lỗi (catch ở tầng gọi).
/// </summary>
public sealed class EmailRepository
{
    private readonly MySqlConnectionFactory _factory;
    public EmailRepository(MySqlConnectionFactory factory) => _factory = factory;

    // ---------- Enqueue (fire-and-forget) ----------

    /// <summary>
    /// Đưa email vào hàng đợi (STATUS='PENDING'). KHÔNG gửi ngay, không ném exception ra ngoài
    /// khi DB lỗi — trả về null (gọi log riêng). Bảo đảm "email fail không fail transaction" (mục 23).
    /// </summary>
    public async Task<string?> EnqueueAsync(string eventType, string subject, string? bodyHtml,
        IReadOnlyList<(string Email, string? Name)> recipients,
        string? referenceType = null, string? referenceId = null, CancellationToken ct = default)
    {
        try
        {
            var id = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow;
            await using var conn = await _factory.OpenAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);
            await conn.ExecuteAsync(
                """
                INSERT INTO W_EMAIL_QUEUE (ID, EVENT_TYPE, REFERENCE_TYPE, REFERENCE_ID, SUBJECT, BODY_HTML, STATUS, CREATED_AT)
                VALUES (@id, @ev, @rt, @ri, @sj, @bh, 'PENDING', @now)
                """,
                new { id, ev = eventType, rt = referenceType, ri = referenceId, sj = subject, bh = bodyHtml, now }, tx);
            foreach (var (email, name) in recipients.Where(r => !string.IsNullOrWhiteSpace(r.Email)))
            {
                await conn.ExecuteAsync(
                    "INSERT INTO W_EMAIL_RECIPIENT (ID, QUEUE_ID, TYPE_, EMAIL, NAME, STATUS) " +
                    "VALUES (@id, @qid, 'TO', @email, @name, 'PENDING')",
                    new { id = Guid.NewGuid().ToString(), qid = id, email = email.Trim(), name }, tx);
            }
            tx.Commit();
            return id;
        }
        catch (Exception)
        {
            return null;   // fire-and-forget (mục 23)
        }
    }

    // ---------- Worker: claim + retry ----------

    public sealed class QueueRow
    {
        public string Id { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? BodyHtml { get; set; }
        public int RetryCount { get; set; }
    }

    public sealed class RecipientRow
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    /// <summary>Claim tối đa @limit email PENDING (đến lượt retry) — atomic UPDATE.</summary>
    public async Task<List<QueueRow>> ClaimPendingAsync(int limit, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var ids = (await conn.QueryAsync<string>(
            """
            SELECT ID FROM W_EMAIL_QUEUE
            WHERE STATUS = 'PENDING' AND (NEXT_RETRY_AT IS NULL OR NEXT_RETRY_AT <= @now)
            ORDER BY CREATED_AT LIMIT @lim
            """, new { now = DateTime.UtcNow, lim = limit })).ToList();
        if (ids.Count == 0) return [];

        var claimed = (await conn.QueryAsync<QueueRow>(
            """
            UPDATE W_EMAIL_QUEUE SET STATUS = 'PROCESSING'
            WHERE ID IN @ids AND STATUS = 'PENDING';
            SELECT ID, SUBJECT AS Subject, BODY_HTML AS BodyHtml, RETRY_COUNT AS RetryCount
            FROM W_EMAIL_QUEUE WHERE ID IN @ids AND STATUS = 'PROCESSING'
            """, new { ids })).ToList();
        return claimed;
    }

    public async Task<List<RecipientRow>> GetPendingRecipientsAsync(string queueId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<RecipientRow>(
            "SELECT ID, EMAIL, NAME FROM W_EMAIL_RECIPIENT WHERE QUEUE_ID = @q AND STATUS = 'PENDING'",
            new { q = queueId })).ToList();
    }

    /// <summary>Backoff 1/5/15/30 phút; quá 4 lần → FAILED vĩnh viễn (EMAIL-ARCHITECTURE §1).</summary>
    public async Task<bool> MarkSentAsync(string queueId, string recipientId, string? smtpResponse, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            "UPDATE W_EMAIL_RECIPIENT SET STATUS = 'SENT' WHERE ID = @rid",
            new { rid = recipientId });
        await conn.ExecuteAsync(
            """
            INSERT INTO W_EMAIL_LOG (ID, QUEUE_ID, EMAIL, SUBJECT, STATUS, SMTP_RESPONSE, SENT_AT)
            SELECT @logId, @qid, EMAIL, SUBJECT, 'SENT', @resp, @now FROM W_EMAIL_QUEUE WHERE ID = @qid
            """, new { logId = Guid.NewGuid().ToString(), qid = queueId, resp = smtpResponse, now = DateTime.UtcNow });
        var remaining = await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM W_EMAIL_RECIPIENT WHERE QUEUE_ID = @q AND STATUS = 'PENDING'", new { q = queueId });
        if (remaining == 0)
        {
            await conn.ExecuteAsync(
                "UPDATE W_EMAIL_QUEUE SET STATUS = 'SENT', SENT_AT = @now WHERE ID = @q",
                new { q = queueId, now = DateTime.UtcNow });
            return true;
        }
        return false;
    }

    /// <summary>Đánh dấu 1 người nhận lỗi; trả về true nếu cả queue FAILED (hết retry).</summary>
    public async Task<bool> MarkRecipientFailedAsync(string queueId, string recipientId, string error, int retryCount, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        int[] backoff = [1, 5, 15, 30];
        var dead = retryCount >= 4;
        await conn.ExecuteAsync(
            "UPDATE W_EMAIL_RECIPIENT SET STATUS = @s, ERROR = @e WHERE ID = @rid",
            new { rid = recipientId, s = dead ? "FAILED" : "PENDING", e = error });
        await conn.ExecuteAsync(
            """
            INSERT INTO W_EMAIL_LOG (ID, QUEUE_ID, EMAIL, SUBJECT, STATUS, ERROR, RETRY_COUNT, SENT_AT)
            SELECT @logId, @qid, EMAIL, SUBJECT, 'FAILED', @e, @rc, @now FROM W_EMAIL_RECIPIENT WHERE ID = @rid
            """, new { logId = Guid.NewGuid().ToString(), qid = queueId, e = error, rc = retryCount, now = DateTime.UtcNow });
        var minutes = backoff[Math.Min(Math.Max(retryCount - 1, 0), backoff.Length - 1)];
        await conn.ExecuteAsync(
            """
            UPDATE W_EMAIL_QUEUE
            SET RETRY_COUNT = @rc, NEXT_RETRY_AT = @next, LAST_ERROR = @e,
                STATUS = CASE WHEN @dead THEN 'FAILED' ELSE 'PENDING' END
            WHERE ID = @qid
            """, new { rc = retryCount, next = DateTime.UtcNow.AddMinutes(minutes), e = error, dead, qid = queueId });
        return dead;
    }

    // ---------- Admin: template / event / queue / settings ----------

    public async Task<List<EmailTemplateDto>> GetTemplatesAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<EmailTemplateDto>(
            "SELECT ID, CODE, NAME, SUBJECT, BODY_HTML AS BodyHtml, BODY_TEXT AS BodyText, " +
            "IS_ACTIVE AS IsActive, CREATED_AT AS CreatedAt, UPDATED_AT AS UpdatedAt " +
            "FROM W_EMAIL_TEMPLATE ORDER BY CODE")).ToList();
    }

    public async Task UpdateTemplateAsync(string code, EmailTemplateUpdateRequest req, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE W_EMAIL_TEMPLATE
            SET SUBJECT = @sj, BODY_HTML = @bh, BODY_TEXT = @bt, IS_ACTIVE = @act, UPDATED_AT = @now
            WHERE CODE = @code
            """, new { code, sj = req.Subject, bh = req.BodyHtml, bt = req.BodyText, act = req.IsActive, now = DateTime.UtcNow });
    }

    public async Task<List<EmailEventDto>> GetEventsAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<EmailEventDto>(
            "SELECT ID, CODE, NAME, TEMPLATE_CODE AS TemplateCode, ENABLED AS Enabled, " +
            "CAST(RECIPIENT_RULE AS CHAR) AS RecipientRule FROM W_EMAIL_EVENT ORDER BY CODE")).ToList();
    }

    public async Task SetEventEnabledAsync(string code, bool enabled, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync("UPDATE W_EMAIL_EVENT SET ENABLED = @e, UPDATED_AT = @now WHERE CODE = @c",
            new { e = enabled, c = code, now = DateTime.UtcNow });
    }

    public async Task<EmailEventDto?> GetEventAsync(string eventType, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<EmailEventDto>(
            "SELECT ID, CODE, NAME, TEMPLATE_CODE AS TemplateCode, ENABLED AS Enabled, " +
            "CAST(RECIPIENT_RULE AS CHAR) AS RecipientRule FROM W_EMAIL_EVENT WHERE CODE = @c",
            new { c = eventType });
    }

    public async Task<EmailTemplateDto?> GetTemplateByCodeAsync(string code, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<EmailTemplateDto>(
            "SELECT ID, CODE, NAME, SUBJECT, BODY_HTML AS BodyHtml, BODY_TEXT AS BodyText, " +
            "IS_ACTIVE AS IsActive, CREATED_AT AS CreatedAt, UPDATED_AT AS UpdatedAt " +
            "FROM W_EMAIL_TEMPLATE WHERE CODE = @c AND IS_ACTIVE = 1", new { c = code });
    }

    public async Task<(IReadOnlyList<EmailQueueItemDto> Items, long Total)> SearchQueueAsync(
        string? status, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, Paging.MaxPageSize);
        var where = "WHERE 1=1";
        var dp = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(status)) { where += " AND q.STATUS = @st"; dp.Add("@st", status.ToUpperInvariant()); }

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM W_EMAIL_QUEUE q {where}", dp);
        var items = (await conn.QueryAsync<EmailQueueItemDto>(
            $"""
            SELECT q.ID, q.EVENT_TYPE AS EventType, q.REFERENCE_TYPE AS ReferenceType,
                   q.SUBJECT AS Subject, q.STATUS, q.RETRY_COUNT AS RetryCount,
                   q.NEXT_RETRY_AT AS NextRetryAt, q.LAST_ERROR AS LastError,
                   q.CREATED_AT AS CreatedAt, q.SENT_AT AS SentAt,
                   (SELECT COUNT(*) FROM W_EMAIL_RECIPIENT r WHERE r.QUEUE_ID = q.ID) AS SoNguoiNhan
            FROM W_EMAIL_QUEUE q {where}
            ORDER BY q.CREATED_AT DESC LIMIT @lim OFFSET @off
            """, Merge(dp, new { lim = pageSize, off = (page - 1) * pageSize }))).ToList();
        return (items, total);
    }

    public async Task<bool> RetryQueueItemAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.ExecuteAsync(
            "UPDATE W_EMAIL_QUEUE SET STATUS = 'PENDING', NEXT_RETRY_AT = NULL, LAST_ERROR = NULL WHERE ID = @id AND STATUS = 'FAILED'",
            new { id });
        await conn.ExecuteAsync("UPDATE W_EMAIL_RECIPIENT SET STATUS = 'PENDING' WHERE QUEUE_ID = @id AND STATUS = 'FAILED'", new { id });
        return rows > 0;
    }

    public async Task<bool> CancelQueueItemAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.ExecuteAsync(
            "UPDATE W_EMAIL_QUEUE SET STATUS = 'CANCELLED' WHERE ID = @id AND STATUS IN ('PENDING','FAILED')", new { id });
        return rows > 0;
    }

    // ---------- Settings (SMTP) ----------

    public async Task<Dictionary<string, string?>> GetSettingsAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<(string Key, string? Value)>(
            "SELECT KEY_, VALUE_ FROM W_EMAIL_SETTINGS");
        return rows.ToDictionary(r => r.Key, r => r.Value);
    }

    public async Task SaveSettingAsync(string key, string? value, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO W_EMAIL_SETTINGS (KEY_, VALUE_, UPDATED_AT) VALUES (@k, @v, @now)
            ON DUPLICATE KEY UPDATE VALUE_ = @v, UPDATED_AT = @now
            """, new { k = key, v = value, now = DateTime.UtcNow });
    }

    public async Task<List<(string Email, string Status, string? Error, DateTime? SentAt)>> GetQueueRecipientsAsync(string queueId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<(string Email, string Status, string? Error, DateTime? SentAt)>(
            "SELECT EMAIL, STATUS, ERROR, NULL FROM W_EMAIL_RECIPIENT WHERE QUEUE_ID = @q", new { q = queueId });
        return rows.ToList();
    }

    /// <summary>Endpoint nội bộ cho EmailWorker trả PROCESSING → PENDING (SMTP chưa cấu hình).</summary>
    public async Task<MySqlConnection> OpenProbeAsync(CancellationToken ct = default)
        => await _factory.OpenAsync(ct);

    private static DynamicParameters Merge(DynamicParameters a, object b)
    {
        var dp = new DynamicParameters();
        foreach (var p in a.ParameterNames) dp.Add(p, a.Get<object?>(p));
        dp.AddDynamicParams(b);
        return dp;
    }
}
