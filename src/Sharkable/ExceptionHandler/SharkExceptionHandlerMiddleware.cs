using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

internal sealed class SharkExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public SharkExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var options = Shark.SharkOption.ExceptionHandlerOptions;
        var statusCode = options.GetStatusCode(exception);
        var errorMessage = options.GetErrorMessage(exception);

        var result = new UnifiedResult<object?>
        {
            StatusCode = statusCode,
            Data = null,
            ErrorMessage = errorMessage,
            TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        return JsonSerializer.SerializeAsync(
            context.Response.Body,
            result,
            typeof(UnifiedResult<object?>),
            UnifiedResultSourceContext.Default);
    }
}
