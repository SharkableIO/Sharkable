using System.Diagnostics.CodeAnalysis;

namespace Sharkable;

/// <summary>
/// delegate of a http method
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SharkMethodAttribute : Attribute
{
    public SharkMethodAttribute(SharkHttpMethod method = SharkHttpMethod.POST)
    {
        Method = method;
    }

    public SharkMethodAttribute([StringSyntax("Route")]string? addressName, SharkHttpMethod method = SharkHttpMethod.POST)
    {
        AddressName = addressName;
        Method = method;
    }

    public string? AddressName { get; set; }
    public SharkHttpMethod Method { get; }
}
