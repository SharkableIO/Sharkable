
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
    public virtual void AddRoutes(IEndpointRouteBuilder app) { }

    protected SharkEndpoint()
    {
        var name = GetType().Name;

        grouName = name.FormatAsGroupName()!;
    }

    protected SharkEndpoint(string? grouName, string apiPrefix = "api")
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(grouName);
        this.grouName = grouName;
        this.apiPrefix = apiPrefix;
        // baseApiPath = apiPrefix + "/" + grouName;
    }
}
