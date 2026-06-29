namespace Sharkable;

/// <summary>
/// Base class for defining a saga (set of distributed transaction steps).
/// Inherit, add steps via <see cref="AddStep"/>, and execute with
/// <see cref="SagaExecutor"/>.
/// </summary>
public abstract class Saga
{
    private readonly List<ISagaStep> _steps = [];

    /// <summary>
    /// The ordered list of steps in this saga.
    /// </summary>
    public IReadOnlyList<ISagaStep> Steps => _steps;

    /// <summary>
    /// Shared state accessible by all steps.
    /// </summary>
    public SagaState State { get; } = new();

    /// <summary>
    /// Adds a step to the saga. Steps are executed in the order they are added.
    /// </summary>
    protected void AddStep(ISagaStep step) => _steps.Add(step);
}
