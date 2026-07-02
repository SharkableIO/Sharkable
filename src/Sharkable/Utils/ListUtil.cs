using System.Reflection;

namespace Sharkable;

public static partial class Utils
{
    /// <summary>
    /// Adds a non-null item to the list, or returns null if the list is null.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="lst">The list to add to.</param>
    /// <param name="item">The item to add if non-null.</param>
    /// <returns>The original list, or null if input was null.</returns>
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