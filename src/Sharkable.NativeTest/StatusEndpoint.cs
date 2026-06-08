namespace Sharkable.NativeTest;

public class StatusEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("info", () => "status ok");

        app.MapGet("numbers", () => new[] { 1, 2, 3, 4, 5 });

        app.MapGet("echo/{value}", (string value) => Results.Ok(value));
    }
}
