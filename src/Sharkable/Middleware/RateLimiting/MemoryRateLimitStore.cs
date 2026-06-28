using System.Collections.Concurrent;

namespace Sharkable;

/// <summary>
/// In-process <see cref="IDistributedRateLimitStore"/> backed by
/// <see cref="ConcurrentDictionary{TKey,TValue}"/>. Single-instance only;
/// for distributed scenarios implement <see cref="IDistributedRateLimitStore"/>
/// with Redis or similar.
/// </summary>
public sealed class MemoryRateLimitStore : IDistributedRateLimitStore
{
    private readonly ConcurrentDictionary<string, (long Count, DateTime ExpiresAt)> _counters = new();

    /// <inheritdoc />
    public Task<long> IncrementAsync(string key, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        long count = 0;
        _counters.AddOrUpdate(key,
            _ => (count = 1, now.Add(window)),
            (_, existing) =>
            {
                if (existing.ExpiresAt < now)
                    return (count = 1, now.Add(window));
                return (count = existing.Count + 1, existing.ExpiresAt);
            });
        return Task.FromResult(count);
    }

    /// <inheritdoc />
    public Task ResetAsync(string key)
    {
        _counters.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
