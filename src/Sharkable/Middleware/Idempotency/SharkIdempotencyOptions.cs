using Microsoft.AspNetCore.Http;

namespace Sharkable;

/// <summary>
/// Configuration options for the idempotency middleware. Configured via
/// <c>SharkOption.ConfigureIdempotency(o =&gt; ...)</c>.
/// </summary>
public sealed class SharkIdempotencyOptions
{
    /// <summary>How long completed records are kept. Default is 24 hours.</summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Auto-eviction for in-flight placeholders. Protects against permanent
    /// deadlocks when a process crashes mid-request. Default is 30 seconds.
    /// </summary>
    public TimeSpan InFlightTtl { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Reject keys longer than this with a 400. Default is 255 (IETF draft max).</summary>
    public int MaxKeyLength { get; set; } = 255;

    /// <summary>
    /// Responses whose buffered body exceeds this size are replaced with a
    /// 500 <c>idempotency_response_too_large</c> error. Default is 1 MiB.
    /// </summary>
    public int MaxResponseSize { get; set; } = 1_048_576;

    /// <summary>Request header name. Default is <c>"Idempotency-Key"</c>.</summary>
    public string HeaderName { get; set; } = "Idempotency-Key";

    /// <summary>Response header set on replays. Default is <c>"X-Idempotent-Replayed"</c>.</summary>
    public string ReplayedHeaderName { get; set; } = "X-Idempotent-Replayed";

    /// <summary>Value for the <c>Retry-After</c> response header in seconds. Default is 1.</summary>
    public int RetryAfterSeconds { get; set; } = 1;

    /// <summary>
    /// Predicate that determines whether a status code should be cached
    /// for future idempotent replays. Default caches 2xx-4xx except 429.
    /// Return <c>true</c> to cache the response.
    /// </summary>
    public Func<int, bool> ShouldCacheStatus { get; set; } = static status =>
        status >= 200 && status < 500 && status != 429;

    /// <summary>
    /// HTTP methods that activate the middleware when <see cref="HeaderName"/>
    /// is present. Default is POST, PUT, PATCH, DELETE.
    /// </summary>
    public IReadOnlySet<HttpMethod> UnsafeMethods { get; set; } = new HashSet<HttpMethod>
    {
        HttpMethod.Post,
        HttpMethod.Put,
        HttpMethod.Patch,
        HttpMethod.Delete,
    };

    /// <summary>
    /// Validates an <c>Idempotency-Key</c> value: non-empty after trim,
    /// length &lt;= <see cref="MaxKeyLength"/>, and printable ASCII only.
    /// </summary>
    /// <param name="key">The candidate key value (the raw header value).</param>
    /// <returns><c>true</c> if the key passes all checks; <c>false</c> otherwise.</returns>
    public bool IsValidKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (key.Length > MaxKeyLength) return false;
        foreach (var c in key)
        {
            if (c < 0x20 || c > 0x7E) return false;
        }
        return true;
    }
}