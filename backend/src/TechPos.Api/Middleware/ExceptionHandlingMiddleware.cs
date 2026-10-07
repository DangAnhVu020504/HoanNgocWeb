using System.Text.Json;
using FluentValidation;
using TechPos.Shared;

namespace TechPos.Api.Middleware;

/// <summary>
/// Centralized error handling (mục 36/37): map exception → ApiResult envelope,
/// không trả raw DB error, gắn traceId.
/// </summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, Serilog.ILogger logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (statusCode, message, errors) = MapException(ex);
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";

            if (statusCode == StatusCodes.Status500InternalServerError)
                logger.Error(ex, "Unhandled exception {Path}", context.Request.Path);

            var payload = new
            {
                success = false,
                data = (object?)null,
                message,
                errors,
                traceId = context.TraceIdentifier
            };
            context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }

    private static (int Status, string Message, List<ApiError> Errors) MapException(Exception ex) => ex switch
    {
        NotFoundException => (StatusCodes.Status404NotFound, ex.Message, []),
        ForbiddenException => (StatusCodes.Status403Forbidden, ex.Message, []),
        ConflictException => (StatusCodes.Status409Conflict, ex.Message, []),
        TechPos.Shared.ValidationException v => (StatusCodes.Status400BadRequest, v.Message,
            v.Errors.Select(e => new ApiError(e.Field, e.Code, e.Message)).ToList()),
        FluentValidation.ValidationException fv => (StatusCodes.Status400BadRequest, "Dữ liệu không hợp lệ",
            fv.Errors.Select(e => new ApiError(e.PropertyName ?? "", e.ErrorCode ?? "", e.ErrorMessage)).ToList()),
        _ => (StatusCodes.Status500InternalServerError, "Đã xảy ra lỗi hệ thống", [])
    };
}