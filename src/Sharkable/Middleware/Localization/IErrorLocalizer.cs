namespace Sharkable;

/// <summary>
/// Configuration options for automatic ETag generation and 304 Not Modified responses.
/// Passed via <c>SharkOption.EnableETag</c>.
/// </summary>
public sealed class ETagOptions
{
    /// <summary>
    /// Paths excluded from ETag processing (case-insensitive prefix match).
    /// Default excludes health check, OpenAPI, and profiler endpoints.
    /// </summary>
    public string[] ExcludePaths { get; set; } = ["/healthz", "/openapi", "/scalar", "/_sharkable"];

    /// <summary>
    /// HTTP methods eligible for ETag caching. Default is <c>GET, HEAD</c>.
    /// </summary>
    public HashSet<string> CacheableMethods { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "HEAD"
    };

    /// <summary>
    /// The <c>Cache-Control</c> header value sent with ETagged responses.
    /// Default is <c>"public, max-age=0, must-revalidate"</c>.
    /// </summary>
    public string CacheControlHeader { get; set; } = "public, max-age=0, must-revalidate";

    /// <summary>
    /// Predicate that determines whether a response status code should be cached.
    /// Return <c>true</c> to skip caching. Default returns <c>true</c> for
    /// status codes below 200 or 300 and above.
    /// </summary>
    public Func<int, bool> ShouldSkipStatus { get; set; } = static statusCode => statusCode is < 200 or >= 300;

    /// <summary>
    /// Maximum response body size (bytes) the middleware will buffer and hash
    /// for ETag generation. When a response exceeds this cap the middleware
    /// stops buffering, skips ETag generation, and forwards the response
    /// unmodified (HTTP 200, no <c>ETag</c> header). Default is 10 MiB
    /// (<c>10 * 1024 * 1024</c>). Increase for endpoints that legitimately
    /// return larger payloads (and pair with [ResponseCache] or a CDN for
    /// server-side caching).
    /// </summary>
    public long MaxResponseSize { get; set; } = 10L * 1024 * 1024;
}

/// <summary>
/// Pluggable localizer for translating error messages based on the
/// <c>Accept-Language</c> request header. Implement this to provide
/// multi-language error responses.
/// </summary>
public interface IErrorLocalizer
{
    /// <summary>
    /// Translates the given message key into the language specified by
    /// <paramref name="culture"/>, falling back to the key itself if no
    /// translation is found.
    /// </summary>
    /// <param name="key">The message key (e.g. <c>"User_NotFound"</c>).</param>
    /// <param name="culture">The target culture, e.g. <c>"zh-CN"</c>.</param>
    /// <returns>The translated message.</returns>
    string Localize(string key, string culture);
}

/// <summary>
/// No-op localizer that returns the key unchanged. Registered as the default
/// when no custom <see cref="IErrorLocalizer"/> is configured.
/// </summary>
internal sealed class DefaultErrorLocalizer : IErrorLocalizer
{
    public string Localize(string key, string culture) => key;
}
