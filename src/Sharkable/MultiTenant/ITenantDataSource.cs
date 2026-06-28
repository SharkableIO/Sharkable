namespace Sharkable;

/// <summary>
/// Provides a tenant-aware data source (connection string) for the current request.
/// Registered as a scoped service when <c>ConfigureDataSource()</c> is set.
/// Inject into any class that needs a per-tenant database connection.
/// </summary>
public interface ITenantDataSource
{
    /// <summary>
    /// Gets the connection string for the current tenant, or <c>null</c>
    /// if no tenant is resolved or no data source is configured.
    /// </summary>
    string? GetConnectionString();

    /// <summary>
    /// The current tenant identifier, or <c>null</c> if no tenant is resolved.
    /// </summary>
    string? TenantId { get; }
}
