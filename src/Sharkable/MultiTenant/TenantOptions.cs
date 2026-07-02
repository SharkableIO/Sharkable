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
    /// SHARK-SEC-L007: optional allowlist of <c>Host</c> header values that
    /// are accepted when a tenant resolver consults the inbound host
    /// (e.g. <see cref="TenantResolver.FromHost"/>). When set, requests
    /// whose <c>Host</c> header is not on the allowlist are rejected with
    /// 400 — otherwise an attacker can spoof the tenant by sending
    /// <c>Host: victim-tenant.app.example</c>. Comparison is case-insensitive.
    /// Leave null to accept any host (legacy behavior).
    /// </summary>
    public string[]? AllowedHosts { get; set; }

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
