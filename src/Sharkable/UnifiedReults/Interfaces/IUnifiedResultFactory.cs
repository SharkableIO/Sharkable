namespace Sharkable;

/// <summary>
/// Factory for creating <see cref="IUnifiedResult"/> responses. Plug a custom
/// implementation into <see cref="SharkOption.UnifiedResultFactory"/> to change
/// the response shape used by all Sharkable middleware (exception handler,
/// validation, auto-wrap, idempotency).
/// </summary>
public interface IUnifiedResultFactory
{
    /// <summary>
    /// Creates a unified result.
    /// </summary>
    /// <param name="data">Response payload. Null for error responses.</param>
    /// <param name="errorMessage">Error message. Null for successful responses.</param>
    /// <param name="statusCode">HTTP status code.</param>
    IUnifiedResult Create(object? data, string? errorMessage, int statusCode);
}
