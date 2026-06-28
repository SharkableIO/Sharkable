namespace Sharkable;

/// <summary>
/// HTTP methods for attribute-based endpoints (legacy style, not AOT-compatible).
/// Use <see cref="ISharkEndpoint.AddRoutes"/> with <c>app.MapGet/MapPost/...</c> instead.
/// </summary>
[Obsolete("Use ISharkEndpoint (AddRoutes method with MapGet/MapPost/...) instead. This enum is only used by [SharkMethod] which is obsolete.")]
public enum SharkHttpMethod
{
    /// <summary>HTTP GET</summary>
    GET,
    /// <summary>HTTP POST</summary>
    POST,
    /// <summary>HTTP PUT</summary>
    PUT,
    /// <summary>HTTP DELETE</summary>
    DELETE,
    /// <summary>HTTP PATCH</summary>
    PATCH,
}
