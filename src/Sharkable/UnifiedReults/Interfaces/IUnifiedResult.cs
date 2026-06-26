namespace Sharkable;

/// <summary>
/// Marker interface for unified API responses.
/// Implement this to define custom response shapes that work with the exception handler and auto-wrap filter.
/// </summary>
public interface IUnifiedResult
{
    /// <summary>HTTP status code for the response.</summary>
    int StatusCode { get; }
    /// <summary>Response payload data.</summary>
    object? Data { get; }
    /// <summary>Error message, null when the request succeeds.</summary>
    string? ErrorMessage { get; }
    /// <summary>
    /// Optional machine-readable error code (e.g. <c>"idempotency_key_conflict"</c>).
    /// Null when not applicable. Default implementation returns null.
    /// </summary>
    string? Code => null;
}
