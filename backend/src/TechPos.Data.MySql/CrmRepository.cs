using Dapper;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository CSKH (Phase 8): W_CUSTOMER_CARE / W_CUSTOMER_FEEDBACK / W_MESSAGE /
/// W_LOYALTY_TXN + đọc mirror DKHACHHANG/TDONHANG.
/// </summary>
public sealed class CrmRepository
{
    private readonly MySqlConnectionFactory _factory;
    public CrmRepository(MySqlConnectionFactory factory) => _factory = factory;

    // ---------- Khách hàng ----------

    public async Task<CustomerContactRow?> GetCustomerContactAsync(string customerId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<CustomerContactRow>(
            """
            SELECT ID AS Id, NAME AS TenKhach, DIENTHOAI AS DienThoai, EMAIL AS Email,
                   CAST(COALESCE(DIEMTICHLUYBANDAU,0) AS SIGNED) AS Points
            FROM DKHACHHANG WHERE ID = @id
            """, new { id = customerId });
    }

    public sealed class CustomerContactRow
    {
        public string Id { get; set; } = string.Empty;
        public string? TenKhach { get; set; }
        public string? DienThoai { get; set; }
        public string? Email { get; set; }
        public int Points { get; set; }
    }

    // ---------- Chăm sóc ----------

    public async Task<(IReadOnlyList<CustomerCareDto> Items, long Total)> SearchCareAsync(
        CustomerCareQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var where = "WHERE 1=1";
        var dp = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where += " AND (k.NAME LIKE @s OR k.DIENTHOAI LIKE @s OR c.CONTENT LIKE @s)";
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(q.CareType))
        {
            where += " AND c.CARE_TYPE = @ct";
            dp.Add("@ct", q.CareType.Trim().ToUpperInvariant());
        }
        if (q.FromDate.HasValue) { where += " AND c.CREATED_AT >= @from"; dp.Add("@from", q.FromDate.Value.Date); }
        if (q.ToDate.HasValue) { where += " AND c.CREATED_AT < DATE_ADD(@to, INTERVAL 1 DAY)"; dp.Add("@to", q.ToDate.Value.Date); }

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM W_CUSTOMER_CARE c JOIN DKHACHHANG k ON k.ID = c.CUSTOMER_ID {where}", dp);
        var items = (await conn.QueryAsync<CustomerCareDto>(
            $"""
            SELECT c.ID, c.CUSTOMER_ID AS CustomerId, k.NAME AS TenKhach,
                   k.DIENTHOAI AS DienThoai, k.EMAIL AS Email,
                   c.CARE_TYPE AS CareType, c.CONTENT AS Content, c.RESULT AS Result,
                   c.NEXT_CARE_AT AS NextCareAt, c.ASSIGNEE_USER_ID AS AssigneeUserId,
                   u.FULL_NAME AS AssigneeName, c.CREATED_BY AS CreatedBy,
                   c.CREATED_AT AS CreatedAt, c.UPDATED_AT AS UpdatedAt
            FROM W_CUSTOMER_CARE c
            JOIN DKHACHHANG k ON k.ID = c.CUSTOMER_ID
            LEFT JOIN W_USER u ON u.ID = c.ASSIGNEE_USER_ID
            {where}
            ORDER BY c.CREATED_AT DESC
            LIMIT @lim OFFSET @off
            """, Merge(dp, new { lim = pageSize, off = (page - 1) * pageSize }))).ToList();
        return (items, total);
    }

    public async Task<CustomerCareDto?> GetCareAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<CustomerCareDto>(
            """
            SELECT c.ID, c.CUSTOMER_ID AS CustomerId, k.NAME AS TenKhach,
                   k.DIENTHOAI AS DienThoai, k.EMAIL AS Email,
                   c.CARE_TYPE AS CareType, c.CONTENT AS Content, c.RESULT AS Result,
                   c.NEXT_CARE_AT AS NextCareAt, c.ASSIGNEE_USER_ID AS AssigneeUserId,
                   u.FULL_NAME AS AssigneeName, c.CREATED_BY AS CreatedBy,
                   c.CREATED_AT AS CreatedAt, c.UPDATED_AT AS UpdatedAt
            FROM W_CUSTOMER_CARE c
            JOIN DKHACHHANG k ON k.ID = c.CUSTOMER_ID
            LEFT JOIN W_USER u ON u.ID = c.ASSIGNEE_USER_ID
            WHERE c.ID = @id
            """, new { id });
    }

    public async Task<string> InsertCareAsync(CustomerCareCreateRequest req, string userId, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO W_CUSTOMER_CARE
            (ID, CUSTOMER_ID, CARE_TYPE, CONTENT, RESULT, NEXT_CARE_AT, ASSIGNEE_USER_ID, CREATED_BY, CREATED_AT)
            VALUES (@id, @cid, @type, @content, @result, @next, @assignee, @uid, @now)
            """,
            new
            {
                id, cid = req.CustomerId, type = req.CareType.Trim().ToUpperInvariant(),
                content = req.Content, result = req.Result, next = req.NextCareAt,
                assignee = (object?)req.AssigneeUserId ?? DBNull.Value, uid = userId, now = DateTime.UtcNow
            });
        return id;
    }

    public async Task<int> UpdateCareAsync(string id, CustomerCareUpdateRequest req, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteAsync(
            """
            UPDATE W_CUSTOMER_CARE SET
              CARE_TYPE = COALESCE(@type, CARE_TYPE),
              CONTENT = @content,
              RESULT = COALESCE(@result, RESULT),
              NEXT_CARE_AT = @next,
              ASSIGNEE_USER_ID = COALESCE(@assignee, ASSIGNEE_USER_ID),
              CREATED_BY = COALESCE(CREATED_BY, @uid),
              UPDATED_AT = @now
            WHERE ID = @id
            """,
            new
            {
                id,
                type = string.IsNullOrWhiteSpace(req.CareType) ? null : req.CareType.Trim().ToUpperInvariant(),
                content = req.Content, result = req.Result, next = req.NextCareAt,
                assignee = (object?)req.AssigneeUserId ?? DBNull.Value, uid = userId, now = DateTime.UtcNow
            });
    }

    public async Task<int> DeleteCareAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteAsync("DELETE FROM W_CUSTOMER_CARE WHERE ID = @id", new { id });
    }

    // ---------- Phản hồi ----------

    public async Task<(IReadOnlyList<CustomerFeedbackDto> Items, long Total)> SearchFeedbackAsync(
        CustomerFeedbackQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var where = "WHERE 1=1";
        var dp = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where += " AND (k.NAME LIKE @s OR f.CONTENT LIKE @s OR h.NAME LIKE @s)";
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }
        if (!string.IsNullOrWhiteSpace(q.Status))
        {
            where += " AND f.STATUS = @st";
            dp.Add("@st", q.Status.Trim().ToUpperInvariant());
        }
        if (q.Rating.HasValue) { where += " AND f.RATING = @r"; dp.Add("@r", q.Rating.Value); }

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            $"""
            SELECT COUNT(*) FROM W_CUSTOMER_FEEDBACK f
            LEFT JOIN DKHACHHANG k ON k.ID = f.CUSTOMER_ID
            LEFT JOIN TDONHANG h ON h.ID = f.INVOICE_ID
            {where}
            """, dp);
        var items = (await conn.QueryAsync<CustomerFeedbackDto>(
            $"""
            SELECT f.ID, f.CUSTOMER_ID AS CustomerId, k.NAME AS TenKhach,
                   f.INVOICE_ID AS InvoiceId, h.NAME AS SoHoaDon,
                   f.RATING AS Rating, f.CONTENT AS Content, f.STATUS AS Status,
                   f.RESPONSE AS Response, f.CREATED_BY AS CreatedBy,
                   f.CREATED_AT AS CreatedAt, f.RESPONDED_BY AS RespondedBy, f.RESPONDED_AT AS RespondedAt
            FROM W_CUSTOMER_FEEDBACK f
            LEFT JOIN DKHACHHANG k ON k.ID = f.CUSTOMER_ID
            LEFT JOIN TDONHANG h ON h.ID = f.INVOICE_ID
            {where}
            ORDER BY f.CREATED_AT DESC
            LIMIT @lim OFFSET @off
            """, Merge(dp, new { lim = pageSize, off = (page - 1) * pageSize }))).ToList();
        return (items, total);
    }

    public async Task<CustomerFeedbackDto?> GetFeedbackAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<CustomerFeedbackDto>(
            """
            SELECT f.ID, f.CUSTOMER_ID AS CustomerId, k.NAME AS TenKhach,
                   f.INVOICE_ID AS InvoiceId, h.NAME AS SoHoaDon,
                   f.RATING AS Rating, f.CONTENT AS Content, f.STATUS AS Status,
                   f.RESPONSE AS Response, f.CREATED_BY AS CreatedBy,
                   f.CREATED_AT AS CreatedAt, f.RESPONDED_BY AS RespondedBy, f.RESPONDED_AT AS RespondedAt
            FROM W_CUSTOMER_FEEDBACK f
            LEFT JOIN DKHACHHANG k ON k.ID = f.CUSTOMER_ID
            LEFT JOIN TDONHANG h ON h.ID = f.INVOICE_ID
            WHERE f.ID = @id
            """, new { id });
    }

    public async Task<string> InsertFeedbackAsync(CustomerFeedbackCreateRequest req, string userId, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO W_CUSTOMER_FEEDBACK
            (ID, CUSTOMER_ID, INVOICE_ID, RATING, CONTENT, STATUS, CREATED_BY, CREATED_AT)
            VALUES (@id, @cid, @iid, @rating, @content, 'NEW', @uid, @now)
            """,
            new
            {
                id, cid = (object?)req.CustomerId ?? DBNull.Value,
                iid = (object?)req.InvoiceId ?? DBNull.Value,
                rating = req.Rating, content = req.Content, uid = userId, now = DateTime.UtcNow
            });
        return id;
    }

    public async Task<int> RespondFeedbackAsync(string id, CustomerFeedbackRespondRequest req, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.ExecuteAsync(
            """
            UPDATE W_CUSTOMER_FEEDBACK
            SET RESPONSE = @response, STATUS = @status,
                RESPONDED_BY = @uid, RESPONDED_AT = @now
            WHERE ID = @id
            """,
            new
            {
                id, response = req.Response,
                status = string.IsNullOrWhiteSpace(req.Status) ? "RESOLVED" : req.Status.Trim().ToUpperInvariant(),
                uid = userId, now = DateTime.UtcNow
            });
    }

    // ---------- Tin nhắn ----------

    public async Task<(IReadOnlyList<MessageDto> Items, long Total)> SearchMessagesAsync(
        MessageQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var where = "WHERE 1=1";
        var dp = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(q.Channel))
        {
            where += " AND m.CHANNEL = @ch";
            dp.Add("@ch", q.Channel.Trim().ToUpperInvariant());
        }
        if (!string.IsNullOrWhiteSpace(q.Status))
        {
            where += " AND m.STATUS = @st";
            dp.Add("@st", q.Status.Trim().ToUpperInvariant());
        }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where += " AND (m.RECIPIENT LIKE @s OR m.SUBJECT LIKE @s OR m.BODY LIKE @s OR k.NAME LIKE @s)";
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM W_MESSAGE m LEFT JOIN DKHACHHANG k ON k.ID = m.CUSTOMER_ID {where}", dp);
        var items = (await conn.QueryAsync<MessageDto>(
            $"""
            SELECT m.ID, m.CHANNEL AS Channel, m.CUSTOMER_ID AS CustomerId, k.NAME AS TenKhach,
                   m.RECIPIENT AS Recipient, m.SUBJECT AS Subject, m.BODY AS Body,
                   m.STATUS AS Status, m.ERROR AS Error, m.CREATED_BY AS CreatedBy,
                   m.CREATED_AT AS CreatedAt, m.SENT_AT AS SentAt
            FROM W_MESSAGE m
            LEFT JOIN DKHACHHANG k ON k.ID = m.CUSTOMER_ID
            {where}
            ORDER BY m.CREATED_AT DESC
            LIMIT @lim OFFSET @off
            """, Merge(dp, new { lim = pageSize, off = (page - 1) * pageSize }))).ToList();
        return (items, total);
    }

    public async Task<string> InsertMessageAsync(string channel, string? customerId, string recipient,
        string? subject, string body, string status, string? error, string userId, CancellationToken ct = default)
    {
        var id = Guid.NewGuid().ToString();
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            INSERT INTO W_MESSAGE
            (ID, CHANNEL, CUSTOMER_ID, RECIPIENT, SUBJECT, BODY, STATUS, ERROR, CREATED_BY, CREATED_AT)
            VALUES (@id, @ch, @cid, @to, @sj, @body, @st, @err, @uid, @now)
            """,
            new
            {
                id, ch = channel, cid = (object?)customerId ?? DBNull.Value, to = recipient,
                sj = subject, body, st = status, err = error, uid = userId, now = DateTime.UtcNow
            });
        return id;
    }

    public async Task UpdateMessageStatusAsync(string id, string status, string? error, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await conn.ExecuteAsync(
            """
            UPDATE W_MESSAGE SET STATUS = @st, ERROR = @err,
                SENT_AT = CASE WHEN @st = 'SENT' THEN @now ELSE SENT_AT END
            WHERE ID = @id
            """, new { id, st = status, err = error, now = DateTime.UtcNow });
    }

    public async Task<MessageDto?> GetMessageAsync(string id, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<MessageDto>(
            """
            SELECT m.ID, m.CHANNEL AS Channel, m.CUSTOMER_ID AS CustomerId, k.NAME AS TenKhach,
                   m.RECIPIENT AS Recipient, m.SUBJECT AS Subject, m.BODY AS Body,
                   m.STATUS AS Status, m.ERROR AS Error, m.CREATED_BY AS CreatedBy,
                   m.CREATED_AT AS CreatedAt, m.SENT_AT AS SentAt
            FROM W_MESSAGE m LEFT JOIN DKHACHHANG k ON k.ID = m.CUSTOMER_ID
            WHERE m.ID = @id
            """, new { id });
    }

    // ---------- Điểm thân thiết ----------

    public async Task<(IReadOnlyList<LoyaltySummaryDto> Items, long Total)> SearchLoyaltyAsync(
        LoyaltyQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var where = "WHERE 1=1";
        var dp = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where += " AND (k.NAME LIKE @s OR k.DIENTHOAI LIKE @s)";
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM DKHACHHANG k {where}", dp);
        var items = (await conn.QueryAsync<LoyaltySummaryDto>(
            $"""
            SELECT k.ID AS CustomerId, k.NAME AS TenKhach, k.DIENTHOAI AS DienThoai,
                   CAST(COALESCE(k.DIEMTICHLUYBANDAU,0) AS SIGNED) AS Points,
                   CAST(COALESCE(SUM(CASE WHEN t.POINTS > 0 THEN t.POINTS ELSE 0 END),0) AS SIGNED) AS TotalEarned,
                   CAST(COALESCE(SUM(CASE WHEN t.POINTS < 0 THEN -t.POINTS ELSE 0 END),0) AS SIGNED) AS TotalRedeemed,
                   MAX(t.CREATED_AT) AS LastTxnAt
            FROM DKHACHHANG k
            LEFT JOIN W_LOYALTY_TXN t ON t.CUSTOMER_ID = k.ID
            {where}
            GROUP BY k.ID, k.NAME, k.DIENTHOAI, k.DIEMTICHLUYBANDAU
            ORDER BY k.NAME
            LIMIT @lim OFFSET @off
            """, Merge(dp, new { lim = pageSize, off = (page - 1) * pageSize }))).ToList();
        return (items, total);
    }

    public async Task<IReadOnlyList<LoyaltyTxnDto>> GetLoyaltyTxnsAsync(
        string customerId, int limit = 100, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<LoyaltyTxnDto>(
            """
            SELECT ID, CUSTOMER_ID AS CustomerId, TXN_TYPE AS TxnType, POINTS AS Points,
                   REFERENCE_TYPE AS ReferenceType, REFERENCE_ID AS ReferenceId,
                   NOTE AS Note, CREATED_BY AS CreatedBy, CREATED_AT AS CreatedAt
            FROM W_LOYALTY_TXN WHERE CUSTOMER_ID = @cid
            ORDER BY CREATED_AT DESC LIMIT @lim
            """, new { cid = customerId, lim = Math.Clamp(limit, 1, 500) });
        return rows.ToList();
    }

    /// <summary>Điều chỉnh điểm: clamp không âm, ghi sổ cái. Trả về (điểm áp dụng, điểm mới).</summary>
    public async Task<(int Applied, int NewPoints)> AdjustLoyaltyAsync(
        string customerId, int points, string? note, string userId, CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var current = await conn.ExecuteScalarAsync<int>(
            "SELECT CAST(COALESCE(DIEMTICHLUYBANDAU,0) AS SIGNED) FROM DKHACHHANG WHERE ID = @id FOR UPDATE",
            new { id = customerId }, tx);
        if (current < 0) current = 0;

        var applied = points;
        if (current + applied < 0) applied = -current;
        var newPoints = current + applied;

        await conn.ExecuteAsync(
            "UPDATE DKHACHHANG SET DIEMTICHLUYBANDAU = @p WHERE ID = @id",
            new { p = newPoints, id = customerId }, tx);

        if (applied != 0)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO W_LOYALTY_TXN
                (ID, CUSTOMER_ID, TXN_TYPE, POINTS, NOTE, CREATED_BY, CREATED_AT)
                VALUES (@id, @cid, @type, @points, @note, @uid, @now)
                """,
                new
                {
                    id = Guid.NewGuid().ToString(), cid = customerId,
                    type = applied > 0 ? "EARN" : "REDEEM", points = applied,
                    note, uid = userId, now = DateTime.UtcNow
                }, tx);
        }
        await tx.CommitAsync(ct);
        return (applied, newPoints);
    }

    private static DynamicParameters Merge(DynamicParameters a, object b)
    {
        var dp = new DynamicParameters();
        foreach (var p in a.ParameterNames) dp.Add(p, a.Get<object?>(p));
        dp.AddDynamicParams(b);
        return dp;
    }
}
