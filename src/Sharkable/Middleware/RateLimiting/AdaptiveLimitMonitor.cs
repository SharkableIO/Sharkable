using System.Diagnostics;

namespace Sharkable;

/// <summary>
/// Periodically monitors CPU usage and GC pressure, then adjusts the
/// effective rate limit up or down. Managed as an internal singleton
/// started by the middleware when adaptive mode is enabled.
/// </summary>
internal sealed class AdaptiveLimitMonitor
{
    private readonly SharkRateLimiterOptions _options;
    private readonly Process _process;
    private TimeSpan _lastTotalProcessorTime;
    private DateTime _lastSampleAt;
    private Timer? _timer;

    /// <summary>
    /// The current dynamically-adjusted permit limit. Read by the middleware
    /// on each request.
    /// </summary>
    internal int CurrentLimit;

    internal AdaptiveLimitMonitor(SharkRateLimiterOptions options)
    {
        _options = options;
        _process = Process.GetCurrentProcess();
        _lastTotalProcessorTime = _process.TotalProcessorTime;
        _lastSampleAt = DateTime.UtcNow;
        CurrentLimit = options.BasePermitLimit;
    }

    internal void Start()
    {
        if (_timer != null) return;
        _timer = new Timer(_ => Adjust(), null,
            TimeSpan.Zero, _options.AdaptiveAdjustmentInterval);
    }

    internal void Stop() => _timer?.Dispose();

    private void Adjust()
    {
        var now = DateTime.UtcNow;
        var elapsedCpu = _process.TotalProcessorTime - _lastTotalProcessorTime;
        var elapsedTime = now - _lastSampleAt;

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
