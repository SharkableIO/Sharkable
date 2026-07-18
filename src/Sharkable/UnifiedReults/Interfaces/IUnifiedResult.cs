namespace Sharkable;

/// <summary>
/// Marker interface for unified API responses.
/// <para>
/// For most use cases, use the built-in <see cref="UnifiedResult{T}"/> and its static
/// factory methods (e.g., <c>UnifiedResult.Ok(data)</c>, <c>UnifiedResult.BadRequest(msg)</c>).
/// </para>
/// <para>
/// To customize the response shape across all endpoints, implement
/// <see cref="IUnifiedResultFactory"/> and register it via
/// <c>SharkOption.UnifiedResultFactory</c>. When you replace the factory, also set
/// <see cref="SharkOption.WrapSchemaFactory"/> to keep the OpenAPI document in sync.
/// </para>
/// </summary>
public interface IUnifiedResult
{
    /// <summary>HTTP status code for the response.</summary>
    int StatusCode { get; }
    /// <summary>Response payload data.</summary>
    object? Data { get; }
    /// <summary>Error message, null when the request succeeds.</summary>
    string? ErrorMessage { get; }
}
