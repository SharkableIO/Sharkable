namespace Sharkable;

/// <summary>
/// Controls the amount of diagnostic detail exposed by the <c>/healthz</c> endpoint.
/// </summary>
public enum HealthCheckDetailLevel
{
    /// <summary>Only the status (Healthy/Degraded/Unhealthy) — no descriptions, data, or exception messages.</summary>
    StatusOnly,
    /// <summary>Status + check description text.</summary>
    Description,
    /// <summary>Status + description + per-check data and exception messages.</summary>
    Full
}
