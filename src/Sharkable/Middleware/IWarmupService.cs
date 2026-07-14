namespace Sharkable;

/// <summary>
/// Defines a warmup task that runs synchronously during application startup,
/// before the server begins accepting requests and before the readiness gate
/// is opened.
/// </summary>
public interface IWarmupService
{
    /// <summary>
    /// Called once during startup. Throw to fail startup.
    /// </summary>
    Task WarmupAsync(CancellationToken cancellationToken);
}
