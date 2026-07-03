using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sharkable;

internal static class HealthCheckEndpoint
{
    /// <summary>
    /// SHARK-SEC-M015: hard timeout for the aggregate <see cref="HealthCheckService"/>
    /// call. A single hung check (DB driver, downstream HTTP call) would otherwise
    /// keep /healthz open indefinitely and fail k8s probes. 10s matches the
    /// typical Kubernetes probe timeoutSeconds default; raise in
    /// <c>HealthCheckTimeoutSeconds</c> for deployments with deliberately slow
    /// checks (e.g. cross-region DB failover probes).
    /// </summary>
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(10);

    internal static void Map(WebApplication app)
    {
        var path = Shark.SharkOption.HealthCheckPath ?? "/healthz";
        app.MapGet(path, async (HealthCheckService healthCheck, CancellationToken cancellationToken) =>
        {
            if (Volatile.Read(ref InternalShark.IsShuttingDown))
                return Results.Json(new HealthCheckResponse(
                    "unhealthy",
                    new Dictionary<string, HealthCheckEntry>
                    {
                        ["shutdown"] = new("unhealthy", "Server is shutting down", null, null)
                    },
                    GetUptime(),
                    InternalShark.AppVersion ?? "0.0.0"
                ), statusCode: 503);

            // SHARK-SEC-M015: bound the aggregate health-check call with a
            // linked CTS so a stuck individual check cannot keep /healthz
            // open beyond the configured timeout. The CheckHealthAsync
            // overload that accepts a CancellationToken forwards cancellation
            // to each registered IHealthCheck.CheckHealthAsync.
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(HealthCheckTimeout);

            HealthReport report;
            try
            {
                report = await healthCheck.CheckHealthAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested
                                                       && !cancellationToken.IsCancellationRequested)
            {
                // Per-check timeout fired. Return a synthetic unhealthy report
                // so the probe can fail fast instead of hanging.
                return Results.Json(new HealthCheckResponse(
                    "unhealthy",
                    new Dictionary<string, HealthCheckEntry>
                    {
                        ["timeout"] = new("unhealthy",
                            $"Health check exceeded {HealthCheckTimeout.TotalSeconds:F0}s timeout",
                            null, null)
                    },
                    GetUptime(),
                    InternalShark.AppVersion ?? "0.0.0"
                ), statusCode: 503);
            }

            var checks = report.Entries.ToDictionary(
                e => e.Key,
                e => new HealthCheckEntry(
                    e.Value.Status.ToString().ToLower(),
                    e.Value.Description,
                    e.Value.Data.Count > 0 ? e.Value.Data : null,
                    e.Value.Exception?.Message
                ));

            var overall = report.Status switch
            {
                HealthStatus.Healthy => "healthy",
                HealthStatus.Degraded => "degraded",
                _ => "unhealthy"
            };

            var statusCode = report.Status == HealthStatus.Healthy ? 200
                : report.Status == HealthStatus.Degraded ? 200 : 503;

            return Results.Json(new HealthCheckResponse(
                overall, checks, GetUptime(), InternalShark.AppVersion ?? "0.0.0"
            ), statusCode: statusCode);
        }).ExcludeFromDescription();
    }

    private static string GetUptime()
    {
        if (InternalShark.StartedAt == default)
            return "00:00:00";
        var span = (DateTimeOffset.UtcNow - InternalShark.StartedAt);
        return $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
    }
}
