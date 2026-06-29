namespace Sharkable;

/// <summary>
/// Runtime state tracked for each cron job.
/// </summary>
public sealed class CronJobState
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Cron { get; set; } = "";
    public bool IsRunning { get; set; }
    public DateTimeOffset? NextRun { get; set; }
    public DateTimeOffset? LastRun { get; set; }
    public long? LastDurationMs { get; set; }
    public string? LastError { get; set; }
    public long RunCount { get; set; }
    public bool Paused { get; set; }
}

/// <summary>
/// Pluggable store for cron job distributed locking and state persistence.
/// Default <see cref="MemoryCronJobStore"/> is in-process only.
/// </summary>
public interface ICronJobStore
{
    Task<bool> TryAcquireJobLockAsync(string jobName, TimeSpan ttl);
    Task ReleaseJobLockAsync(string jobName);
    Task SaveStateAsync(string jobName, CronJobState state);
    Task<CronJobState?> LoadStateAsync(string jobName);
    Task<IReadOnlyList<CronJobState>> ListStatesAsync();
}
