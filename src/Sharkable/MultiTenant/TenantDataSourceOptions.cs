namespace Sharkable;

/// <summary>
/// Configuration options for tenant data source isolation.
/// Passed via <c>TenantOptions.ConfigureDataSource()</c>.
/// </summary>
public sealed class TenantDataSourceOptions
{
    /// <summary>
    /// Maps a tenant identifier to a database connection string.
    /// Return <c>null</c> to indicate no data source for the given tenant.
    /// </summary>
    /// <example>
    /// <code>
    /// o.ConnectionStringResolver = tenantId =>
    ///     $"Server=db-{tenantId}.internal;Database=app";
    /// </code>
    /// </example>
    public Func<string, string>? ConnectionStringResolver { get; set; }
}
