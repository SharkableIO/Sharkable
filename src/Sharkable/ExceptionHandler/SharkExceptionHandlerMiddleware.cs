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

        var factory = Shark.SharkOption.UnifiedResultFactory ?? new DefaultUnifiedResultFactory();
        var result = factory.Create(null, errorMessage, statusCode);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(result, result.GetType());
    }
}
