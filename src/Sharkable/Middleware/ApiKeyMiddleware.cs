using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Sharkable;

internal sealed class ApiKeyFilter : IEndpointFilter
{
    private readonly ApiKeyValidator _validator;

    public ApiKeyFilter(ApiKeyValidator validator)
    {
        _validator = validator;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            return await next(context);

        var opt = Shark.SharkOption;

        if (!context.HttpContext.Request.Headers.TryGetValue(opt.ApiKeyHeaderName, out var apiKey))
        {
            context.HttpContext.Response.StatusCode = 401;
            await ProblemDetailsResult.WriteAsync(context.HttpContext, 401, "Missing or invalid API key");
            return Results.Empty;
        }

        if (!_validator.Validate(apiKey.ToString()))
        {
            context.HttpContext.Response.StatusCode = 401;
            await ProblemDetailsResult.WriteAsync(context.HttpContext, 401, "Missing or invalid API key");
            return Results.Empty;
        }

        return await next(context);
    }
}
