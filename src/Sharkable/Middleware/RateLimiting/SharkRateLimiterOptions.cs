namespace Sharkable;

/// <summary>
/// Configuration options for the distributed rate limiter middleware.
/// Configured via <c>SharkOption.ConfigureRateLimiting(o => ...)</c>.
/// </summary>
public sealed class SharkRateLimiterOptions
{
    /// <summary>
    /// Maximum number of requests allowed within <see cref="DefaultWindow"/>.
    /// Default is 100.
    /// </summary>
    public int DefaultLimit { get; set; } = 100;

    /// <summary>
    /// The fixed window duration for rate limiting. Default is 1 minute.
    /// </summary>
    public TimeSpan DefaultWindow { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Optional custom key generator. Receives <see cref="HttpContext"/> and
    /// returns a rate limit partition key. Default uses
    /// <c>{clientIp}:{requestPath}</c>.
    /// </summary>
    public Func<HttpContext, string>? KeyGenerator { get; set; }

    /// <summary>
    /// Response header prefix for rate limit status headers.
    /// Default is <c>"X-RateLimit"</c>.
    /// </summary>
    public string HeaderPrefix { get; set; } = "X-RateLimit";

    /// <summary>
    /// When <c>true</c>, the middleware writes <c>X-RateLimit-Limit</c>,
    /// <c>X-RateLimit-Remaining</c>, and <c>X-RateLimit-Reset</c> response
    /// headers. Default is <c>true</c>.
    /// </summary>
    public bool IncludeHeaders { get; set; } = true;

    /// <summary>
    /// Generates the default rate limit key from the HTTP context.
    /// Can be overridden via <see cref="KeyGenerator"/>.
    /// </summary>
    public string DefaultKeyGenerator(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value ?? "/";
        return $"rate:{ip}:{path}";
    }
}
