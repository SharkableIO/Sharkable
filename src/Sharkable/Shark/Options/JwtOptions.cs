using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Sharkable;

/// <summary>
/// Configuration for JWT Bearer authentication.
/// Passed via <c>opt.ConfigureJwt(jwt => { ... })</c>.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// The trusted token authority (issuer) URL. Required.
    /// </summary>
    public string Authority { get; set; } = "";

    /// <summary>
    /// Accepted audience values. At least one non-empty entry is required.
    /// An empty list silently disables audience validation and accepts tokens
    /// minted for any audience.
    /// </summary>
    public string[] Audiences { get; set; } = [];

    /// <summary>
    /// Optional additional <see cref="JwtBearerOptions"/> configuration.
    /// </summary>
    public Action<JwtBearerOptions>? BearerConfigure { get; set; }
}
