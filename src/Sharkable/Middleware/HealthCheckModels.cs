namespace Sharkable;

/// <summary>A single health check entry with its status, optional description, data, and exception.</summary>
public sealed record HealthCheckEntry(string Status, string? Description, object? Data, string? Exception);

/// <summary>Aggregate health check response returned by the /healthz endpoint.</summary>
public sealed record HealthCheckResponse(string Status, Dictionary<string, HealthCheckEntry> Checks, string Uptime, string Version);
