using System.Security.Claims;

namespace Sharkable;

/// <summary>
/// Static helper methods for resolving a tenant identifier from common sources.
/// Use with <see cref="TenantOptions.ResolveTenant"/>.
/// </summary>
public static class TenantResolver
{
    /// <summary>
    /// Resolves tenant from the first subdomain segment of the host.
    /// Example: <c>tenant1.myapp.com</c> → <c>"tenant1"</c>.
    /// Returns <c>null</c> if the host has no subdomain.
    /// </summary>
    public static string? FromHost(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (string.IsNullOrEmpty(host))
            return null;
        var dotIndex = host.IndexOf('.');
        return dotIndex > 0 ? host[..dotIndex] : null;
    }

    /// <summary>
    /// Resolves tenant from a JWT claim on the current user.
    /// Returns <c>null</c> if the claim is not present or the user is unauthenticated.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="claimType">The claim type to read. Default is <c>"tenant_id"</c>.</param>
    public static string? FromClaim(HttpContext context, string claimType = "tenant_id")
    {
        return context.User.FindFirstValue(claimType);
    }
}
