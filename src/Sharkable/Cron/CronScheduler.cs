using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// Cron scheduler implementation. Runs as a singleton, managing all
/// registered jobs and their lifecycle.
/// </summary>
public sealed class CronScheduler : ICronScheduler
{
    private readonly Dictionary<string, CronJob> _jobs = [];
    private readonly Dictionary<string, CronJobState> _states = [];
    private readonly Dictionary<string, CronExpression> _expressions = [];
    private readonly ICronJobStore _store;
    private readonly ILogger<CronScheduler> _logger;

    /// <summary>
    /// Distributed lock TTL applied when a cron job lock is acquired and on
    /// each renewal. Default 10 minutes.
    /// </summary>
    /// <remarks>
    /// MUST exceed the 99.99th-percentile job duration of the deployment. If
    /// a job runs longer than <c>CronLockTtl</c>, the lock will expire while
    /// the job is still executing, allowing a second instance to acquire the
    /// same lock and produce split-brain execution. The scheduler runs a
    /// background renewal task at <c>CronLockTtl / 3</c> intervals to keep the
    /// lock alive for long-running jobs (mirrors <see cref="SagaExecutor"/>).
    /// </remarks>
    public TimeSpan CronLockTtl { get; set; } = TimeSpan.FromMinutes(10);

    public CronScheduler(ICronJobStore store, ILogger<CronScheduler> logger)
    {
        _store = store;
        _logger = logger;
    }

    internal IReadOnlyCollection<CronJob> Jobs
    {
        get { lock (_jobs) return _jobs.Values.ToList(); }
    }

    public async Task RegisterAsync(CronJob job)
    {
        // Parse cron first — fail fast before any state mutation
        CronExpression expr;
        try { expr = CronExpression.Parse(job.Cron); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cron job {Name} has invalid expression: {Cron}", job.Name, job.Cron);
            return;
        }

        lock (_expressions) _expressions[job.Name] = expr;
        lock (_jobs) _jobs[job.Name] = job;

        // SHARK-SEC-017: await the store directly instead of blocking via
        // .GetAwaiter().GetResult(). The sync-over-async form previously used
        // here deadlocks under contention with distributed stores (Redis,
        // PostgreSQL) whose IO completions need the thread pool.
        var existing = await _store.LoadStateAsync(job.Name);
        var state = existing ?? new CronJobState
        {
            Name = job.Name,
            Description = job.Options.Description ?? "",
            Cron = job.Cron,
            Paused = job.Options.Paused,
        };
        state.Description = job.Options.Description ?? "";
        state.Cron = job.Cron;

        lock (_states) _states[job.Name] = state;
    }

    internal async Task<List<(CronJob Job, CronJobState State, bool LockHeld)>> GetDueJobsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var due = new List<(CronJob, CronJobState, bool)>();

        CronJob[] jobs;
        lock (_jobs) jobs = _jobs.Values.ToArray();

        foreach (var job in jobs)
        {
            CronJobState? state;
            lock (_states) state = _states.GetValueOrDefault(job.Name);
            if (state == null) continue;

            if (state.Paused) continue;
            if (state.IsRunning && job.Options.Concurrency == CronJobConcurrency.SkipIfRunning)
                continue;

            CronExpression? expr;
            lock (_expressions) _expressions.TryGetValue(job.Name, out expr);
            if (expr == null) continue;

            var next = expr.GetNext(now - TimeSpan.FromSeconds(1));
            if (next == null) continue;

            state.NextRun = next;
            if (next > now) continue;

            var lockHeld = false;
            if (job.Options.Concurrency == CronJobConcurrency.SkipIfRunning)
            {
                if (!await _store.TryAcquireJobLockAsync(job.Name, CronLockTtl))
                    continue;
                state.IsRunning = true;
                lockHeld = true;
            }

            due.Add((job, state, lockHeld));
        }

        due.Sort((a, b) => (a.Item2.NextRun ?? DateTimeOffset.MaxValue)
            .CompareTo(b.Item2.NextRun ?? DateTimeOffset.MaxValue));
        return due;
    }

    internal async Task ExecuteJobAsync(CronJob job, CronJobState state, bool lockHeld)
    {
        CancellationTokenSource? renewCts = null;
        Task? renewalTask = null;
        if (lockHeld && CronLockTtl > TimeSpan.Zero)
        {
            renewCts = new CancellationTokenSource();
            renewalTask = StartLockRenewalAsync(job.Name, renewCts.Token);
        }

        try
        {
            state.IsRunning = true;
            state.LastRun = DateTimeOffset.UtcNow;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var success = false;

            for (var attempt = 0; attempt <= job.Options.RetryCount && !success; attempt++)
            {
                try
                {
                    using var cts = job.Options.Timeout.HasValue
                        ? new CancellationTokenSource(job.Options.Timeout.Value)
                        : new CancellationTokenSource();

                    await job.Handler(cts.Token);
                    success = true;
                    state.LastError = null;
                }
                catch (Exception ex)
                {
                    state.LastError = ex.Message;
                    _logger.LogError(ex, "Cron job {Name} failed (attempt {Attempt})", job.Name, attempt + 1);
                    if (attempt < job.Options.RetryCount)
                        await Task.Delay(job.Options.RetryDelay);
                }
            }

            sw.Stop();
            state.IsRunning = false;
            state.LastDurationMs = sw.ElapsedMilliseconds;
            if (success) state.RunCount++;
        }
        finally
        {
            if (lockHeld)
            {
                renewCts?.Cancel();
                if (renewalTask != null)
                {
                    try { await renewalTask; }
                    catch (OperationCanceledException) { }
                }
                await _store.ReleaseJobLockAsync(job.Name);
            }

            await _store.SaveStateAsync(job.Name, state);
        }
    }

    /// <summary>
    /// Background loop that periodically renews the cron job lock until the
    /// cancellation token fires. Mirrors <see cref="SagaExecutor"/>'s renewal
    /// pattern so long-running jobs do not lose their lock to TTL expiry.
    /// </summary>
    private async Task StartLockRenewalAsync(string jobName, CancellationToken ct)
    {
        var interval = TimeSpan.FromTicks(CronLockTtl.Ticks / 3);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(interval, ct);
                await _store.RenewJobLockAsync(jobName, CronLockTtl);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cron job {Name} lock renewal failed", jobName);
        }
    }

    public async Task<CronJobState?> TriggerAsync(string name)
    {
        CronJob? job;
        lock (_jobs) _jobs.TryGetValue(name, out job);
        if (job == null) return null;

        CronJobState? state;
        lock (_states) _states.TryGetValue(name, out state);
        if (state == null) return null;

        _ = Task.Run(async () => await ExecuteJobAsync(job, state, lockHeld: false));
        return state;
    }

    public Task PauseAsync(string name)
    {
        lock (_states) { if (_states.TryGetValue(name, out var s)) s.Paused = true; }
        return Task.CompletedTask;
    }

    public Task ResumeAsync(string name)
    {
        lock (_states) { if (_states.TryGetValue(name, out var s)) s.Paused = false; }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CronJobState>> ListAsync()
    {
        lock (_states) return Task.FromResult<IReadOnlyList<CronJobState>>(_states.Values.ToList());
    }
}