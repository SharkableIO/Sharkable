
namespace Sharkable;

/// <summary>
/// Mininal api endpoint
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class SharkEndpoint : ISharkEndpoint
{
    //internal string corsName;
    internal string? groupName;
    internal string? apiPrefix;
    internal string? baseApiPath;
    internal bool addPrefix = true;
    internal string? version;
    internal Action<IEndpointRouteBuilder>? BuildAction { get; set; }
    /// <summary>
    /// add routes to build endpoints
    /// </summary>
    public virtual void AddRoutes(IEndpointRouteBuilder app)
    {
    }

    public SharkEndpoint()
    {
        var name = GetType().Name;
        groupName = name.FormatAsGroupName()!;
    }

    public SharkEndpoint(string? groupName, string apiPrefix = "api")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        this.groupName = groupName;
        this.apiPrefix = apiPrefix;
    }
}
