namespace Sharkable;

/// <summary>
/// Helper for resolving and applying <see cref="IErrorLocalizer"/>.
/// </summary>
internal static class ErrorLocalizerHelper
{
    /// <summary>
    /// Gets the current <see cref="IErrorLocalizer"/> from the DI container,
    /// falling back to <see cref="DefaultErrorLocalizer"/> if none is registered.
    /// </summary>
    internal static IErrorLocalizer GetLocalizer()
    {
        return Shark.Services.GetService(typeof(IErrorLocalizer)) as IErrorLocalizer
            ?? new DefaultErrorLocalizer();
    }

    /// <summary>
    /// Resolves the culture from the Accept-Language header, defaulting to "en".
    /// </summary>
    internal static string ResolveCulture(HttpContext context)
    {
        var defaultCulture = Shark.SharkOption.DefaultCulture ?? "en";
        var header = context.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(header))
            return defaultCulture;

        var comma = header.IndexOf(',');
        var lang = comma > 0 ? header[..comma].Trim() : header.Trim();

        return string.IsNullOrWhiteSpace(lang) ? defaultCulture : lang;
    }
}
