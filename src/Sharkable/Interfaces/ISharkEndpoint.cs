using Microsoft.AspNetCore.Routing;

namespace Sharkable;

/// <summary>
/// Mininal api endpoint
/// </summary>
public interface ISharkEndpoint
{
    void AddRoutes(IEndpointRouteBuilder app);
}