using FluentValidation;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Sales;

/// <summary>Validator phiếu gửi COD (mục 4.10).</summary>
public sealed class CodShipmentRequestValidator : AbstractValidator<CodShipmentRequest>
{
    public CodShipmentRequestValidator()
    {
        RuleFor(x => x.DonViChuyenPhatId).NotEmpty().WithMessage("Chưa chọn đơn vị chuyển phát");
        RuleFor(x => x.DonHangIds).NotEmpty().WithMessage("Phiếu gửi COD phải có ít nhất 1 đơn hàng");
    }
}

/// <summary>Validator giao hàng theo xe (mục 4.10).</summary>
public sealed class VehicleDeliveryRequestValidator : AbstractValidator<VehicleDeliveryRequest>
{
    public VehicleDeliveryRequestValidator()
    {
        RuleFor(x => x.XeGiaoHangId).NotEmpty().WithMessage("Chưa chọn xe giao hàng");
        RuleFor(x => x.DonHangIds).NotEmpty().WithMessage("Phiếu giao xe phải có ít nhất 1 đơn hàng");
    }
}

/// <summary>Use-case giao hàng COD + giao hàng theo xe (mục 4.10). KHÔNG chứa SQL.</summary>
public sealed class DeliveryService(CodRepository cod, VehicleDeliveryRepository vehicles)
{
    private readonly CodShipmentRequestValidator _codValidator = new();
    private readonly VehicleDeliveryRequestValidator _vehicleValidator = new();

    public async Task<CodShipmentResult> CreateCodShipmentAsync(CodShipmentRequest req, string userId, CancellationToken ct = default)
    {
        Ensure(_codValidator.Validate(req), "phiếu gửi COD");
        return await cod.CreateShipmentAsync(req, userId, ct);
    }

    public async Task<CodResultEntry> CreateCodResultAsync(CodResultRequest req, string userId, CancellationToken ct = default)
    {
        return await cod.CreateResultAsync(req, userId, ct);
    }

    public async Task<VehicleDeliveryResult> CreateVehicleDeliveryAsync(VehicleDeliveryRequest req, string userId, CancellationToken ct = default)
    {
        Ensure(_vehicleValidator.Validate(req), "phiếu giao hàng theo xe");
        return await vehicles.CreateAsync(req, userId, ct);
    }

    private static void Ensure(FluentValidation.Results.ValidationResult vr, string label)
    {
        if (!vr.IsValid)
            throw new TechPos.Shared.ValidationException($"Dữ liệu {label} không hợp lệ",
                vr.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList());
    }
}