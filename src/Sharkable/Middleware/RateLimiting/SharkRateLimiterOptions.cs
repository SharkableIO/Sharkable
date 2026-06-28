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

    // --- Adaptive rate limiting ---

    /// <summary>
    /// Enables adaptive rate limiting. When <c>true</c>, the permit limit
    /// is dynamically adjusted based on CPU usage and GC pressure instead
    /// of using a fixed <see cref="DefaultLimit"/>.
    /// Default is <c>false</c>.
    /// </summary>
    public bool EnableAdaptive { get; set; } = false;

    /// <summary>
    /// The base permit limit before adaptive adjustments.
    /// Default is <see cref="DefaultLimit"/>.
    /// </summary>
    public int BasePermitLimit { get; set; } = 100;

    /// <summary>
    /// Minimum permit limit when system is under high load.
    /// Default is 10.
    /// </summary>
    public int MinPermitLimit { get; set; } = 10;

    /// <summary>
    /// Maximum permit limit when system is idle.
    /// Default is <see cref="BasePermitLimit"/> * 5.
    /// </summary>
    public int MaxPermitLimit { get; set; } = 500;

    /// <summary>
    /// CPU usage percentage above which permits are reduced.
    /// Default is 80.
    /// </summary>
    public int AdaptiveCpuHighThreshold { get; set; } = 80;

    /// <summary>
    /// CPU usage percentage below which permits are increased.
    /// Default is 40.
    /// </summary>
    public int AdaptiveCpuLowThreshold { get; set; } = 40;

    /// <summary>
    /// How often the adaptive monitor re-evaluates system load.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan AdaptiveAdjustmentInterval { get; set; } = TimeSpan.FromSeconds(5);
}
