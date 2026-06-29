using System.Diagnostics;

namespace Sharkable;

/// <summary>
/// Helper that writes either Sharkable's unified result or
/// RFC 7807 ProblemDetails format, depending on
/// <see cref="SharkOption.UseProblemDetails"/>.
/// </summary>
internal static class ProblemDetailsResult
{
    internal static async Task WriteAsync(HttpContext ctx, int statusCode, string detail)
    {
        if (Shark.SharkOption.UseProblemDetails)
        {
            ctx.Response.ContentType = "application/problem+json";
            await ctx.Response.WriteAsJsonAsync(new
            {
                type = $"https://httpstatuses.com/{statusCode}",
                title = GetTitle(statusCode),
                status = statusCode,
                detail,
                instance = ctx.Request.Path.Value ?? "/",
                traceId = Activity.Current?.TraceId.ToString(),
            });
        }
        else
        {
            var factory = Shark.SharkOption.UnifiedResultFactory ?? new DefaultUnifiedResultFactory();
            var result = factory.Create(data: null, errorMessage: detail, statusCode: statusCode);
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(result, result.GetType());
        }
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        429 => "Too Many Requests",
        500 => "Internal Server Error",
        503 => "Service Unavailable",
        _ => "Error",
    };
}
