namespace Sharkable;

/// <summary>
/// Provides access to the current tenant identifier.
/// Registered as a scoped service — inject into any class that needs tenant awareness.
/// </summary>
public interface ITenant
{
    /// <summary>
    /// The resolved tenant identifier for the current request, or <c>null</c> if no tenant could be resolved.
    /// </summary>
    string? TenantId { get; set; }
}
