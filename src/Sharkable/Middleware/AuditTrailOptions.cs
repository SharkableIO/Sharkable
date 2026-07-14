using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// Configuration options for the structured request logging / audit trail middleware.
/// Passed via <c>opt.ConfigureAuditTrail()</c> callback in <c>AddShark()</c>.
/// </summary>
public sealed class AuditTrailOptions
{
    /// <summary>
    /// Log level for successful responses (status codes &lt; 400). Default is <see cref="LogLevel.Information"/>.
    /// </summary>
    public LogLevel SuccessLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Log level for client error responses (status codes 400–499). Default is <see cref="LogLevel.Warning"/>.
    /// </summary>
    public LogLevel WarningLogLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// Log level for server error responses (status codes &gt;= 500). Default is <see cref="LogLevel.Error"/>.
    /// </summary>
    public LogLevel ErrorLogLevel { get; set; } = LogLevel.Error;

    /// <summary>
    /// Path prefixes to exclude from audit logging (e.g. <c>"/healthz"</c>, <c>"/openapi"</c>).
    /// Matches by prefix — any path starting with an entry is skipped.
    /// </summary>
    public string[] ExcludePaths { get; set; } = [];

    /// <summary>
    /// Request header names whose values should be redacted in audit log output
    /// (e.g. <c>"Authorization"</c>, <c>"X-Api-Key"</c>, <c>"Cookie"</c>).
    /// Matched case-insensitively against the incoming request headers.
    /// Redacted headers have their value replaced with <c>"***"</c> in the log
    /// entry while the header name itself is preserved so reviewers can see
    /// which credentials were presented. Defaults cover the common
    /// credential-bearing headers — extend the list to add application-specific
    /// secrets (SHARK-SEC-010).
    /// </summary>
    public string[] RedactHeaders { get; set; } = ["Authorization", "X-Api-Key", "Cookie"];

    /// <summary>
    /// Query parameter names whose values should be redacted in logs (e.g. <c>"token"</c>, <c>"api_key"</c>).
    /// Matched case-insensitively.
    /// </summary>
    public string[] RedactQueryParams { get; set; } = ["token", "api_key", "secret"];

    /// <summary>
    /// Whether to include the query string in log output. Default is <c>true</c>.
    /// </summary>
    public bool IncludeQueryString { get; set; } = true;

    /// <summary>
    /// HTTP header name used for correlation ID. Default is <c>"X-Correlation-Id"</c>.
    /// </summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// When <c>true</c>, reuses an incoming <c>CorrelationIdHeader</c> value if present;
    /// otherwise generates a new UUID. Response header is always set.
    /// <para>
    /// SHARK-SEC-M020: defaults to <c>false</c> so an attacker cannot inject
    /// log lines via the header (e.g. <c>X-Correlation-Id: foo\n[CRITICAL]
    /// admin login OK</c>). When enabled, the value is also validated
    /// against <see cref="AuditTrailMiddleware"/>'s safe-character pattern
    /// (alphanumerics, dot, underscore, dash) and a fresh UUID is generated
    /// when the supplied value fails validation.
    /// </para>
    /// </summary>
    public bool ForwardCorrelationId { get; set; } = false;

    /// <summary>
    /// Maximum log entries to buffer before flushing. Default is <c>1</c> (flush immediately).
    /// Set higher (e.g. 50–100) to batch writes when <see cref="AsyncWrite"/> is <c>true</c>.
    /// </summary>
    public int BatchSize { get; set; } = 1;

    /// <summary>
    /// Maximum interval between flushes when <see cref="AsyncWrite"/> is <c>true</c>.
    /// Only applies when <see cref="BatchSize"/> &gt; 1 and entries haven't reached the batch threshold.
    /// Default is 5 seconds.
    /// </summary>
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// When <c>true</c>, log entries are written on a background thread without blocking the request.
    /// Default is <c>false</c> (synchronous, matching prior behavior).
    /// </summary>
    public bool AsyncWrite { get; set; }

    /// <summary>
    /// When <c>true</c>, remaining buffered log entries are flushed during graceful shutdown.
    /// Default is <c>true</c>.
    /// </summary>
    public bool EnsureFlushOnShutdown { get; set; } = true;

    /// <summary>
    /// Log output format preset. Default is <see cref="AuditTrailFormat.Default"/>.
    /// </summary>
    public AuditTrailFormat LogFormat { get; set; } = AuditTrailFormat.Default;

    internal static string FormatEntry(AuditLogEntry entry, AuditTrailFormat format)
    {
        var query = entry.Query ?? string.Empty;

        return format switch
        {
            AuditTrailFormat.DotnetLogger =>
                $"{entry.Method} {entry.Path}{query} => {entry.StatusCode} in {entry.ElapsedMs}ms [{entry.CorrelationId}] Headers={entry.Headers}",
            AuditTrailFormat.JsonStyle =>
                $$"""{"method":"{{EscapeJson(entry.Method)}}","path":"{{EscapeJson(entry.Path)}}","query":"{{EscapeJson(query)}}","statusCode":{{entry.StatusCode}},"durationMs":{{entry.ElapsedMs}},"correlationId":"{{EscapeJson(entry.CorrelationId)}}","headers":{{entry.Headers}}}""",
            AuditTrailFormat.Compact =>
                $"{entry.Method} {entry.Path}{query} {entry.StatusCode} {entry.ElapsedMs}ms",
            _ =>
                $"HTTP {entry.Method} {entry.Path}{query} responded {entry.StatusCode} in {entry.ElapsedMs}ms [CorrelationId: {entry.CorrelationId}] Headers={entry.Headers}",
        };
    }

    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
