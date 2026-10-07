using FluentValidation;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Inventory;

/// <summary>Validator phiếu nhập kho (mục 4.4).</summary>
public sealed class ReceiptRequestValidator : FluentValidation.AbstractValidator<ReceiptRequest>
{
    public ReceiptRequestValidator()
    {
        RuleFor(x => x.KhoNhapId).NotEmpty().WithMessage("Chưa chọn kho nhập");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Phiếu nhập phải có ít nhất 1 dòng hàng");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.MatHangId).NotEmpty();
            l.RuleFor(x => x.SlNhap).GreaterThan(0).WithMessage("Số lượng nhập phải > 0");
            l.RuleFor(x => x.DonGia).GreaterThanOrEqualTo(0);
        });
    }
}

/// <summary>Validator phiếu kiểm kê (mục 4.7).</summary>
public sealed class StocktakeRequestValidator : FluentValidation.AbstractValidator<StocktakeRequest>
{
    public StocktakeRequestValidator()
    {
        RuleFor(x => x.KhoId).NotEmpty().WithMessage("Chưa chọn kho kiểm kê");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Phiếu kiểm kê phải có ít nhất 1 dòng hàng");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.MatHangId).NotEmpty();
            l.RuleFor(x => x.SlThucTe).GreaterThanOrEqualTo(0).WithMessage("SL thực tế phải >= 0");
        });
    }
}

/// <summary>Validator phiếu xuất kho (mục 4.5).</summary>
public sealed class StockIssueRequestValidator : FluentValidation.AbstractValidator<StockIssueRequest>
{
    public StockIssueRequestValidator()
    {
        RuleFor(x => x.KhoXuatId).NotEmpty().WithMessage("Chưa chọn kho xuất");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Phiếu xuất phải có ít nhất 1 dòng hàng");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.MatHangId).NotEmpty();
            l.RuleFor(x => x.SoLuong).GreaterThan(0).WithMessage("Số lượng xuất phải > 0");
            l.RuleFor(x => x.DonGia).GreaterThanOrEqualTo(0);
        });
    }
}

/// <summary>Validator phiếu chuyển kho (mục 4.6).</summary>
public sealed class StockTransferRequestValidator : FluentValidation.AbstractValidator<StockTransferRequest>
{
    public StockTransferRequestValidator()
    {
        RuleFor(x => x.KhoXuatId).NotEmpty().WithMessage("Chưa chọn kho xuất");
        RuleFor(x => x.KhoNhapId).NotEmpty().WithMessage("Chưa chọn kho nhập");
        RuleFor(x => x.KhoNhapId).NotEqual(x => x.KhoXuatId)
            .WithMessage("Kho nhập phải khác kho xuất");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Phiếu chuyển kho phải có ít nhất 1 dòng hàng");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.MatHangId).NotEmpty();
            l.RuleFor(x => x.SoLuong).GreaterThan(0).WithMessage("Số lượng chuyển phải > 0");
        });
    }
}

/// <summary>Validator phiếu xuất trả NCC (mục 4.5).</summary>
public sealed class SupplierReturnRequestValidator : FluentValidation.AbstractValidator<SupplierReturnRequest>
{
    public SupplierReturnRequestValidator()
    {
        RuleFor(x => x.KhoXuatId).NotEmpty().WithMessage("Chưa chọn kho xuất");
        RuleFor(x => x.NhaCungCapId).NotEmpty().WithMessage("Chưa chọn nhà cung cấp");
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Phiếu xuất trả phải có ít nhất 1 dòng hàng");
        RuleForEach(x => x.Lines).ChildRules(l =>
        {
            l.RuleFor(x => x.MatHangId).NotEmpty();
            l.RuleFor(x => x.SoLuong).GreaterThan(0).WithMessage("Số lượng trả phải > 0");
            l.RuleFor(x => x.DonGia).GreaterThanOrEqualTo(0);
        });
    }
}

/// <summary>Use-case Tồn kho + Thẻ kho (4.8), Phiếu nhập kho (4.4), Kiểm kê (4.7),
/// Xuất kho (4.5), Chuyển kho (4.6), Xuất trả NCC. KHÔNG chứa SQL.</summary>
public sealed class InventoryService(
    InventoryRepository inventory, ReceiptRepository receipts, StocktakeRepository stocktakes,
    StockIssueRepository issues, StockTransferRepository transfers, SupplierReturnRepository supplierReturns)
{
    private readonly ReceiptRequestValidator _validator = new();
    private readonly StocktakeRequestValidator _stocktakeValidator = new();
    private readonly StockIssueRequestValidator _issueValidator = new();
    private readonly StockTransferRequestValidator _transferValidator = new();
    private readonly SupplierReturnRequestValidator _supplierReturnValidator = new();

    public async Task<PageResult<InventoryDto>> SearchAsync(InventoryListQuery q, CancellationToken ct = default)
    {
        var (items, total) = await inventory.SearchAsync(q, ct);
        return new PageResult<InventoryDto>
        {
            Items = items,
            Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize),
            Total = total
        };
    }

    public async Task<IReadOnlyList<LedgerEntryDto>> GetLedgerAsync(LedgerQuery q, CancellationToken ct = default)
        => await inventory.GetLedgerAsync(q, ct);

    /// <summary>Tồn tất cả các kho (mục 4.8) — ma trận mặt hàng × kho.</summary>
    public async Task<PageResult<AllWarehouseStockDto>> SearchAllWarehousesAsync(
        AllWarehouseStockQuery q, CancellationToken ct = default)
    {
        var (items, total) = await inventory.SearchAllWarehousesAsync(q, ct);
        return new PageResult<AllWarehouseStockDto>
        {
            Items = items,
            Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize),
            Total = total
        };
    }

    public async Task<ReceiptResult> CreateReceiptAsync(ReceiptRequest req, string userId, CancellationToken ct = default)
    {
        var vr = _validator.Validate(req);
        if (!vr.IsValid)
            throw new TechPos.Shared.ValidationException("Dữ liệu phiếu nhập không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());

        return await receipts.CreateReceiptAsync(req, userId, ct);
    }

    public async Task<StocktakeResult> CreateStocktakeAsync(StocktakeRequest req, string userId, CancellationToken ct = default)
    {
        var vr = _stocktakeValidator.Validate(req);
        if (!vr.IsValid)
            throw new TechPos.Shared.ValidationException("Dữ liệu phiếu kiểm kê không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());

        return await stocktakes.CreateStocktakeAsync(req, userId, ct);
    }

    /// <summary>Tạo phiếu xuất kho (mục 4.5).</summary>
    public async Task<StockIssueResult> CreateIssueAsync(StockIssueRequest req, string userId, CancellationToken ct = default)
    {
        var vr = _issueValidator.Validate(req);
        if (!vr.IsValid)
            throw new TechPos.Shared.ValidationException("Dữ liệu phiếu xuất kho không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());

        return await issues.CreateAsync(req, userId, ct);
    }

    /// <summary>Tạo phiếu chuyển kho (mục 4.6).</summary>
    public async Task<StockTransferResult> CreateTransferAsync(StockTransferRequest req, string userId, CancellationToken ct = default)
    {
        var vr = _transferValidator.Validate(req);
        if (!vr.IsValid)
            throw new TechPos.Shared.ValidationException("Dữ liệu phiếu chuyển kho không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());

        return await transfers.CreateAsync(req, userId, ct);
    }

    /// <summary>Tạo phiếu xuất trả NCC (mục 4.5).</summary>
    public async Task<SupplierReturnResult> CreateSupplierReturnAsync(SupplierReturnRequest req, string userId, CancellationToken ct = default)
    {
        var vr = _supplierReturnValidator.Validate(req);
        if (!vr.IsValid)
            throw new TechPos.Shared.ValidationException("Dữ liệu phiếu xuất trả NCC không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());

        return await supplierReturns.CreateAsync(req, userId, ct);
    }
}
