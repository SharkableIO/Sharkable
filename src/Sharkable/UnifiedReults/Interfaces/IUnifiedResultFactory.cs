namespace Sharkable;

/// <summary>
/// Factory for creating <see cref="IUnifiedResult"/> instances.
/// Implement this to plug in a custom response format that works with the exception handler,
/// auto-wrap filter, and validation filter.
/// </summary>
public interface IUnifiedResultFactory
{
    /// <summary>Creates a unified result without an error code.</summary>
    IUnifiedResult Create(object? data, string? errorMessage, int statusCode)
        => Create(data, errorMessage, statusCode, code: null);

    /// <summary>Creates a unified result with an optional machine-readable error code.</summary>
    IUnifiedResult Create(object? data, string? errorMessage, int statusCode, string? code);
}
