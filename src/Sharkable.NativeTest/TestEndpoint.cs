namespace Sharkable.NativeTest;

public class TestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("lost", () =>
        { 
            Results.Ok("lost and found");
        });

    }
}