using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>
/// Dashboard (mục 19) — số liệu thật từ mirror MySQL.
/// Chỉ dùng giá trị ĐÃ xác minh trên DB thật: phân loại chứng từ đọc từ W_DOC_TYPE_MAP,
/// KHÔNG hard-code LOAI bán lẻ/bán sỉ (chưa có dữ liệu xác minh — xem PHASE0-FINDINGS §2).
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(MySqlConnectionFactory mySql) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        await using var conn = await mySql.OpenAsync(ct);

        var todayDocs = (await conn.QueryAsync<dynamic>(
            "SELECT LOAI, COUNT(*) as N, COALESCE(SUM(TIENHANG),0) as TienHang, COALESCE(SUM(TONGCONG),0) as TongCong " +
            "FROM TDONHANG WHERE DATE(NGAY) = CURDATE() AND W_DELETED = 0 GROUP BY LOAI")).ToList();

        var monthDocs = (await conn.QueryAsync<dynamic>(
            "SELECT LOAI, COUNT(*) as N, COALESCE(SUM(TIENHANG),0) as TienHang, COALESCE(SUM(TONGCONG),0) as TongCong " +
            "FROM TDONHANG WHERE YEAR(NGAY) = YEAR(CURDATE()) AND MONTH(NGAY) = MONTH(CURDATE()) AND W_DELETED = 0 GROUP BY LOAI")).ToList();

        var counts = await conn.QueryFirstOrDefaultAsync<dynamic>("""
            SELECT
              (SELECT COUNT(*) FROM DMATHANG   WHERE STATUS = 30 AND W_DELETED = 0) as Products,
              (SELECT COUNT(*) FROM DKHACHHANG WHERE STATUS = 30 AND W_DELETED = 0) as Customers,
              (SELECT COUNT(*) FROM DNHACUNGCAP WHERE STATUS = 30 AND W_DELETED = 0) as Suppliers,
              (SELECT COUNT(*) FROM DNHANVIEN  WHERE STATUS = 30 AND W_DELETED = 0) as Employees,
              (SELECT COUNT(*) FROM DKHOHANG   WHERE STATUS = 30 AND W_DELETED = 0) as Warehouses
            """);

        var stock = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT COUNT(*) as SkuCount, COALESCE(SUM(TON),0) as TotalQty FROM OTONKHO WHERE W_DELETED = 0");

        var cash = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT COALESCE(SUM(THU),0) as Thu, COALESCE(SUM(CHI),0) as Chi FROM TTHUCHI WHERE W_DELETED = 0");

        var docTypes = (await conn.QueryAsync<dynamic>(
            "SELECT LOAI, DOC_TYPE, PREFIX, VERIFIED FROM W_DOC_TYPE_MAP")).ToList();

        return Ok(ApiResult<object>.Ok(new
        {
            todayDocs, monthDocs, counts, stock, cash, docTypes
        }, HttpContext.TraceIdentifier));
    }
}
