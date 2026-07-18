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

    public LoggingAuditSink(ILogger<LoggingAuditSink> logger)
    {
        _logger = logger;
    }

    public Task WriteBatchAsync(IReadOnlyList<AuditLogEntry> entries, CancellationToken cancellationToken)
    {
        var options = Shark.SharkOption.AuditTrailOptions!;
        var successLevel = options.SuccessLogLevel;
        var warningLevel = options.WarningLogLevel;
        var errorLevel = options.ErrorLogLevel;
        var logFormat = options.LogFormat;

        foreach (var entry in entries)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var level = entry.StatusCode >= 500 ? errorLevel
                       : entry.StatusCode >= 400 ? warningLevel
                       : successLevel;

            if (!_logger.IsEnabled(level))
                continue;

            if (logFormat == AuditTrailFormat.Default)
            {
                _logger.Log(level,
                    "HTTP {Method} {Path}{Query} responded {StatusCode} in {ElapsedMs}ms [CorrelationId: {CorrelationId}] Headers={Headers}",
                    entry.Method, entry.Path, entry.Query, entry.StatusCode, entry.ElapsedMs, entry.CorrelationId, entry.Headers);
            }
            else
            {
                var message = AuditTrailOptions.FormatEntry(entry, logFormat);
                _logger.Log(level, "{Message}", message);
            }
        }

        return Task.CompletedTask;
    }
}
