using FluentValidation;
using Microsoft.Extensions.Options;
using TechPos.Data.MySql;
using TechPos.Shared;
using ValidationException = TechPos.Shared.ValidationException;

namespace TechPos.Application.Catalog;

/// <summary>Use-case đọc + ghi danh mục khách hàng (Phase 4). KHÔNG chứa SQL.</summary>
public sealed class CustomerService(CustomerRepository repo, IOptions<FeatureFlagOptions> flags)
{
    private readonly CustomerValidator _validator = new();

    public async Task<PageResult<CustomerDto>> SearchAsync(CustomerListQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchAsync(q, ct);
        return new PageResult<CustomerDto>
        {
            Items = items,
            Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize),
            Total = total
        };
    }

    public async Task<CustomerDto> GetAsync(string id, CancellationToken ct = default)
    {
        var c = await repo.GetByIdAsync(id, ct);
        return c ?? throw new NotFoundException($"Không tìm thấy khách hàng {id}");
    }

    public async Task<IReadOnlyList<CustomerGroupDto>> GetGroupsAsync(CancellationToken ct = default)
        => await repo.GetGroupsAsync(ct);

    // ---------- Phase 4 CRUD ----------

    public async Task<CatalogCreateResult> CreateAsync(CustomerCreateRequest req, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        var vr = _validator.Validate(req);
        if (!vr.IsValid)
            throw new ValidationException("Dữ liệu khách hàng không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());
        if (!string.IsNullOrWhiteSpace(req.MaKhach) && await repo.MaKhachExistsAsync(req.MaKhach, null, ct))
            throw new ConflictException($"Mã khách '{req.MaKhach}' đã tồn tại");
        var id = await repo.InsertAsync(req, userId, ct);
        return new CatalogCreateResult(id, req.Name);
    }

    public async Task<CustomerDto> UpdateAsync(string id, CustomerUpdateRequest req, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        var current = await GetAsync(id, ct);
        if (req.Status is not (null or 30) && req.Status.Value != current.Status && req.Status.Value != 0)
            throw new ValidationException("STATUS chỉ nhận 30 (hoạt động) hoặc ngừng hoạt động", []);
        var updated = await repo.UpdateAsync(id, req, userId, ct);
        if (updated == 0) throw new NotFoundException($"Không tìm thấy khách hàng {id}");
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(string id, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        _ = await GetAsync(id, ct); // 404 nếu không tồn tại
        await repo.SoftDeleteAsync(id, userId, ct);
    }
}

/// <summary>Validator khách hàng (Phase 4).</summary>
public sealed class CustomerValidator : AbstractValidator<CustomerCreateRequest>
{
    public CustomerValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255).WithMessage("Tên khách hàng bắt buộc, ≤ 255 ký tự");
        RuleFor(x => x.MaKhach).MaximumLength(255);
        RuleFor(x => x.DienThoai).MaximumLength(255);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email không hợp lệ");
        RuleFor(x => x.SongayChoNo).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DiemTichLuy).GreaterThanOrEqualTo(0);
    }
}

/// <summary>Use-case đọc + ghi danh mục nhà cung cấp (Phase 4). KHÔNG chứa SQL.</summary>
public sealed class SupplierService(SupplierRepository repo, IOptions<FeatureFlagOptions> flags)
{
    private readonly SupplierValidator _validator = new();

    public async Task<PageResult<SupplierDto>> SearchAsync(SupplierListQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchAsync(q, ct);
        return new PageResult<SupplierDto>
        {
            Items = items,
            Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize),
            Total = total
        };
    }

    public async Task<SupplierDto> GetAsync(string id, CancellationToken ct = default)
    {
        var s = await repo.GetByIdAsync(id, ct);
        return s ?? throw new NotFoundException($"Không tìm thấy nhà cung cấp {id}");
    }

    public async Task<IReadOnlyList<SupplierGroupDto>> GetGroupsAsync(CancellationToken ct = default)
        => await repo.GetGroupsAsync(ct);

    // ---------- Phase 4 CRUD ----------

    public async Task<CatalogCreateResult> CreateAsync(SupplierCreateRequest req, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        var vr = _validator.Validate(req);
        if (!vr.IsValid)
            throw new ValidationException("Dữ liệu nhà cung cấp không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());
        var id = await repo.InsertAsync(req, userId, ct);
        return new CatalogCreateResult(id, req.Name);
    }

    public async Task<SupplierDto> UpdateAsync(string id, SupplierUpdateRequest req, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        _ = await GetAsync(id, ct);
        var updated = await repo.UpdateAsync(id, req, userId, ct);
        if (updated == 0) throw new NotFoundException($"Không tìm thấy nhà cung cấp {id}");
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(string id, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        _ = await GetAsync(id, ct);
        await repo.SoftDeleteAsync(id, userId, ct);
    }
}

/// <summary>Validator nhà cung cấp (Phase 4).</summary>
public sealed class SupplierValidator : AbstractValidator<SupplierCreateRequest>
{
    public SupplierValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255).WithMessage("Tên nhà cung cấp bắt buộc, ≤ 255 ký tự");
        RuleFor(x => x.MaNhaCungCap).MaximumLength(255);
        RuleFor(x => x.DienThoai).MaximumLength(255);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email))
            .WithMessage("Email không hợp lệ");
        RuleFor(x => x.Website).MaximumLength(255);
    }
}

/// <summary>Use-case đọc + ghi danh mục kho hàng (Phase 4). KHÔNG chứa SQL.</summary>
public sealed class WarehouseService(WarehouseRepository repo, IOptions<FeatureFlagOptions> flags)
{
    private readonly WarehouseValidator _validator = new();

    public async Task<PageResult<WarehouseDto>> SearchAsync(WarehouseListQuery q, CancellationToken ct = default)
    {
        var (items, total) = await repo.SearchAsync(q, ct);
        return new PageResult<WarehouseDto>
        {
            Items = items,
            Page = Paging.NormalizePage(q.Page),
            PageSize = Paging.NormalizePageSize(q.PageSize),
            Total = total
        };
    }

    public async Task<WarehouseDto> GetAsync(string id, CancellationToken ct = default)
    {
        var w = await repo.GetByIdAsync(id, ct);
        return w ?? throw new NotFoundException($"Không tìm thấy kho hàng {id}");
    }

    // ---------- Phase 4 CRUD ----------

    public async Task<CatalogCreateResult> CreateAsync(WarehouseCreateRequest req, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        var vr = _validator.Validate(req);
        if (!vr.IsValid)
            throw new ValidationException("Dữ liệu kho hàng không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());
        if (!string.IsNullOrWhiteSpace(req.ParentId) && !await repo.ExistsAsync(req.ParentId, ct))
            throw new ValidationException("Kho cha không tồn tại", []);
        var id = await repo.InsertAsync(req, userId, ct);
        return new CatalogCreateResult(id, req.Name);
    }

    public async Task<WarehouseDto> UpdateAsync(string id, WarehouseUpdateRequest req, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        _ = await GetAsync(id, ct);
        if (!string.IsNullOrWhiteSpace(req.ParentId))
        {
            if (req.ParentId == id) throw new ValidationException("Kho không thể là cha của chính nó", []);
            if (!await repo.ExistsAsync(req.ParentId, ct)) throw new ValidationException("Kho cha không tồn tại", []);
        }
        var updated = await repo.UpdateAsync(id, req, userId, ct);
        if (updated == 0) throw new NotFoundException($"Không tìm thấy kho hàng {id}");
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(string id, string userId, CancellationToken ct = default)
    {
        CatalogWriteGuard.EnsureWriteAllowed(flags.Value);
        _ = await GetAsync(id, ct);
        await repo.SoftDeleteAsync(id, userId, ct);
    }
}

/// <summary>Validator kho hàng (Phase 4).</summary>
public sealed class WarehouseValidator : AbstractValidator<WarehouseCreateRequest>
{
    public WarehouseValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255).WithMessage("Tên kho bắt buộc, ≤ 255 ký tự");
        RuleFor(x => x.Note).MaximumLength(255);
    }
}
