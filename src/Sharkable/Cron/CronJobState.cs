namespace Sharkable;

/// <summary>
/// Runtime state tracked for each cron job.
/// </summary>
public sealed class CronJobState
{
    /// <summary>Display name of the cron job.</summary>
    public string Name { get; set; } = "";
    /// <summary>Free-text description of what this job does.</summary>
    public string Description { get; set; } = "";
    /// <summary>Cron expression that defines the job schedule.</summary>
    public string Cron { get; set; } = "";
    /// <summary>Whether the job is currently executing.</summary>
    public bool IsRunning { get; set; }
    /// <summary>Next scheduled execution time (UTC).</summary>
    public DateTimeOffset? NextRun { get; set; }
    /// <summary>Most recent execution time (UTC).</summary>
    public DateTimeOffset? LastRun { get; set; }
    /// <summary>Duration of the most recent execution in milliseconds.</summary>
    public long? LastDurationMs { get; set; }
    /// <summary>Error message from the most recent failed execution.</summary>
    public string? LastError { get; set; }
    /// <summary>Total number of times this job has executed.</summary>
    public long RunCount { get; set; }
    /// <summary>Whether this job is paused and will not execute.</summary>
    public bool Paused { get; set; }
}

/// <summary>
/// Pluggable store for cron job distributed locking and state persistence.
/// Default <see cref="MemoryCronJobStore"/> is in-process only.
/// </summary>
public interface ICronJobStore
{
    /// <summary>
    /// Acquires a distributed lock for the given cron job. Returns <c>true</c>
    /// if the lock was acquired; <c>false</c> if another instance is already
    /// processing this job.
    /// </summary>
    Task<bool> TryAcquireJobLockAsync(string jobName, TimeSpan ttl);

    /// <summary>
    /// Extends the TTL of an already-held distributed lock. Called periodically
    /// by <see cref="CronScheduler"/> while a cron job is executing so that
    /// long-running jobs do not cause the lock to expire and split-brain
    /// execution. Stores that do not support TTL renewal (in-process) should
    /// override to return <see cref="Task.CompletedTask"/>.
    /// Default returns <see cref="Task.CompletedTask"/> (no-op).
    /// </summary>
    /// <param name="jobName">The cron job whose lock should be renewed.</param>
    /// <param name="ttl">The new TTL to apply. Typically equal to the original
    /// lock TTL so the renewal cadence is consistent.</param>
    Task RenewJobLockAsync(string jobName, TimeSpan ttl) => Task.CompletedTask;

    /// <summary>
    /// Releases the distributed lock for the given cron job.
    /// </summary>
    Task ReleaseJobLockAsync(string jobName);

    /// <summary>
    /// Persists runtime state for a cron job (last run, duration, run count,
    /// error message, etc.) so it survives process restarts.
    /// </summary>
    Task SaveStateAsync(string jobName, CronJobState state);

    /// <summary>
    /// Loads the persisted state for a cron job, or <c>null</c> if none recorded.
    /// </summary>
    Task<CronJobState?> LoadStateAsync(string jobName);

    /// <summary>
    /// Lists persisted state for every cron job known to the store.
    /// </summary>
    Task<IReadOnlyList<CronJobState>> ListStatesAsync();
}