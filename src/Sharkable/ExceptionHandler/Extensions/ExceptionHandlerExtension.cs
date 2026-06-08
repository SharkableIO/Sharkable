namespace Sharkable;

/// <summary>
/// Extension methods for wiring the Sharkable global exception handler.
/// </summary>
public static class ExceptionHandlerExtension
{
    /// <summary>
    /// Adds a global exception handler middleware that converts unhandled exceptions
    /// to <see cref="UnifiedResult{T}"/> JSON responses.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="configure">Optional configuration for exception handling behavior.</param>
    public static IApplicationBuilder UseSharkExceptionHandler(
        this IApplicationBuilder app,
        Action<ExceptionHandlerOptions>? configure = null)
    {
        configure?.Invoke(Shark.SharkOption.ExceptionHandlerOptions);
        return app.UseMiddleware<SharkExceptionHandlerMiddleware>();
    }
}
