using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechPos.Api.Middleware;
using TechPos.Application.Hr;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Api.Controllers;

/// <summary>Nhân viên (mục 5.3) — DNHANVIEN, đọc + ghi mirror MySQL (W_ORIGIN='WEB').</summary>
[ApiController]
[Route("api/employees")]
[Authorize]
public sealed class EmployeesController(HrService hr, MySqlConnectionFactory db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("HR.EMPLOYEE.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] bool? activeOnly,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "NAME", CancellationToken ct = default)
    {
        var result = await hr.SearchEmployeesAsync(new EmployeeListQuery(search, activeOnly, page, pageSize, sort), ct);
        return Ok(ApiResult<PageResult<EmployeeDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id}")]
    [RequirePermission("HR.EMPLOYEE.VIEW")]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
        => Ok(ApiResult<EmployeeDto>.Ok(await hr.GetEmployeeAsync(id, ct), HttpContext.TraceIdentifier));

    [HttpPost]
    [RequirePermission("HR.EMPLOYEE.CREATE")]
    public async Task<IActionResult> Create([FromBody] EmployeeCreateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await hr.CreateEmployeeAsync(req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "CREATE", "DNHANVIEN", result.Id,
            newValue: req, ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<CatalogCreateResult>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("{id}")]
    [RequirePermission("HR.EMPLOYEE.UPDATE")]
    public async Task<IActionResult> Update(string id, [FromBody] EmployeeUpdateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var old = await hr.GetEmployeeAsync(id, ct);
        var result = await hr.UpdateEmployeeAsync(id, req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "UPDATE", "DNHANVIEN", id,
            oldValue: new { old.Name, old.DiaChi, old.DienThoai, old.CachTinhLuong, old.LuongCa, old.LuongThang, old.PhuCap, old.Status },
            newValue: req, ip, ua, HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<EmployeeDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id}")]
    [RequirePermission("HR.EMPLOYEE.DELETE")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var old = await hr.GetEmployeeAsync(id, ct);
        await hr.DeleteEmployeeAsync(id, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "SOFT_DELETE", "DNHANVIEN", id,
            oldValue: new { old.Name }, ip: ip, userAgent: ua,
            correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }
}

/// <summary>Chấm công (mục 5.3) — TBANGLUONGCHITIET + DCALAMVIEC.</summary>
[ApiController]
[Route("api/attendance")]
[Authorize]
public sealed class AttendanceController(HrService hr) : ControllerBase
{
    [HttpGet]
    [RequirePermission("HR.ATTENDANCE.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? nhanVienId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await hr.SearchAttendanceAsync(new AttendanceQuery(from, to, nhanVienId, page, pageSize), ct);
        return Ok(ApiResult<PageResult<AttendanceDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost]
    [RequirePermission("HR.ATTENDANCE.CREATE")]
    public async Task<IActionResult> Create([FromBody] AttendanceCreateRequest req, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var result = await hr.CreateAttendanceAsync(req, userId, ct);
        return Ok(ApiResult<AttendanceDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Danh sách ca làm việc (DCALAMVIEC).</summary>
    [HttpGet("shifts")]
    [RequirePermission("HR.ATTENDANCE.VIEW")]
    public async Task<IActionResult> Shifts(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<ShiftDto>>.Ok(await hr.GetShiftsAsync(ct), HttpContext.TraceIdentifier));
}

/// <summary>Thưởng phạt (mục 5.3) — TTHUONGPHAT + DLYDOTHUONGPHAT.</summary>
[ApiController]
[Route("api/bonus-penalties")]
[Authorize]
public sealed class BonusPenaltiesController(HrService hr) : ControllerBase
{
    [HttpGet]
    [RequirePermission("HR.BONUS.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? nhanVienId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var result = await hr.SearchBonusAsync(new BonusPenaltyQuery(from, to, nhanVienId, page, pageSize), ct);
        return Ok(ApiResult<PageResult<BonusPenaltyDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("reasons")]
    [RequirePermission("HR.BONUS.VIEW")]
    public async Task<IActionResult> Reasons(CancellationToken ct)
        => Ok(ApiResult<IReadOnlyList<LookupItem>>.Ok(await hr.GetBonusReasonsAsync(ct), HttpContext.TraceIdentifier));

    [HttpPost]
    [RequirePermission("HR.BONUS.CREATE")]
    public async Task<IActionResult> Create([FromBody] BonusPenaltyCreateRequest req, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var result = await hr.CreateBonusAsync(req, userId, ct);
        return Ok(ApiResult<CatalogCreateResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Tính lương (mục 5.3) — TBANGLUONG + TBANGLUONGTONGHOP.</summary>
[ApiController]
[Route("api/payroll")]
[Authorize]
public sealed class PayrollController(HrService hr, TechPos.Application.Reports.ReportService reports) : ControllerBase
{
    [HttpGet]
    [RequirePermission("HR.PAYROLL.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await hr.SearchPayrollsAsync(new PayrollListQuery(page, pageSize), ct);
        return Ok(ApiResult<PageResult<PayrollSummaryDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id}")]
    [RequirePermission("HR.PAYROLL.VIEW")]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
        => Ok(ApiResult<PayrollDto>.Ok(await hr.GetPayrollAsync(id, ct), HttpContext.TraceIdentifier));

    /// <summary>In bảng lương tháng A4 (PDF) — Phase 6.3 (KE-HOACH §6.3 "bảng lương").</summary>
    [HttpGet("{id}/pdf")]
    [RequirePermission("HR.PAYROLL.VIEW")]
    public async Task<IActionResult> PayrollPdf(string id, CancellationToken ct)
    {
        var p = await hr.GetPayrollAsync(id, ct);
        return File(reports.PayrollPdf(p), "application/pdf", $"bang-luong-{p.Thang:00}{p.Nam}.pdf");
    }

    /// <summary>Tạo bảng lương tháng (tổng hợp chấm công + thưởng phạt + tạm ứng + phụ cấp).</summary>
    [HttpPost]
    [RequirePermission("HR.PAYROLL.CREATE")]
    public async Task<IActionResult> Create([FromBody] PayrollCreateRequest req, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var result = await hr.CreatePayrollAsync(req, userId, ct);
        return Ok(ApiResult<PayrollDto>.Ok(result, HttpContext.TraceIdentifier));
    }

    /// <summary>Tạm ứng lương 1 nhân viên — ghi phiếu chi TTHUCHI.LATAMUNG=1.</summary>
    [HttpPost("advances")]
    [RequirePermission("HR.PAYROLL.CREATE")]
    public async Task<IActionResult> Advance([FromBody] SalaryAdvanceRequest req, CancellationToken ct)
    {
        var userId = this.GetUserId();
        var result = await hr.CreateSalaryAdvanceAsync(req, userId, ct);
        return Ok(ApiResult<SalaryAdvanceResult>.Ok(result, HttpContext.TraceIdentifier));
    }
}

/// <summary>Giấy giới thiệu (mục 5.3) — TGIAYGIOITHIEU.</summary>
[ApiController]
[Route("api/intro-letters")]
[Authorize]
public sealed class IntroLettersController(HrService hr, MySqlConnectionFactory db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("HR.INTRO_LETTER.VIEW")]
    public async Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await hr.SearchIntroLettersAsync(new IntroLetterQuery(search, page, pageSize), ct);
        return Ok(ApiResult<PageResult<IntroLetterDto>>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("{id}")]
    [RequirePermission("HR.INTRO_LETTER.VIEW")]
    public async Task<IActionResult> Detail(string id, CancellationToken ct)
        => Ok(ApiResult<IntroLetterDto>.Ok(await hr.GetIntroLetterAsync(id, ct), HttpContext.TraceIdentifier));

    [HttpPost]
    [RequirePermission("HR.INTRO_LETTER.CREATE")]
    public async Task<IActionResult> Create([FromBody] IntroLetterCreateRequest req, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        var result = await hr.CreateIntroLetterAsync(req, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "CREATE", "TGIAYGIOITHIEU", result.Id,
            newValue: req, ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<CatalogCreateResult>.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpDelete("{id}")]
    [RequirePermission("HR.INTRO_LETTER.DELETE")]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        var (uid, uname, ip, ua) = CurrentUser.Info(HttpContext);
        await hr.DeleteIntroLetterAsync(id, uid, ct);
        await AuditLogger.WriteAsync(db, uid, uname, "SOFT_DELETE", "TGIAYGIOITHIEU", id,
            ip: ip, userAgent: ua, correlationId: HttpContext.TraceIdentifier, ct: ct);
        return Ok(ApiResult<bool>.Ok(true, HttpContext.TraceIdentifier));
    }
}
