using System.Collections.Concurrent;
using Sharkable;

namespace Sharkable.Testing;

/// <summary>
/// In-memory fake for <see cref="IDistributedRateLimitStore"/> suitable for unit tests.
/// Thread-safe, no expiration — counters persist until manually cleared.
/// </summary>
public sealed class FakeRateLimitStore : IDistributedRateLimitStore
{
    private readonly ConcurrentDictionary<string, long> _counters = new();

    /// <summary>
    /// Returns the number of tracked rate-limit keys.
    /// </summary>
    public int Count => _counters.Count;

    /// <inheritdoc/>
    public Task<long> IncrementAsync(string key, TimeSpan window)
    {
        var count = _counters.AddOrUpdate(key, 1, (_, v) => v + 1);
        return Task.FromResult(count);
    }

    /// <inheritdoc/>
    public Task ResetAsync(string key)
    {
        _counters.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the current count for a given key, or 0 if not found.
    /// </summary>
    public long GetCount(string key)
        => _counters.TryGetValue(key, out var count) ? count : 0;

    /// <summary>
    /// Removes all tracked keys.
    /// </summary>
    public void Clear() => _counters.Clear();
}
