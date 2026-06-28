namespace Sharkable;

/// <summary>
/// Defines a route method for attribute-based endpoints (legacy style, not AOT-compatible).
/// Use <see cref="ISharkEndpoint.AddRoutes"/> with <c>app.MapGet/MapPost/...</c> instead.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[Obsolete("Use ISharkEndpoint (AddRoutes method with MapGet/MapPost/...) instead. This attribute-based style is not AOT-compatible and will be removed in a future version.")]
public sealed class SharkMethodAttribute(
    [StringSyntax("Route")] string? pattern,
    SharkHttpMethod method = SharkHttpMethod.POST)
    : Attribute
{
    public SharkMethodAttribute(SharkHttpMethod method = SharkHttpMethod.POST) : this(null, method)
    {
    }

    [StringSyntax("Route")]
    public string? Pattern { get; internal set; } = pattern;
    public SharkHttpMethod Method { get; } = method;
}
