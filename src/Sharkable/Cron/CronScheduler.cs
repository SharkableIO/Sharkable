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

    public CronScheduler(ICronJobStore store, ILogger<CronScheduler> logger)
    {
        _store = store;
        _logger = logger;
    }

    internal IReadOnlyCollection<CronJob> Jobs
    {
        get { lock (_jobs) return _jobs.Values.ToList(); }
    }

    public void Register(CronJob job)
    {
        lock (_jobs) _jobs[job.Name] = job;
        lock (_expressions) _expressions[job.Name] = CronExpression.Parse(job.Cron);

        var state = new CronJobState
        {
            Name = job.Name,
            Description = job.Options.Description ?? "",
            Cron = job.Cron,
            Paused = job.Options.Paused,
        };
        lock (_states) _states[job.Name] = state;
    }

    internal async Task<List<(CronJob Job, CronJobState State)>> GetDueJobsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var due = new List<(CronJob, CronJobState)>();

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

            if (job.Options.Concurrency == CronJobConcurrency.SkipIfRunning)
            {
                if (!await _store.TryAcquireJobLockAsync(job.Name, TimeSpan.FromMinutes(10)))
                    continue;
                state.IsRunning = true;
            }

            CronExpression? expr;
            lock (_expressions) _expressions.TryGetValue(job.Name, out expr);
            if (expr == null) continue;

            var next = expr.GetNext(now - TimeSpan.FromSeconds(1));
            if (next == null) continue;

            state.NextRun = next;
            if (next <= now)
                due.Add((job, state));
        }

        due.Sort((a, b) => (a.Item2.NextRun ?? DateTimeOffset.MaxValue)
            .CompareTo(b.Item2.NextRun ?? DateTimeOffset.MaxValue));
        return due;
    }

    internal async Task ExecuteJobAsync(CronJob job, CronJobState state)
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

        if (job.Options.Concurrency == CronJobConcurrency.SkipIfRunning)
            await _store.ReleaseJobLockAsync(job.Name);

        await _store.SaveStateAsync(job.Name, state);
    }

    public async Task<CronJobState?> TriggerAsync(string name)
    {
        CronJob? job;
        lock (_jobs) _jobs.TryGetValue(name, out job);
        if (job == null) return null;

        CronJobState? state;
        lock (_states) _states.TryGetValue(name, out state);
        if (state == null) return null;

        _ = Task.Run(async () => await ExecuteJobAsync(job, state));
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
