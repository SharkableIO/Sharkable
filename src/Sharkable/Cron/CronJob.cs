namespace Sharkable;

/// <summary>
/// A registered cron job definition.
/// </summary>
/// <param name="Name">Unique job name.</param>
/// <param name="Cron">6-field cron expression.</param>
/// <param name="Handler">Async handler delegate (receives CancellationToken).</param>
/// <param name="Options">Job options (retry, timeout, concurrency, etc.).</param>
public sealed record CronJob(
    string Name,
    string Cron,
    Func<CancellationToken, Task> Handler,
    CronJobOptions Options);

/// <summary>
/// Public API for managing cron jobs at runtime.
/// </summary>
public interface ICronScheduler
{
    /// <summary>Registers a cron job from configuration.</summary>
    /// <remarks>
    /// Awaits the underlying <see cref="ICronJobStore"/> call so distributed
    /// stores (Redis, PostgreSQL) are loaded without sync-over-async blocking
    /// (SHARK-SEC-017). Replace any calls to the legacy synchronous
    /// <c>Register</c> overload with <c>await RegisterAsync</c>.
    /// </remarks>
    Task RegisterAsync(CronJob job);

    /// <summary>Manually triggers a job immediately, regardless of its cron schedule.</summary>
    Task<CronJobState?> TriggerAsync(string name);

    /// <summary>Pauses a job — cron ticks are ignored until resumed.</summary>
    Task PauseAsync(string name);

    /// <summary>Resumes a paused job.</summary>
    Task ResumeAsync(string name);

    /// <summary>Returns the current state of all registered jobs.</summary>
    Task<IReadOnlyList<CronJobState>> ListAsync();
}
