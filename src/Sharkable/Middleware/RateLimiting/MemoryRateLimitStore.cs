using Microsoft.Extensions.Caching.Memory;

namespace Sharkable;

/// <summary>
/// In-process <see cref="IDistributedRateLimitStore"/> backed by
/// <see cref="IMemoryCache"/> with a hard <see cref="MemoryCacheOptions.SizeLimit"/>
/// cap. Single-instance only; for distributed scenarios implement
/// <see cref="IDistributedRateLimitStore"/> with Redis or similar.
///
/// The cache's <c>SizeLimit</c> together with per-entry <c>Size = 256</c>
/// cost accounting caps the total number of distinct rate limit keys
/// (default 100,000), so a slow-loris attacker cannot exhaust process
/// memory by probing unique URLs in a tight loop. Entries expire
/// automatically when their fixed window elapses via
/// <c>AbsoluteExpirationRelativeToNow</c>; <see cref="MemoryCache"/>'s
/// built-in background compaction evicts them on the next scan.
/// </summary>
public sealed class MemoryRateLimitStore : IDistributedRateLimitStore
{
    private const int EntrySize = 256;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Creates a new <see cref="MemoryRateLimitStore"/> with the default
    /// cap of 100,000 distinct rate limit keys.
    /// </summary>
    public MemoryRateLimitStore() : this(100_000) { }

    /// <summary>
    /// Creates a new <see cref="MemoryRateLimitStore"/> with a custom cap.
    /// </summary>
    /// <param name="maxEntries">
    /// Maximum number of distinct rate limit keys to retain. Must be &gt; 0.
    /// When the cap is reached, the cache evicts the least-recently-used
    /// entries.
    /// </param>
    public MemoryRateLimitStore(long maxEntries)
    {
        if (maxEntries <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxEntries), "MaxEntries must be > 0.");
        _cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = maxEntries });
    }

    /// <inheritdoc />
    public Task<long> IncrementAsync(string key, TimeSpan window)
    {
        var now = DateTime.UtcNow;
        var existing = _cache.Get<RateLimitEntry>(key);

        long count;
        DateTime expiresAt;
        if (existing is null || existing.ExpiresAt <= now)
        {
            count = 1;
            expiresAt = now.Add(window);
        }
        else
        {
            count = existing.Count + 1;
            expiresAt = existing.ExpiresAt;
        }

        using var entry = _cache.CreateEntry(key);
        entry.Size = EntrySize;
        entry.AbsoluteExpirationRelativeToNow = window;
        entry.Value = new RateLimitEntry(count, expiresAt);

        return Task.FromResult(count);
    }

    /// <inheritdoc />
    public Task ResetAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    private sealed record RateLimitEntry(long Count, DateTime ExpiresAt);
}