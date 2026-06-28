using System.Diagnostics;

namespace Sharkable;

/// <summary>
/// Pluggable tracing exporter. Implement this to integrate with
/// OpenTelemetry exporters (Jaeger, Zipkin, OTLP, Prometheus, etc.).
/// Register via <see cref="TracingOptions.Exporter"/> or a NuGet plugin
/// like <c>Sharkable.OpenTelemetry</c>.
/// </summary>
public interface ITracingExporter
{
    /// <summary>
    /// Called when a request activity starts. The <paramref name="activity"/>
    /// is the W3C <c>traceparent</c>-compliant span created by the middleware.
    /// Use <see cref="ActivityListener"/> or OpenTelemetry SDK to export it.
    /// </summary>
    /// <param name="activity">The started <see cref="Activity"/> instance.</param>
    void OnActivityStarted(Activity activity);

    /// <summary>
    /// Called when a request activity stops. Use to flush or finalize export.
    /// </summary>
    /// <param name="activity">The stopped <see cref="Activity"/> instance.</param>
    void OnActivityStopped(Activity activity);
}

/// <summary>
/// Default no-op tracing exporter. OpenTelemetry SDK hooks into
/// <see cref="ActivitySource"/> automatically when installed.
/// </summary>
internal sealed class NoOpTracingExporter : ITracingExporter
{
    public void OnActivityStarted(Activity activity) { }
    public void OnActivityStopped(Activity activity) { }
}
