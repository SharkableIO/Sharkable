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
}
