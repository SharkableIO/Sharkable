namespace Sharkable;

/// <summary>
/// Configuration options for the built-in distributed tracing middleware.
/// Configured via <c>SharkOption.ConfigureTracing(t => ...)</c>.
/// The middleware uses <see cref="System.Diagnostics.ActivitySource"/> and
/// is compatible with OpenTelemetry exporters (Jaeger, Zipkin, OTLP)
/// out of the box — no additional Sharkable packages required.
/// </summary>
public sealed class TracingOptions
{
    /// <summary>
    /// Service name reported in the <c>service.name</c> tag. Default is the
    /// entry assembly name. Also exposed as <see cref="Sharkable.Tracing.ActivitySourceName"/>.
    /// </summary>
    public string ServiceName { get; set; } = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "sharkable";

    /// <summary>
    /// Optional exporter for custom trace processing. Set to <c>null</c> to use
    /// the default no-op exporter (OpenTelemetry SDK hooks automatically via
    /// <see cref="System.Diagnostics.ActivitySource"/> when installed).
    /// </summary>
    public ITracingExporter? Exporter { get; set; }

    /// <summary>
    /// Optional callback for configuring OpenTelemetry SDK.
    /// The <see cref="TracerProviderBuilder"/> instance is provided by the
    /// <c>OpenTelemetry.Extensions.Hosting</c> NuGet package.
    /// Use this to add exporters like Jaeger, Zipkin, or OTLP.
    /// </summary>
    /// <example>
    /// <code>
    /// opt.ConfigureTracing(t =>
    /// {
    ///     t.ConfigureOpenTelemetry = builder =>
    ///     {
    ///         builder.AddJaegerExporter(o => o.AgentHost = "localhost");
    ///     };
    /// });
    /// </code>
    /// </example>
    public Action<object>? ConfigureOpenTelemetry { get; set; }
}

