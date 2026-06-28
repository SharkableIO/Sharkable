using System.Linq.Expressions;

namespace Sharkable;

/// <summary>
/// Utilities for reflecting on types and creating delegates at runtime.
/// Internal API of the legacy attribute-based endpoint system.
/// </summary>
[Obsolete("Internal API of the legacy [SharkEndpoint] system. No replacement needed — migrate to ISharkEndpoint.")]
public partial class Reflector
{
    internal static IEnumerable<MethodInfo>? GetMethods<T>(T type, 
        BindingFlags flags = BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly, Func<MethodInfo, int, bool>? predicate = null)
    {
        return predicate is null ? 
            type?.GetType().GetMethods(bindingAttr: flags) : 
            type?.GetType().GetMethods(bindingAttr: flags).Where(predicate);
    }
    
    internal static Delegate? GetMethodDelegate(MethodInfo methodInfo, object? instance)
    {
        if (instance == null)
            return null;
        
        var parameters = methodInfo.GetParameters()
            .Select(p => p.ParameterType)
            .ToArray();
        return methodInfo.CreateDelegate(methodInfo.ReturnType == typeof(void) ? 
            Expression.GetDelegateType(parameters.Concat([typeof(void)]).ToArray()) : 
            Expression.GetDelegateType(parameters.Concat([methodInfo.ReturnType]).ToArray()), instance);
    }
}