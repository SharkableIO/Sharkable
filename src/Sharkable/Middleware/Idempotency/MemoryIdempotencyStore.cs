using Microsoft.Extensions.Caching.Memory;

namespace Sharkable;

/// <summary>
/// In-process <see cref="IIdempotencyStore"/> backed by
/// <see cref="IMemoryCache"/>. Single-instance only; for distributed
/// scenarios implement <see cref="IIdempotencyStore"/> with Redis or
/// similar.
/// </summary>
public sealed class MemoryIdempotencyStore : IIdempotencyStore
{
    private readonly IMemoryCache _cache;
    private readonly object _reservationLock = new();

    /// <summary>Marker for an in-flight slot (no record yet).</summary>
    private sealed record InFlightMarker;

    public MemoryIdempotencyStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool TryReserve(string key, TimeSpan inFlightTtl)
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
                entry.AbsoluteExpirationRelativeToNow = inFlightTtl;
                return marker;
            });
            return ReferenceEquals(actual, marker);
        }
    }

    public IdempotencyLookup? Get(string key)
    {
        if (!_cache.TryGetValue(key, out var value) || value is null)
            return null;
        return value switch
        {
            InFlightMarker => new IdempotencyInFlight(),
            IdempotencyRecord r => new IdempotencyHit(r),
            _ => null,
        };
    }

    public void Store(string key, IdempotencyRecord record, TimeSpan ttl)
    {
        using var entry = _cache.CreateEntry(key);
        entry.AbsoluteExpirationRelativeToNow = ttl;
        entry.Value = record;
    }

    public void Release(string key)
    {
        _cache.Remove(key);
    }
}
