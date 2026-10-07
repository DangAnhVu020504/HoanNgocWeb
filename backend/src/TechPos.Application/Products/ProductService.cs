using FluentValidation;
using Microsoft.Extensions.Options;
using TechPos.Application.Catalog;
using TechPos.Data.MySql;
using TechPos.Shared;
using ValidationException = TechPos.Shared.ValidationException;

namespace TechPos.Application.Products;


/// <summary>Use-case đọc + ghi danh mục mặt hàng (mục 60). KHÔNG chứa SQL.</summary>
public sealed class ProductService(ProductRepository repo, IOptions<FeatureFlagOptions> flags)
{
    private readonly ProductValidator _validator = new();

    public async Task<PageResult<ProductDto>> SearchAsync(ProductListQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchAsync(q, ct);
        return new PageResult<ProductDto>
        {
            Items = items,
            Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize),
            Total = total
        };
    }

    public async Task<ProductDto> GetAsync(string id, CancellationToken ct = default)
    {
        var p = await repo.GetByIdAsync(id, ct);
        return p ?? throw new NotFoundException($"Không tìm thấy mặt hàng {id}");
    }

    public async Task<ProductDto?> FindByBarcodeAsync(string barcode, CancellationToken ct = default)
        => await repo.GetByBarcodeAsync(barcode, ct);

    public async Task<IReadOnlyList<ProductGroupDto>> GetGroupsAsync(CancellationToken ct = default)
        => await repo.GetGroupsAsync(ct);

    public async Task<IReadOnlyList<ProductStockDto>> GetStockAsync(string id, CancellationToken ct = default)
    {
        await GetAsync(id, ct); // đảm bảo tồn tại
        return await repo.GetStockAsync(id, ct);
    }

    // ---------- Phase 4 CRUD ----------

    public async Task<CatalogCreateResult> CreateAsync(ProductCreateRequest req, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        var vr = _validator.Validate(req);
        if (!vr.IsValid)
            throw new ValidationException("Dữ liệu mặt hàng không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());
        if (!string.IsNullOrWhiteSpace(req.Code) && await repo.CodeExistsAsync(req.Code, null, ct))
            throw new ConflictException($"Mã mặt hàng '{req.Code}' đã tồn tại");
        var groupIds = new[] { req.NhomMatHangId, req.DonViTinhId, req.HangSanXuatId }
            .Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g!).ToList();
        if (groupIds.Count > 0 && !await repo.GroupsExistAsync(groupIds, ct))
            throw new ValidationException("Nhóm hàng / ĐVT / hãng sản xuất không tồn tại", []);
        var id = await repo.InsertAsync(req, userId, ct);
        return new CatalogCreateResult(id, req.Name);
    }

    public async Task<ProductDto> UpdateAsync(string id, ProductUpdateRequest req, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        _ = await GetAsync(id, ct);
        var groupIds = new[] { req.NhomMatHangId, req.DonViTinhId }
            .Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g!).ToList();
        if (groupIds.Count > 0 && !await repo.GroupsExistAsync(groupIds, ct))
            throw new ValidationException("Nhóm hàng / ĐVT không tồn tại", []);
        var updated = await repo.UpdateAsync(id, req, userId, ct);
        if (updated == 0) throw new NotFoundException($"Không tìm thấy mặt hàng {id}");
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(string id, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        _ = await GetAsync(id, ct);
        await repo.SoftDeleteAsync(id, userId, ct);
    }
}

/// <summary>Validator mặt hàng (Phase 4).</summary>
public sealed class ProductValidator : AbstractValidator<ProductCreateRequest>
{
    public ProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255).WithMessage("Tên mặt hàng bắt buộc, ≤ 255 ký tự");
        RuleFor(x => x.Code).MaximumLength(255);
        RuleFor(x => x.MaSanCo).MaximumLength(255);
        RuleFor(x => x.GiaBan).GreaterThanOrEqualTo(0);
        RuleFor(x => x.GiaNhap).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TonToiThieu).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TonToiDa).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Note).MaximumLength(255);
    }
}
