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
    private readonly AuditTrailOptions _options;

    public LoggingAuditSink(ILogger<LoggingAuditSink> logger)
    {
        _logger = logger;
        _options = Shark.SharkOption.AuditTrailOptions!;
    }

    public Task WriteBatchAsync(IReadOnlyList<AuditLogEntry> entries, CancellationToken cancellationToken)
    {
        foreach (var entry in entries)
        {
            if (cancellationToken.IsCancellationRequested)
                break;
            var level = entry.StatusCode >= 500 ? _options.ErrorLogLevel
                       : entry.StatusCode >= 400 ? _options.WarningLogLevel
                       : _options.SuccessLogLevel;

            if (!_logger.IsEnabled(level))
                continue;

            if (_options.LogFormat == AuditTrailFormat.Default)
            {
                _logger.Log(level,
                    "HTTP {Method} {Path}{Query} responded {StatusCode} in {ElapsedMs}ms [CorrelationId: {CorrelationId}] Headers={Headers}",
                    entry.Method, entry.Path, entry.Query, entry.StatusCode, entry.ElapsedMs, entry.CorrelationId, entry.Headers);
            }
            else
            {
                var message = AuditTrailOptions.FormatEntry(entry, _options.LogFormat);
                _logger.Log(level, "{Message}", message);
            }
        }

        return Task.CompletedTask;
    }
}
