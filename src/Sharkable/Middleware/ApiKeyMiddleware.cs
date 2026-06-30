using Microsoft.AspNetCore.Http;

namespace Sharkable;

internal sealed class ApiKeyFilter : IEndpointFilter
{
    private readonly string[] _validKeys;

    public ApiKeyFilter()
    {
        _validKeys = Shark.SharkOption.ApiKeys ?? [];
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            return await next(context);

        if (!context.HttpContext.Request.Headers.TryGetValue(Shark.SharkOption.ApiKeyHeaderName, out var apiKey) ||
            !_validKeys.Contains(apiKey.ToString()))
        {
            context.HttpContext.Response.StatusCode = 401;
            await ProblemDetailsResult.WriteAsync(context.HttpContext, 401, "Missing or invalid API key");
            return Results.Empty;
        }

        return await next(context);
    }
}
