
namespace Sharkable;

/// <summary>
/// Minimal API endpoint. Implement <see cref="AddRoutes"/> to define routes.
/// The class name (minus "Endpoint"/"Service" suffix) becomes the URL group prefix.
/// </summary>
public interface ISharkEndpoint
{
    /// <summary>
    /// Define routes by calling <c>app.MapGet()</c>, <c>app.MapPost()</c>, etc.
    /// Routes are automatically grouped under <c>api/{group-name}</c>.
    /// </summary>
    void AddRoutes(IEndpointRouteBuilder app);
}