namespace Sharkable;

/// <summary>
/// An api endpoint of Sharkable
/// </summary>
/// <param name="group"></param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class SharkEndpointAttribute(string? group = null) : Attribute
{
    public string? Group { get; set; } = group;
}
