using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sharkable;

internal static class HealthCheckEndpoint
{
    internal static void Map(WebApplication app)
    {
        app.MapGet("/healthz", async (HealthCheckService healthCheck) =>
        {
            if (Volatile.Read(ref InternalShark.IsShuttingDown))
                return Results.Json(new
                {
                    status = "unhealthy",
                    checks = new Dictionary<string, object>
                    {
                        ["shutdown"] = new { status = "unhealthy", message = "Server is shutting down" }
                    },
                    uptime = GetUptime(),
                    version = InternalShark.AppVersion ?? "0.0.0",
                }, statusCode: 503);

            var report = await healthCheck.CheckHealthAsync();

            var checks = report.Entries.ToDictionary(
                e => e.Key,
                e => (object)new
                {
                    status = e.Value.Status.ToString().ToLower(),
                    description = e.Value.Description,
                    data = e.Value.Data.Count > 0 ? e.Value.Data : null,
                    exception = e.Value.Exception?.Message,
                });

            var overall = report.Status switch
            {
                HealthStatus.Healthy => "healthy",
                HealthStatus.Degraded => "degraded",
                _ => "unhealthy"
            };

            var statusCode = report.Status == HealthStatus.Healthy ? 200
                : report.Status == HealthStatus.Degraded ? 200 : 503;

            return Results.Json(new
            {
                status = overall,
                checks,
                uptime = GetUptime(),
                version = InternalShark.AppVersion ?? "0.0.0",
            }, statusCode: statusCode);
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
