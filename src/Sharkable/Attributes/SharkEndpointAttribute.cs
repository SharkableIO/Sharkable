namespace Sharkable;

/// <summary>
/// An api endpoint of Sharkable
/// </summary>
/// <param name="group"></param>
[Obsolete("due to aot incompatablily, SharkEndpoint will update new features in v0.0.5 and above, \n use ISharkEndpoint instead")]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class SharkEndpointAttribute(string? group = null) : Attribute
{
    public string? Group { get; set; } = group;
}
