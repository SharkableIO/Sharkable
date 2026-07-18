namespace Sharkable;

/// <summary>
/// Sets a per-endpoint distributed rate limit on an <see cref="ISharkEndpoint"/> class.
/// The <see cref="SharkRateLimiterMiddleware"/> reads this metadata and applies the
/// specified limit instead of the global defaults for this endpoint group.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class SharkRateLimitAttribute : Attribute
{
    /// <summary>
    /// Maximum requests allowed within the window.
    /// </summary>
    public int Limit { get; }

    /// <summary>
    /// Window duration in seconds.
    /// </summary>
    public int WindowSeconds { get; }

    /// <summary>
    /// Initializes a new instance with the required limit and window.
    /// </summary>
    /// <param name="limit">Maximum requests allowed within the window.</param>
    /// <param name="windowSeconds">Window duration in seconds.</param>
    public SharkRateLimitAttribute(int limit, int windowSeconds)
    {
        Limit = limit;
        WindowSeconds = windowSeconds;
    }
}
