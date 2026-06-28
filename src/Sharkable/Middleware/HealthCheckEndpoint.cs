using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

internal static class HealthCheckEndpoint
{
    internal static void Map(WebApplication app)
    {
        app.MapGet("/healthz", () =>
        {
            if (Volatile.Read(ref InternalShark.IsShuttingDown))
                return Results.Text("unhealthy", statusCode: 503);
            return Results.Text("healthy");
        });
    }
}
