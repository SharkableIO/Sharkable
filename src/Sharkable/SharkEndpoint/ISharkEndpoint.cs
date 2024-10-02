
namespace Sharkable;

/// <summary>
/// Mininal api endpoint
/// </summary>
public interface ISharkEndpoint
{
    /// <summary>
    /// add routes to build endpoints
    /// </summary>
    void AddRoutes(IEndpointRouteBuilder app);
}