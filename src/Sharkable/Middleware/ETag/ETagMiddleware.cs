using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// Middleware that adds ETag support to GET/HEAD responses.
/// On first request: computes SHA256 hash of the response body, stores it as ETag.
/// On subsequent requests with matching <c>If-None-Match</c>: returns 304 Not Modified.
/// </summary>
internal sealed class ETagMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ETagOptions _options;
    private readonly ILogger<ETagMiddleware> _logger;

    public ETagMiddleware(RequestDelegate next, ETagOptions options, ILogger<ETagMiddleware> logger)
    {
        _next = next;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.CacheableMethods.Contains(context.Request.Method) || ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        await _next(context);

        if (IsUncacheableStatus(context.Response.StatusCode))
        {
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody);
            return;
        }

        buffer.Position = 0;
        var bytes = buffer.ToArray();
        var hash = ComputeHash(bytes);
        var etag = $"\"{hash}\"";

        context.Response.Headers["ETag"] = etag;
        context.Response.Headers["Cache-Control"] = _options.CacheControlHeader;

        if (context.Request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch) &&
            ifNoneMatch.ToString().Trim('"') == hash)
        {
            context.Response.StatusCode = 304;
            context.Response.Body = originalBody;
            context.Response.ContentLength = 0;
            return;
        }

        context.Response.Body = originalBody;
        await context.Response.Body.WriteAsync(bytes);
    }

    private bool ShouldSkip(string path)
    {
        foreach (var exclude in _options.ExcludePaths)
        {
            if (path.StartsWith(exclude, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private bool IsUncacheableStatus(int statusCode)
        => _options.ShouldSkipStatus(statusCode);

    private static string ComputeHash(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
