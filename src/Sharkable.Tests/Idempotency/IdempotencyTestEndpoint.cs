using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sharkable;

namespace Sharkable.Tests.Idempotency;

/// <summary>
/// Test endpoint that records every invocation. Used by integration tests
/// to assert that the idempotency middleware deduplicates or passes through
/// as expected. Configure behavior via query string:
///   ?status=200|400|500  -&gt; return that status
///   ?delay=2000          -&gt; wait N ms before responding (for in-flight tests)
///   ?size=2000000        -&gt; return a body of N bytes
/// </summary>
[EndpointGroup("idempotency")]
public class IdempotencyTestEndpoint : ISharkEndpoint
{
    /// <summary>Every call appends a unique string; tests reset this between cases.</summary>
    public static readonly ConcurrentBag<string> Invocations = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("test", async (HttpContext ctx) =>
        {
            Invocations.Add(ctx.Connection.Id + ":" + DateTime.UtcNow.Ticks);

            var qs = ctx.Request.Query;
            int status = int.TryParse(qs["status"], out var s) ? s : 200;
            int delay = int.TryParse(qs["delay"], out var d) ? d : 0;
            int size = int.TryParse(qs["size"], out var sz) ? sz : 0;

            if (delay > 0) await Task.Delay(delay);

            if (size > 0)
            {
                ctx.Response.StatusCode = status;
                ctx.Response.ContentType = "application/octet-stream";
                await ctx.Response.Body.WriteAsync(new byte[size]);
                return;
            }

            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/json";
            var json = System.Text.Encoding.UTF8.GetBytes($"{{\"ok\":{(status < 400).ToString().ToLowerInvariant()},\"status\":{status}}}");
            await ctx.Response.Body.WriteAsync(json);
        });

        app.MapGet("test", (HttpContext ctx) =>
        {
            Invocations.Add(ctx.Connection.Id + ":" + DateTime.UtcNow.Ticks);
            return Results.Ok(new { method = "GET" });
        });
    }

    /// <summary>Clears <see cref="Invocations"/>; call between test cases.</summary>
    public static void Reset() => Invocations.Clear();
}