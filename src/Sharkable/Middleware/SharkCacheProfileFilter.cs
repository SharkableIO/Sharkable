namespace Sharkable;

internal sealed class SharkCacheProfileFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint == null)
            return result;

        var profile = endpoint.Metadata.GetMetadata<SharkCacheProfileAttribute>();
        if (profile == null)
            return result;

        var cacheControl = profile.PrivateOnly ? "private" : "public";
        cacheControl += $", max-age={profile.DurationSeconds}";

        if (profile.ExtraDirectives != null)
            cacheControl += $", {profile.ExtraDirectives}";

        context.HttpContext.Response.Headers["Cache-Control"] = cacheControl;

        if (profile.VaryByHeader != null)
            context.HttpContext.Response.Headers["Vary"] = profile.VaryByHeader;

        return result;
    }
}
