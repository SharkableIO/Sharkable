namespace Sharkable;

/// <summary>
/// Extension methods for per-route auto-wrap exclusion on <see cref="RouteHandlerBuilder"/>.
/// </summary>
public static class DisableAutoWrapExtensions
{
    /// <summary>
    /// Excludes this route from auto-wrap. The raw return value will be
    /// returned as-is without being wrapped in <see cref="UnifiedResult{T}"/>.
    /// </summary>
    public static RouteHandlerBuilder DisableAutoWrap(this RouteHandlerBuilder builder)
    {
        builder.Add(endpointBuilder =>
            endpointBuilder.Metadata.Add(new DisableAutoWrapMetadata()));
        return builder;
    }
}

/// <summary>
/// Internal metadata marker that signals <see cref="UnifiedResultWrapFilter"/>
/// to skip wrapping for the current endpoint.
/// </summary>
internal sealed class DisableAutoWrapMetadata { }
