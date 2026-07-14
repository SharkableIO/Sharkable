namespace Sharkable;

/// <summary>
/// Preset log formats for audit trail output.
/// </summary>
public enum AuditTrailFormat
{
    /// <summary>
    /// Structured format with named placeholders (default):
    /// <c>HTTP GET /api/foo responded 200 in 42ms [CorrelationId: ...] Headers={...}</c>
    /// </summary>
    Default,

    /// <summary>
    /// .NET console-logger-inspired style:
    /// <c>GET /api/foo => 200 in 42ms [...] Headers={...}</c>
    /// </summary>
    DotnetLogger,

    /// <summary>
    /// Single-line JSON output:
    /// <c>{"method":"GET","path":"/api/foo",...,"headers":{...}}</c>
    /// </summary>
    JsonStyle,

    /// <summary>
    /// Minimal single-line format:
    /// <c>GET /api/foo 200 42ms</c>
    /// </summary>
    Compact,
}
