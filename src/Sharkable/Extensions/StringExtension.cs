﻿using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Sharkable;

internal static class StringExtension
{
    internal static string? FormatAsGroupName(this string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return str;
        string pattern = "(endpoint|service|services|controller|controllers|apicontroller)(?=V?\\d*$)";
        return Regex.Replace(str, pattern, "", RegexOptions.IgnoreCase).GetVersionFormat();
    }
    static string? RemoveSuffix(this string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;
        string[] suffixes = ["Service", "Controller", "Endpoint"];
        foreach (var suffix in suffixes)
        {
            int index = input.LastIndexOf(suffix, StringComparison.OrdinalIgnoreCase);
            if (index != -1 && index + suffix.Length == input.Length)
            {
                return input[..index];
            }
        }
        return input;
    }
    internal static string? ToCamelCase(this string? str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return char.ToLower(str[0]) + str[1..];
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

    public static string? GetVersionFormat(this string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return str;
        
        string pattern = @"V(\d+)";
        string replacement = @"@$1";

        return Regex.Replace(str, pattern, replacement);
    }
}
