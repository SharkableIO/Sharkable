using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// Middleware that creates a W3C <c>traceparent</c>-compliant <see cref="Activity"/>
/// for every request, enabling distributed tracing with zero external dependencies.
/// OpenTelemetry exporters (Jaeger, Zipkin, etc.) hook into the <see cref="ActivitySource"/>
/// automatically when the OTel SDK is installed — no additional Sharkable packages required.
/// </summary>
internal sealed class TracingMiddleware
{
    internal const string ActivitySourceName = "Sharkable";

    private static readonly ActivitySource Source = new(
        Shark.SharkOption.TracingOptions?.ActivitySourceName ?? ActivitySourceName);

    private readonly RequestDelegate _next;
    private readonly ILogger<TracingMiddleware> _logger;
    private readonly ITracingExporter _exporter;

    public TracingMiddleware(RequestDelegate next, ILogger<TracingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _exporter = Shark.SharkOption.TracingOptions?.Exporter ?? new NoOpTracingExporter();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var activity = Source.StartActivity(
            $"{context.Request.Method} {context.Request.Path}",
            ActivityKind.Server);

        if (activity != null)
        {
            activity.SetTag("http.method", context.Request.Method);
            activity.SetTag("http.target", context.Request.Path.Value ?? "/");
            activity.SetTag("http.host", context.Request.Host.Value ?? string.Empty);
            activity.SetTag("http.client_ip", context.Connection.RemoteIpAddress?.ToString() ?? string.Empty);
            activity.SetTag("service.name", Shark.SharkOption.TracingOptions?.ServiceName ?? "sharkable");

            _exporter.OnActivityStarted(activity);

            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Trace-Id"] = activity.TraceId.ToString();
                return Task.CompletedTask;
            });
        }

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
            activity?.SetStatus(context.Response.StatusCode < 500
                ? ActivityStatusCode.Ok
                : ActivityStatusCode.Error);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.AddTag("exception.message", ex.Message);
            activity?.AddTag("exception.type", ex.GetType().FullName);
            throw;
        }
        finally
        {
            sw.Stop();
            activity?.SetTag("http.status_code", context.Response.StatusCode);
            activity?.SetTag("http.duration_ms", sw.ElapsedMilliseconds);

            if (activity != null)
                _exporter.OnActivityStopped(activity);
        }
    }
}
