namespace Sharkable;

/// <summary>
/// Default implementation of <see cref="ITenantDataSource"/> that resolves
/// the connection string from <see cref="TenantDataSourceOptions"/> using
/// the current <see cref="ITenant"/>.
/// </summary>
internal sealed class DefaultTenantDataSource : ITenantDataSource
{
    private readonly ITenant _tenant;
    private readonly TenantDataSourceOptions _options;

    public DefaultTenantDataSource(ITenant tenant, TenantDataSourceOptions options)
    {
        _tenant = tenant;
        _options = options;
    }

    public string? TenantId => _tenant.TenantId;

    public string? GetConnectionString()
    {
        if (string.IsNullOrEmpty(_tenant.TenantId))
            return null;

        return _options.ConnectionStringResolver?.Invoke(_tenant.TenantId);
    }
}
