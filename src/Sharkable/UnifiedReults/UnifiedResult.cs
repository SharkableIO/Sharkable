using System.Net;

namespace Sharkable;

/// <summary>
/// Default unified response type. Returned by the exception handler, auto-wrap filter,
/// and extension methods like <c>AsOkResult()</c>. Pluggable via <see cref="IUnifiedResultFactory"/>.
/// </summary>
/// <typeparam name="T">Type of the response payload.</typeparam>
public class UnifiedResult<T> : IUnifiedResult
{
    /// <summary>HTTP status code for the response.</summary>
    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;
    /// <summary>Response payload data.</summary>
    public T? Data { get; set; }
    /// <summary>Error message. Null when the request succeeds.</summary>
    public string? ErrorMessage { get; init; }
    /// <summary>Optional extra metadata attached to the response.</summary>
    public string? Extra { get; init; }
    /// <summary>UTC timestamp in milliseconds since epoch.</summary>
    public long? TimeStamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Creates an empty unified result with default OK status.</summary>
    public UnifiedResult() { }

    /// <summary>Creates a unified result with the given data and optional error/status metadata.</summary>
    /// <param name="data">Response payload.</param>
    /// <param name="errorMessage">Error message (null when successful).</param>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="extra">Optional extra metadata.</param>
    public UnifiedResult(T? data, string? errorMessage = null, HttpStatusCode statusCode = HttpStatusCode.OK, string? extra = null)
    {
        Data = data;
        ErrorMessage = errorMessage;
        StatusCode = statusCode;
        Extra = extra;
    }

    int IUnifiedResult.StatusCode => (int)StatusCode;
    object? IUnifiedResult.Data => Data;
    string? IUnifiedResult.ErrorMessage => ErrorMessage;
}

/// <summary>
/// Static helpers for creating <see cref="UnifiedResult{T}"/> instances without specifying the type parameter.
/// </summary>
public static class UnifiedResult
{
    /// <summary>
    /// Creates a <see cref="UnifiedResult{T}"/> with the given data and a 200 status code.
    /// </summary>
    public static object? GetUnifiedResult<T>(T? data)
    {
        return new UnifiedResult<T> { Data = data };
    }
}
