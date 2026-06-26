namespace Sharkable;

internal sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenantOptions _options;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
        _options = Shark.SharkOption.TenantOptions!;
    }

    public async Task InvokeAsync(HttpContext context, ITenant tenant)
    {
        if (_options.ResolveTenant != null && tenant is Tenant t)
        {
            t.TenantId = _options.ResolveTenant(context);
        }
        await _next(context);
    }
}
