
namespace Sharkable;

/// <summary>
/// Mininal api endpoint
/// </summary>
public class SharkEndpoint : ISharkEndpoint
{
    //internal string corsName;
    internal string? grouName;
    internal string? apiPrefix;
    internal string? baseApiPath;
    internal bool addPrefix = true;
    internal string? version;
    internal Action<IEndpointRouteBuilder>? BuildAction { get; set; }
    public virtual void AddRoutes(IEndpointRouteBuilder app)
    {

    }

    public SharkEndpoint()
    {
        var name = GetType().Name;

        grouName = name.FormatAsGroupName()!;
    }

    public SharkEndpoint(string? grouName, string apiPrefix = "api")
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(grouName);
        this.grouName = grouName;
        this.apiPrefix = apiPrefix;
    }
}
