using System.Text.RegularExpressions;

namespace Sharkable.Extensions;

internal static class StringExtension
{
    public static string? FormatAsGroupName(this string? value)
    {
        if (value == null)
            return "";

        if(value.StartsWith('/'))
            value = value.Remove(0, 1);

        string pattern = @"(endpoint|service|services|controller|controllers)$";

        string result = Regex.Replace(value, pattern, "", RegexOptions.IgnoreCase);

        return Shark.ApiPrefix + "/" + result;
    }
}
