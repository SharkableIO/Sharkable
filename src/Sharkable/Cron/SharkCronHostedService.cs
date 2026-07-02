using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// Background service that polls registered cron jobs every second
/// and executes those whose cron expression matches the current time.
/// </summary>
internal sealed class SharkCronHostedService : BackgroundService
{
    private readonly CronScheduler _scheduler;
    private readonly ILogger<SharkCronHostedService> _logger;

    public SharkCronHostedService(CronScheduler scheduler, ILogger<SharkCronHostedService> logger)
    {
        _scheduler = scheduler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Invoke user-registered cron jobs
        var configureAction = Shark.SharkOption.ConfigureCronJobs;
        if (configureAction != null)
        {
            foreach (var job in _scheduler.Jobs.ToList()) { } // force registration sync
            configureAction(_scheduler);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var due = await _scheduler.GetDueJobsAsync();
                foreach (var (job, state, lockHeld) in due)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _scheduler.ExecuteJobAsync(job, state, lockHeld);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Cron job {Name} unhandled failure", job.Name);
                        }
                    }, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cron scheduler loop error");
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}