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

        if (!context.HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var apiKey) ||
            !_validKeys.Contains(apiKey.ToString()))
        {
            var factory = Shark.SharkOption.UnifiedResultFactory ?? new DefaultUnifiedResultFactory();
            var result = factory.Create(null, "Missing or invalid API key", 401);
            return Results.Json(result, statusCode: 401);
        }

        return await next(context);
    }
}
