namespace Sharkable;

/// <summary>
/// Distributed counter store for rate limiting. Implement with Redis, PostgreSQL,
/// or any distributed KV store. The default <see cref="MemoryRateLimitStore"/>
/// keeps counters in-process and is suitable for single-instance deployments.
/// </summary>
public interface IDistributedRateLimitStore
{
    /// <summary>
    /// Atomically increments the counter for <paramref name="key"/> within the
    /// given <paramref name="window"/>, returning the new count. The first call
    /// within a window starts the expiration timer; subsequent calls increment
    /// the same counter until the window elapses.
    /// </summary>
    /// <param name="key">The rate limit key (e.g. "fixed:192.168.1.1:/api/orders").</param>
    /// <param name="window">The sliding window duration.</param>
    /// <returns>The new counter value after increment.</returns>
    Task<long> IncrementAsync(string key, TimeSpan window);

    /// <summary>
    /// Removes the counter for <paramref name="key"/>, resetting the window.
    /// </summary>
    /// <param name="key">The rate limit key to reset.</param>
    Task ResetAsync(string key);
}
