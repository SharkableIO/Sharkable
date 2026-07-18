namespace Sharkable;

/// <summary>
/// Orchestrates saga execution: acquires a distributed lock, loads
/// crash-recovery progress, executes steps, compensates on failure,
/// and persists progress for durability.
/// Implement to provide a custom executor; register via
/// <see cref="SharkOption.SagaExecutorFactory"/>.
/// </summary>
public interface ISagaExecutor
{
    /// <summary>
    /// Distributed lock TTL applied when the saga lock is acquired and on each renewal.
    /// </summary>
    TimeSpan LockTtl { get; set; }

    /// <summary>
    /// Timeout for compensation steps.
    /// </summary>
    TimeSpan CompensationTimeout { get; set; }

    /// <summary>
    /// Interval between automatic lock TTL renewals while a saga is in progress.
    /// </summary>
    TimeSpan LockRenewalInterval { get; set; }

    /// <summary>
    /// Executes a saga. Steps run in order. If a step fails, previously
    /// completed steps are compensated in reverse order.
    /// </summary>
    /// <param name="sagaId">Unique identifier for this saga instance.</param>
    /// <param name="saga">The saga definition with ordered steps.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="SagaResult"/> indicating success or failure.</returns>
    Task<SagaResult> ExecuteAsync(string sagaId, Saga saga, CancellationToken ct = default);
}
