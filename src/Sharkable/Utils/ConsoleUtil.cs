using System;

namespace Sharkable;

public partial class Utils
{
    internal static void WriteDebug(string? value)
    {
#if DEBUG
        Console.WriteLine(value);
#endif
    }
}
