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

    /// <summary>
    /// Translates and formats a message key with positional arguments.
    /// Culture is resolved from the <c>Accept-Language</c> request header.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="key">The message key to translate.</param>
    /// <param name="args">Positional format arguments passed to <c>string.Format()</c>.</param>
    /// <returns>The formatted localized string.</returns>
    /// <example>
    /// Translation: <c>"WelcomeUser" = "Welcome, {0}!"</c>
    /// <code>ctx.Localize("WelcomeUser", username);</code>
    /// </example>
    public static string Localize(this HttpContext context, string key, params object[] args)
    {
        var msg = context.Localize(key);
        return args.Length > 0 ? string.Format(msg, args) : msg;
    }

    /// <summary>
    /// Resolves the client's preferred culture from the <c>Accept-Language</c> request header.
    /// Falls back to <see cref="SharkOption.DefaultCulture"/> when the header is missing.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>The culture string, e.g. <c>"zh-CN"</c>, <c>"en"</c>, <c>"ja"</c>.</returns>
    public static string GetCulture(this HttpContext context)
    {
        return ErrorLocalizerHelper.ResolveCulture(context);
    }
}
