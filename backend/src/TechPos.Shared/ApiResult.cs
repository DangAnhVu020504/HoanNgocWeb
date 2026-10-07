namespace TechPos.Shared;

/// <summary>Envelope chuẩn cho mọi API response (mục 36).</summary>
public sealed class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public List<ApiError> Errors { get; init; } = [];
    public string TraceId { get; init; } = string.Empty;

    public static ApiResult<T> Ok(T data, string traceId = "") =>
        new() { Success = true, Data = data, TraceId = traceId };

    public static ApiResult<T> Fail(string message, string traceId = "", List<ApiError>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors ?? [], TraceId = traceId };
}

public sealed record ApiError(string Field, string Code, string Message);

/// <summary>Pagination chuẩn (mục 36).</summary>
public sealed class PageResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public long Total { get; init; }
}

public class NotFoundException(string message) : Exception(message);
public class ConflictException(string message) : Exception(message);
public class ForbiddenException(string message) : Exception(message);
public class ValidationException(string message, List<ApiError> errors) : Exception(message)
{
    public List<ApiError> Errors { get; } = errors;
}