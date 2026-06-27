namespace Sharkable;

/// <summary>
/// Provides default OpenAPI summary and description for all endpoints in an <see cref="ISharkEndpoint"/> class.
/// Individual endpoints can override via <c>WithSummary</c> / <c>WithDescription</c> on the route handler.
/// </summary>
/// <param name="summary">Short summary for the OpenAPI operation.</param>
/// <param name="description">Detailed description for the OpenAPI operation.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class SharkDescriptionAttribute(string? summary = null, string? description = null) : Attribute
{
    /// <summary>Short summary for the OpenAPI operation. Passed to <c>WithSummary</c>.</summary>
    public string? Summary { get; } = summary;

    /// <summary>Detailed description for the OpenAPI operation. Passed to <c>WithDescription</c>.</summary>
    public string? Description { get; } = description;
}
