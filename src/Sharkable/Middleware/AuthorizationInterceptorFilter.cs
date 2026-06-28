namespace Sharkable;

/// <summary>
/// Endpoint filter that invokes the registered
/// <see cref="IAuthorizationInterceptor"/> before every endpoint.
/// </summary>
internal sealed class AuthorizationInterceptorFilter : IEndpointFilter
{
    private readonly IAuthorizationInterceptor? _interceptor;

    public AuthorizationInterceptorFilter(IServiceProvider services)
    {
        _interceptor = services.GetService<IAuthorizationInterceptor>();
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (_interceptor != null)
        {
            var result = _interceptor.Authorize(context.HttpContext);
            if (result != null)
                return result;
        }
        return await next(context);
    }
}
