namespace Sharkable;

/// <summary>
/// Sets <c>Cache-Control</c> (and optional <c>Vary</c>) headers on responses
/// from the decorated endpoint class. Composes with ETag and ASP.NET Output Cache.
/// Apply on an <see cref="ISharkEndpoint"/> class.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class SharkCacheProfileAttribute : Attribute
{
    /// <summary>
    /// <c>max-age</c> value in seconds for the <c>Cache-Control</c> header.
    /// </summary>
    public int DurationSeconds { get; }

    /// <summary>
    /// When set, adds a <c>Vary</c> response header with the given value
    /// (e.g. <c>"Accept-Language"</c>, <c>"Accept-Encoding"</c>).
    /// </summary>
    public string? VaryByHeader { get; set; }

    /// <summary>
    /// When <c>true</c>, sets <c>Cache-Control: private, max-age=...</c>
    /// instead of <c>public, max-age=...</c>. Default is <c>false</c> (public).
    /// </summary>
    public bool PrivateOnly { get; set; }

    /// <summary>
    /// Additional directives to append to the <c>Cache-Control</c> header
    /// (e.g. <c>"no-transform"</c>, <c>"must-revalidate"</c>).
    /// </summary>
    public string? ExtraDirectives { get; set; }

    /// <summary>
    /// Initializes a new instance with the required <c>max-age</c> duration.
    /// </summary>
    /// <param name="durationSeconds">Cache duration in seconds.</param>
    public SharkCacheProfileAttribute(int durationSeconds)
    {
        DurationSeconds = durationSeconds;
    }
}
