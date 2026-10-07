namespace TechPos.Shared;

/// <summary>Query lịch sử chứng từ (lọc theo từ khoá + phân trang).</summary>
public sealed record HistoryQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 20);
