using System.Collections.Concurrent;

namespace Sharkable;

/// <summary>
/// In-memory <see cref="ICronJobStore"/>. Suitable for development.
/// For multi-instance production, use <c>RedisCronJobStore</c> from
/// <c>Sharkable.Cache.Redis</c>.
/// </summary>
public sealed class MemoryCronJobStore : ICronJobStore
{
    private readonly ConcurrentDictionary<string, byte> _locks = new();
    private readonly ConcurrentDictionary<string, CronJobState> _states = new();

    public Task<bool> TryAcquireJobLockAsync(string jobName, TimeSpan ttl)
        => Task.FromResult(_locks.TryAdd(jobName, 0));

    /// <summary>
    /// No-op: in-process locks survive until <see cref="ReleaseJobLockAsync"/>
    /// is called, so TTL renewal is unnecessary.
    /// </summary>
    public Task RenewJobLockAsync(string jobName, TimeSpan ttl)
        => Task.CompletedTask;

    public Task ReleaseJobLockAsync(string jobName)
    {
        _locks.TryRemove(jobName, out _);
        return Task.CompletedTask;
    }

    public Task SaveStateAsync(string jobName, CronJobState state)
    {
        _states[jobName] = state;
        return Task.CompletedTask;
    }

    public Task<CronJobState?> LoadStateAsync(string jobName)
        => Task.FromResult(_states.TryGetValue(jobName, out var s) ? s : null);

    public Task<IReadOnlyList<CronJobState>> ListStatesAsync()
        => Task.FromResult<IReadOnlyList<CronJobState>>(_states.Values.ToList());
}