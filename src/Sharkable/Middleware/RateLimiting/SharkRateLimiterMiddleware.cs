using System.Globalization;

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
    private readonly AdaptiveLimitMonitor? _adaptiveMonitor;

    public SharkRateLimiterMiddleware(
        RequestDelegate next,
        IDistributedRateLimitStore store,
        SharkRateLimiterOptions options,
        AdaptiveLimitMonitor? adaptiveMonitor = null)
    {
        _next = next;
        _store = store;
        _options = options;
        _adaptiveMonitor = adaptiveMonitor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var keyGenerator = _options.KeyGenerator ?? _options.DefaultKeyGenerator;
        var key = keyGenerator(context);
        var count = await _store.IncrementAsync(key, _options.DefaultWindow);

        var effectiveLimit = _adaptiveMonitor != null
            ? _adaptiveMonitor.CurrentLimit
            : _options.DefaultLimit;

        var remaining = effectiveLimit - count;

        if (_options.IncludeHeaders)
        {
            var limit = effectiveLimit;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[$"{_options.HeaderPrefix}-Limit"] = limit.ToString(CultureInfo.InvariantCulture);
                context.Response.Headers[$"{_options.HeaderPrefix}-Remaining"] = remaining > 0 ? remaining.ToString(CultureInfo.InvariantCulture) : "0";
                context.Response.Headers[$"{_options.HeaderPrefix}-Reset"] = ((long)_options.DefaultWindow.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                return Task.CompletedTask;
            });
        }

        if (count > effectiveLimit)
        {
            context.Response.StatusCode = 429;
            var localizer = ErrorLocalizerHelper.GetLocalizer();
            var culture = ErrorLocalizerHelper.ResolveCulture(context);
            await ProblemDetailsResult.WriteAsync(context, 429,
                localizer.Localize("RateLimitExceeded", culture));
            return;
        }

        await _next(context);
    }
}
