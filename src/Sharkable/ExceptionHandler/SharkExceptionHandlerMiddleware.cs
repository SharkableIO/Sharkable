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
        var statusCode = (int)options.GetStatusCode(exception);
        var errorMessage = options.GetErrorMessage(exception);

        var factory = Shark.SharkOption.UnifiedResultFactory ?? new DefaultUnifiedResultFactory();
        var result = factory.Create(null, errorMessage, statusCode);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(result, result.GetType());
    }
}
