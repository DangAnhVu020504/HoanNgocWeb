using Dapper;
using MySqlConnector;
using TechPos.Shared;

namespace TechPos.Data.MySql;

/// <summary>
/// Repository quỹ (mục 5.1) — TTHUCHI (phiếu thu/chi) + TCHITIETTHANHTOAN (phân bổ
/// trả nợ từng hóa đơn) + DLYDOTHUCHI (lý do thu chi). LOAI (0=thu/1=chi) đã xác minh
/// PHASE0-FINDINGS §4; LOAIDOITUONG đọc từ W_DOC_TYPE_MAP theo 4 tổ hợp THU/CHI × KH/NCC,
/// VERIFIED=0 chặn ghi cho tới khi xác minh production (quy tắc vàng 5).
/// Phân bổ nợ chỉ giảm CONNO/CONLAI trên chứng từ W_ORIGIN='WEB' (quy tắc vàng 1).
/// </summary>
public sealed class CashbookRepository
{
    private readonly MySqlConnectionFactory _factory;
    public CashbookRepository(MySqlConnectionFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<CashReasonDto>> GetReasonsAsync(CancellationToken ct = default)
    {
        await using var conn = await _factory.OpenAsync(ct);
        return (await conn.QueryAsync<CashReasonDto>(
            "SELECT ID AS Id, NAME AS Name, COALESCE(LALYDOTHU, 0) <> 0 AS LaLyDoThu " +
            "FROM DLYDOTHUCHI WHERE W_DELETED = 0 AND STATUS = 30 ORDER BY LALYDOTHU DESC, NAME")).ToList();
    }

    /// <summary>Tạo phiếu thu/chi. loaiThu=false ⇒ chi. Trả về kết quả + tổng tiền đã phân bổ nợ.</summary>
    public async Task<CashVoucherResult> CreateAsync(
        CashVoucherRequest req, bool loaiThu, string userId, CancellationToken ct = default)
    {
        var docType = (loaiThu, req.DoiTuong) switch
        {
            (true, CashTargetNames.KhachHang) => "PHIEU_THU_KH",
            (true, CashTargetNames.NhaCungCap) => "PHIEU_THU_NCC",
            (false, CashTargetNames.KhachHang) => "PHIEU_CHI_KH",
            (false, CashTargetNames.NhaCungCap) => "PHIEU_CHI_NCC",
            _ => throw new TechPos.Shared.ValidationException(
                $"Đối tượng phiếu không hợp lệ: '{req.DoiTuong}' (KH | NCC)",
                [new ApiError(nameof(req.DoiTuong), "DOI_TUONG_KHONG_HOP_LE", "Chọn KH hoặc NCC")])
        };

        await using var conn = await _factory.OpenAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);
        try
        {
            var map = await DocRepositoryHelpers.GetDocTypeAsync(conn, tx, docType, ct);

            // Tên đối tượng (snapshot) từ danh mục
            string? tenDoiTuong = null;
            if (req.DoiTuong == CashTargetNames.KhachHang && !string.IsNullOrEmpty(req.KhachHangId))
            {
                if (!await ExistsAsync(conn, tx, "DKHACHHANG", req.KhachHangId, ct))
                    throw new NotFoundException($"Không tìm thấy khách hàng {req.KhachHangId}");
                tenDoiTuong = await conn.ExecuteScalarAsync<string>(
                    "SELECT NAME FROM DKHACHHANG WHERE ID = @id", new { id = req.KhachHangId }, tx);
            }
            else if (req.DoiTuong == CashTargetNames.NhaCungCap && !string.IsNullOrEmpty(req.NhaCungCapId))
            {
                if (!await ExistsAsync(conn, tx, "DNHACUNGCAP", req.NhaCungCapId, ct))
                    throw new NotFoundException($"Không tìm thấy nhà cung cấp {req.NhaCungCapId}");
                tenDoiTuong = await conn.ExecuteScalarAsync<string>(
                    "SELECT NAME FROM DNHACUNGCAP WHERE ID = @id", new { id = req.NhaCungCapId }, tx);
            }

            var soPhieu = await DocRepositoryHelpers.NextSoPhieuAsync(conn, tx, map.Prefix, ct);
            var id = Guid.NewGuid().ToString();
            var ngay = DateTime.Now;
            var allocations = req.Allocations ?? [];
            decimal daGanNo = 0;

            await conn.ExecuteAsync(
                """
                INSERT INTO TTHUCHI
                (ID, NAME, NOTE, STATUS, NGAY, USERCREATEDID, TIMECREATED,
                 TENDOITUONG, LOAI, LOAIDOITUONG, DIENGIAI,
                 THU, CHI, CHUYENKHOAN, LAPHIEUTHUCONGNO, KHONGTHAYDOICONGNO,
                 DNHACUNGCAPID, DKHACHHANGID, DNHANVIENID, DLYDOTHUCHIID,
                 W_ORIGIN, W_DELETED)
                VALUES
                (@id, @sophieu, @note, 30, @ngay, @uc, @tc,
                 @tendoituong, @loai, @loaidoituong, @diengiai,
                 @thu, @chi, @ck, @lacongno, @khongdoi,
                 @ncc, @kh, @nv, @lydo,
                 'WEB', 0)
                """,
                new
                {
                    id,
                    sophieu = soPhieu,
                    note = req.GhiChu ?? (object?)null,
                    ngay,
                    uc = userId,
                    tc = ngay,
                    tendoituong = tenDoiTuong,
                    loai = map.Loai,
                    loaidoituong = map.Mode,
                    diengiai = loaiThu ? "Phiếu thu" : "Phiếu chi",
                    thu = loaiThu ? req.Tien : 0,
                    chi = loaiThu ? 0 : req.Tien,
                    ck = req.ChuyenKhoan ? 1 : 0,
                    lacongno = allocations.Count > 0 ? 1 : 0,
                    khongdoi = allocations.Count == 0 ? 1 : 0,
                    ncc = (object?)req.NhaCungCapId ?? DBNull.Value,
                    kh = (object?)req.KhachHangId ?? DBNull.Value,
                    nv = (object?)req.NhanVienId ?? DBNull.Value,
                    lydo = (object?)req.LyDoId ?? DBNull.Value
                }, tx);

            // Phân bổ trả nợ — chỉ THU_KH (giảm CONNO) và CHI_NCC (giảm CONLAI), chứng từ web
            if (allocations.Count > 0)
            {
                var isDebtCustomer = docType == "PHIEU_THU_KH";
                var debtColumn = isDebtCustomer ? "CONNO" : "CONLAI";
                foreach (var a in allocations)
                {
                    await conn.ExecuteAsync(
                        $"""
                        INSERT INTO TCHITIETTHANHTOAN
                        (ID, STATUS, USERCREATEDID, TIMECREATED, TDONHANGID, TTHUCHIID, SOTIEN,
                         W_ORIGIN, W_DELETED)
                        VALUES (@id, 30, @uc, @tc, @dh, @thuchi, @sotien, 'WEB', 0)
                        """,
                        new
                        {
                            id = Guid.NewGuid().ToString(),
                            uc = userId,
                            tc = ngay,
                            dh = a.DonHangId,
                            thuchi = id,
                            sotien = a.SoTien
                        }, tx);

                    var updated = await conn.ExecuteAsync(
                        $"UPDATE TDONHANG SET {debtColumn} = GREATEST(0, {debtColumn} - @sotien), TIMEMODIFIED = NOW(3) " +
                        "WHERE ID = @dh AND W_ORIGIN = 'WEB' AND W_DELETED = 0",
                        new { sotien = a.SoTien, dh = a.DonHangId }, tx);
                    if (updated == 0)
                        throw new TechPos.Shared.ValidationException(
                            $"Không thể phân bổ vào chứng từ {a.DonHangId} — chỉ phân bổ được cho hóa đơn/phiếu do web tạo.",
                            [new ApiError(a.DonHangId, "KHONG_THE_GAN_NO", "Chứng từ không phải do web tạo hoặc đã xóa")]);
                    daGanNo += a.SoTien;
                }
            }

            await tx.CommitAsync(ct);
            return new CashVoucherResult(id, soPhieu, req.Tien, daGanNo);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private static async Task<bool> ExistsAsync(
        MySqlConnection conn, MySqlTransaction tx, string table, string id, CancellationToken ct)
        => await conn.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM {table} WHERE ID = @id AND W_DELETED = 0", new { id }, tx) > 0;

    /// <summary>Danh sách phiếu thu/chi.</summary>
    public async Task<(IReadOnlyList<CashVoucherDto> Items, long Total)> SearchAsync(
        CashVoucherListQuery q, CancellationToken ct = default)
    {
        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, Paging.MaxPageSize);
        var offset = (page - 1) * pageSize;

        var where = new List<string> { "t.W_DELETED = 0" };
        var dp = new DynamicParameters();
        if (q.IsThu.HasValue) { where.Add("t.LOAI = @loai"); dp.Add("@loai", q.IsThu.Value ? 0 : 1); }
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            where.Add("(t.NAME LIKE @s OR t.TENDOITUONG LIKE @s OR t.NOTE LIKE @s)");
            dp.Add("@s", $"%{q.Search.Trim()}%");
        }
        if (q.FromDate.HasValue) { where.Add("t.NGAY >= @from"); dp.Add("@from", q.FromDate.Value.Date); }
        if (q.ToDate.HasValue) { where.Add("t.NGAY < DATE_ADD(@to, INTERVAL 1 DAY)"); dp.Add("@to", q.ToDate.Value.Date); }
        var whereSql = string.Join(" AND ", where);

        await using var conn = await _factory.OpenAsync(ct);
        var total = await conn.ExecuteScalarAsync<long>(
            $"SELECT COUNT(*) FROM TTHUCHI t WHERE {whereSql}", dp);
        var rows = (await conn.QueryAsync<CashVoucherDto>(
            $"""
            SELECT t.ID AS Id, t.NAME AS SoPhieu, t.NGAY AS Ngay, t.LOAI AS Loai,
                   t.TENDOITUONG AS TenDoiTuong, t.DIENGIAI AS DienGiai,
                   COALESCE(t.THU,0) AS Thu, COALESCE(t.CHI,0) AS Chi,
                   COALESCE(t.CHUYENKHOAN,0) <> 0 AS ChuyenKhoan,
                   COALESCE(t.LAPHIEUTHUCONGNO,0) <> 0 AS LaPhieuThuCongNo,
                   t.NOTE AS GhiChu, k.NAME AS KhachHang, n.NAME AS NhaCungCap, l.NAME AS LyDo,
                   t.TIMECREATED AS TimeCreated
            FROM TTHUCHI t
            LEFT JOIN DKHACHHANG k ON k.ID = t.DKHACHHANGID
            LEFT JOIN DNHACUNGCAP n ON n.ID = t.DNHACUNGCAPID
            LEFT JOIN DLYDOTHUCHI l ON l.ID = t.DLYDOTHUCHIID
            WHERE {whereSql}
            ORDER BY t.TIMECREATED DESC
            LIMIT @limit OFFSET @offset
            """, Merge(dp, new { limit = pageSize, offset }))).ToList();
        return (rows, total);
    }

    /// <summary>Tồn quỹ theo kỳ: tiền mặt (CHUYENKHOAN=0) + chuyển khoản tách riêng.</summary>
    public async Task<CashBalanceDto> GetBalanceAsync(CashBalanceQuery q, CancellationToken ct = default)
    {
        var where = new List<string> { "t.W_DELETED = 0" };
        var dp = new DynamicParameters();
        if (q.FromDate.HasValue) { where.Add("t.NGAY >= @from"); dp.Add("@from", q.FromDate.Value.Date); }
        if (q.ToDate.HasValue) { where.Add("t.NGAY < DATE_ADD(@to, INTERVAL 1 DAY)"); dp.Add("@to", q.ToDate.Value.Date); }
        var whereSql = string.Join(" AND ", where);

        await using var conn = await _factory.OpenAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync(
            $"""
            SELECT
              COALESCE(SUM(CASE WHEN COALESCE(t.CHUYENKHOAN,0) = 0 THEN t.THU ELSE 0 END), 0) AS TongThu,
              COALESCE(SUM(CASE WHEN COALESCE(t.CHUYENKHOAN,0) = 0 THEN t.CHI ELSE 0 END), 0) AS TongChi,
              COALESCE(SUM(CASE WHEN COALESCE(t.CHUYENKHOAN,0) <> 0 THEN t.THU ELSE 0 END), 0) AS TongThuChuyenKhoan,
              COALESCE(SUM(CASE WHEN COALESCE(t.CHUYENKHOAN,0) <> 0 THEN t.CHI ELSE 0 END), 0) AS TongChiChuyenKhoan
            FROM TTHUCHI t
            WHERE {whereSql}
            """, dp) ?? new { TongThu = 0m, TongChi = 0m, TongThuChuyenKhoan = 0m, TongChiChuyenKhoan = 0m };

        return new CashBalanceDto
        {
            TongThu = row.TongThu,
            TongChi = row.TongChi,
            TonQuy = row.TongThu - row.TongChi,
            TongThuChuyenKhoan = row.TongThuChuyenKhoan,
            TongChiChuyenKhoan = row.TongChiChuyenKhoan,
            TonChuyenKhoan = row.TongThuChuyenKhoan - row.TongChiChuyenKhoan
        };
    }

    private static DynamicParameters Merge(DynamicParameters a, object b)
    {
        var dp = new DynamicParameters();
        foreach (var p in a.ParameterNames) dp.Add(p, a.Get<object?>(p));
        dp.AddDynamicParams(b);
        return dp;
    }
}