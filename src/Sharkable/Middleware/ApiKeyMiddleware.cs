using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

internal sealed class ApiKeyFilter : IEndpointFilter
{
    /// <summary>
    /// SHA-256 digests of every valid API key, pre-computed once at construction.
    /// Comparing fixed-length hashes via <see cref="CryptographicOperations.FixedTimeEquals"/>
    /// avoids leaking the position of the first mismatching byte (SHARK-SEC-008).
    /// </summary>
    private readonly byte[][] _validKeyHashes;

    public ApiKeyFilter()
    {
        var keys = Shark.SharkOption.ApiKeys ?? [];
        _validKeyHashes = new byte[keys.Length][];
        for (var i = 0; i < keys.Length; i++)
            _validKeyHashes[i] = SHA256.HashData(Encoding.UTF8.GetBytes(keys[i]));
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() != null)
            return await next(context);

        if (!context.HttpContext.Request.Headers.TryGetValue(Shark.SharkOption.ApiKeyHeaderName, out var apiKey))
        {
            context.HttpContext.Response.StatusCode = 401;
            await ProblemDetailsResult.WriteAsync(context.HttpContext, 401, "Missing or invalid API key");
            return Results.Empty;
        }

        // SHARK-SEC-008: hash the candidate and compare every stored hash in
        // constant time so attackers can't recover key bytes via timing.
        var candidateHash = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey.ToString()));
        var matched = false;
        for (var i = 0; i < _validKeyHashes.Length; i++)
        {
            if (CryptographicOperations.FixedTimeEquals(candidateHash, _validKeyHashes[i]))
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
