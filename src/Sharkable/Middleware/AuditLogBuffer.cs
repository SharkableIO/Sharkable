using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Sharkable;

internal sealed class AuditLogBuffer : IDisposable
{
    private readonly Channel<AuditLogEntry> _channel;
    private readonly int _batchSize;
    private readonly TimeSpan _flushInterval;
    private readonly ILogger _logger;
    private readonly LogLevel _successLevel;
    private readonly LogLevel _warningLevel;
    private readonly LogLevel _errorLevel;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _consumerTask;
    private bool _disposed;

    internal AuditLogBuffer(AuditTrailOptions options, ILogger logger)
    {
        _channel = options.AsyncWrite
            ? Channel.CreateBounded<AuditLogEntry>(new BoundedChannelOptions(4096)
            {
                FullMode = BoundedChannelFullMode.DropWrite
            })
            : Channel.CreateUnbounded<AuditLogEntry>(new UnboundedChannelOptions { SingleReader = true });

        _batchSize = options.BatchSize;
        _flushInterval = options.FlushInterval;
        _logger = logger;
        _successLevel = options.SuccessLogLevel;
        _warningLevel = options.WarningLogLevel;
        _errorLevel = options.ErrorLogLevel;

        _consumerTask = ConsumeAsync(_cts.Token);
    }

    internal void Write(AuditLogEntry entry)
    {
        _channel.Writer.TryWrite(entry);
    }

    internal void FlushRemaining()
    {
        _cts.Cancel();
        try
        {
            _consumerTask.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ConsumeAsync(CancellationToken ct)
    {
        var buffer = new List<AuditLogEntry>(_batchSize);
        var reader = _channel.Reader;

        while (!ct.IsCancellationRequested)
        {
            if (!await reader.WaitToReadAsync(ct).ConfigureAwait(false))
                break;

            buffer.Clear();
            var timer = _batchSize > 1 ? Task.Delay(_flushInterval, ct) : Task.CompletedTask;

            while (buffer.Count < _batchSize && reader.TryRead(out var entry))
            {
                buffer.Add(entry);
            }

            FlushBatch(buffer);

            if (_batchSize > 1)
            {
                try { await timer; } catch (OperationCanceledException) { }
            }
        }

        // drain remaining on cancellation
        buffer.Clear();
        while (reader.TryRead(out var entry))
            buffer.Add(entry);
        if (buffer.Count > 0)
            FlushBatch(buffer);

        // SHARK-SEC-L020: a debug-level log so operators can tell whether
        // the consumer exited cleanly (graceful shutdown) vs. an
        // unrelated cancellation hit the loop. The exception is
        // swallowed by design here — cancellation on graceful shutdown
        // is the trigger for draining the channel.
        _logger.LogDebug("AuditLogBuffer consumer exiting (cancellation token fired)");
    }

    private void FlushBatch(List<AuditLogEntry> batch)
    {
        foreach (var entry in batch)
        {
            var level = entry.StatusCode >= 500 ? _errorLevel
                       : entry.StatusCode >= 400 ? _warningLevel
                       : _successLevel;

            if (!_logger.IsEnabled(level))
                continue;

            _logger.Log(level,
                "HTTP {Method} {Path}{Query} responded {StatusCode} in {ElapsedMs}ms [CorrelationId: {CorrelationId}] Headers={Headers}",
                entry.Method, entry.Path, entry.Query, entry.StatusCode, entry.ElapsedMs, entry.CorrelationId, entry.Headers);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
    }
}

internal readonly record struct AuditLogEntry(
    string Method,
    string Path,
    string? Query,
    string Headers,
    int StatusCode,
    long ElapsedMs,
    string CorrelationId);
