namespace Sharkable;

/// <summary>
/// A step in a distributed saga. Each step has an <see cref="ExecuteAsync"/>
/// (forward action) and a <see cref="CompensateAsync"/> (rollback).
/// Steps are executed in order; on failure, completed steps are compensated
/// in reverse order.
/// </summary>
public interface ISagaStep
{
    /// <summary>
    /// The forward action. Throw or return a failed <see cref="SagaResult"/>
    /// to trigger compensation of previously-completed steps.
    /// </summary>
    Task<SagaResult> ExecuteAsync(CancellationToken ct = default);

    /// <summary>
    /// The compensating (rollback) action. Called only for steps that
    /// previously completed successfully, in reverse order.
    /// </summary>
    Task CompensateAsync(CancellationToken ct = default);
}

/// <summary>
/// Shared state passed between saga steps. Steps read/write data
/// via the <see cref="Data"/> dictionary.
/// </summary>
public sealed class SagaState
{
    /// <summary>
    /// Typed key-value store shared across all steps in a saga.
    /// </summary>
    public Dictionary<string, object?> Data { get; } = [];
}

/// <summary>
/// Result of a saga step execution.
/// </summary>
/// <param name="Success">Whether the step completed successfully.</param>
/// <param name="Error">Error message if <paramref name="Success"/> is <c>false</c>.</param>
public sealed record SagaStepResult(bool Success, string? Error = null);

/// <summary>
/// Final result of a saga execution.
/// </summary>
/// <param name="Success">Whether all steps completed successfully.</param>
/// <param name="Error">Error message from the failing step, or <c>null</c>.</param>
/// <param name="FailedStepIndex">Index of the step that failed, or -1 on success.</param>
public sealed record SagaResult(bool Success, string? Error = null, int FailedStepIndex = -1);

/// <summary>
/// Pluggable store for saga progress and distributed locking.
/// Default <see cref="MemorySagaStore"/> keeps state in-process;
/// use <see cref="SagaStoreFactory"/> to plug in Redis or database.
/// </summary>
public interface ISagaStore
{
    /// <summary>
    /// Acquires a distributed lock for the given saga ID.
    /// Returns <c>true</c> if the lock was acquired; <c>false</c> if
    /// another instance is already processing this saga.
    /// </summary>
    Task<bool> TryAcquireLockAsync(string sagaId, TimeSpan ttl);

    /// <summary>
    /// Extends the TTL of an already-held distributed lock. Called periodically
    /// by <see cref="SagaExecutor"/> while saga steps are in progress so that
    /// long-running steps do not cause the lock to expire and split-brain
    /// execution.
    /// </summary>
    /// <remarks>
    /// Default implementation is a no-op (<see cref="Task.CompletedTask"/>),
    /// suitable for in-process stores where locks survive the process lifetime
    /// and TTL renewal is unnecessary. Distributed stores (Redis, SQL, etc.)
    /// should override this method to extend the underlying lock's TTL.
    /// </remarks>
    /// <param name="sagaId">The saga whose lock should be renewed.</param>
    /// <param name="ttl">The new TTL to apply. Typically equal to the original
    /// <c>LockTtl</c> so the renewal cadence is consistent.</param>
    /// <returns>A task that completes when the renewal attempt is finished.</returns>
    public Task RenewLockAsync(string sagaId, TimeSpan ttl) => Task.CompletedTask;

    /// <summary>
    /// Releases the distributed lock for the given saga ID.
    /// </summary>
    Task ReleaseLockAsync(string sagaId);

    /// <summary>
    /// Saves the current step index (0-based) for crash-recovery.
    /// </summary>
    Task SaveProgressAsync(string sagaId, int stepIndex, CancellationToken ct);

    /// <summary>
    /// Loads the last completed step index, or -1 if no progress recorded.
    /// </summary>
    Task<int> LoadProgressAsync(string sagaId, CancellationToken ct);

    /// <summary>
    /// Deletes saga state after successful completion.
    /// </summary>
    Task DeleteAsync(string sagaId, CancellationToken ct);
}
