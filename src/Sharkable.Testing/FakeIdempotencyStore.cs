using System.Collections.Concurrent;
using Sharkable;

namespace Sharkable.Testing;

/// <summary>
/// In-memory fake for <see cref="IIdempotencyStore"/> suitable for unit tests.
/// Thread-safe, no expiration — records persist until manually cleared.
/// </summary>
public sealed class FakeIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, object> _store = new();

    /// <summary>
    /// Returns the number of stored entries.
    /// </summary>
    public int Count => _store.Count;

    /// <inheritdoc/>
    public Task<bool> TryReserveAsync(string key, TimeSpan inFlightTtl)
    {
        return Task.FromResult(_store.TryAdd(key, new object()));
    }

    /// <inheritdoc/>
    public Task<IdempotencyLookup?> GetAsync(string key)
    {
        if (!_store.TryGetValue(key, out var value))
            return Task.FromResult<IdempotencyLookup?>(null);

        if (value is IdempotencyRecord record)
            return Task.FromResult<IdempotencyLookup?>(new IdempotencyHit(record));

        return Task.FromResult<IdempotencyLookup?>(new IdempotencyInFlight());
    }

    /// <inheritdoc/>
    public Task StoreAsync(string key, IdempotencyRecord record, TimeSpan ttl)
    {
        _store[key] = record;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ReleaseAsync(string key)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes all stored entries.
    /// </summary>
    public void Clear() => _store.Clear();
}
