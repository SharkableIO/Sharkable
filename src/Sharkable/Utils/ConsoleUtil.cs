using System;

namespace Sharkable;

public partial class Utils
{
    public static void WriteDebug(string? value)
    {
#if DEBUG
        Console.WriteLine(value);
#endif
    }
}
