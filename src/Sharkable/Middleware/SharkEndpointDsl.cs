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
}
