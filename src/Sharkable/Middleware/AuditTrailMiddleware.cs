using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Sharkable;

internal sealed class AuditTrailMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditTrailMiddleware> _logger;
    private readonly AuditTrailOptions _options;

    public AuditTrailMiddleware(RequestDelegate next, ILogger<AuditTrailMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _options = Shark.SharkOption.AuditTrailOptions!;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (ShouldSkip(path))
        {
            await _next(context);
            return;
        }

        var correlationId = ResolveCorrelationId(context);
        SetResponseCorrelationId(context, correlationId);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            LogRequest(context, path, correlationId, stopwatch.ElapsedMilliseconds);
        }
    }

    private bool ShouldSkip(string path)
    {
        if (_options.ExcludePaths.Length == 0)
            return false;

        return _options.ExcludePaths.Any(exclude =>
            !string.IsNullOrEmpty(exclude) &&
            (path.Equals(exclude, StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith(exclude.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase)));
    }

    private string ResolveCorrelationId(HttpContext context)
    {
        if (_options.ForwardCorrelationId &&
            context.Request.Headers.TryGetValue(_options.CorrelationIdHeader, out var existing))
        {
            return existing.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }

    private static void SetResponseCorrelationId(HttpContext context, string correlationId)
    {
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("X-Correlation-Id"))
                context.Response.Headers["X-Correlation-Id"] = correlationId;
            return Task.CompletedTask;
        });
    }

    private void LogRequest(HttpContext context, string path, string correlationId, long elapsedMs)
    {
        var statusCode = context.Response.StatusCode;
        var logLevel = statusCode >= 500 ? _options.ErrorLogLevel
                     : statusCode >= 400 ? _options.WarningLogLevel
                     : _options.SuccessLogLevel;

        if (!_logger.IsEnabled(logLevel))
            return;

        var query = _options.IncludeQueryString
            ? RedactQueryString(context.Request.QueryString.Value)
            : null;

        var headerLog = _options.RedactHeaders.Length > 0
            ? BuildHeaderLog(context)
            : null;

        _logger.Log(logLevel,
            "HTTP {Method} {Path}{Query} responded {StatusCode} in {ElapsedMs}ms [CorrelationId: {CorrelationId}]",
            context.Request.Method,
            path,
            query,
            statusCode,
            elapsedMs,
            correlationId);
    }

    private string? BuildHeaderLog(HttpContext context)
    {
        var sb = new StringBuilder();
        var redacted = new HashSet<string>(_options.RedactHeaders, StringComparer.OrdinalIgnoreCase);

        foreach (var header in context.Request.Headers)
        {
            if (redacted.Contains(header.Key))
                sb.Append($"{header.Key}=[REDACTED] ");
            else
                sb.Append($"{header.Key}={header.Value} ");
        }

        return sb.Length > 0 ? sb.ToString(0, sb.Length - 1) : null;
    }

    private string? RedactQueryString(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString) || _options.RedactQueryParams.Length == 0)
            return queryString;

        var redacted = new HashSet<string>(_options.RedactQueryParams, StringComparer.OrdinalIgnoreCase);
        var query = System.Web.HttpUtility.ParseQueryString(queryString);
        var modified = false;

        foreach (var key in query.AllKeys)
        {
            if (key != null && redacted.Contains(key))
            {
                query[key] = "***";
                modified = true;
            }
        }

        return modified ? $"?{query}" : queryString;
    }
}
