using Microsoft.AspNetCore.Http;

namespace Sharkable;

internal sealed class UnifiedResultWrapFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var result = await next(context);

        if (result == null || result is IResult || result is IUnifiedResult)
            return result;

        var factory = Shark.SharkOption.UnifiedResultFactory ?? new DefaultUnifiedResultFactory();
        var statusCode = context.HttpContext.Response.StatusCode;
        return factory.Create(result, null, statusCode);
    }
}
