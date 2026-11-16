namespace TechPos.Shared;

/// <summary>1 file trong thư viện media (W_MEDIA — metadata BLOB trích từ Firebird).</summary>
public sealed class MediaItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Hash { get; set; }
    public long? Size { get; set; }
    public string? MimeType { get; set; }
    public string? ReferenceType { get; set; }
    public string? ColumnName { get; set; }
    public string? ReferenceId { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>URL API để xem/tải file (tương đối, vd /api/media/{id}/file).</summary>
    public string Url { get; set; } = string.Empty;
}

public sealed record MediaQuery(
    string? Search = null,
    string? ReferenceType = null,
    string? MimeType = null,
    int Page = 1,
    int PageSize = 24);
