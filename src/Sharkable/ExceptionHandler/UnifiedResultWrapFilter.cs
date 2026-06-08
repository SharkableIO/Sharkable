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
        return factory.Create(result, null, 200);
    }
}
