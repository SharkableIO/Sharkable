namespace Sharkable;

/// <summary>
/// Configuration options for the built-in profiler endpoint.
/// Configured via <c>SharkOption.ConfigureProfiler(p => ...)</c>.
/// </summary>
public sealed class ProfilerOptions
{
    /// <summary>
    /// The endpoint path for the profiler panel.
    /// Default is <c>/_sharkable/profiler</c>.
    /// </summary>
    public string Endpoint { get; set; } = "/_sharkable/profiler";

    /// <summary>
    /// Maximum number of slow requests to track.
    /// Default is 20.
    /// </summary>
    public int TopSlowRequests { get; set; } = 20;

    /// <summary>
    /// Maximum number of profiler entries kept in the ring buffer.
    /// Default is 1000.
    /// </summary>
    public int MaxEntries { get; set; } = 1000;

    /// <summary>
    /// When <c>true</c>, the profiler samples <c>GC.GetTotalMemory(false)</c>
    /// before and after each request. Disabled by default (adds GC bookkeeping
    /// overhead under high RPS). Default is <c>false</c>.
    /// </summary>
    public bool TrackMemory { get; set; } = false;
}
