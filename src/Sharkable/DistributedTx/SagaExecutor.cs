using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// Orchestrates saga execution: acquires a distributed lock, loads
/// crash-recovery progress, executes steps, compensates on failure,
/// and persists progress for durability.
/// Use via DI: inject <c>SagaExecutor</c> and call <c>ExecuteAsync</c>.
/// </summary>
public sealed class SagaExecutor
{
    private readonly ISagaStore _store;
    private readonly ILogger<SagaExecutor> _logger;

    /// <summary>
    /// Default lock TTL for saga execution.
    /// </summary>
    public TimeSpan LockTtl { get; set; } = TimeSpan.FromMinutes(5);

    public SagaExecutor(ISagaStore store, ILogger<SagaExecutor> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Executes a saga. Steps run in order. If a step fails, previously
    /// completed steps are compensated in reverse order.
    /// </summary>
    /// <param name="sagaId">Unique identifier for this saga instance.</param>
    /// <param name="saga">The saga definition with ordered steps.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see cref="SagaResult"/> indicating success or failure.</returns>
    public async Task<SagaResult> ExecuteAsync(string sagaId, Saga saga, CancellationToken ct = default)
    {
        if (!await _store.TryAcquireLockAsync(sagaId, LockTtl))
        {
            _logger.LogWarning("Saga {SagaId} is already executing", sagaId);
            return new SagaResult(false, "Saga is already executing");
        }

        try
        {
            var resumeFrom = await _store.LoadProgressAsync(sagaId, ct);
            return await ExecuteStepsAsync(sagaId, saga, resumeFrom, ct);
        }
        finally
        {
            await _store.ReleaseLockAsync(sagaId);
        }
    }

    private async Task<SagaResult> ExecuteStepsAsync(
        string sagaId, Saga saga, int resumeFrom, CancellationToken ct)
    {
        var steps = saga.Steps;
        var completedCount = resumeFrom + 1; // steps 0..resumeFrom are done

        for (var i = resumeFrom < 0 ? 0 : resumeFrom + 1; i < steps.Count; i++)
        {
            if (ct.IsCancellationRequested)
                return await CompensateAsync(sagaId, saga, completedCount, ct, "Cancelled");

            _logger.LogInformation("Saga {SagaId} step {Index}/{Total}", sagaId, i + 1, steps.Count);

            SagaStepResult stepResult;
            try
            {
                var result = await steps[i].ExecuteAsync(ct);
                stepResult = result.Success
                    ? new SagaStepResult(true)
                    : new SagaStepResult(false, result.Error);
            }
            catch (Exception ex)
            {
                stepResult = new SagaStepResult(false, ex.Message);
            }

            if (!stepResult.Success)
            {
                _logger.LogError("Saga {SagaId} step {Index} failed: {Error}", sagaId, i, stepResult.Error);
                return await CompensateAsync(sagaId, saga, completedCount, ct, stepResult.Error);
            }

            await _store.SaveProgressAsync(sagaId, i, ct);
            completedCount++;
        }

        await _store.DeleteAsync(sagaId, ct);
        _logger.LogInformation("Saga {SagaId} completed successfully", sagaId);
        return new SagaResult(true);
    }

    private async Task<SagaResult> CompensateAsync(
        string sagaId, Saga saga, int completedCount, CancellationToken ct, string error)
    {
        for (var i = completedCount - 1; i >= 0; i--)
        {
            _logger.LogWarning("Saga {SagaId} compensating step {Index}", sagaId, i + 1);
            try
            {
                await saga.Steps[i].CompensateAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saga {SagaId} compensation for step {Index} failed", sagaId, i);
            }
        }

        await _store.DeleteAsync(sagaId, ct);
        return new SagaResult(false, error, completedCount);
    }
}
