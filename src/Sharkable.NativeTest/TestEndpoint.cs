namespace Sharkable.NativeTest;

public class TestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("hello", () => "hello from sharkable");

        app.MapPost("echo", (PostBody body, ILogger<TestEndpoint> logger) =>
        {
            var opt = Shark.GetOptions<SharkOption>();
            var monitor = Shark.GetService<IMonitor>();
            monitor?.Show();
            logger.LogInformation("api prefix: {Prefix}", opt?.ApiPrefix);
            return body;
        });
    }
}

public sealed record PostBody(string Name, int Age);
