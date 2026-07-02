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
    /// <para>
    /// SHARK-SEC-M017: includes the authenticated user identity (when present)
    /// so a single malicious client cannot bypass per-user limits by sharing
    /// a source IP with legitimate traffic. Behind a reverse proxy/CDN,
    /// <c>RemoteIpAddress</c> is the proxy address, so deployers should also
    /// configure <c>ForwardedHeadersMiddleware</c> with a <c>KnownProxies</c>
    /// allowlist — that is the only way the framework can trust the source
    /// IP for unauthenticated traffic.
    /// </para>
    /// </summary>
    public string DefaultKeyGenerator(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        // Prefer the authenticated principal name; fall back to the remote IP.
        // Using the user identity as the partition key prevents one attacker
        // from starving legitimate users when they share a NAT / proxy IP.
        var partition = context.User?.Identity?.IsAuthenticated == true
            && !string.IsNullOrEmpty(context.User.Identity.Name)
                ? "u:" + context.User.Identity.Name
                : "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        return $"rate:{partition}:{path}";
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

    /// <summary>
    /// GC pressure percentage above which permits are reduced.
    /// Default is 80.
    /// </summary>
    public int AdaptiveGcHighThreshold { get; set; } = 80;

    /// <summary>
    /// GC pressure percentage below which permits are increased.
    /// Default is 50.
    /// </summary>
    public int AdaptiveGcLowThreshold { get; set; } = 50;

    /// <summary>
    /// Fraction (1/N) by which permits are reduced each adjustment cycle.
    /// Default is 10 (i.e. reduce by 1/10).
    /// </summary>
    public int AdaptiveReductionDivisor { get; set; } = 10;

    /// <summary>
    /// Maximum number of distinct rate limit keys retained by the in-process
    /// <see cref="MemoryRateLimitStore"/>. Each entry accounts for ~256 bytes
    /// of cache size. When the cap is reached the cache evicts
    /// least-recently-used entries; this prevents a slow-loris attacker from
    /// exhausting memory by probing unique URLs in a tight loop. Default is
    /// 100,000. Set to <c>-1</c> to disable the cap (not recommended for
    /// untrusted traffic).
    /// </summary>
    public long MaxEntries { get; set; } = 100_000;
}
