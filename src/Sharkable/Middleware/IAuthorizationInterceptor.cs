namespace Sharkable;

/// <summary>
/// Pluggable authorization hook that runs before every endpoint.
/// Return <c>null</c> to allow the request; return an <see cref="IResult"/>
/// to reject with a custom response (403, 401, etc.).
/// Useful for claim-based permissions, RBAC, tenant-scoped access control,
/// and custom API-key validation logic.
/// </summary>
public interface IAuthorizationInterceptor
{
    /// <summary>
    /// Authorize the current request.
    /// </summary>
    /// <param name="context">The HTTP context. Use <c>context.User</c> for JWT claims,
    /// <c>context.Request.Headers["X-Api-Key"]</c> for API key.</param>
    /// <returns><c>null</c> to allow; a non-null <see cref="IResult"/> to reject.</returns>
    IResult? Authorize(HttpContext context);
}
