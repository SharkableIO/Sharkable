using System;

namespace Sharkable.Sample;

public class SharkTest : SharkEndpoint
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("like", () => { Console.WriteLine("like"); return Results.Ok("rejklw"); });
        app.MapGet("hello", () =>
        {
            var sw = Shark.Configuration["Logging"];
            var monitor = Shark.GetService<IMonitor>();
            monitor.Show();
        });
    }
}
