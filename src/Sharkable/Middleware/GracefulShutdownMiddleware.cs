using System.Threading;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

internal sealed class GracefulShutdownMiddleware
{
    private readonly RequestDelegate _next;

    public GracefulShutdownMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (Volatile.Read(ref InternalShark.IsShuttingDown))
        {
            context.Response.StatusCode = 503;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync($"{{\"statusCode\":503,\"errorMessage\":\"Server is shutting down\"}}");
            return;
        }

        Interlocked.Increment(ref InternalShark.ActiveRequests);
        try
        {
            await _next(context);
        }
        finally
        {
            Interlocked.Decrement(ref InternalShark.ActiveRequests);
        }
    }
}
