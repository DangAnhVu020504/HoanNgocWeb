using Microsoft.Extensions.Options;
using TechPos.Data.MySql;
using TechPos.Shared;

namespace TechPos.Application.Admin;

/// <summary>
/// Thư viện media (W_MEDIA). File vật lý do Sync MediaExtractor ghi vào
/// &lt;Sync.Service&gt;/media-store; API phục vụ file theo Media:RootPath.
/// </summary>
public sealed class MediaService(MediaRepository repo, IOptions<MediaOptions> options)
{
    private readonly string _root = ResolveRoot(options.Value.RootPath);

    public Task<(IReadOnlyList<MediaItemDto> Items, long Total)> SearchAsync(
        MediaQuery q, CancellationToken ct = default)
        => repo.SearchAsync(q, ct);

    public Task<MediaItemDto?> GetAsync(string id, CancellationToken ct = default)
        => repo.GetAsync(id, ct);

    public Task<IReadOnlyList<string>> GetReferenceTypesAsync(CancellationToken ct = default)
        => repo.GetReferenceTypesAsync(ct);

    /// <summary>Trả về đường dẫn vật lý an toàn (chặn path traversal) hoặc null nếu không tồn tại.</summary>
    public string? ResolvePhysicalPath(MediaItemDto item)
    {
        if (string.IsNullOrWhiteSpace(item.Path)) return null;
        var root = Path.GetFullPath(_root);
        var candidate = Path.GetFullPath(Path.Combine(root, item.Path.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSep = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase)) return null;
        return File.Exists(candidate) ? candidate : null;
    }

    public string RootPath => _root;

    private static string ResolveRoot(string? configured)
    {
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        var env = Environment.GetEnvironmentVariable("TECHPOS_MEDIA_ROOT");
        if (!string.IsNullOrWhiteSpace(env)) return env;
        return Path.Combine(AppContext.BaseDirectory, "media-store");
    }
}
