using Microsoft.AspNetCore.Mvc;

namespace Sharkable.NativeTest;

public class TestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("lost", () => Results.Ok("lost and found"));
        app.MapPost("lost", ([FromBody]Todo[] todos) => Results.Ok((object?)todos));
    }
}