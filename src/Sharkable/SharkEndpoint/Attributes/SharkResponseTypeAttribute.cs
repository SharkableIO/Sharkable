namespace Sharkable;

/// <summary>
/// Adds a response metadata entry (status code + optional type + description) to all endpoints in an <see cref="ISharkEndpoint"/> class.
/// Repeatable for multiple status codes.
/// </summary>
/// <param name="statusCode">HTTP status code (e.g., 200, 400, 404).</param>
/// <param name="responseType">Optional response type for serialization metadata.</param>
/// <param name="description">Optional description of the response.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class SharkResponseTypeAttribute(int statusCode, Type? responseType = null, string? description = null) : Attribute
{
    /// <summary>HTTP status code.</summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>Response type, if any.</summary>
    public Type? ResponseType { get; } = responseType;

    /// <summary>Response description.</summary>
    public string? Description { get; } = description;
}
