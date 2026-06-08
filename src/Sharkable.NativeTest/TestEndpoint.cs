using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Sharkable.NativeTest;

public class TestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("lost", () => Results.Ok(new UnifiedResult<string>("hello")));
        app.MapPost("lost", ([FromBody]Todo[] todos, [FromServices]ILogger<TestEndpoint> logger) =>
        {
            var opt = Shark.GetOptions<SharkOption>();
            var monitor = Shark.GetService<IMonitor>();
            monitor?.Show();
            logger.LogError(opt?.ApiPrefix);
            return todos.AsOkResult();
        });
        app.MapPost("create", ([FromBody] Todo todo) => todo.AsOkResult());
    }
}