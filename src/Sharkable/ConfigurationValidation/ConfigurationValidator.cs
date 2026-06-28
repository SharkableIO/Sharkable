namespace Sharkable;

/// <summary>
/// Validates <see cref="SharkOption"/> configuration at startup,
/// catching common misconfigurations before the application runs.
/// </summary>
internal static class ConfigurationValidator
{
    /// <summary>
    /// Runs all validation rules against the current <see cref="SharkOption"/>.
    /// Returns an empty list when no issues are found.
    /// </summary>
    internal static List<string> Validate()
    {
        var errors = new List<string>();
        var opt = Shark.SharkOption;

        ValidateJwt(opt, errors);
        ValidateMultiTenant(opt, errors);

        return errors;
    }

    private static void ValidateJwt(SharkOption opt, List<string> errors)
    {
        if (opt.JwtAuthority == null)
            return;

        if (string.IsNullOrWhiteSpace(opt.JwtAuthority))
            errors.Add("JWT is configured but Authority is empty. Set it via: opt.ConfigureJwt(authority: \"https://your-tenant.authority.com\", ...)");

        if (opt.JwtAudiences == null || opt.JwtAudiences.Length == 0)
            errors.Add("JWT is configured but no Audiences specified. Set them via: opt.ConfigureJwt(authority: \"...\", audiences: [\"api://default\"], ...)");
    }

    private static void ValidateMultiTenant(SharkOption opt, List<string> errors)
    {
        if (opt.TenantOptions == null)
            return;

        if (opt.TenantOptions.ResolveTenant == null)
            errors.Add("Multi-tenant is configured but ResolveTenant delegate is not set. Provide it via: opt.ConfigureMultiTenant(cfg => cfg.ResolveTenant = ctx => ...)");
    }
}
