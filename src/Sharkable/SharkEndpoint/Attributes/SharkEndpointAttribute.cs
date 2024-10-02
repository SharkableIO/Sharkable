namespace Sharkable;

/// <summary>
/// An api endpoint of Sharkable
/// </summary>
/// <param name="group"></param>
//[Obsolete("due to aot incompatiblily, SharkEndpoint will update new features in v0.0.5 and above, \n use ISharkEndpoint instead")]
[AttributeUsage(AttributeTargets.Class)]
//[RequiresDynamicCode("please use ISharkEndpoint instead")]
public sealed class SharkEndpointAttribute(string? group = null, string? apiPrefix = "api", string? version = null) : Attribute
{
    public string? Group { get; set; } = group;
    public string? ApiPrefix { get;} = apiPrefix;
    public string? Version { get; } = version;
}
