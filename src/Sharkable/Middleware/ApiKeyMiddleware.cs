using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Sharkable;

internal sealed class ApiKeyFilter : IEndpointFilter
{
    private readonly IOptionsMonitor<SharkOption> _options;

    /// <summary>
    /// SHARK-SEC-L003: accept <see cref="IOptionsMonitor{SharkOption}"/> so
    /// the filter re-reads <c>ApiKeys</c> on every invocation. The previous
    /// implementation captured the key list once in the constructor and was
    /// permanently stale after hot-reload via configuration changes.
    /// </summary>
    public ApiKeyFilter(IOptionsMonitor<SharkOption> options)
    {
        _options = options;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            return await next(context);

        var opt = _options.CurrentValue;
        var keys = opt.ApiKeys ?? [];

        if (!context.HttpContext.Request.Headers.TryGetValue(opt.ApiKeyHeaderName, out var apiKey))
        {
            context.HttpContext.Response.StatusCode = 401;
            await ProblemDetailsResult.WriteAsync(context.HttpContext, 401, "Missing or invalid API key");
            return Results.Empty;
        }

        // SHARK-SEC-008: hash the candidate and compare every stored hash in
        // constant time so attackers can't recover key bytes via timing.
        var candidateHash = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey.ToString()));
        var matched = false;
        for (var i = 0; i < keys.Length; i++)
        {
            var keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(keys[i]));
            if (CryptographicOperations.FixedTimeEquals(candidateHash, keyHash))
                matched = true;
        }

        if (!matched)
        {
            context.HttpContext.Response.StatusCode = 401;
            await ProblemDetailsResult.WriteAsync(context.HttpContext, 401, "Missing or invalid API key");
            return Results.Empty;
        }

        return await next(context);
    }
}
