namespace Sharkable;

/// <summary>
/// Marks a class as an attribute-based endpoint (legacy style, not AOT-compatible).
/// Use <see cref="ISharkEndpoint"/> for new code.
/// </summary>
/// <param name="group">URL group name. Derived from class name if null.</param>
/// <param name="apiPrefix">URL prefix. Set to null to omit prefix.</param>
/// <param name="version">Optional version segment appended to the URL path.</param>
[AttributeUsage(AttributeTargets.Class)]
[Obsolete("Use ISharkEndpoint (AddRoutes method) instead. This attribute-based style is not AOT-compatible and will be removed in a future version.")]
public sealed class SharkEndpointAttribute(string? group = null, string? apiPrefix = "api", string? version = null) : Attribute
{
    /// <summary>URL group name. When null, derived automatically from the class name.</summary>
    public string? Group { get; set; } = group;
    /// <summary>API prefix (default "api"). Set to null to omit.</summary>
    public string? ApiPrefix { get;} = apiPrefix;
    /// <summary>Optional version segment (e.g., "v1" → "/v1/..." in the URL).</summary>
    public string? Version { get; } = version;
}
