using System.Diagnostics;
using System.Text.Json;

namespace Sharkable;

/// <summary>
/// Helper that writes either Sharkable's unified result or
/// RFC 7807 ProblemDetails format, depending on
/// <see cref="SharkOption.UseProblemDetails"/>.
/// Uses a concrete type for AOT safety.
/// </summary>
internal static class ProblemDetailsResult
{
    internal static async Task WriteAsync(HttpContext ctx, int statusCode, string detail)
    {
        if (Shark.SharkOption.UseProblemDetails)
        {
            ctx.Response.ContentType = "application/problem+json";
            var problem = new ProblemDetailsData
            {
                type = $"https://httpstatuses.com/{statusCode}",
                title = GetTitle(statusCode),
                status = statusCode,
                detail = detail,
                instance = ctx.Request.Path.Value ?? "/",
                traceId = Activity.Current?.TraceId.ToString(),
            };
            await ctx.Response.WriteAsJsonAsync(problem);
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

/// <summary>
/// RFC 7807 ProblemDetails response type. Used as a concrete
/// type for AOT-safe JSON serialization.
/// </summary>
internal sealed class ProblemDetailsData
{
    public string type { get; set; } = "";
    public string title { get; set; } = "";
    public int status { get; set; }
    public string detail { get; set; } = "";
    public string instance { get; set; } = "";
    public string? traceId { get; set; }
}
