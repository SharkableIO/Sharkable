using System.Diagnostics.Metrics;

namespace Sharkable;

/// <summary>
/// Exposes Sharkable framework instrumentation counters.
/// Implement to customize metric names, add tags, or push to external
/// systems. Register via <see cref="SharkOption.MetricsFactory"/>.
/// Default implementation uses a <see cref="Meter"/> named <c>"Sharkable"</c>
/// (configurable via <see cref="MetricsOptions.MeterName"/>).
/// </summary>
public interface ISharkMetrics : IDisposable
{
    /// <summary>
    /// Total number of requests processed by the pipeline.
    /// </summary>
    Counter<long> Requests { get; }

    /// <summary>
    /// Requests rejected by the distributed rate limiter.
    /// </summary>
    Counter<long> RateLimitRejected { get; }

    /// <summary>
    /// Idempotency-key cache hits (replayed responses).
    /// </summary>
    Counter<long> IdempotencyHit { get; }

    /// <summary>
    /// Idempotency-key cache misses (new requests).
    /// </summary>
    Counter<long> IdempotencyMiss { get; }

    /// <summary>
    /// Idempotency-key conflicts (different payload, same key).
    /// </summary>
    Counter<long> IdempotencyConflict { get; }

    /// <summary>
    /// Authentication/authorization failures.
    /// </summary>
    Counter<long> AuthFailures { get; }

    /// <summary>
    /// Audit log entries dropped due to channel overflow.
    /// </summary>
    Counter<long> AuditDropped { get; }

    /// <summary>
    /// Cron job executions.
    /// </summary>
    Counter<long> CronRuns { get; }

    /// <summary>
    /// Failed cron job executions.
    /// </summary>
    Counter<long> CronFailures { get; }

    /// <summary>
    /// Successfully completed saga transactions.
    /// </summary>
    Counter<long> SagaCompleted { get; }

    /// <summary>
    /// Compensated (rolled back) saga transactions.
    /// </summary>
    Counter<long> SagaCompensated { get; }
}
