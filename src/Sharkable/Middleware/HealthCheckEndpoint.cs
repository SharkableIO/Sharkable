using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

internal static class HealthCheckEndpoint
{
    internal static void Map(WebApplication app)
    {
        app.MapGet("/healthz", () => Results.Text("healthy"));
    }
}
