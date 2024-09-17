using System.Globalization;
using System.Text.RegularExpressions;

namespace Sharkable;

internal static class StringExtension
{
    internal static string? FormatAsGroupName(this string? value)
    {
        if (value == null)
            return "";

        if(value.StartsWith('/'))
            value = value.Remove(0, 1);

        string pattern = @"(endpoint|service|services|controller|controllers|apicontroller)$";

        return Regex.Replace(value, pattern, "", RegexOptions.IgnoreCase);
    }
    internal static string? ToCamelCase(this string? str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return char.ToLower(str[0]) + str[1..];
    }        
}
