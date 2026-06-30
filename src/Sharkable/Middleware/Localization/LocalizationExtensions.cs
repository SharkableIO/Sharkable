namespace Sharkable;

/// <summary>
/// Extension methods for localizing error messages in endpoints.
/// </summary>
public static class LocalizationExtensions
{
    /// <summary>
    /// Translates a message key into the client's preferred language.
    /// Culture is resolved from the <c>Accept-Language</c> request header.
    /// Falls back to <see cref="SharkOption.DefaultCulture"/> when the header is missing.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="key">The message key to translate.</param>
    /// <returns>The localized string, or <paramref name="key"/> itself if no translation is registered.</returns>
    public static string Localize(this HttpContext context, string key)
    {
        var localizer = ErrorLocalizerHelper.GetLocalizer();
        var culture = ErrorLocalizerHelper.ResolveCulture(context);
        return localizer.Localize(key, culture);
    }
}
