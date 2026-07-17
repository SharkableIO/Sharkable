using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// Periodically monitors CPU usage and GC pressure, then adjusts the
/// effective rate limit up or down. Managed as an internal singleton
/// started by the middleware when adaptive mode is enabled.
/// </summary>
internal sealed class AdaptiveLimitMonitor : IDisposable
{
    private readonly SharkRateLimiterOptions _options;
    private Process? _process;
    private readonly ILogger? _logger;
    private TimeSpan _lastTotalProcessorTime;
    private DateTime _lastSampleAt;
    private Timer? _timer;
    private bool _disposed;

    /// <summary>
    /// The current dynamically-adjusted permit limit. Read by the middleware
    /// on each request.
    /// </summary>
    internal int CurrentLimit;

    internal AdaptiveLimitMonitor(SharkRateLimiterOptions options)
    {
        _options = options;
        _lastTotalProcessorTime = TimeSpan.Zero;
        _lastSampleAt = DateTime.UtcNow;
        CurrentLimit = options.BasePermitLimit;
        _logger = InternalShark.ServiceProvider?.GetService<ILoggerFactory>()
            ?.CreateLogger<AdaptiveLimitMonitor>();
    }

    internal AdaptiveLimitMonitor(SharkRateLimiterOptions options, bool autoStart) : this(options)
    {
        if (autoStart) Start();
    }

    internal void Start()
    {
        if (_timer != null || _disposed) return;

        try
        {
            _process = Process.GetCurrentProcess();
            _lastTotalProcessorTime = _process.TotalProcessorTime;
        }
        catch (Exception ex)
        {
            // Running in restricted environments (e.g. some sandboxes) can
            // throw on Process.GetCurrentProcess(). Without a process handle
            // the monitor still works for the GC-pressure branch — the CPU
            // branch will simply report 0.
            _logger?.LogWarning(ex, "AdaptiveLimitMonitor could not acquire process handle");
        }

        _timer = new Timer(_ => AdjustSafely(), null,
            TimeSpan.Zero, _options.AdaptiveAdjustmentInterval);
    }

    /// <summary>
    /// Stops the adaptive monitor and releases the timer + process handle.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        var timer = _timer;
        _timer = null;
        timer?.Dispose();

        var process = _process;
        _process = null;
        if (process != null)
        {
            try { process.Dispose(); }
            catch { /* best-effort cleanup */ }
        }
    }

    private void AdjustSafely()
    {
        // SHARK-SEC-M010: a timer callback that throws is promoted to the
        // process-wide unhandled exception handler and can tear the process
        // down under .NET 10. Wrap Adjust() so a transient failure is
        // logged and the timer keeps firing on the next interval.
        try
        {
            Adjust();
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "AdaptiveLimitMonitor adjustment failed");
        }
    }

    private void Adjust()
    {
        var now = DateTime.UtcNow;
        var elapsedCpu = _process != null
            ? _process.TotalProcessorTime - _lastTotalProcessorTime
            : TimeSpan.Zero;
        var elapsedTime = now - _lastSampleAt;

        if (_process != null)
            _lastTotalProcessorTime = _process.TotalProcessorTime;
        _lastSampleAt = now;

        if (elapsedTime.TotalMilliseconds <= 0)
            return;

        // CPU usage as a percentage of total elapsed wall-clock time
        // (process-level CPU across all cores)
        var cpuPercent = (elapsedCpu.TotalMilliseconds / (elapsedTime.TotalMilliseconds * Environment.ProcessorCount)) * 100;

        // GC pressure: ratio of current heap to high-load threshold
        var gcInfo = GC.GetGCMemoryInfo();
        long gcPressure = 0;
        if (gcInfo.HighMemoryLoadThresholdBytes > 0)
            gcPressure = (long)((double)gcInfo.HeapSizeBytes / gcInfo.HighMemoryLoadThresholdBytes * 100);

        var current = Volatile.Read(ref CurrentLimit);
        int newLimit;

        if (cpuPercent > _options.AdaptiveCpuHighThreshold || gcPressure > _options.AdaptiveGcHighThreshold)
        {
            // High load: gradually reduce permits
            var divisor = Math.Max(_options.AdaptiveReductionDivisor, 1);
            newLimit = Math.Max(_options.MinPermitLimit, current - (current / divisor));
        }
        else if (cpuPercent < _options.AdaptiveCpuLowThreshold && gcPressure < _options.AdaptiveGcLowThreshold)
        {
            // Low load: gradually increase permits toward base
            newLimit = Math.Min(_options.MaxPermitLimit, current + (current / 10 + 1));
        }
        else
        {
            // Moderate load: drift toward base
            if (current > _options.BasePermitLimit)
                newLimit = current - 1;
            else if (current < _options.BasePermitLimit)
                newLimit = current + 1;
            else
                newLimit = current;
        }

        Volatile.Write(ref CurrentLimit, newLimit);
    }
}