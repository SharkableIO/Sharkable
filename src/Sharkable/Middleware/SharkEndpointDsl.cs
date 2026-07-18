using Microsoft.AspNetCore.Builder;

namespace Sharkable;

/// <summary>
/// Extension methods for <see cref="RouteHandlerBuilder"/> to apply Sharkable middleware features per-endpoint.
/// </summary>
public static class SharkEndpointDsl
{
    /// <summary>
    /// Applies a rate limiting policy to this endpoint.
    /// Requires a policy with the given name to be defined via <c>SharkOption.ConfigureRateLimiter()</c>.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="policyName">The rate limiter policy name.</param>
    public static RouteHandlerBuilder SharkRequireRateLimiting(this RouteHandlerBuilder builder, string policyName)
        => builder.RequireRateLimiting(policyName);

    /// <summary>
    /// Enables output caching for this endpoint.
    /// Requires a cache profile or policy to be defined via <c>SharkOption.ConfigureOutputCache()</c>.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="policyName">Optional output cache policy name.</param>
    public static RouteHandlerBuilder SharkCacheOutput(this RouteHandlerBuilder builder, string? policyName = null)
        => policyName != null ? builder.CacheOutput(policyName) : builder.CacheOutput();

    /// <summary>
    /// Applies a request timeout policy to this endpoint.
    /// Requires <c>SharkOption.RequestTimeoutsConfigure</c> to be set.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="policyName">The request timeout policy name.</param>
    public static IEndpointConventionBuilder SharkRequestTimeout(this RouteHandlerBuilder builder, string policyName)
    {
        builder.WithRequestTimeout(policyName);
        return builder;
    }

    /// <summary>
    /// Applies a per-endpoint rate limit policy via endpoint metadata.
    /// The distributed rate limiter middleware reads this metadata and applies
    /// the specified limit for this endpoint, falling back to global defaults.
    /// </summary>
    /// <param name="builder">The route handler builder.</param>
    /// <param name="limit">Maximum requests within the window.</param>
    /// <param name="windowSeconds">Window duration in seconds.</param>
    public static RouteHandlerBuilder SharkRateLimit(this RouteHandlerBuilder builder, int limit, int windowSeconds)
    {
        builder.WithMetadata(new SharkRateLimitMetadata(limit, TimeSpan.FromSeconds(windowSeconds)));
        return builder;
    }
}
