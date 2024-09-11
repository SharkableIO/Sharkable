using System.Reflection;

namespace Sharkable;

public static partial class Utils
{
    public static IList<T?>? AddNonNull<T>(this IList<T?>? lst, T? item)
    {
        if (lst == null)
            return null;

        if(item != null)
        {
            lst.Add(item);
        }
        return lst;
    }
}