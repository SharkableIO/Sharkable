namespace Sharkable;

public static partial class Utils
{
    /// <summary>
    /// use forearch in async way
    /// </summary>
    /// <typeparam name="T">class to use foreach</typeparam>
    /// <param name="list">collection</param>
    /// <param name="func">Lambda expression</param>
    /// <returns> </returns>
    public static async Task ForEachAsync<T>(this IEnumerable<T>? list, Func<T, Task> func)
    {
        if (list == null)
            return;
            
        foreach (T value in list)
        {
            await func(value);
        }
    }
    
    /// <summary>
    /// use foreach in a linq way
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <param name="func"></param>
    public static void MyForEach<T>(this IEnumerable<T>? list, Action<T> func)
    {
        if (list == null)
            return;
            
        foreach (var value in list)
        { 
            func(value);
        }
    }
}