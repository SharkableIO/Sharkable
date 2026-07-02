using System.Security.Cryptography;
using System.Text;

namespace Sharkable;

internal static class SharkProfilerEndpoint
{
    /// <summary>
    /// Upper bound on the <c>top-N</c> slow requests surfaceable from the
    /// profiler endpoint. Caps accidental or hostile large <c>top</c> values.
    /// </summary>
    private const int MaxTopAllowed = 50;

    internal static void MapProfilerEndpoint(this IEndpointRouteBuilder app)
    {
        var endpointPath = Shark.SharkOption.ProfilerOptions?.Endpoint ?? "/_sharkable/profiler";
        // Strip leading slash — MapGet pattern is relative to group
        var pattern = endpointPath.TrimStart('/');

        app.MapGet(pattern, (HttpContext context) =>
        {
            // SHARK-SEC-015: gate on API key by default. Fail-closed — if no
            // keys are configured, the endpoint reports 404 so its existence
            // is not leaked to unauthenticated probes.
            if (Shark.SharkOption.ProfilerRequireApiKey && !IsApiKeyAuthorized(context))
                return Results.NotFound();

            var uptime = DateTimeOffset.UtcNow - ProfilerStore.StartedAt;
            var avgMs = ProfilerStore.RequestCount > 0
                ? ProfilerStore.TotalElapsedMs / (double)ProfilerStore.RequestCount
                : 0;
            var configuredTop = Shark.SharkOption.ProfilerOptions?.TopSlowRequests ?? 20;
            var top = Math.Clamp(configuredTop, 1, MaxTopAllowed);
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

    /// <summary>
    /// Returns <c>true</c> when the request carries a configured API key.
    /// Mirrors the constant-time SHA-256 comparison used by
    /// <see cref="ApiKeyFilter"/> (SHARK-SEC-008). Returns <c>false</c> — and the
    /// caller maps that to <c>404</c> — when no keys are configured at all so
    /// the endpoint's existence is not advertised.
    /// </summary>
    private static bool IsApiKeyAuthorized(HttpContext context)
    {
        var keys = Shark.SharkOption.ApiKeys;
        if (keys == null || keys.Length == 0)
            return false;

        if (!context.Request.Headers.TryGetValue(Shark.SharkOption.ApiKeyHeaderName, out var provided))
            return false;

        var candidateHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided.ToString()));
        var matched = false;
        for (var i = 0; i < keys.Length; i++)
        {
            var stored = SHA256.HashData(Encoding.UTF8.GetBytes(keys[i]));
            if (CryptographicOperations.FixedTimeEquals(candidateHash, stored))
                matched = true;
        }
        return matched;
    }
}
