using System.Collections.Concurrent;

namespace Sharkable;

/// <summary>
/// In-memory <see cref="ISagaStore"/> implementation. Suitable for
/// development and single-instance deployments. Multi-instance
/// deployments should use a distributed store (e.g., Redis via
/// <c>Sharkable.Cache.Redis</c>).
/// </summary>
public sealed class MemorySagaStore : ISagaStore
{
    private readonly ConcurrentDictionary<string, int> _progress = new();
    private readonly ConcurrentDictionary<string, byte> _locks = new();

    /// <inheritdoc />
    /// <remarks>
    /// Lock TTL is ignored — <see cref="ConcurrentDictionary"/> has no
    /// expiration. In-process only; for production with crash recovery,
    /// use a distributed store such as <c>RedisSagaStore</c>.
    /// </remarks>
    public Task<bool> TryAcquireLockAsync(string sagaId, TimeSpan ttl)
    {
        return Task.FromResult(_locks.TryAdd(sagaId, 0));
    }

    /// <inheritdoc />
    /// <remarks>
    /// No-op: in-process locks survive until <see cref="ReleaseLockAsync"/>
    /// or <see cref="DeleteAsync"/> is called, so renewal is unnecessary.
    /// </remarks>
    public Task RenewLockAsync(string sagaId, TimeSpan ttl)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReleaseLockAsync(string sagaId)
    {
        _locks.TryRemove(sagaId, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveProgressAsync(string sagaId, int stepIndex, CancellationToken ct)
    {
        _progress[sagaId] = stepIndex;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> LoadProgressAsync(string sagaId, CancellationToken ct)
    {
        return Task.FromResult(_progress.TryGetValue(sagaId, out var idx) ? idx : -1);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string sagaId, CancellationToken ct)
    {
        _progress.TryRemove(sagaId, out _);
        _locks.TryRemove(sagaId, out _);
        return Task.CompletedTask;
    }
}
