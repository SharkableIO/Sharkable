namespace Sharkable;

/// <summary>
/// Configuration options for a cron job registered via
/// <c>ConfigureCronJobs()</c>.
/// </summary>
public sealed class CronJobOptions
{
    /// <summary>Human-readable description shown in admin endpoint.</summary>
    public string? Description { get; set; }

    /// <summary>Max retry count on failure. 0 = no retry. Default 0.</summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>Delay between retries. Default 5 seconds.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Max execution duration before cancellation. null = unlimited.</summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>When true, job is registered but skipped on cron ticks.</summary>
    public bool Paused { get; set; } = false;

    /// <summary>Concurrency behavior. Default: <see cref="CronJobConcurrency.AllowConcurrent"/>.</summary>
    public CronJobConcurrency Concurrency { get; set; } = CronJobConcurrency.AllowConcurrent;
}

/// <summary>
/// Controls what happens when a cron job is triggered while a previous
/// execution is still running.
/// </summary>
public enum CronJobConcurrency
{
    /// <summary>Execute anyway — no mutual exclusion.</summary>
    AllowConcurrent = 0,

    /// <summary>Skip this tick if the previous execution hasn't finished.</summary>
    SkipIfRunning = 1,
}
