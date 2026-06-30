using Sharkable;
public sealed class LoverEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/version", () =>
        {
            return Results.Ok("we2");
        })
        .SharkCacheOutput();

        app.MapGet("/features", () =>
        {
            return Results.Ok("hey");
        });
    }
}
