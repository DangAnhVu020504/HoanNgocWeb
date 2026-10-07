namespace TechPos.Shared;

/// <summary>Helper phân trang thuần — dùng chung service/repository, test được (mục 60).</summary>
public static class Paging
{
    public const int MaxPageSize = 200;

    public static int NormalizePage(int page) => Math.Max(1, page);

    public static int NormalizePageSize(int pageSize) => Math.Clamp(pageSize, 1, MaxPageSize);

    public static int Offset(int page, int pageSize) => (NormalizePage(page) - 1) * NormalizePageSize(pageSize);
}
