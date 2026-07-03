using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// Cron scheduler implementation. Runs as a singleton, managing all
/// registered jobs and their lifecycle.
/// </summary>
public sealed class CronScheduler : ICronScheduler
{
    // SHARK-SEC-M013: collapse the previous three correlated dictionaries
    // (jobs / states / expressions) into a single JobEntry record under
    // one lock. The previous design had TOCTOU gaps where Register could
    // mutate _expressions and _jobs but a concurrent GetDueJobsAsync
    // could read the partial state between the two writes.
    private sealed record JobEntry(CronJob Job, CronJobState State, CronExpression Expression);

    private readonly Dictionary<string, JobEntry> _entries = [];
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

    /// <summary>Creates a scheduler with the given store and logger.</summary>
    public CronScheduler(ICronJobStore store, ILogger<CronScheduler> logger)
    {
        _store = store;
        _logger = logger;
    }

    internal IReadOnlyCollection<CronJob> Jobs
    {
        get { lock (_entries) return _entries.Values.Select(e => e.Job).ToList(); }
    }

    /// <summary>Registers a new cron job and starts tracking its schedule.</summary>
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

        // Single dictionary insert under one lock — readers see the fully
        // initialized (Job, State, Expression) tuple atomically.
        lock (_entries) _entries[job.Name] = new JobEntry(job, state, expr);
    }

    internal async Task<List<(CronJob Job, CronJobState State, bool LockHeld)>> GetDueJobsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var due = new List<(CronJob, CronJobState, bool)>();

        JobEntry[] snapshot;
        lock (_entries) snapshot = _entries.Values.ToArray();

        foreach (var entry in snapshot)
        {
            var job = entry.Job;
            var state = entry.State;
            var expr = entry.Expression;

            if (state.Paused) continue;
            if (state.IsRunning && job.Options.Concurrency == CronJobConcurrency.SkipIfRunning)
                continue;

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

    /// <summary>Manually triggers a registered cron job by name.</summary>
    public async Task<CronJobState?> TriggerAsync(string name)
    {
        JobEntry? entry;
        lock (_entries) _entries.TryGetValue(name, out entry);
        if (entry == null) return null;

        var job = entry.Job;
        var state = entry.State;

        _ = Task.Run(async () => await ExecuteJobAsync(job, state, lockHeld: false));
        return state;
    }

    /// <summary>Pauses a cron job by name. Paused jobs skip their scheduled runs.</summary>
    public Task PauseAsync(string name)
    {
        lock (_entries) { if (_entries.TryGetValue(name, out var e)) e.State.Paused = true; }
        return Task.CompletedTask;
    }

    /// <summary>Resumes a previously paused cron job.</summary>
    public Task ResumeAsync(string name)
    {
        lock (_entries) { if (_entries.TryGetValue(name, out var e)) e.State.Paused = false; }
        return Task.CompletedTask;
    }

    /// <summary>Returns the runtime state of all registered cron jobs.</summary>
    public Task<IReadOnlyList<CronJobState>> ListAsync()
    {
        lock (_entries)
        {
            return Task.FromResult<IReadOnlyList<CronJobState>>(
                _entries.Values.Select(e => e.State).ToList());
        }
    }
}