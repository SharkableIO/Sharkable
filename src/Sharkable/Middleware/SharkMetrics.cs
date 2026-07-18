using System.Diagnostics.Metrics;

namespace Sharkable;

/// <summary>
/// Default implementation of <see cref="ISharkMetrics"/> backed by
/// <see cref="Meter"/>. Counters are created lazily on first access.
/// Inactive when <see cref="MetricsOptions.Enabled"/> is <c>false</c>.
/// </summary>
internal sealed class SharkMetrics : ISharkMetrics
{
    private readonly Meter _meter;
    private Counter<long>? _requests;
    private Counter<long>? _rateLimitRejected;
    private Counter<long>? _idempotencyHit;
    private Counter<long>? _idempotencyMiss;
    private Counter<long>? _idempotencyConflict;
    private Counter<long>? _authFailures;
    private Counter<long>? _auditDropped;
    private Counter<long>? _cronRuns;
    private Counter<long>? _cronFailures;
    private Counter<long>? _sagaCompleted;
    private Counter<long>? _sagaCompensated;

    public SharkMetrics(MetricsOptions options)
    {
        _meter = new Meter(options.MeterName, options.MeterVersion);
    }

    public Counter<long> Requests =>
        _requests ??= _meter.CreateCounter<long>(
            "sharkable.requests", description: "Total number of requests processed by the pipeline");

    public Counter<long> RateLimitRejected =>
        _rateLimitRejected ??= _meter.CreateCounter<long>(
            "sharkable.ratelimit.rejected", description: "Requests rejected by the distributed rate limiter");

    public Counter<long> IdempotencyHit =>
        _idempotencyHit ??= _meter.CreateCounter<long>(
            "sharkable.idempotency.hit", description: "Idempotency-key cache hits (replayed responses)");

    public Counter<long> IdempotencyMiss =>
        _idempotencyMiss ??= _meter.CreateCounter<long>(
            "sharkable.idempotency.miss", description: "Idempotency-key cache misses (new requests)");

    public Counter<long> IdempotencyConflict =>
        _idempotencyConflict ??= _meter.CreateCounter<long>(
            "sharkable.idempotency.conflict", description: "Idempotency-key conflicts (different payload, same key)");

    public Counter<long> AuthFailures =>
        _authFailures ??= _meter.CreateCounter<long>(
            "sharkable.auth.failures", description: "Authentication/authorization failures");

    public Counter<long> AuditDropped =>
        _auditDropped ??= _meter.CreateCounter<long>(
            "sharkable.audit.dropped", description: "Audit log entries dropped due to channel overflow");

    public Counter<long> CronRuns =>
        _cronRuns ??= _meter.CreateCounter<long>(
            "sharkable.cron.runs", description: "Cron job executions");

    public Counter<long> CronFailures =>
        _cronFailures ??= _meter.CreateCounter<long>(
            "sharkable.cron.failures", description: "Failed cron job executions");

    public Counter<long> SagaCompleted =>
        _sagaCompleted ??= _meter.CreateCounter<long>(
            "sharkable.saga.completed", description: "Successfully completed saga transactions");

    public Counter<long> SagaCompensated =>
        _sagaCompensated ??= _meter.CreateCounter<long>(
            "sharkable.saga.compensated", description: "Compensated (rolled back) saga transactions");

    public void Dispose()
    {
        _meter.Dispose();
    }
}
