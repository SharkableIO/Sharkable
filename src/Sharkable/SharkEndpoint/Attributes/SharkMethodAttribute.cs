using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace Sharkable;

/// <summary>
/// delegate of a http method
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[Obsolete("this method is removed in version 0.0.24 and above due to aot incompatible")]
public sealed class SharkMethodAttribute : Attribute
{
    public SharkMethodAttribute(SharkHttpMethod method = SharkHttpMethod.POST)
    {
        Method = method;
    }

    public SharkMethodAttribute([StringSyntax("Route")]string? pattern, SharkHttpMethod method = SharkHttpMethod.POST)
    {
        Pattern = pattern;
        Method = method;
    }

    public string? Pattern { get; set; }
    public SharkHttpMethod Method { get; }
}
