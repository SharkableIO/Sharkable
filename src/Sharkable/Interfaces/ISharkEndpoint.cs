using Microsoft.AspNetCore.Routing;

namespace Sharkable;

public interface ISharkEndpoint
{
    void AddRoutes(IEndpointRouteBuilder app);
}