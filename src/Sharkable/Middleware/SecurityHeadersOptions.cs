namespace Sharkable;

/// <summary>
/// Configuration options for the security headers middleware.
/// Passed via <c>opt.ConfigureSecurityHeaders()</c> callback in <c>AddShark()</c>.
/// All headers are opt-in; no headers are added unless explicitly enabled.
/// </summary>
public sealed class SecurityHeadersOptions
{
    /// <summary>
    /// When <c>true</c>, adds <c>X-Content-Type-Options: nosniff</c> to every response.
    /// Default is <c>false</c>.
    /// </summary>
    public bool ContentTypeOptions { get; set; }

    /// <summary>
    /// Value for <c>X-Frame-Options</c> header. Null (default) disables the header.
    /// Common values: <c>"DENY"</c>, <c>"SAMEORIGIN"</c>.
    /// </summary>
    public string? FrameOptions { get; set; }

    /// <summary>
    /// Value for <c>Referrer-Policy</c> header. Null (default) disables the header.
    /// Common values: <c>"no-referrer"</c>, <c>"strict-origin-when-cross-origin"</c>.
    /// </summary>
    public string? ReferrerPolicy { get; set; }

    /// <summary>
    /// Value for <c>Content-Security-Policy</c> header. Null (default) disables the header.
    /// Example: <c>"default-src 'none'"</c>.
    /// </summary>
    public string? ContentSecurityPolicy { get; set; }

    /// <summary>
    /// Value for <c>Permissions-Policy</c> header. Null (default) disables the header.
    /// Example: <c>"geolocation=()"</c>.
    /// </summary>
    public string? PermissionsPolicy { get; set; }

    /// <summary>
    /// When <c>true</c>, adds <c>Strict-Transport-Security: max-age=31536000</c> header.
    /// Only enable when you serve exclusively over HTTPS. Default is <c>false</c>.
    /// </summary>
    public bool StrictTransportSecurity { get; set; }

    /// <summary>
    /// Custom max-age for <c>Strict-Transport-Security</c> in seconds.
    /// Default is 31536000 (1 year). Only used when <see cref="StrictTransportSecurity"/> is <c>true</c>.
    /// </summary>
    public int HstsMaxAge { get; set; } = 31536000;

    /// <summary>
    /// Custom value for <c>Cross-Origin-Resource-Policy</c> header. Null (default) disables the header.
    /// Common values: <c>"same-origin"</c>, <c>"cross-origin"</c>.
    /// </summary>
    public string? CrossOriginResourcePolicy { get; set; }

    /// <summary>
    /// Custom value for <c>Cross-Origin-Opener-Policy</c> header. Null (default) disables the header.
    /// Common values: <c>"same-origin"</c>, <c>"same-origin-allow-popups"</c>.
    /// </summary>
    public string? CrossOriginOpenerPolicy { get; set; }

    /// <summary>
    /// Custom value for <c>Cross-Origin-Embedder-Policy</c> header. Null (default) disables the header.
    /// Common values: <c>"require-corp"</c>, <c>"credentialless"</c>.
    /// </summary>
    public string? CrossOriginEmbedderPolicy { get; set; }

    /// <summary>
    /// Path prefixes to exclude from security header addition.
    /// Matches by prefix. Default is empty (headers applied to all paths).
    /// </summary>
    public string[] ExcludePaths { get; set; } = [];
}
