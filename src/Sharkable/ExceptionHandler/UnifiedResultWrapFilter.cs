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

        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<DisableAutoWrapMetadata>() is not null)
            return result;

        var factory = Shark.SharkOption.UnifiedResultFactory ?? new DefaultUnifiedResultFactory();
        var wrapped = factory.Create(result, errorMessage: null, statusCode: 200);
        return new UnifiedResultResult(wrapped);
    }
}
