namespace Sharkable;

/// <summary>
/// Pluggable sink for audit trail log entries captured by
/// <see cref="AuditTrailMiddleware"/>. Implement this to ship entries
/// to external systems (Seq, Elastic, Kafka, etc.) without replacing the middleware.
/// </summary>
public interface IAuditSink
{
    /// <summary>
    /// Writes a batch of audit log entries to the sink.
    /// Called by <see cref="AuditTrailMiddleware"/> when
    /// <see cref="AuditTrailOptions.AsyncWrite"/> is <c>true</c> (batched)
    /// or individually (batch size of 1) when <c>false</c>.
    /// </summary>
    /// <param name="entries">Non-empty list of audit log entries.</param>
    /// <param name="cancellationToken">Cancellation token triggered on shutdown.</param>
    Task WriteBatchAsync(IReadOnlyList<AuditLogEntry> entries, CancellationToken cancellationToken);
}
