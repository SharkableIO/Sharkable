using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace Sharkable;

/// <summary>
/// delegate of a http method
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[RequiresDynamicCode("map SharkMethodAttribute only supported in aot mode")]
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
