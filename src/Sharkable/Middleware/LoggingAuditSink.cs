using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// Default implementation of <see cref="IAuditSink"/> that writes audit log
/// entries via <see cref="ILogger"/>. Preserves the existing structured logging
/// behavior with format selection (<see cref="AuditTrailFormat"/>).
/// </summary>
internal sealed class LoggingAuditSink : IAuditSink
{
    private readonly ILogger _logger;
    private readonly LogLevel _successLevel;
    private readonly LogLevel _warningLevel;
    private readonly LogLevel _errorLevel;
    private readonly AuditTrailFormat _logFormat;

    public LoggingAuditSink(ILogger<LoggingAuditSink> logger, AuditTrailOptions options)
    {
        _logger = logger;
        _successLevel = options.SuccessLogLevel;
        _warningLevel = options.WarningLogLevel;
        _errorLevel = options.ErrorLogLevel;
        _logFormat = options.LogFormat;
    }

    public Task WriteBatchAsync(IReadOnlyList<AuditLogEntry> entries, CancellationToken cancellationToken)
    {
        foreach (var entry in entries)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            var level = entry.StatusCode >= 500 ? _errorLevel
                       : entry.StatusCode >= 400 ? _warningLevel
                       : _successLevel;

            if (!_logger.IsEnabled(level))
                continue;

            if (_logFormat == AuditTrailFormat.Default)
            {
                _logger.Log(level,
                    "HTTP {Method} {Path}{Query} responded {StatusCode} in {ElapsedMs}ms [CorrelationId: {CorrelationId}] Headers={Headers}",
                    entry.Method, entry.Path, entry.Query, entry.StatusCode, entry.ElapsedMs, entry.CorrelationId, entry.Headers);
            }
            else
            {
                var message = AuditTrailOptions.FormatEntry(entry, _logFormat);
                _logger.Log(level, "{Message}", message);
            }
        }

        return Task.CompletedTask;
    }
}
