namespace Sharkable;

/// <summary>
/// Factory for creating <see cref="IUnifiedResult"/> instances.
/// Implement this to plug in a custom response format that works with the exception handler,
/// auto-wrap filter, and validation filter.
/// </summary>
public interface IUnifiedResultFactory
{
    /// <summary>Creates a unified result with the given data, error message, and status code.</summary>
    /// <param name="data">Response payload (null on error).</param>
    /// <param name="errorMessage">Error description (null on success).</param>
    /// <param name="statusCode">HTTP status code.</param>
    IUnifiedResult Create(object? data, string? errorMessage, int statusCode);
}
