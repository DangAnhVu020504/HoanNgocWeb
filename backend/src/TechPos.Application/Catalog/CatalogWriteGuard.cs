using Microsoft.Extensions.Options;
using TechPos.Shared;

namespace TechPos.Application.Catalog;

/// <summary>
/// Chặn ghi danh mục khi feature flag ENABLE_CATALOG_WRITE = false (ADR-02: GĐ1–6 web không ghi nghiệp vụ,
/// chỉ bật khi đã bật 2 chiều có kiểm soát). Đọc từ FeatureFlags.EnableCatalogWrite (Options) —
/// service kiểm tra trước khi gọi repo.
/// </summary>
public static class CatalogWriteGuard
{
    public static void EnsureWriteAllowed(FeatureFlagOptions flags)
    {
        if (!flags.EnableCatalogWrite)
            throw new ForbiddenException(
                "Chức năng ghi danh mục đang TẮT (ENABLE_CATALOG_WRITE=false). " +
                "Bật flag trong cài đặt hệ thống khi cho phép web ghi danh mục.");
    }
}
