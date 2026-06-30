using System.Collections.Concurrent;
using System.Diagnostics;

namespace Sharkable;

/// <summary>
/// Lightweight middleware that records per-request latency and memory delta
/// for the profiler panel. Data is exposed via <see cref="SharkProfilerEndpoint"/>.
/// </summary>
internal sealed class ProfilerMiddleware
{
    private readonly RequestDelegate _next;

    public ProfilerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        var startMem = GC.GetTotalMemory(false);

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var endMem = GC.GetTotalMemory(false);
            var entry = new ProfilerEntry(
                context.Request.Method,
                context.Request.Path.Value ?? "/",
                context.Response.StatusCode,
                sw.ElapsedMilliseconds,
                endMem > startMem ? endMem - startMem : 0,
                DateTimeOffset.UtcNow);

            ProfilerStore.Record(entry);
        }
    }
}

/// <summary>
/// Lightweight request profiling data for the slow-request leaderboard.
/// </summary>
/// <param name="Method">HTTP method.</param>
/// <param name="Path">Request path.</param>
/// <param name="StatusCode">Response status code.</param>
/// <param name="ElapsedMs">Request duration in milliseconds.</param>
/// <param name="MemoryDelta">Approximate memory change (bytes).</param>
/// <param name="Timestamp">When the request completed.</param>
internal sealed record ProfilerEntry(
    string Method,
    string Path,
    int StatusCode,
    long ElapsedMs,
    long MemoryDelta,
    DateTimeOffset Timestamp);

/// <summary>
/// Concurrent ring buffer for profiler entries. Thread-safe, lock-free reads.
/// </summary>
internal static class ProfilerStore
{
    private static readonly ConcurrentQueue<ProfilerEntry> Entries = new();
    private static volatile int _count;

    internal static int RequestCount;
    internal static long TotalElapsedMs;
    internal static readonly DateTimeOffset StartedAt = DateTimeOffset.UtcNow;

    internal static void Record(ProfilerEntry entry)
    {
        Interlocked.Increment(ref RequestCount);
        Interlocked.Add(ref TotalElapsedMs, entry.ElapsedMs);
        Entries.Enqueue(entry);
        Interlocked.Increment(ref _count);

        var maxEntries = Shark.SharkOption.ProfilerOptions?.MaxEntries ?? 1000;
        while (_count > maxEntries)
        {
            if (Entries.TryDequeue(out _))
                Interlocked.Decrement(ref _count);
            else
                break;
        }
    }

    internal static IReadOnlyList<ProfilerEntry> SnapTopSlow(int top)
    {
        return Entries.OrderByDescending(e => e.ElapsedMs).Take(top).ToList();
    }
}
