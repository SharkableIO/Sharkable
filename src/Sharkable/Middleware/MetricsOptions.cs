namespace Sharkable;

/// <summary>
/// Configuration options for Sharkable framework metrics.
/// Powered by <see cref="System.Diagnostics.Metrics.Meter"/> — zero dependencies,
/// AOT-compatible, and auto-flows into OpenTelemetry when exporters are configured.
/// </summary>
public sealed class MetricsOptions
{
    /// <summary>
    /// Enable framework metrics. Default is <c>false</c>.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Name of the <see cref="System.Diagnostics.Metrics.Meter"/>.
    /// Default is <c>"Sharkable"</c>.
    /// </summary>
    public string MeterName { get; set; } = "Sharkable";

    /// <summary>
    /// Version of the meter. Default is <c>"1.0"</c>.
    /// </summary>
    public string MeterVersion { get; set; } = "1.0";
}
