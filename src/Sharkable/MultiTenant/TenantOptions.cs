namespace Sharkable;

/// <summary>
/// Options for configuring multi-tenant support.
/// Passed via <c>opt.ConfigureMultiTenant()</c> in <c>AddShark()</c>.
/// </summary>
public sealed class TenantOptions
{
    /// <summary>
    /// A delegate that resolves the tenant identifier from the current <see cref="HttpContext"/>.
    /// Return <c>null</c> to indicate no tenant could be resolved.
    /// Use <see cref="TenantResolver"/> helpers or provide a custom lambda.
    /// </summary>
    public Func<HttpContext, string?>? ResolveTenant { get; set; }

    /// <summary>
    /// Configures tenant-aware data source routing. When set, an
    /// <see cref="ITenantDataSource"/> scoped service is registered that
    /// resolves per-tenant connection strings via the configured resolver.
    /// </summary>
    /// <param name="configure">Callback to set up the connection string mapping.</param>
    public void ConfigureDataSource(Action<TenantDataSourceOptions> configure)
    {
        var opt = new TenantDataSourceOptions();
        configure(opt);
        DataSourceOptions = opt;
    }

    internal TenantDataSourceOptions? DataSourceOptions { get; set; }
}
