using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Sharkable;

internal sealed class AuditTrailMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditTrailMiddleware> _logger;
    private readonly AuditTrailOptions _options;
    private readonly AuditLogBuffer? _buffer;
    private readonly HashSet<string> _redactHeaders;

    public AuditTrailMiddleware(RequestDelegate next, ILogger<AuditTrailMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _options = Shark.SharkOption.AuditTrailOptions!;
        _redactHeaders = new HashSet<string>(_options.RedactHeaders, StringComparer.OrdinalIgnoreCase);
        _buffer = _options.AsyncWrite
            ? new AuditLogBuffer(_options, _logger)
            : null;
        if (_buffer != null)
            InternalShark.AuditLogBuffer = _buffer;
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
            var statusCode = context.Response.StatusCode;
            var query = _options.IncludeQueryString
                ? RedactQueryString(context.Request.QueryString.Value)
                : null;
            var headers = CaptureHeaders(context.Request.Headers);

            if (_buffer != null)
            {
                _buffer.Write(new AuditLogEntry(
                    context.Request.Method,
                    path,
                    query,
                    headers,
                    statusCode,
                    stopwatch.ElapsedMilliseconds,
                    correlationId));
            }
            else
            {
                LogRequest(context.Request.Method, path, query, headers, statusCode, stopwatch.ElapsedMilliseconds, correlationId);
            }
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
            // SHARK-SEC-M020: validate the inbound value against a strict
            // safe-character pattern (alphanumerics + dot/underscore/dash,
            // 1-128 chars). Anything else — including CR/LF, log format
            // placeholders, or anything that could smuggle a forged log
            // line — is rejected and replaced with a fresh UUID. The
            // IETF draft "The Trace Context" recommends similar bounds.
            var candidate = existing.ToString();
            if (IsSafeCorrelationId(candidate))
                return candidate;
        }

        return Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="value"/> consists only of
    /// <c>[A-Za-z0-9._-]</c> characters and is between 1 and 128 characters
    /// long. Anything else (whitespace, control characters, log-format
    /// placeholders, etc.) is rejected to prevent log injection.
    /// </summary>
    private static bool IsSafeCorrelationId(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 128) return false;
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            var safe =
                (c >= '0' && c <= '9') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                c == '.' || c == '_' || c == '-';
            if (!safe) return false;
        }
        return true;
    }

    private void SetResponseCorrelationId(HttpContext context, string correlationId)
    {
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(_options.CorrelationIdHeader))
                context.Response.Headers[_options.CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });
    }

    private string CaptureHeaders(IHeaderDictionary headers)
    {
        // SHARK-SEC-010 + SHARK-SEC-010 follow-up: hand-rolled JSON formatter
        // replaces JsonSerializer.Serialize<Dictionary<string,string>> so the
        // path emits no IL2026 / IL3050 (reflection-based serialization is not
        // AOT-trim-safe). Header names configured in RedactHeaders have their
        // value replaced with "***" before being written to the audit log.
        if (headers.Count == 0)
            return "{}";

        var sb = new StringBuilder();
        sb.Append('{');
        var first = true;
        foreach (var entry in headers)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append('"').Append(entry.Key.Replace("\"", "\\\"")).Append("\":\"");
            var value = _redactHeaders.Contains(entry.Key)
                ? "***"
                : entry.Value.ToString().Replace("\\", "\\\\").Replace("\"", "\\\"");
            sb.Append(value).Append('"');
        }
        sb.Append('}');
        return sb.ToString();
    }

    private void LogRequest(string method, string path, string? query, string headers, int statusCode, long elapsedMs, string correlationId)
    {
        var logLevel = statusCode >= 500 ? _options.ErrorLogLevel
                     : statusCode >= 400 ? _options.WarningLogLevel
                     : _options.SuccessLogLevel;

        if (!_logger.IsEnabled(logLevel))
            return;

        _logger.Log(logLevel,
            "HTTP {Method} {Path}{Query} responded {StatusCode} in {ElapsedMs}ms [CorrelationId: {CorrelationId}] Headers={Headers}",
            method,
            path,
            query,
            statusCode,
            elapsedMs,
            correlationId,
            headers);
    }

    /// <summary>
    /// SHARK-SEC-M009: cap the query-string length before redacting so a
    /// multi-MB query (e.g. <c>?x=&lt;10MB-of-data&gt;</c>) cannot bloat
    /// every log line. Values past the cap are truncated to
    /// <c>MaxQueryStringLogLength</c> with an ellipsis marker so reviewers
    /// can still see the prefix + which params were redacted.
    /// </summary>
    internal const int MaxQueryStringLogLength = 4_096;

    private string? RedactQueryString(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString))
            return TruncateQueryString(queryString);

        // Cap first so the per-key redaction pass can't be tricked into
        // allocating large intermediate strings.
        var capped = TruncateQueryString(queryString);

        if (_options.RedactQueryParams.Length == 0)
            return capped;

        var redacted = new HashSet<string>(_options.RedactQueryParams, StringComparer.OrdinalIgnoreCase);
        var query = System.Web.HttpUtility.ParseQueryString(capped!.TrimStart('?'));
        var modified = false;

        foreach (var key in query.AllKeys)
        {
            if (key != null && redacted.Contains(key))
            {
                query[key] = "***";
                modified = true;
            }
        }

        return modified ? $"?{query}" : capped;
    }

    private static string? TruncateQueryString(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString)) return queryString;
        if (queryString.Length <= MaxQueryStringLogLength) return queryString;
        return queryString[..MaxQueryStringLogLength] + "...[truncated]";
    }
}
