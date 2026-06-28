namespace Sharkable;

internal static class SharkProfilerEndpoint
{
    internal static void MapProfilerEndpoint(this IEndpointRouteBuilder app)
    {
        var endpointPath = Shark.SharkOption.ProfilerOptions?.Endpoint ?? "/_sharkable/profiler";
        // Strip leading slash — MapGet pattern is relative to group
        var pattern = endpointPath.TrimStart('/');

        app.MapGet(pattern, () =>
        {
            var uptime = DateTimeOffset.UtcNow - ProfilerStore.StartedAt;
            var avgMs = ProfilerStore.RequestCount > 0
                ? ProfilerStore.TotalElapsedMs / (double)ProfilerStore.RequestCount
                : 0;
            var top = Shark.SharkOption.ProfilerOptions?.TopSlowRequests ?? 20;
            var slow = ProfilerStore.SnapTopSlow(top);

            return Results.Ok(new
            {
                uptime = $"{(int)uptime.TotalHours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}",
                totalRequests = ProfilerStore.RequestCount,
                avgLatencyMs = Math.Round(avgMs, 1),
                topSlow = slow.Select(e => new
                {
                    method = e.Method,
                    path = e.Path,
                    statusCode = e.StatusCode,
                    elapsedMs = e.ElapsedMs,
                    memoryDeltaBytes = e.MemoryDelta,
                    at = e.Timestamp.ToString("O"),
                }),
            });
        }).ExcludeFromDescription();
    }
}
