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
    internal static string? ToCamelCase(this string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
            
        TextInfo _textInfo = CultureInfo.InvariantCulture.TextInfo;
        char[] _camelCase = _textInfo.ToTitleCase(text).Replace(" ","").ToCharArray();

        _camelCase[0] = char.ToLower(_camelCase[0]);
        return new string(_camelCase);
    }        
}
