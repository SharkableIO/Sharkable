using System;
using Microsoft.AspNetCore.Mvc;

namespace Sharkable.AotSample;

public class AutoCrudEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("init", async ([FromServices]IMonitor monitor) =>
        {
             await monitor.InitTask();
            return Results.Ok("init");
        });
        app.MapGet("getall", async([FromServices]IMonitor monitor) =>
        {
            var data = await monitor.GetTasks();
            return Results.Ok(data);
        });
    }
}
