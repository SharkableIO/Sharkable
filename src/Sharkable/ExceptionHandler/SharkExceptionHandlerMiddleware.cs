using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Sharkable;

internal sealed class SharkExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SharkExceptionHandlerMiddleware> _logger;

    public SharkExceptionHandlerMiddleware(RequestDelegate next, ILogger<SharkExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected — not an error. Silently return.
        }
        catch (Exception ex) when (context.Response.HasStarted)
        {
            _logger.LogError(ex, "Unhandled exception after response started for {Method} {Path}",
                context.Request.Method, context.Request.Path);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var options = Shark.SharkOption.ExceptionHandlerOptions;
        var statusCode = (int)options.GetStatusCode(exception);
        var errorMessage = options.GetErrorMessage(exception);

        context.Response.StatusCode = statusCode;
        return ProblemDetailsResult.WriteAsync(context, statusCode, errorMessage);
    }
}
