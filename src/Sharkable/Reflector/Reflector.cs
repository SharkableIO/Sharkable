namespace Sharkable;

public class Reflector
{
    public static IEnumerable<MethodInfo>? GetMethods<T>(T type, 
        BindingFlags flags = BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly, Func<MethodInfo, int, bool>? predicate = null)
    {
        return predicate is null ? 
            type?.GetType().GetMethods(bindingAttr: flags) : 
            type?.GetType().GetMethods(bindingAttr: flags).Where(predicate);
    }
}