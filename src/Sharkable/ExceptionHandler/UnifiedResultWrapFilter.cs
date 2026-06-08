using System.Net;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

internal sealed class UnifiedResultWrapFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var result = await next(context);

        if (result == null || result is IResult)
            return result;

        var resultType = result.GetType();
        var unifiedResultType = typeof(UnifiedResult<>).MakeGenericType(resultType);
        var unifiedResult = Activator.CreateInstance(unifiedResultType);

        if (unifiedResult is not null)
        {
            var dataProperty = unifiedResultType.GetProperty("Data");
            dataProperty?.SetValue(unifiedResult, result);
            var statusCodeProperty = unifiedResultType.GetProperty("StatusCode");
            statusCodeProperty?.SetValue(unifiedResult, HttpStatusCode.OK);
        }

        return unifiedResult;
    }
}
