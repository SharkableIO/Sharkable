namespace Sharkable;

/// <summary>
/// Endpoint metadata carrying a per-endpoint distributed rate limit.
/// Applied via <c>[SharkRateLimit]</c> attribute or <c>.SharkRateLimit()</c> DSL.
/// Read by <see cref="SharkRateLimiterMiddleware"/> to override the global defaults.
/// </summary>
public sealed class SharkRateLimitMetadata
{
    /// <summary>
    /// Maximum requests allowed within the window for this endpoint.
    /// </summary>
    public int Limit { get; }

    /// <summary>
    /// Time window for the rate limit.
    /// </summary>
    public TimeSpan Window { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="SharkRateLimitMetadata"/>.
    /// </summary>
    /// <param name="limit">Maximum requests within the window.</param>
    /// <param name="window">Time window for the rate limit.</param>
    public SharkRateLimitMetadata(int limit, TimeSpan window)
    {
        Limit = limit;
        Window = window;
    }
}
