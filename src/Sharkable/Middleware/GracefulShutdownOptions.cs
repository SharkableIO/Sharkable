namespace Sharkable;

/// <summary>
/// Options for graceful shutdown behavior.
/// Configured via <c>opt.ConfigureGracefulShutdown()</c> in <c>AddShark()</c>.
/// </summary>
public sealed class GracefulShutdownOptions
{
    /// <summary>
    /// Maximum time to wait for in-flight requests to complete before forceful shutdown.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan DrainTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Grace period after marking health as unhealthy before shutdown begins.
    /// Gives load balancers time to detect the 503 and stop routing new traffic.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan HealthCheckGracePeriod { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// HTTP status code returned when the server is shutting down.
    /// Default is 503.
    /// </summary>
    public int ShutdownStatusCode { get; set; } = 503;

    /// <summary>
    /// Polling interval for draining in-flight requests during shutdown.
    /// Default is 100 milliseconds.
    /// </summary>
    public TimeSpan DrainPollingInterval { get; set; } = TimeSpan.FromMilliseconds(100);
}
