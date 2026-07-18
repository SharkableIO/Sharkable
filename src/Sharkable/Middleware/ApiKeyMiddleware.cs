using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

internal sealed class ApiKeyFilter : IEndpointFilter
{
    private readonly IApiKeyValidator _validator;

    public ApiKeyFilter(IApiKeyValidator validator)
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

        var validationResult = _validator.Validate(apiKey.ToString());
        if (!validationResult.IsValid)
        {
            context.HttpContext.Response.StatusCode = 401;
            await ProblemDetailsResult.WriteAsync(context.HttpContext, 401, "Missing or invalid API key");
            return Results.Empty;
        }

        if (validationResult.Claims is { Count: > 0 })
        {
            var identity = new ClaimsIdentity(validationResult.Claims, "ApiKey");
            context.HttpContext.User = new ClaimsPrincipal(identity);
        }

        context.HttpContext.Items["Sharkable.RateLimitMultiplier"] = validationResult.RateLimitMultiplier;

        return await next(context);
    }
}
