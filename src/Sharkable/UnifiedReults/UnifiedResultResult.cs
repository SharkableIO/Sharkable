namespace Sharkable;

/// <summary>
/// Internal <see cref="IResult"/> adapter that writes an <see cref="IUnifiedResult"/>
/// to the HTTP response with the correct status code and content type.
/// Eliminates one allocation compared to <c>Results.Json()</c>.
/// </summary>
internal sealed class UnifiedResultResult(IUnifiedResult data) : IResult
{
    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = data.StatusCode;
        return context.Response.WriteAsJsonAsync(data, data.GetType());
    }
}
