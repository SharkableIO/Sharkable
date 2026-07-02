using Microsoft.Extensions.Caching.Memory;

namespace Sharkable;

/// <summary>
/// In-process <see cref="IIdempotencyStore"/> backed by
/// <see cref="IMemoryCache"/>. Single-instance only; for distributed
/// scenarios implement <see cref="IIdempotencyStore"/> with Redis or
/// similar.
///
/// The injected <see cref="IMemoryCache"/> MUST have
/// <see cref="MemoryCacheOptions.SizeLimit"/> set to bound the total
/// number of distinct idempotency keys. Each entry records its cost via
/// <c>entry.Size</c> (completed records charge <c>Body.Length + 256</c>,
/// in-flight markers charge 256) so the cap reflects realistic memory
/// pressure. Without a configured <c>SizeLimit</c>, an attacker sending
/// random unique <c>Idempotency-Key</c> headers can exhaust process memory.
/// </summary>
public sealed class MemoryIdempotencyStore : IIdempotencyStore
{
    private const int MarkerSize = 256;
    private readonly IMemoryCache _cache;
    private readonly object _reservationLock = new();

    /// <summary>Marker for an in-flight slot (no record yet).</summary>
    private sealed record InFlightMarker;

    public MemoryIdempotencyStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public Task<bool> TryReserveAsync(string key, TimeSpan inFlightTtl)
    {
        // IMemoryCache.GetOrCreate is NOT atomic across threads: the
        // factory may run concurrently on multiple threads, each creating
        // its own InFlightMarker and each believing it won the reservation.
        // We serialize the check-and-set with a lock so only one caller
        // sees its own marker come back from the cache.
        lock (_reservationLock)
        {
            var marker = new InFlightMarker();
            var actual = _cache.GetOrCreate<object>(key, entry =>
            {
                entry.Size = MarkerSize;
                entry.AbsoluteExpirationRelativeToNow = inFlightTtl;
                return marker;
            });
            return Task.FromResult(ReferenceEquals(actual, marker));
        }
    }

    public Task<IdempotencyLookup?> GetAsync(string key)
    {
        if (!_cache.TryGetValue(key, out var value) || value is null)
            return Task.FromResult<IdempotencyLookup?>(null);
        var result = value switch
        {
            InFlightMarker => (IdempotencyLookup)new IdempotencyInFlight(),
            IdempotencyRecord r => new IdempotencyHit(r),
            _ => null,
        };
        return Task.FromResult(result);
    }

    public Task StoreAsync(string key, IdempotencyRecord record, TimeSpan ttl)
    {
        using var entry = _cache.CreateEntry(key);
        entry.Size = record.Body.Length + MarkerSize;
        entry.AbsoluteExpirationRelativeToNow = ttl;
        entry.Value = record;
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string key)
    {
        _cache.Remove(key);
        return Task.CompletedTask;
    }
}