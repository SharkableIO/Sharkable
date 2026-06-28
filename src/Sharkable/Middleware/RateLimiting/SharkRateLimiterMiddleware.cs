namespace Sharkable;

/// <summary>
/// Fixed-window rate limiting middleware backed by <see cref="IDistributedRateLimitStore"/>.
/// Configure via <c>SharkOption.ConfigureRateLimiting(o =&gt; ...)</c>.
/// When the limit is exceeded, returns 429 with a unified error response.
/// </summary>
internal sealed class SharkRateLimiterMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDistributedRateLimitStore _store;
    private readonly SharkRateLimiterOptions _options;

    public SharkRateLimiterMiddleware(
        RequestDelegate next,
        IDistributedRateLimitStore store,
        SharkRateLimiterOptions options)
    {
        _next = next;
        _store = store;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var keyGenerator = _options.KeyGenerator ?? _options.DefaultKeyGenerator;
        var key = keyGenerator(context);
        var count = await _store.IncrementAsync(key, _options.DefaultWindow);
        var remaining = _options.DefaultLimit - count;

        if (_options.IncludeHeaders)
        {
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[$"{_options.HeaderPrefix}-Limit"] = _options.DefaultLimit.ToString();
                context.Response.Headers[$"{_options.HeaderPrefix}-Remaining"] = remaining > 0 ? remaining.ToString() : "0";
                context.Response.Headers[$"{_options.HeaderPrefix}-Reset"] = ((long)_options.DefaultWindow.TotalSeconds).ToString();
                return Task.CompletedTask;
            });
        }

        if (count > _options.DefaultLimit)
        {
            context.Response.StatusCode = 429;
            context.Response.ContentType = "application/json";
            var factory = Shark.SharkOption.UnifiedResultFactory ?? new DefaultUnifiedResultFactory();
            var result = factory.Create(data: null, errorMessage: "Rate limit exceeded. Please retry later.", statusCode: 429);
            await context.Response.WriteAsJsonAsync(result, result.GetType());
            return;
        }

        await _next(context);
    }
}
