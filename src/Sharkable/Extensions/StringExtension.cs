using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Sharkable;

internal static class StringExtension
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    // SHARK-SEC-L012: cache the default-pattern compiled regex instances so
    // we don't re-parse on every call. Custom patterns set via SharkOption
    // are still compiled per call but are validated once at startup by
    // ConfigurationValidator so the runtime path is bounded.
    private static readonly Regex DefaultGroupNameSuffixRegex = new(
        @"(endpoint|service|services|controller|controllers|apicontroller)(?=V?\d*$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        RegexTimeout);
    private static readonly Regex DefaultVersionFormatRegex = new(
        @"V(\d+)",
        RegexOptions.Compiled,
        RegexTimeout);

    internal static string? FormatAsGroupName(this string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return str;
        var configured = Shark.SharkOption?.GroupNameSuffixPattern;
        Regex suffixRegex;
        if (string.IsNullOrEmpty(configured) ||
            configured == DefaultGroupNameSuffixRegex.ToString())
        {
            suffixRegex = DefaultGroupNameSuffixRegex;
        }
        else
        {
            try
            {
                suffixRegex = new Regex(configured, RegexOptions.IgnoreCase, RegexTimeout);
            }
            catch (ArgumentException)
            {
                // Pattern validated at startup; defensive fallback if a
                // user mutates the option at runtime.
                return str;
            }
        }
        try
        {
            var result = suffixRegex.Replace(str, "");
            return result.GetVersionFormat();
        }
        catch (RegexMatchTimeoutException)
        {
            return str;
        }
    }

    internal static string? ToCamelCase(this string? str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return char.ToLowerInvariant(str[0]) + str[1..];
    }

    internal static string? ToSnakeCase(this string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var builder = new StringBuilder(text.Length + Math.Min(2, text.Length / 5));
        var previousCategory = default(UnicodeCategory?);

        for (var currentIndex = 0; currentIndex < text.Length; currentIndex++)
        {
            var currentChar = text[currentIndex];
            if (currentChar == '_')
            {
                builder.Append('_');
                previousCategory = null;
                continue;
            }

            if(currentChar == '@')
            {
                builder.Append('@');
                previousCategory = null;
                continue;
            }
            var currentCategory = char.GetUnicodeCategory(currentChar);
            switch (currentCategory)
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                    if (previousCategory == UnicodeCategory.SpaceSeparator ||
                        previousCategory == UnicodeCategory.LowercaseLetter ||
                        (previousCategory != UnicodeCategory.DecimalDigitNumber && previousCategory != null &&
                         currentIndex > 0 && currentIndex + 1 < text.Length && char.IsLower(text[currentIndex + 1])))
                    {
                        builder.Append('_');
                    }
                    currentChar = char.ToLower(currentChar, CultureInfo.InvariantCulture);
                    break;
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.DecimalDigitNumber:
                    if (previousCategory == UnicodeCategory.SpaceSeparator)
                    {
                        builder.Append('_');
                    }
                    break;
                default:
                    if (previousCategory != null)
                    {
                        previousCategory = UnicodeCategory.SpaceSeparator;
                    }
                    continue;
            }
            builder.Append(currentChar);
            previousCategory = currentCategory;
        }

        return builder.ToString();
    }

    internal static string? GetVersionFormat(this string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return str;

        var configured = Shark.SharkOption?.VersionFormatPattern;
        var replacement = Shark.SharkOption?.VersionFormatReplacement ?? @"@$1";

        Regex regex;
        if (string.IsNullOrEmpty(configured) ||
            configured == DefaultVersionFormatRegex.ToString())
        {
            regex = DefaultVersionFormatRegex;
        }
        else
        {
            try
            {
                regex = new Regex(configured, RegexOptions.None, RegexTimeout);
            }
            catch (ArgumentException)
            {
                return str;
            }
        }

        try
        {
            return regex.Replace(str, replacement);
        }
        catch (RegexMatchTimeoutException)
        {
            return str;
        }
    }
    internal static string? GetCaseFormat(this string? str, EndpointFormat format = EndpointFormat.UnChanged)
    {
        if (string.IsNullOrWhiteSpace(str))
            return str;
        
        var _str = format switch 
        {
            EndpointFormat.CamelCase => str.ToCamelCase(),
            EndpointFormat.ToLower => str.ToLowerInvariant(),
            EndpointFormat.SnakeCase => str.ToSnakeCase(),
            _ => str,
        };

        return _str;
    }
}
